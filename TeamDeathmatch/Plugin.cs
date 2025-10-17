using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using Discord;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;
using Exiled.Loader;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Server;
using GhostPlugin.API;
using GhostPlugin.Custom.Roles.Chaos;
using GhostPlugin.Custom.Roles.Foundation;
using Interactables.Interobjects.DoorUtils;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Utilities;
using HintServiceMeow.UI.Utilities;
using RueI.API;
using RueI.API.Elements;
using TeamDeathmatch.EventHandlers;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;

namespace TeamDeathmatch
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => "Team deathmatch";
        public override string Author => "Hanbin-GW";
        public override Version Version { get; } = new Version(3, 0, 1);
        public Dictionary<Player, string> playerTeams = new();
        public List<Player> WaitingPlayers = new();
        public bool TdmStarted = false;
        public ZoneType StartZone;
        public AnnouncerEventHandlers AnnouncerEventHandlers;
        public readonly List<CoroutineHandle> AudioTimers = new();
        private DateTime matchStartTime;
        private CoroutineHandle reversalCheckCoroutine;
        public static Plugin Instance { get; private set; }
        public override PluginPriority Priority { get; } = PluginPriority.Lowest;
        private void OnVerified(VerifiedEventArgs ev)
        {
            
            //TestDisplay
            RueDisplay display = RueDisplay.Get(ev.Player);
            Tag welcomeTag = new();
            display.Show(welcomeTag, new BasicElement(800, "Welcome to the Ghost TDM server!"));
            display.Show(new BasicElement(300, "Don't forget to read the rules!"), 10f);
            Timing.CallDelayed(5f, () =>
            {
                display.Show(welcomeTag, new BasicElement(800, "New update: We added support for multiple hints at once!"), 10f);
            });
            
            
            Hint hint = new Hint()
            {
                Text = "Ghost Server [Team Death Match]",
                FontSize = 18,
                YCoordinate = 1000,
                Alignment = HintAlignment.Center,
            };
            PlayerDisplay playerDisplay = PlayerDisplay.Get(ev.Player);
            playerDisplay.AddHint(hint);
            if (!TdmStarted)
            {
                if (!WaitingPlayers.Contains(ev.Player))
                    WaitingPlayers.Add(ev.Player);

                ev.Player.Broadcast(5, $"TDM 대기 중... ({WaitingPlayers.Count}/{Config.TeamSize * 2})");

                if (WaitingPlayers.Count >= Config.TeamSize * 2)
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
        private string lastLeadingTeam = null;
        private CoroutineHandle scoreHintCoroutine;
        private bool TryAssignRandomCustomRole(Player player)
        {
            try
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
                var ui = PlayerUI.Get(player);
                ui.CommonHint.ShowRoleHint(selected.Name, new[] { $"{selected.Description}", $"You have CustomAbilitis: {selected.CustomAbilities.ToString()}" });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[TDM] 커스텀 롤 '{player}' 지급 중 오류: {ex.Message}");
                return false;
            }

        }
        
        private void ResetTdmState()
        {
            TdmStarted = false;
            WaitingPlayers.Clear();
            team1.Clear();
            team2.Clear();
            playerTeams.Clear();
            TeamScores.Clear();
        }
        public void StartTdm()
        {
            if (WaitingPlayers.Count < 2)
            {
                Log.Warn("[TDM] Don't have enough people to start.");
                return;
            }
            AnnouncerEventHandlers.ResetTeamStateCache();
            TdmStarted = true;
            Round.IsLocked = true;
            //AnnouncerEventHandlers.PlayTeamStartAudio("Team2","ChaosLoad");
            //AnnouncerEventHandlers.PlayTeamStartAudio("Team1","ChaosLoad");
            AnnouncerEventHandlers.ScheduleAudioAfter(TimeSpan.FromMinutes(9), () => AnnouncerEventHandlers.PlayTeamStartAudio("Team2","ONE_MINUTE_LEFT"));

            // 남은 10초(= 시작 9:50): 카운트다운 시작
            AnnouncerEventHandlers.ScheduleAudioAfter(TimeSpan.FromMinutes(9).Add(TimeSpan.FromSeconds(50)), () => AnnouncerEventHandlers.PlayTeamStartAudio("Team2","TEN_SECONDS_LEFT"));
            matchStartTime = DateTime.Now;
            ZoneType[] zones = new[]
            {
                ZoneType.LightContainment,
                ZoneType.HeavyContainment,
                ZoneType.Entrance,
                ZoneType.Surface
            };
            StartZone = zones[UnityEngine.Random.Range(0, zones.Length)];
            /*foreach (var player in Player.List)
            {
                var display = PlayerDisplay.Get(player);
                display.RemoveHint("tdm_score");
            }*/
            TeamScores["Team1"] = 0;
            TeamScores["Team2"] = 0;
            scoreHintCoroutine = Timing.RunCoroutine(ShowScoreHints());
            Log.Info($"[TDM] This round's battle zone is :{StartZone}.");
            //Map.Broadcast(10, $"<b><color=yellow>{StartZone}</color></b> 구역에서 전투가 시작됩니다!");
            
            var shuffled = WaitingPlayers.OrderBy(x => UnityEngine.Random.value).ToList();
            int half = shuffled.Count / 2;

            for (int i = 0; i < half; i++)
            {
                var p = shuffled[i];
                p.Role.Set(RoleTypeId.NtfSergeant);
                team1.Add(p);
                playerTeams[p] = "Team1";
                p.Broadcast(5, "당신은 NTF 팀입니다!");
            }

            for (int i = half; i < shuffled.Count; i++)
            {
                var p = shuffled[i];
                p.Role.Set(RoleTypeId.ChaosRifleman);
                team2.Add(p);
                playerTeams[p] = "Team2";
                p.Broadcast(5, "당신은 카오스 팀입니다!");
            }
            //scoreHintCoroutine = Timing.RunCoroutine(ShowScoreHints());
            Map.Broadcast(10, $"Team Deathmatch 시작! {Instance.Config.TeamScoreToWin}킬 먼저 하는 팀이 승리합니다.\n전투위치: <b><color=yellow>{StartZone}</color></b>");
            // StartTdm() 팀 배정 및 Broadcast 끝난 직후
            Timing.CallDelayed(1f, () =>
            {
                AnnouncerEventHandlers.PlayTeamStartAudio("Team2","Team2_Start");
                // AnnouncerEventHandlers.PlayTeamStartAudio("Team1","Team1_Start");
            });
        }
        /*public void OnPlayerDied(DiedEventArgs ev)
        {
            if (!TdmStarted) return;

            if (!playerTeams.TryGetValue(ev.Player, out var team))
                return;

            if (ev.Attacker is Player attacker && attacker != ev.Player)
            {
                if (playerTeams.TryGetValue(attacker, out string attackerTeam))
                {
                    if (!TeamScores.ContainsKey(attackerTeam))
                        TeamScores[attackerTeam] = 0;

                    TeamScores[attackerTeam]++;
                    
                    string currentLeadingTeam = TeamScores["Team1"] > TeamScores["Team2"] ? "Team1" :  
                        TeamScores["Team1"] < TeamScores["Team2"] ? "Team2" : null;
                    if (currentLeadingTeam != null && currentLeadingTeam != lastLeadingTeam)
                    {
                        Log.Debug($"[TDM] Score lead change detected: {lastLeadingTeam} -> {currentLeadingTeam}");

                        // Cancel if existing reverse check is undergoing
                        if (reversalCheckCoroutine.IsRunning)
                            Timing.KillCoroutines(reversalCheckCoroutine);
                        
                        reversalCheckCoroutine = Timing.CallDelayed(3f, () =>
                        {
                            string leading = TeamScores["Team1"] > TeamScores["Team2"] ? "Team1" :
                                TeamScores["Team1"] < TeamScores["Team2"] ? "Team2" : null;
                            if (leading == currentLeadingTeam)
                            {
                                AnnouncerEventHandlers.RunReversalEvent(leading);
                                lastLeadingTeam = currentLeadingTeam;
                            }
                        });
                    }

                    foreach (var p in Player.List)
                    {
                        p.ShowHint(
                            $"<b><color=blue>MTF: {TeamScores["Team1"]}</color> | <color=green>CI: {TeamScores["Team2"]}</color></b>",
                            3f
                        );
                    }

                    if (TeamScores[attackerTeam] >= Instance.Config.TeamScoreToWin)
                    {
                        EndTdm(attackerTeam);
                        return;
                    }
                }
            }

            // ✅ 리스폰은 항상 실행
            Timing.CallDelayed(5f, () =>
            {
                if (ev.Player == null || !ev.Player.IsConnected) return;

                ev.Player.Role.Set(team == "Team1" ? RoleTypeId.NtfSergeant : RoleTypeId.ChaosRifleman);
                ev.Player.ClearInventory();
                GiveLoadout(ev.Player);
                ev.Player.Position = GetSpawnPointForTeam(team); // ⬅ 여기도 수정 필요!
            });
        }*/

        public void OnPlayerDied(DiedEventArgs ev)
        {
            if (!TdmStarted) return;

            if (!playerTeams.TryGetValue(ev.Player, out var team))
                return;
            if (ev.Attacker is Player attacker && attacker != ev.Player)
            {
                if (playerTeams.TryGetValue(attacker, out string attackerTeam))
                {
                    if (!TeamScores.ContainsKey("Team1")) TeamScores["Team1"] = 0;
                    if (!TeamScores.ContainsKey("Team2")) TeamScores["Team2"] = 0;

                    if (!TeamScores.ContainsKey(attackerTeam))
                        TeamScores[attackerTeam] = 0;

                    TeamScores[attackerTeam]++;

                    // ── A) 점수 상황에 따른 상태 음성 재생 ───────────────────────
                    // 리드/루징/동점
                    if (TeamScores["Team1"] > TeamScores["Team2"])
                    {
                        AnnouncerEventHandlers.PlayTeamStateAudio("Team1", TeamDeathmatchAPI.API.TeamState.Leading);
                        AnnouncerEventHandlers.PlayTeamStateAudio("Team2", TeamDeathmatchAPI.API.TeamState.Losing);
                    }
                    else if (TeamScores["Team2"] > TeamScores["Team1"])
                    {
                        AnnouncerEventHandlers.PlayTeamStateAudio("Team2", TeamDeathmatchAPI.API.TeamState.Leading);
                        AnnouncerEventHandlers.PlayTeamStateAudio("Team1", TeamDeathmatchAPI.API.TeamState.Losing);
                    }
                    else
                    {
                        AnnouncerEventHandlers.PlayTeamStateAudio("Team1", TeamDeathmatchAPI.API.TeamState.Tied);
                        AnnouncerEventHandlers.PlayTeamStateAudio("Team2", TeamDeathmatchAPI.API.TeamState.Tied);
                    }

                    // 매치포인트(승점 1 남음)
                    int target = Instance.Config.TeamScoreToWin;
                    if (TeamScores["Team1"] == target - 5)
                        AnnouncerEventHandlers.PlayTeamStateAudio("Team1", TeamDeathmatchAPI.API.TeamState.HitHard);
                    if (TeamScores["Team2"] == target - 5)
                        AnnouncerEventHandlers.PlayTeamStateAudio("Team2", TeamDeathmatchAPI.API.TeamState.HitHard);

                    // ── B) 리버설(선두 바뀜) 감지 & 3초 안정화 후 확정 ─────────────
                    string currentLeadingTeam =
                        TeamScores["Team1"] > TeamScores["Team2"] ? "Team1" :
                        TeamScores["Team1"] < TeamScores["Team2"] ? "Team2" : null;

                    if (currentLeadingTeam != null && currentLeadingTeam != lastLeadingTeam)
                    {
                        Log.Debug($"[TDM] Score lead change detected: {lastLeadingTeam} -> {currentLeadingTeam}");

                        // 이전 코루틴 안전 종료
                        if (reversalCheckCoroutine.IsRunning)
                            Timing.KillCoroutines(reversalCheckCoroutine);

                        reversalCheckCoroutine = Timing.CallDelayed(3f, () =>
                        {
                            string leadingNow =
                                TeamScores["Team1"] > TeamScores["Team2"] ? "Team1" :
                                TeamScores["Team1"] < TeamScores["Team2"] ? "Team2" : null;

                            if (leadingNow == currentLeadingTeam)
                            {
                                // 리버설 전용 음성(이기는 팀/지는 팀) + 힌트
                                AnnouncerEventHandlers.RunReversalEvent(leadingNow);
                                lastLeadingTeam = currentLeadingTeam;
                            }
                        });
                    }

                    // ── C) HUD 점수 힌트 ────────────────────────────────────────
                    foreach (var p in Player.List)
                    {
                        p.ShowHint(
                            $"<b><color=blue>MTF: {TeamScores["Team1"]}</color> | <color=green>CI: {TeamScores["Team2"]}</color></b>",
                            3f
                        );
                    }

                    // ── D) 승리 조건 충족 → 승/패 음성 + 종료 ─────────────────────
                    if (TeamScores[attackerTeam] >= target)
                    {
                        // 상태 기반 승/패 음성
                        AnnouncerEventHandlers.PlayTeamStateAudio(attackerTeam,
                            TeamDeathmatchAPI.API.TeamState.Victory);
                        string loser = attackerTeam == "Team1" ? "Team2" : "Team1";
                        AnnouncerEventHandlers.PlayTeamStateAudio(loser, TeamDeathmatchAPI.API.TeamState.Defeat);

                        EndTdm(attackerTeam);
                        return;
                    }
                }
            }

            // ✅ 리스폰(그대로 유지)
            Timing.CallDelayed(5f, () =>
            {
                if (ev.Player == null || !ev.Player.IsConnected) return;

                ev.Player.Role.Set(team == "Team1" ? RoleTypeId.NtfSergeant : RoleTypeId.ChaosRifleman);
                ev.Player.ClearInventory();
                GiveLoadout(ev.Player);
                ev.Player.Position = GetSpawnPointForTeam(team);
            });
        }

        private void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            if (TdmStarted)
                ev.IsAllowed = false;
        }
        private void OnRoundStarted()
        {
            if (TdmStarted)
                return;
            foreach (var lift in Lift.List)
            {
                lift.ChangeLock(DoorLockReason.Warhead);
            }

            foreach (var door in Door.List)
            {
                if (door.Type == DoorType.CheckpointLczA || door.Type == DoorType.CheckpointLczB)
                {
                    door.IsOpen = false;
                    door.ChangeLock(DoorLockType.Lockdown079);
                }            
            }

            if (StartZone == ZoneType.LightContainment)
            {
                Map.IsDecontaminationEnabled = false;
            }
            // 모든 플레이어를 대기열에 추가
            WaitingPlayers.Clear();
            team1.Clear();
            team2.Clear();
            playerTeams.Clear();

            foreach (var p in Player.List)
            {
                if (p.Role.Team is Team.FoundationForces or Team.ChaosInsurgency)
                {
                    WaitingPlayers.Add(p);
                }
                else
                {
                    // SCP / D-Class / Scientist → Spectator로 전환 또는 제거
                    p.Role.Set(RoleTypeId.Spectator);
                    p.Broadcast(5, "TDM 모드에서는 SCP/과학자/디클래스는 참여할 수 없습니다.");
                }
            }

            if (WaitingPlayers.Count >= 2)
            {
                StartTdm();
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
                player.AddItem(ItemType.KeycardO5);
                player.AddItem(ItemType.GunE11SR);
                player.AddItem(ItemType.GunCOM18);
                player.AddItem(ItemType.ArmorHeavy);
                player.AddItem(ItemType.Medkit);
                player.AddItem(ItemType.GrenadeHE);
                player.AddItem(ItemType.GrenadeFlash);
                player.AddAmmo(AmmoType.Nato556,200);
                player.AddAmmo(AmmoType.Nato9, 70);
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
                player.AddItem(ItemType.KeycardO5);
                player.AddItem(ItemType.GunAK);
                player.AddItem(ItemType.GunRevolver);
                player.AddItem(ItemType.ArmorHeavy);
                player.AddItem(ItemType.Medkit);
                player.AddItem(ItemType.GrenadeHE);
                player.AddItem(ItemType.GrenadeFlash);
                player.AddAmmo(AmmoType.Nato762,200);
                player.AddAmmo(AmmoType.Ammo44Cal, 42);
            }
        }

        private void EndTdm(string winningTeam)
        {
            Timing.KillCoroutines(scoreHintCoroutine);
            AnnouncerEventHandlers.ResetTeamStateCache();
            Map.Broadcast(10, $"{winningTeam} 승리! 라운드를 재시작합니다.");
            TdmStarted = false;
            // 이건 그대로 유지
            Timing.CallDelayed(5f, () => Round.Restart());
            ResetTdmState();
        }

        
        /*private Vector3 GetSpawnPointForTeam(string team)
        {
            return team switch
            {
                "Team1" => new Vector3(125, 296, -41),
                "Team2" => new Vector3(6, 292, -42),
                _ => new Vector3(0, 301, 0) // fallback 위치
            };
        }*/
        private Vector3 GetSpawnPointForTeam(string team)
        {
            Room room = null;
            switch (StartZone)
            {
                case ZoneType.LightContainment:
                    if (Instance.Config.LczRespawns.TryGetValue(team, out var lczList))
                    {
                        var randomRoom = lczList[UnityEngine.Random.Range(0, lczList.Count)];
                        room = Room.Get(randomRoom);
                    }

                    break;
                case ZoneType.HeavyContainment:
                    if (Instance.Config.HczRespawns.TryGetValue(team, out var hczList))
                    {
                        var randomRoom = hczList[UnityEngine.Random.Range(0, hczList.Count)];
                        room = Room.Get(randomRoom);
                    }

                    break;
                case ZoneType.Entrance:
                    if (Instance.Config.EzRespawns.TryGetValue(team, out var ezList))
                    {
                        var randomRoom = ezList[UnityEngine.Random.Range(0, ezList.Count)];
                        room = Room.Get(randomRoom);
                    }

                    break;
                case ZoneType.Surface:
                    if (Instance.Config.SurfaceRespawns.TryGetValue(team, out var surfaceList))
                    {
                        return surfaceList[UnityEngine.Random.Range(0, surfaceList.Count)];
                    }

                    break;
            }
            if (room != null)
            {
                return room.Position + Vector3.up;
            }

            Log.Warn($"[TDM] No valid room found for team={team}, zone={StartZone}");
            return new Vector3(0f, 300f, 0f); // fallback
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
            MtfRoles.Remove(new Enforcer());
            MtfRoles.Remove(new HugoBoss());
            ChaosRoles.Remove(new FedoraAgent());
            Log.Info($"[TDM] 커스텀 롤 로딩 완료: MTF {MtfRoles.Count}개, Chaos {ChaosRoles.Count}개.");
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (!Round.IsStarted || TdmStarted)
                return;

            // SCP / Scientist / D-Class 등 허용되지 않은 진영 필터링
            if (ev.Player.Role.Team is Team.SCPs or Team.ClassD or Team.Scientists)
            {
                Log.Debug($"[TDM] {ev.Player.Nickname}은 허용되지 않은 팀이므로 인간 진영으로 강제 이동됩니다.");

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
                WaitingPlayers.Add(ev.Player);
                Timing.CallDelayed(1f, () =>
                {
                    ev.Player.ClearInventory();
                    GiveLoadout(ev.Player);
                    ev.Player.Position = GetSpawnPointForTeam(team);
                    //ev.Player.Broadcast(5, $"TDM: {team} 팀으로 자동 배정되었습니다.");
                });
            }
        }


        private void OnLeft(LeftEventArgs ev)
        {
            // 플레이어 리스트에서 제거
            WaitingPlayers.Remove(ev.Player);
            team1.Remove(ev.Player);
            team2.Remove(ev.Player);
            playerTeams.Remove(ev.Player);
            if (!TdmStarted) return;

            int aliveTeam1 = team1.Count;
            int aliveTeam2 = team2.Count;

            if (aliveTeam1 == 0 || aliveTeam2 == 0)
            {
                TdmStarted = false;
                Round.IsLocked = false;
                Map.Broadcast(10, "⚠️ 플레이어 수 부족으로 라운드를 종료합니다!");
                Timing.CallDelayed(5f, () => Round.Restart());
            }
        }
        private IEnumerator<float> ShowScoreHints()
        {
            while (TdmStarted)
            {
                // 남은 시간 계산
                TimeSpan elapsed = DateTime.Now - matchStartTime;
                TimeSpan remaining = TimeSpan.FromMinutes(10) - elapsed;

                if (remaining.TotalSeconds <= 0)
                {
                    // 시간이 다 됨
                    string winner = TeamScores["Team1"] > TeamScores["Team2"] ? "Team1" :
                        TeamScores["Team2"] > TeamScores["Team1"] ? "Team2" : null;

                    if (winner == null)
                        EndTdm("Draw");
                    else
                        EndTdm(winner);
                    yield break;
                }

                string scoreText = "<size=130%><b>⚔ TEAM SCORE ⚔</b></size>\n" +
                                   $"<color=#4FA9FF><b>MTF: {TeamScores["Team1"]}</b></color>  |  " +
                                   $"<color=#58D68D><b>CI: {TeamScores["Team2"]}</b></color>\n" +
                                   $"<color=yellow>⏰ Time Left: {remaining.Minutes:D2}:{remaining.Seconds:D2}</color>";

                foreach (var player in Player.List.Where(p => p.IsAlive))
                {
                    var display = PlayerDisplay.Get(player);
                    display.RemoveHint("tdm_score");
                    display.AddHint(new Hint
                    {
                        Id = "tdm_score",
                        Text = scoreText,
                        FontSize = 18,
                        YCoordinate = 300,
                        Alignment = HintAlignment.Left
                    });
                }

                yield return Timing.WaitForSeconds(1f);
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
            AnnouncerEventHandlers = new AnnouncerEventHandlers();
            AnnouncerEventHandlers.Plugin = this;
            //AnnouncerEventHandlers.EnsureMusicDirectoryExists();
            LoadTeamRoles();
            AnnouncerEventHandlers.OnPluginLoad();
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundStarted += AnnouncerEventHandlers.OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Died += OnPlayerDied;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Server.RespawningTeam += OnRespawningTeam;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundStarted -= AnnouncerEventHandlers.OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Died -= OnPlayerDied;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Server.RespawningTeam -= OnRespawningTeam;
            Instance = null;
            AnnouncerEventHandlers = null;
            AnnouncerEventHandlers.Plugin = null;
            base.OnDisabled();
        }
    }
}