using Exiled.API.Interfaces;

namespace TeamDeathmatch
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
        public int TeamSize { get; set; } = 6;
        public int TeamScoreToWin { get; set; } = 30;
    }
}