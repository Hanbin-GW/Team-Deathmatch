using System;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using PlayerRoles;

namespace TeamDeathmatch.EventHandlers
{
    public class AnnouncerEventHandlers
    {
        public Plugin Plugin;
        public readonly string AudioDirectory;
        
        
        public AnnouncerEventHandlers()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AudioDirectory = Path.Combine(appDataPath, "EXILED", "Plugins", "DeathMatch");
        }
        public void OnPluginLoad()
        {
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "LoadUpLetsGo.ogg"), "ChaosLoad");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", ""), "ChaosFail");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "tf141", ""), "Team1_WinningReversal");
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", ""), "Team2_WinningReversal");
            // AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "tf141", "ReadyToMove.ogg"), "MtfLoad");
        }
        public void EnsureMusicDirectoryExists()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EXILED", "Plugins", "DeathMatch");

            // 폴더가 없으면 생성
            if (!Directory.Exists(path))
            {
                Log.Warn($"music folder is not existed create new one : {path}");
                Directory.CreateDirectory(path);  // 폴더 생성
            }
            else
            {
                Log.Info("music folder already exists.");
            }
        }
        public void OnRoundStarted()
        {
            PlayTeamStartAudio("Team2", "ChaosLoad");
            PlayTeamStartAudio("Team1", "ChaosLoad");
            Log.Info("Playing LoadUp");
            //PlayTeamStartAudio("Foundation", "MtfLoad");
        }

        private void PlayTeamStartAudio(string teamName, string clipName)
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
        }
        public void RunReversalEvent(string newLeadingTeam)
        {
            AudioPlayer audioPlayerWinning = AudioPlayer.CreateOrGet(
                $"Announcer_{newLeadingTeam}_Winning",
                condition: (hub) =>
                {
                    Player player = Player.Get(hub);
                    return player != null
                           && Plugin.Instance.playerTeams.ContainsKey(player)
                           && Plugin.Instance.playerTeams[player] == newLeadingTeam;
                },
                onIntialCreation: (p) =>
                {
                    p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f);
                });

            audioPlayerWinning.AddClip("WinningReversal");

            // ✅ 패배한 팀용 오디오 플레이어
            AudioPlayer audioPlayerLosing = AudioPlayer.CreateOrGet(
                $"Announcer_{newLeadingTeam}_Losing",
                condition: (hub) =>
                {
                    Player player = Player.Get(hub);
                    return player != null
                           && Plugin.Instance.playerTeams.ContainsKey(player)
                           && Plugin.Instance.playerTeams[player] != newLeadingTeam;
                },
                onIntialCreation: (p) =>
                {
                    p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f);
                });

            audioPlayerLosing.AddClip("LosingReversal");

            foreach (var player in Player.List.Where(p => GetTeamName(p) == newLeadingTeam))
            {
                player.ShowHint($"<color=yellow>{newLeadingTeam} Announcer: We taken the lead!</color>");
            }
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