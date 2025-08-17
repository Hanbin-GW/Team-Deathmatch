namespace TeamDeathmatchAPI
{
    public class API
    {
        public enum TeamState
        {
            Leading,   // 리드 중
            Losing,    // 지는 중
            Tied,      // 동점
            MatchPoint,// 매치포인트(승점 1 남음)
            Victory,   // 승리
            Defeat     // 패배
        }
        
        public sealed class TeamAudioProfile
        {
            // 상황별로 재생할 클립 키(= 소스)
            public string LeadingClip { get; init; }
            public string LosingClip { get; init; }
            public string TiedClip { get; init; }
            public string MatchPointClip { get; init; }
            public string VictoryClip { get; init; }
            public string DefeatClip { get; init; }

            public string GetClip(TeamState state) => state switch
            {
                TeamState.Leading    => LeadingClip,
                TeamState.Losing     => LosingClip,
                TeamState.Tied       => TiedClip,
                TeamState.MatchPoint => MatchPointClip,
                TeamState.Victory    => VictoryClip,
                TeamState.Defeat     => DefeatClip,
                _ => null
            };
        }
    }
}