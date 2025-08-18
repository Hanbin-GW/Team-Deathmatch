using System;
using System.Collections.Generic;
using Exiled.API.Features;
using TeamDeathmatchAPI;

namespace TeamDeathmatch.Audio
{
    public static class AudioManager
    {
        private static readonly Dictionary<string, AudioPlayer> _teamPlayers = new();
        private static readonly Dictionary<string, TeamAudioProfile> _teamProfiles = new();
        private static readonly Dictionary<string, API.TeamState> _lastTeamState = new();

        public static void InitTeamAudioProfiles()
        {
            _teamProfiles["Team1"] = new TeamAudioProfile
            {
                LeadingClip     = "MTF_LEADING",
                LosingClip      = "MTF_LOSING",
                TiedClip        = "MTF_TIED",
                MatchPointClip  = "MTF_MATCH_POINT",
                VictoryClip     = "MTF_VICTORY",
                DefeatClip      = "MTF_DEFEAT",
            };
            _teamProfiles["Team2"] = new TeamAudioProfile
            {
                LeadingClip     = "CI_LEADING",
                LosingClip      = "CI_LOSING",
                TiedClip        = "CI_TIED",
                MatchPointClip  = "CI_MATCH_POINT",
                VictoryClip     = "CI_VICTORY",
                DefeatClip      = "CI_DEFEAT",
            };
        }

        /*private static AudioPlayer EnsureTeamPlayer(string teamName)
        {
            if (_teamPlayers.TryGetValue(teamName, out var p)) return p;

            var player = AudioPlayer.CreateOrGet(
                $"Announcer_{teamName}",
                condition: hub =>
                {
                    var pl = Player.Get(hub);
                    return pl != null
                           && TeamDeathmatch.Plugin.Instance.playerTeams.TryGetValue(pl, out var t)
                           && t == teamName;
                },
                onIntialCreation: ap =>
                {
                    ap.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f);
                }
            );
            _teamPlayers[teamName] = player;
            return player;
        }*/
    }
}
