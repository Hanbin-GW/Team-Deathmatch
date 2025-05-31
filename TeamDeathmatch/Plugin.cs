using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
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
        public override Version Version { get; } = new Version(1, 0, 3);
        
        public Dictionary<Player, string> playerTeams = new();
        public List<Player> waitingPlayers = new();
        public bool TdmStarted = false;
        public static Plugin Instance { get; private set; }
        public override PluginPriority Priority { get; } = PluginPriority.Lowest;
        public void OnVerified(VerifiedEventArgs ev)
        {
            if (!TdmStarted)
            {
                if (!waitingPlayers.Contains(ev.Player))
                    waitingPlayers.Add(ev.Player);

                ev.Player.Broadcast(5, $"TDM 대기 중... ({waitingPlayers.Count}/{Config.TeamSize * 2})");

                if (waitingPlayers.Count >= Config.TeamSize * 2)
                    StartTdm();

                return;
            }

            string team;
            if (team1.Count <= team2.Count)
            {
                team = "Team1";
                team1.Add(ev.Player);
                ev.Player.Role.Set(RoleTypeId.NtfSergeant);
            }
            else
            {
                team = "Team2";
                team2.Add(ev.Player);
                ev.Player.Role.Set(RoleTypeId.ChaosRifleman);
            }

            playerTeams[ev.Player] = team;
            ev.Player.Broadcast(5, $"게임 도중 참가: {team} 팀에 배정되었습니다.");
            Timing.CallDelayed(1f, () =>
            {
                ev.Player.ClearInventory();
                GiveLoadout(ev.Player);
                ev.Player.Position = GetSpawnPointForTeam(team);
            });
        }

        private List<Player> team1 = new();
        private List<Player> team2 = new();
        List<CustomRole> MtfRoles = new();
        List<CustomRole> ChaosRoles = new();
        public Dictionary<string, int> TeamScores = new();
        
        private bool TryAssignRandomCustomRole(Player player)
        {
            if (!playerTeams.TryGetValue(player, out var team))
                return false;

            List<CustomRole> source = team == "Team1" ? MtfRoles : ChaosRoles;

            if (source.Count == 0)
                return false;

            int index = UnityEngine.Random.Range(0, source.Count);
            CustomRole selected = source[index];

            if (selected is ICustomRole cr && UnityEngine.Random.Range(0, 100) >= cr.Chance)
                return false;

            selected.AddRole(player);
            return true;
        }



        public void StartTdm()
        {
            TdmStarted = true;
            Round.IsLocked = true;
            Round.Start();
            TeamScores["Team1"] = 0;
            TeamScores["Team2"] = 0;

            var shuffled = waitingPlayers.OrderBy(x => UnityEngine.Random.value).ToList();

            for (int i = 0; i < Config.TeamSize; i++)
            {
                var p = shuffled[i];
                p.Role.Set(RoleTypeId.NtfSergeant);
                Timing.CallDelayed(0.1f, () =>
                {
                    Log.Info($"[{p.Nickname}] 팀 확인: {p.Role.Team}");
                });
                team1.Add(p);
                playerTeams[p] = "Team1"; 
                p.Broadcast(5, "당신은 NTF 팀입니다!");
            }

            for (int i = 5; i < Config.TeamSize; i++)
            {
                var p = shuffled[i];
                p.Role.Set(RoleTypeId.ChaosRifleman);
                team2.Add(p);
                playerTeams[p] = "Team2"; 
                p.Broadcast(5, "당신은 카오스 팀입니다!");
            }

            Map.Broadcast(10, "Team Deathmatch 시작! 30킬 먼저 하는 팀이 승리합니다.");
        }

        private void OnPlayerDied(DiedEventArgs ev)
        {
            if (!TdmStarted) return;

            string team;
            if (!playerTeams.TryGetValue(ev.Player, out team))
                return;

            // 팀 정보 확인
            if (!playerTeams.TryGetValue(ev.Player, out string victimTeam))
                return;

            if (ev.Attacker is not { } attacker || attacker == ev.Player)
                return; 

            if (!playerTeams.TryGetValue(attacker, out string attackerTeam))
                return;

            if (!TeamScores.ContainsKey(attackerTeam))
                TeamScores[attackerTeam] = 0;

            TeamScores[attackerTeam]++;      
            
            foreach (var p in Player.List)
            {
                p.ShowHint(
                    $"<b><color=blue>Team1: {TeamScores["Team1"]}</color> | <color=green>Team2: {TeamScores["Team2"]}</color></b>",
                    3f
                );
            }
            
            Timing.CallDelayed(5f, () =>
            {
                if (ev.Player == null || !ev.Player.IsConnected) return;

                ev.Player.Role.Set(team == "Team1" ? RoleTypeId.NtfSergeant : RoleTypeId.ChaosRifleman);
                ev.Player.ClearInventory();
                GiveLoadout(ev.Player);
                ev.Player.Position = GetSpawnPointForTeam(team); // 팀별 스폰 지점 지정
            });
            if (TeamScores[team] >= 30)
            {
                EndTdm(team);
            }
        }

        private void OnRoundStarted()
        {
            if (TdmStarted)
                return;

            // 모든 플레이어를 대기열에 추가
            waitingPlayers.Clear();
            team1.Clear();
            team2.Clear();
            playerTeams.Clear();

            foreach (var p in Player.List)
            {
                if (p.Role.Team is Team.FoundationForces or Team.ChaosInsurgency)
                {
                    waitingPlayers.Add(p);
                }
                else
                {
                    // SCP / D-Class / Scientist → Spectator로 전환 또는 제거
                    p.Role.Set(RoleTypeId.Spectator);
                    p.Broadcast(5, "TDM 모드에서는 SCP/과학자/디클래스는 참여할 수 없습니다.");
                }
            }

            if (waitingPlayers.Count >= 2) // 또는 무조건 실행도 가능
            {
                StartTdm(); // ✅ TDM 즉시 시작
            }
            else
            {
                Log.Send("[TDM] 대기 인원이 부족하여 시작하지 못했습니다.", LogLevel.Warn, ConsoleColor.Green);
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
            Map.Broadcast(10, $"{winningTeam} 승리! 라운드를 재시작합니다.");
            TdmStarted = false;
            // 이건 그대로 유지
            Timing.CallDelayed(5f, () => Round.Restart());
        }
        
        private Vector3 GetSpawnPointForTeam(string team)
        {
            if (team == "Team1")
                return new Vector3(125, 296, -41);
            else
                return new Vector3(6, 292, -42);
        }
        
        private void LoadTeamRoles()
        {
            MtfRoles.Clear();
            ChaosRoles.Clear();

            foreach (var role in CustomRole.Registered)
            {
                if (role is ICustomRole custom)
                {
                    switch (custom.StartTeam)
                    {
                        case StartTeam.Ntf:
                            MtfRoles.Add(role);
                            break;
                        case StartTeam.Chaos:
                            ChaosRoles.Add(role);
                            break;
                    }
                }
            }

            Log.Info($"[TDM] 커스텀 롤 로딩 완료: MTF {MtfRoles.Count}개, Chaos {ChaosRoles.Count}개.");
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (!Round.IsStarted || TdmStarted)
                return;

            // SCP / Scientist / D-Class 등 허용되지 않은 진영 필터링
            if (ev.Player.Role.Team is Team.SCPs or Team.ClassD or Team.Scientists)
            {
                Log.Info($"[TDM] {ev.Player.Nickname}은 허용되지 않은 팀이므로 인간 진영으로 강제 이동됩니다.");

                string team;
                if (team1.Count <= team2.Count)
                {
                    team = "Team1";
                    team1.Add(ev.Player);
                    ev.Player.Role.Set(RoleTypeId.NtfSergeant);
                }
                else
                {
                    team = "Team2";
                    team2.Add(ev.Player);
                    ev.Player.Role.Set(RoleTypeId.ChaosRifleman);
                }

                playerTeams[ev.Player] = team;
                waitingPlayers.Add(ev.Player);
                Timing.CallDelayed(1f, () =>
                {
                    ev.Player.ClearInventory();
                    GiveLoadout(ev.Player);
                    ev.Player.Position = GetSpawnPointForTeam(team);
                    ev.Player.Broadcast(5, $"TDM: {team} 팀으로 자동 배정되었습니다.");
                });
            }
        }

        private void OnLeft(LeftEventArgs ev)
        {
            if (!TdmStarted) return;

            // 플레이어 리스트에서 제거
            waitingPlayers.Remove(ev.Player);
            team1.Remove(ev.Player);
            team2.Remove(ev.Player);
            playerTeams.Remove(ev.Player);

            int alive = team1.Count + team2.Count;

            if (alive <= 2)
            {
                TdmStarted = false;
                Round.IsLocked = false;
                Map.Broadcast(10, "⚠️ 플레이어 수 부족으로 라운드를 종료합니다!");
                Timing.CallDelayed(5f, () => Round.Restart());
            }
        }

        private void OnWaitingForPlayers()
        {
            Round.IsLocked = true;
            Log.Send("[TDM] 라운드 잠금: 일반 라운드 비활성화", LogLevel.Info, ConsoleColor.Blue);
        }
        
        public override void OnEnabled()
        {
            Instance = this;
            LoadTeamRoles();
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Died += OnPlayerDied;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Died -= OnPlayerDied;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            base.OnDisabled();
            Instance = null;
        }
    }
}