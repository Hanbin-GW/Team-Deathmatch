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
        
        public void OnPluginLoad()
        {
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "Opfor", "LoadUpLetsGo.ogg"), "ChaosLoad");
            // AudioClipStorage.LoadClip(Path.Combine(AudioDirectory, "tf141", "ReadyToMove.ogg"), "MtfLoad");
        }
        public AnnouncerEventHandlers()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AudioDirectory = Path.Combine(appDataPath, "EXILED", "Plugins", "DeathMatch");
        }
        
        public void OnRoundStarted()
        {
            PlayTeamStartAudio("Chaos", "ChaosLoad");
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
        private void RunReversalEvent(string newLeadingTeam)
        {
            // ✅ 역전한 팀용 오디오 플레이어
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
                player.ShowHint($"<color=yellow>{newLeadingTeam} 어나운서: 우리가 선두를 잡았다!</color>");
            }
        }
        private string GetTeamName(Player player)
        {
            return player.Role.Team switch
            {
                Team.ChaosInsurgency => "Chaos",
                Team.FoundationForces => "Foundation",
                _ => "Others"
            };
        }
    }
}