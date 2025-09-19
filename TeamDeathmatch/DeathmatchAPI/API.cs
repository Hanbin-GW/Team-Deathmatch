namespace TeamDeathmatch.TeamDeathmatchAPI
{
    public class API
    {
        public enum TeamState
        {
            Leading,   // Leading
            Losing,    // Losing
            Tied,      // Tied
            HitHard,// MatchPoint
            Victory,   // Victory
            Defeat     // Lost
        }
        
        public sealed class TeamAudioProfile
        {
            // Clip key (= source) to play contextually
            public string LeadingClip { get; init; }
            public string LosingClip { get; init; }
            public string TiedClip { get; init; }
            public string HitHardClip { get; init; }
            public string VictoryClip { get; init; }
            public string DefeatClip { get; init; }

            public string GetClip(TeamState state) => state switch
            {
                TeamState.Leading    => LeadingClip,
                TeamState.Losing     => LosingClip,
                TeamState.Tied       => TiedClip,
                TeamState.HitHard    => HitHardClip,
                TeamState.Victory    => VictoryClip,
                TeamState.Defeat     => DefeatClip,
                _ => null
            };
        }
    }
}