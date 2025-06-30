using System;
using CommandSystem;

namespace TeamDeathmatch.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class ScoreCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Plugin.Instance.TdmStarted)
            {
                response = "❌ Team Death Match is not in progress right now.";
                return false;
            }

            var scores = Plugin.Instance.TeamScores;
            response = $"📊 Score Status:\nTeam1 (NTF): {scores["Team1"]}\nTeam2 (Chaos): {scores["Team2"]}";
            return true;
        }


        public string Command { get; } = "Score";
        public string[] Aliases { get; } = new[] { "sc", "score" };
        public string Description { get; } = "Check the current round score.";
    }
}