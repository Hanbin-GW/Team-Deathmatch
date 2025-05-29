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
                response = "❌ 현재 팀 데스매치가 진행 중이 아닙니다.";
                return false;
            }

            var scores = Plugin.Instance.TeamScores;
            response = $"📊 점수 현황:\nTeam1 (NTF): {scores["Team1"]}점\nTeam2 (Chaos): {scores["Team2"]}점";
            return true;
        }


        public string Command { get; } = "Score";
        public string[] Aliases { get; } = new[] { "sc", "score" };
        public string Description { get; } = "현재 TDM 점수를 보여줍니다.";
    }
}