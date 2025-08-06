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
            AudioClipStorage.LoadClip(Path.Combine(AudioDirectory,"\\Opfor","\\LoadUpLetsGo.ogg"), "LoadUpLetsGo");
        }
        public AnnouncerEventHandlers()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AudioDirectory = Path.Combine(appDataPath, "EXILED", "Plugins", "DeathMatch");
        }
        
        private void RunReversalEvent(string newLeadingTeam)
        {
            foreach (var player in Player.List)
            {
                AudioPlayer audioPlayerWinning = AudioPlayer.CreateOrGet(
                    $"Announcer AudioPlayer",
                    condition: (hub) =>
                    {
                        Player player = new Player(hub);
                        return player != null
                               && Plugin.Instance.playerTeams.ContainsKey(player)
                               && Plugin.Instance.playerTeams[player] == newLeadingTeam;
                    },
                    onIntialCreation: (p) =>
                    {
                        Speaker speaker = p.AddSpeaker("Main", isSpatial: true, maxDistance: 5000f);
                    });
                
                AudioPlayer audioPlayerLosing = AudioPlayer.CreateOrGet(
                    $"Announcer AudioPlayer",
                    condition: (hub) =>
                    {
                        Player player = new Player(hub);
                        return player != null
                               && Plugin.Instance.playerTeams.ContainsKey(player)
                               && Plugin.Instance.playerTeams[player] != newLeadingTeam;
                    },
                    onIntialCreation: (p) =>
                    {
                        Speaker speaker = p.AddSpeaker("Main", isSpatial: true, maxDistance: 5000f);
                    });
            }
            
            foreach (var player in Player.List.Where(p => GetTeamName(p) == newLeadingTeam))
            {
                //player.AddItem(ItemType.GunE11SR);
                //player.ShowHint($"<color=yellow>{newLeadingTeam} 어나운서: 우리가 선두를 잡았다!</color>");
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