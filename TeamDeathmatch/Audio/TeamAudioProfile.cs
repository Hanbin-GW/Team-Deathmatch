using TeamDeathmatchAPI;
namespace TeamDeathmatch.Audio
{
    /// <summary>
    /// Simple data class with team contextual audio clip mapping
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