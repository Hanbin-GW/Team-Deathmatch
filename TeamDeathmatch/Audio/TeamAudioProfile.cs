using TeamDeathmatchAPI;
namespace TeamDeathmatch.Audio
{
    /// <summary>
    /// 팀 상황별 오디오 클립 매핑을 담는 단순 데이터 클래스
    /// </summary>
    public class TeamAudioProfile
    {
        public string LeadingClip { get; set; }
        public string LosingClip { get; set; }
        public string TiedClip { get; set; }
        public string MatchPointClip { get; set; }
        public string VictoryClip { get; set; }
        public string DefeatClip { get; set; }

        public string GetClip(API.TeamState state)
        {
            return state switch
            {
                API.TeamState.Leading    => LeadingClip,
                API.TeamState.Losing     => LosingClip,
                API.TeamState.Tied       => TiedClip,
                API.TeamState.MatchPoint => MatchPointClip,
                API.TeamState.Victory    => VictoryClip,
                API.TeamState.Defeat     => DefeatClip,
                _ => null
            };
        }
    }
}