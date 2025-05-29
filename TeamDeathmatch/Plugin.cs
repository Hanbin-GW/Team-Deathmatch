using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;
using Exiled.Loader;
using Exiled.CustomRoles.API.Features;
using GhostPlugin.API;

namespace TeamDeathmatch
{
    public class Plugin : Plugin<Config>
    {

        public override string Name => "Team deathmatch";
        public override string Author => "Hanbin-GW";
        public override Version Version { get; } = new Version(1, 0, 0);
        
        private Dictionary<Player, string> playerTeams = new();
        private List<Player> waitingPlayers = new();
        private bool tdmStarted = false;

        private void OnVerified(VerifiedEventArgs ev)
        {
            if (tdmStarted)
                return;

            waitingPlayers.Add(ev.Player);
            ev.Player.Broadcast(5, $"대기 중... ({waitingPlayers.Count}/10)");

            if (waitingPlayers.Count == 10)
                StartTdm();
        }
        private List<Player> team1 = new();
        private List<Player> team2 = new();
        List<CustomRole> MtfRoles = new();
        List<CustomRole> ChaosRoles = new();
        private Dictionary<string, int> teamScores = new();
        
        private bool TryAssignRandomCustomRole(Player player)
        {
            List<CustomRole> source = null;

            if (player.Role.Team == Team.FoundationForces)
                source = MtfRoles;
            else if (player.Role.Team == Team.ChaosInsurgency)
                source = ChaosRoles;

            if (source == null || source.Count == 0)
                return false;

            int index = UnityEngine.Random.Range(0, source.Count);
            CustomRole selected = source[index];

            if (selected is ICustomRole cr && UnityEngine.Random.Range(0, 100) >= cr.Chance)
                return false;

            selected.AddRole(player);
            return true;
        }


        private void StartTdm()
        {
            tdmStarted = true;
            Round.IsLocked = true;
            Round.Start();
            teamScores["Team1"] = 0;
            teamScores["Team2"] = 0;

            var shuffled = waitingPlayers.OrderBy(x => UnityEngine.Random.value).ToList();

            for (int i = 0; i < 5; i++)
            {
                var p = shuffled[i];
                p.Role.Set(RoleTypeId.NtfSergeant);
                team1.Add(p);
                playerTeams[p] = "Team1"; 
                p.Broadcast(5, "당신은 NTF 팀입니다!");
            }

            for (int i = 5; i < 10; i++)
            {
                var p = shuffled[i];
                p.Role.Set(RoleTypeId.ChaosRifleman);
                team2.Add(p);
                playerTeams[p] = "Team2"; 
                p.Broadcast(5, "당신은 카오스 팀입니다!");
            }

            Map.Broadcast(10, "Team Deathmatch 시작! 30킬 먼저 하는 팀이 승리합니다.");
        }
        
        public void OnPlayerDied(DiedEventArgs ev)
        {
            if (!tdmStarted) return;

            string team;
            if (!playerTeams.TryGetValue(ev.Player, out team))
                return;

            Timing.CallDelayed(5f, () =>
            {
                if (ev.Player == null || !ev.Player.IsConnected) return;

                ev.Player.Role.Set(team == "Team1" ? RoleTypeId.NtfSergeant : RoleTypeId.ChaosRifleman);
                ev.Player.ClearInventory();
                GiveLoadout(ev.Player);
                ev.Player.Position = GetSpawnPointForTeam(team); // 팀별 스폰 지점 지정
            });
            if (teamScores[team] >= 30)
            {
                EndTdm(team);
            }
        }

        private void GiveLoadout(Player player)
        {
            if (!playerTeams.TryGetValue(player, out string team))
                return;

            player.ClearInventory();

            if (team == "Team1")
            {
                if (Loader.Random.Next(0, 100) <= 30)
                {
                    if (TryAssignRandomCustomRole(player))
                    {
                        player.Broadcast(10,"당신한테 특수직업이 적용되었습니다!");
                    }
                }
                player.AddItem(ItemType.GunE11SR);
                player.AddItem(ItemType.ArmorHeavy);
                player.AddItem(ItemType.Medkit);
                player.AddAmmo(AmmoType.Nato556,120);
            }
            else if (team == "Team2")
            {
                if (Loader.Random.Next(0, 100) <= 30)
                {
                    if (TryAssignRandomCustomRole(player))
                    {
                        player.Broadcast(10,"당신한테 특수직업이 적용되었습니다!");
                    }
                }
                player.AddItem(ItemType.GunAK);
                player.AddItem(ItemType.ArmorHeavy);
                player.AddItem(ItemType.Adrenaline);
                player.Ammo[ItemType.Ammo762x39] = 90;
            }
        }

        private void EndTdm(string winningTeam)
        {
            Map.Broadcast(10, $"{winningTeam} 승리! 게임을 재시작합니다.");
            Round.IsLocked = false;
            Timing.CallDelayed(10,()=>Round.Restart());
        }
        
        private Vector3 GetSpawnPointForTeam(string team)
        {
            if (team == "Team1")
                return new Vector3(0, 300, 0);
            else
                return new Vector3(50, 300, 0);
        }

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Verified += OnVerified;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            base.OnDisabled();
        }
    }
}