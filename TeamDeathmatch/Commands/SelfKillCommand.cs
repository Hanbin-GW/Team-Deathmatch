using System;
using CommandSystem;
using Exiled.API.Features;
using MEC;

namespace TeamDeathmatch.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class SelfKillCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Plugin.Instance.TdmStarted)
            {
                response = "❌ Team Death Match is not in progress right now.";
                return false;
            }
            Player player = Player.Get(sender);
            
            if (player == null)
            {
                response = "Cannot Find Player.";
                return false;
            }

            response = "You'll respawn 20 secounds later";
            if (player.CurrentRoom.Zone == Plugin.Instance.StartZone)
            {
                response = "You've been spawned up in the zone!";
                return false;
            }
            Timing.CallDelayed(20, () => player.Kill("Bug kill"));
            return true;
        }

        public string Command { get; } = "Self Respawn";
        public string[] Aliases { get; } = new[] { "SR" , "sr"};
        public string Description { get; } = "If a bug occurs, you can respawn by entering '.SR' (Consumption time: 20-30 seconds)";
    }
}