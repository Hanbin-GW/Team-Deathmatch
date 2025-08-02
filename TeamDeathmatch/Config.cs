using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Interfaces;
using UnityEngine;
using YamlDotNet.Serialization;

namespace TeamDeathmatch
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
        public int TeamSize { get; set; } = 6;
        public int TeamScoreToWin { get; set; } = 30;

        public Dictionary<string, List<RoomType>> LczRespawns { get; set; } = new()
        {
            { "Team1", new List<RoomType> { RoomType.LczClassDSpawn, RoomType.LczGlassBox, RoomType.Lcz173, RoomType.LczToilets } },
            { "Team2", new List<RoomType> { RoomType.LczCheckpointA, RoomType.LczCafe, RoomType.LczAirlock, RoomType.LczCheckpointB } }
        };

        public Dictionary<string, List<RoomType>> HczRespawns { get; set; } = new()
        {
            { "Team1", new List<RoomType> { RoomType.HczCrossing, RoomType.HczElevatorA, RoomType.Hcz127 } },
            { "Team2", new List<RoomType> { RoomType.HczServerRoom, RoomType.HczElevatorB, RoomType.HczNuke } }
        };
        public Dictionary<string, List<RoomType>> EzRespawns { get; set; } = new()
        {
            { "Team1", new List<RoomType> { RoomType.EzCheckpointHallwayA, RoomType.EzCheckpointHallwayB } },
            { "Team2", new List<RoomType> { RoomType.EzGateA, RoomType.EzGateB } }
        };
        [YamlIgnore]
        public Dictionary<string, List<Vector3>> SurfaceRespawns { get; set; } = new()
        {
            { "Team1", new List<Vector3> { new Vector3(125, 297, -41), new Vector3(120, 297, -30) } },
            { "Team2", new List<Vector3> { new Vector3(6, 293, -42), new Vector3(10, 293, -50) } }
        };
    }
}