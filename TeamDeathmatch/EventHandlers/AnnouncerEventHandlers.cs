using System.Linq;
using Exiled.API.Features;
using PlayerRoles;

namespace TeamDeathmatch.EventHandlers
{
    public class AnnouncerEventHandlers
    {
        public Plugin Plugin;
        
        private void RunReversalEvent(string newLeadingTeam)
        {
            foreach (var player in Player.List)
            {
                player.ShowHint($"<color=yellow>{newLeadingTeam} 어나운서: 우리가 선두를 잡았다!</color>");
            }

            foreach (var player in Player.List.Where(p => GetTeamName(p) == newLeadingTeam))
            {
                //player.AddItem(ItemType.GunE11SR);
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