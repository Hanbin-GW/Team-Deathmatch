using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using TeamDeathmatch.Audio;
using TeamDeathmatchAPI; // Use TeamState, TeamAudioProfile

namespace TeamDeathmatch.EventHandlers
{
    public class AnnouncerEventHandlers
    {
        public Plugin Plugin;
        public readonly string AudioDirectory;

        // Audio cache by team
        private readonly Dictionary<string, AudioPlayer> _teamPlayers = new();
        private readonly Dictionary<string, TeamAudioProfile> _teamProfiles = new();
        private readonly Dictionary<string, API.TeamState> _lastTeamState = new();

        public AnnouncerEventHandlers()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AudioDirectory = Path.Combine(appDataPath, "EXILED", "Plugins", "DeathMatch");
        }

        // ----- 초기화/로드 -----
        public void OnPluginLoad()
        {
            EnsureMusicDirectoryExists();
            InitTeamAudioProfiles();

            // Load audio files (Clip keys must match the profile below)
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "LoadUpLetsGo.ogg"), "ChaosLoad");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "Returntobase.ogg"), "Team2Defeat");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "EnemyLead.ogg"), "Team2Losing");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "clocksticking.ogg"), "Team2ClockTicking");

            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "tf141", "WinningReversal.ogg"), "Team1_WinningReversal");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "WinningReversal.ogg"), "Team2_WinningReversal");
        }

        private void EnsureMusicDirectoryExists()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EXILED", "Plugins", "DeathMatch");

            if (!Directory.Exists(path))
            {
                Log.Warn($"music folder is not existed. creating: {path}");
                Directory.CreateDirectory(path);
            }
            else
            {
                Log.Info("music folder already exists.");
            }
        }

        private void InitTeamAudioProfiles()
        {
            // Profile for Team 1 (MTF)
            _teamProfiles["Team1"] = new TeamAudioProfile
            {
                LeadingClip     = "MTF_LEADING",        // need to chance to registared audio key
                LosingClip      = "MTF_LOSING",
                TiedClip        = "MTF_TIED",
                MatchPointClip  = "MTF_MATCH_POINT",
                VictoryClip     = "MTF_VICTORY",
                DefeatClip      = "MTF_DEFEAT",
            };

            // Profile for Team 2 (Chaos)
            _teamProfiles["Team2"] = new TeamAudioProfile
            {
                LeadingClip     = "CI_LEADING",
                LosingClip      = "Team2Losing",       // Match top Load Clip
                TiedClip        = "CI_TIED",
                MatchPointClip  = "CI_MATCH_POINT",
                VictoryClip     = "CI_VICTORY",
                DefeatClip      = "Team2Defeat",        // Match top Load Clip
            };
        }

        // ----- Get players by team -----
        private AudioPlayer EnsureTeamPlayer(string teamName)
        {
            if (_teamPlayers.TryGetValue(teamName, out var player))
                return player;

            var created = AudioPlayer.CreateOrGet(
                $"Announcer_{teamName}",
                condition: hub =>
                {
                    var p = Player.Get(hub);
                    return p != null
                           && Plugin.Instance.playerTeams.TryGetValue(p, out var t)
                           && t == teamName;
                },
                onIntialCreation: ap =>
                {
                    ap.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f);
                }
            );

            _teamPlayers[teamName] = created;
            return created;
        }

        // ----- Situation playback (only when status changes) -----
        private void PlayTeamStateAudio(string teamName, API.TeamState state)
        {
            if (!_teamProfiles.TryGetValue(teamName, out var profile))
                return;

            if (_lastTeamState.TryGetValue(teamName, out var last) && last == state)
                return; // Same status -> Prevent duplicate playback

            _lastTeamState[teamName] = state;

            var clip = profile.GetClip(state);
            if (string.IsNullOrWhiteSpace(clip))
                return;

            var player = EnsureTeamPlayer(teamName);
            player.AddClip(clip);
        }

        // ----- Start-up bobble playback -----
        public void PlayTeamStartAudio(string teamName, string clipName)
        {
            if (string.IsNullOrWhiteSpace(teamName) || string.IsNullOrWhiteSpace(clipName))
            {
                Log.Warn($"[Announcer] invalid args: team='{teamName}', clip='{clipName}'");
                return;
            }

            var audioPlayer = EnsureTeamPlayer(teamName);
            audioPlayer.AddClip(clipName);
        }

        // ----- reversal event -----
        public void RunReversalEvent(string newLeadingTeam)
        {
            if (string.IsNullOrWhiteSpace(newLeadingTeam))
                return;

            var losingTeam = newLeadingTeam == "Team1" ? "Team2" : "Team1";

            // Lead Team: Reversal Victory Clips
            var winningPlayer = AudioPlayer.CreateOrGet(
                $"Announcer_{newLeadingTeam}_Winning",
                condition: hub =>
                {
                    var pl = Player.Get(hub);
                    return pl != null
                           && Plugin.Instance.playerTeams.TryGetValue(pl, out var t)
                           && t == newLeadingTeam;
                },
                onIntialCreation: p => p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f)
            );

            winningPlayer.AddClip(newLeadingTeam == "Team1"
                ? "Team1_WinningReversal"
                : "Team2_WinningReversal");

            // Losing Team: Losing/Warning Clips
            var losingPlayer = AudioPlayer.CreateOrGet(
                $"Announcer_{losingTeam}_Losing",
                condition: hub =>
                {
                    var pl = Player.Get(hub);
                    return pl != null
                           && Plugin.Instance.playerTeams.TryGetValue(pl, out var t)
                           && t == losingTeam;
                },
                onIntialCreation: p => p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f)
            );

            // Example for Team 2: "Team2Losing" Already LoadCliped. Register Team1 with the right package if needed.
            losingPlayer.AddClip(losingTeam == "Team2" ? "Team2Losing" : "MTF_LOSING");

            // Showing hint
            foreach (var p in Player.List.Where(p => GetTeamName(p) == newLeadingTeam))
                p.ShowHint($"<color=yellow>{newLeadingTeam} Announcer: We have taken the lead!</color>");
        }

        // ----- Timer Util -----
        public void CancelAudioTimers()
        {
            foreach (var h in Plugin.Instance.AudioTimers)
                if (h.IsRunning) Timing.KillCoroutines(h);
            Plugin.Instance.AudioTimers.Clear();
        }

        public void ScheduleAudioAfter(TimeSpan after, Action action)
        {
            var delay = (float)Math.Max(0, after.TotalSeconds);
            var h = Timing.CallDelayed(delay, () =>
            {
                if (Plugin.Instance.TdmStarted) action?.Invoke();
            });
            Plugin.Instance.AudioTimers.Add(h);
        }

        public void ScheduleAudioAt(DateTime when, Action action)
        {
            var delta = (float)(when - DateTime.Now).TotalSeconds;
            if (delta <= 0) return;

            var h = Timing.CallDelayed(delta, () =>
            {
                if (Plugin.Instance.TdmStarted) action?.Invoke();
            });
            Plugin.Instance.AudioTimers.Add(h);
        }

        // ----- No More Use -----
        /*public void OnRoundStarted()
        {
            // 필요 시 라운드 시작 시점에서 테스트 재생
            // PlayTeamStartAudio("Team2", "ChaosLoad");
            // PlayTeamStartAudio("Team1", "MtfLoad");
        }*/

        private string GetTeamName(Player player)
        {
            return player.Role.Team switch
            {
                Team.ChaosInsurgency => "Team2",
                Team.FoundationForces => "Team1",
                _ => "Others"
            };
        }
    }
}
