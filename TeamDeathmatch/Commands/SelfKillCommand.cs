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
                response = "❌ 현재 팀 데스매치가 진행 중이 아닙니다.";
                return false;
            }
            Player player = Player.Get(sender);
            
            if (player == null)
            {
                response = "플레이어를 찾을 수 없습니다.";
                return false;
            }

            response = "20초뒤 리스폰 됩니다!";
            if (player.CurrentRoom.Zone == Plugin.Instance.StartZone)
            {
                response = "구역에 재대로 스폰되어 있습니다!";
                return false;
            }
            Timing.CallDelayed(20, () => player.Kill("스폰버그 처리"));
            return true;
        }

        public string Command { get; } = "Self Respawn";
        public string[] Aliases { get; } = new[] { "SR" };
        public string Description { get; } = "버그가 발생할시 `.SR` 를 입력하여 리스폰하실수 있습니다. (소모시간: 20~30초)";
    }
}