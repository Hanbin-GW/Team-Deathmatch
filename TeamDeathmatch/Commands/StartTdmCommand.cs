using System;
using CommandSystem;
using Exiled.API.Features;

namespace TeamDeathmatch.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class StartTdmCommand : ICommand
    {
        public string Command => "startdm";
        public string[] Aliases => new[] { "fst" };
        public string Description => "강제로 TDM을 시작합니다.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsStarted)
            {
                response = "❌ 라운드가 아직 시작되지 않았습니다.";
                return false;
            }

            if (TeamDeathmatch.Plugin.Instance.TdmStarted)
            {
                response = "⚠️ TDM은 이미 시작되었습니다.";
                return false;
            }

            if (TeamDeathmatch.Plugin.Instance.WaitingPlayers.Count < 10)
            {
                response = $"❌ TDM을 시작하려면 최소 10명이 필요합니다. 현재: {TeamDeathmatch.Plugin.Instance.WaitingPlayers.Count}/10";
                return false;
            }

            TeamDeathmatch.Plugin.Instance.StartTdm();
            response = "✅ TDM이 강제로 시작되었습니다!";
            return true;
        }
    }

}