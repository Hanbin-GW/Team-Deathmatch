using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using TeamDeathmatch.Audio;
using TeamDeathmatch.TeamDeathmatchAPI; // Use TeamState, TeamAudioProfile

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

        /// <summary>
        /// 상태 캐시 초기화 (TDM 시작 시점에 호출)
        /// </summary>
        public void ResetTeamStateCache()
        {
            _lastTeamState.Clear();
            Log.Debug("[Announcer] TeamState cache reset.");
        }
        public void OnPluginLoad()
        {
            EnsureMusicDirectoryExists();
            InitTeamAudioProfiles();

            // Load audio files (Clip keys must match the profile below)
                AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "LoadUpLetsGo.ogg"),   "Team2_Start");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "Returntobase.ogg"),   "Team2_Defeat");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "TakenLead.ogg"),      "Team2_Leading");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "EnemyLead.ogg"),      "Team2_Losing");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "tied.ogg"),           "Team2_TIED");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "clocksticking.ogg"),  "Team2_ClockTick");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "tensec(2).ogg"),  "TEN_SECONDS_LEFT");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "clocksticking.ogg"),  "Team2_ClockTick");

            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "tf141", "WinningReversal.ogg"), "Team1_WinningReversal");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "tf141", "tied141.ogg"), "Team1_Tied");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "LostLead141.ogg"),      "Team1_Losing");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "TakenLead.ogg"), "Team2_WinningReversal");
        }

        private void EnsureMusicDirectoryExists()
        {
            string rootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EXILED", "Plugins", "DeathMatch");

            string opforPath = Path.Combine(rootPath, "Opfor");
            string tf141Path = Path.Combine(rootPath, "tf141");

            // 최상위 폴더 체크
            if (!Directory.Exists(rootPath))
            {
                Log.Warn($"[Announcer] Root music folder not found, creating: {rootPath}");
                Directory.CreateDirectory(rootPath);
            }
            else
            {
                Log.Info("[Announcer] Root music folder already exists.");
            }

            // 하위 폴더 체크
            if (!Directory.Exists(opforPath))
            {
                Log.Warn($"[Announcer] Missing subfolder 'Opfor'. Creating: {opforPath}");
                Directory.CreateDirectory(opforPath);
            }
            if (!Directory.Exists(tf141Path))
            {
                Log.Warn($"[Announcer] Missing subfolder 'tf141'. Creating: {tf141Path}");
                Directory.CreateDirectory(tf141Path);
            }
        }

        private void InitTeamAudioProfiles()
        {
            // Profile for Team 1 (MTF)
            _teamProfiles["Team1"] = new TeamAudioProfile
            {
                LeadingClip     = "MTF_LEADING",
                LosingClip      = "MTF_LOSING",
                TiedClip        = "MTF_TIED",
                HitHardClip  = "MTF_MATCH_POINT",
                VictoryClip     = "MTF_VICTORY",
                DefeatClip      = "MTF_DEFEAT",
            };

            // Profile for Team 2 (Chaos)
            _teamProfiles["Team2"] = new TeamAudioProfile
            {
                LeadingClip     = "Team2_Leading",
                LosingClip      = "Team2_Losing",
                TiedClip        = "Team2_TIED",
                HitHardClip  = "Team2_ClockTick",
                VictoryClip     = "CI_VICTORY",
                DefeatClip      = "Team2_Defeat",        // Match top Load Clip
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
        public void PlayTeamStateAudio(string teamName, API.TeamState state)
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
        /*public void PlayTeamStartAudio(string teamName, string clipName)
        {
            AudioPlayer audioPlayer = AudioPlayer.CreateOrGet(
                $"Announcer_{teamName}",
                condition: (hub) =>
                {
                    Player player = Player.Get(hub);
                    return player != null
                           && Plugin.Instance.playerTeams.ContainsKey(player)
                           && Plugin.Instance.playerTeams[player] == teamName;
                },
                onIntialCreation: (p) =>
                {
                    Speaker speaker = p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f);
                }
            );

            audioPlayer.AddClip(clipName);
        }*/
        public void PlayTeamStartAudio(string teamName, string clipName)
        {
            AudioPlayer audioPlayer = AudioPlayer.CreateOrGet(
                $"Announcer_{teamName}",
                condition: hub =>
                {
                    Player player = Player.Get(hub);
                    if (player == null) return false;

                    // 1) playerTeams 매핑이 있으면 그것으로 필터
                    if (Plugin.Instance.playerTeams.TryGetValue(player, out var mapped))
                        return mapped == teamName;

                    // 2) 매핑 전이면 Role.Team으로 폴백
                    return teamName switch
                    {
                        "Team1" => player.Role.Team == Team.FoundationForces,
                        "Team2" => player.Role.Team == Team.ChaosInsurgency,
                        _ => false
                    };
                },
                onIntialCreation: p => p.AddSpeaker("Main", isSpatial: true, maxDistance: 5000f)
            );

            audioPlayer.AddClip(clipName); // AddClip 후 자동 재생 환경이라고 했으니 OK
        }




        // ----- reversal event -----
        public void RunReversalEvent(string newLeadingTeam)
        {
            if (string.IsNullOrWhiteSpace(newLeadingTeam)) return;

            var losingTeam = newLeadingTeam == "Team1" ? "Team2" : "Team1";

            // 디버그: 해당 팀으로 매핑된 인원 수 체크
            int winCnt = Player.List.Count(pl => Plugin.Instance.playerTeams.TryGetValue(pl, out var t) && t == newLeadingTeam);
            int loseCnt = Player.List.Count(pl => Plugin.Instance.playerTeams.TryGetValue(pl, out var t) && t == losingTeam);
            Log.Info($"[Announcer] Reversal -> lead:{newLeadingTeam}({winCnt} players), lose:{losingTeam}({loseCnt} players)");

            // 윈팀 플레이어
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

            winningPlayer.AddClip(newLeadingTeam == "Team1" ? "Team1_WinningReversal" : "Team2_WinningReversal");

            // 루징팀 플레이어
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

            losingPlayer.AddClip(losingTeam == "Team2" ? "Team2_Losing" : "MTF_LOSING");

            foreach (var p in Player.List.Where(p => GetTeamName(p) == newLeadingTeam))
                p.ShowHint($"<color=yellow>{newLeadingTeam} 어나운서: 우리가 선두를 잡았다!</color>");
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

        public void OnRoundStarted()
        {
            // 라운드 시작 직후는 playerTeams가 비어있을 수 있으니 약간 딜레이
            ScheduleAudioAfter(TimeSpan.FromSeconds(2), () =>
            {
                PlayTeamStartAudio("Team2", "Team2_Start");
                PlayTeamStartAudio("Team1", "Team2_Start"); // Team1 전용 시작음이 없으면 임시로 같은 키 사용
                Log.Info($"[Announcer] RoundStart start-audio fired from: {AudioDirectory}");
            });
        }
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