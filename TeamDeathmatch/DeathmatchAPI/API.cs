namespace TeamDeathmatchAPI
{
    public class API
    {
        public enum TeamState
        {
            Leading,   // Leading
            Losing,    // Losing
            Tied,      // Tied
            MatchPoint,// MatchPoint
            Victory,   // Victory
            Defeat     // Lost
        }
        
        public sealed class TeamAudioProfile
        {
            // Clip key (= source) to play contextually
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