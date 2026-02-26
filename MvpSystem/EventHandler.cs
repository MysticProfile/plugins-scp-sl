using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using InventorySystem.Items;
using PlayerRoles;
using UnityEngine;

#if EXILED
using ExiledPlayer = Exiled.API.Features.Player;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.API.Features;
using MEC;
#else
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
#endif

namespace MvpSystem;

public static class EventHandler
{
    // --- State ---
    private static readonly Dictionary<int, Stats> PlayerStats = new();
    private static readonly HashSet<ushort> CollectedScpSerialIds = new();
    private static readonly Stopwatch Stopwatch = new();
    private static string PendingLobbyMessage;
    private static CoroutineHandle _lobbyCoroutine;
    private static bool _lobbyHintPrewarmed;
    private static bool _lobbyHintShown;
    
    private const float LobbyHintDurationSeconds = 120f;
    private const float LobbyHintRefreshSeconds = 10f;
    private const float LobbyHintSeconds = 30f;
    private static float LastLobbyHintShownAt = -999f;

    // --- Lifecycle / Core ---

    internal static void OnRoundStart()
    {
        Stopwatch.Restart();
        PlayerStats.Clear();
        CollectedScpSerialIds.Clear();
        _lobbyHintPrewarmed = false;
        _lobbyHintShown = false;
        
        if (_lobbyCoroutine.IsRunning)
            Timing.KillCoroutines(_lobbyCoroutine);

        ClearLobbyHintFromAll();
    }

    internal static void OnRoundRestarted()
    {
    }

    internal static void OnRoundEnded(
#if EXILED
        Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev
#else
        RoundEndedEventArgs ev
#endif
    )
    {
        PendingLobbyMessage = BuildMessage();
    }

    internal static void OnWaitingForPlayers()
    {
        if (MvpSystem.Singleton?.Config is null)
            return;

#if EXILED
        if (MvpSystem.Singleton.Config.Debug)
            Log.Debug($"[MvpSystem] WaitingForPlayers fired.");
#endif

        if (string.IsNullOrEmpty(PendingLobbyMessage))
            PendingLobbyMessage = BuildMessage();

        _lobbyHintPrewarmed = false;

        if (!_lobbyHintShown)
        {
            ShowLobbyHintToAll();
            _lobbyHintShown = true;
        }

        if (!_lobbyCoroutine.IsRunning)
            _lobbyCoroutine = Timing.RunCoroutine(LobbyDisplayCoroutine());
    }

    // --- Player Events ---

#if EXILED
    internal static void OnPlayerVerified(VerifiedEventArgs ev)
    {
        if (ev?.Player is null)
            return;

        Upsert(ev.Player.Id, ev.Player.UserId, ev.Player.Nickname);
    }

    internal static void OnPlayerSpawned(SpawnedEventArgs ev)
    {
        if (ev?.Player is null)
            return;

        ClearLobbyHintFromPlayer(ev.Player);
    }

    internal static void OnPlayerChangingRole(ChangingRoleEventArgs ev)
    {
        if (ev?.Player is null)
            return;

        if (ev.NewRole == RoleTypeId.Spectator)
            ClearLobbyHintFromPlayer(ev.Player);
    }

    internal static void OnPlayerDied(DiedEventArgs ev)
    {
        if (ev?.Player is null || ev.Attacker is null || ev.Player == ev.Attacker)
            return;

        Upsert(ev.Attacker.Id, ev.Attacker.UserId, ev.Attacker.Nickname);
        if (!PlayerStats.TryGetValue(ev.Attacker.Id, out var a))
            return;

        RoleTypeId attackerRole = ev.Attacker.Role.Type;
        RoleTypeId victimRole = ev.TargetOldRole;

        if (!ShouldCountKill(attackerRole, ev.Attacker.Role.Team, victimRole, ev.Player.Role.Team))
            return;

        ProcessKill(a, attackerRole, victimRole);
    }

    internal static void OnPlayerEscaping(EscapingEventArgs ev)
    {
        if (ev?.Player is null || !ev.IsAllowed)
            return;

        Upsert(ev.Player.Id, ev.Player.UserId, ev.Player.Nickname);
        if (!PlayerStats.TryGetValue(ev.Player.Id, out var s))
            return;

        TrackEscape(s, ev.Player.Nickname, ev.Player.Role.Type);
    }

    internal static void OnPickingUpItem(Exiled.Events.EventArgs.Player.PickingUpItemEventArgs ev)
    {
        if (ev?.Player is null || ev.Pickup is null) return;
        
        if (IsScpItem(ev.Pickup.Type))
        {
            if (CollectedScpSerialIds.Add(ev.Pickup.Serial))
            {
                Upsert(ev.Player.Id, ev.Player.UserId, ev.Player.Nickname);
                if (PlayerStats.TryGetValue(ev.Player.Id, out var s))
                {
                    s.ScpItemsHeld++;
                    if (MvpSystem.Singleton.Config.Debug)
                        Log.Debug($"[MvpSystem] {ev.Player.Nickname} collected unique SCP item: {ev.Pickup.Type} (Serial: {ev.Pickup.Serial})");
                }
            }
        }
    }
#else
    internal static void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        if (ev?.Player is null)
            return;

        Upsert(ev.Player.PlayerId, ev.Player.UserId, ev.Player.Nickname);

        if (RoundSummary.RoundInProgress())
            return;

        TryShowLobbyHintToPlayer(ev.Player);
    }

    internal static void OnPlayerDeath(PlayerDeathEventArgs ev)
    {
        if (ev?.Player is null || ev.Attacker is null || ev.Player == ev.Attacker)
            return;

        Upsert(ev.Attacker.PlayerId, ev.Attacker.UserId, ev.Attacker.Nickname);
        if (!PlayerStats.TryGetValue(ev.Attacker.PlayerId, out var a))
            return;

        RoleTypeId attackerRole = ev.Attacker.Role;
        RoleTypeId victimRole = ev.Player.Role;

        if (!ShouldCountKill(attackerRole, ev.Attacker.Team, victimRole, ev.Player.Team))
            return;

        ProcessKill(a, attackerRole, victimRole);
    }

    internal static void OnPlayerEscaping(PlayerEscapingEventArgs ev)
    {
        if (ev?.Player is null || !ev.IsAllowed)
            return;

        Upsert(ev.Player.PlayerId, ev.Player.UserId, ev.Player.Nickname);
        if (!PlayerStats.TryGetValue(ev.Player.PlayerId, out var s))
            return;

        TrackEscape(s, ev.Player.Nickname, ev.Player.Role);
    }

    internal static void OnPlayerSearchingPickup(LabApi.Events.Arguments.PlayerEvents.PlayerSearchingPickupEventArgs ev)
    {
        if (ev?.Player is null || ev.Pickup is null) return;

        var pickup = ev.Pickup.Base;
        if (IsScpItem(pickup.Info.ItemId))
        {
            if (CollectedScpSerialIds.Add(pickup.Info.Serial))
            {
                Upsert(ev.Player.PlayerId, ev.Player.UserId, ev.Player.Nickname);
                if (PlayerStats.TryGetValue(ev.Player.PlayerId, out var s))
                {
                    s.ScpItemsHeld++;
                    if (MvpSystem.Singleton.Config.Debug)
                        LabApi.Features.Console.Logger.Debug($"[MvpSystem] {ev.Player.Nickname} collected unique SCP item: {pickup.Info.ItemId} (Serial: {pickup.Info.Serial})");
                }
            }
        }
    }
#endif

    // --- Logic Helpers ---

    private static void ProcessKill(Stats a, RoleTypeId attackerRole, RoleTypeId victimRole)
    {
        if (IsAliveScpRole(attackerRole))
        {
            a.ScpRole = attackerRole;
            a.KillsAsScp++;
        }
        else
        {
            Increment(a.KillsByRole, attackerRole);
            if (IsAliveScpRole(victimRole))
                a.ScpsKilled++;
        }
    }

    private static void TrackEscape(Stats s, string nickname, RoleTypeId role)
    {
        if (s.FirstEscapeTime >= 0f)
            return;

        s.FirstEscapeTime = (float)Stopwatch.Elapsed.TotalSeconds;
        s.FirstEscapeRole = role;
        
        if (MvpSystem.Singleton.Config.Debug)
        {
#if EXILED
            Log.Debug($"[MvpSystem] Escape tracked: {nickname} as {s.FirstEscapeRole}");
#else
            LabApi.Features.Console.Logger.Debug($"[MvpSystem] Escape tracked: {nickname} as {s.FirstEscapeRole}");
#endif
        }
    }

    private static bool ShouldCountKill(RoleTypeId attackerRole, PlayerRoles.Team attackerTeam, RoleTypeId victimRole, PlayerRoles.Team victimTeam)
    {
        if (attackerRole is RoleTypeId.None or RoleTypeId.Spectator or RoleTypeId.Tutorial)
            return false;

        if (victimRole is RoleTypeId.None or RoleTypeId.Spectator or RoleTypeId.Tutorial)
            return false;

        if (MvpSystem.Singleton?.Config != null && !MvpSystem.Singleton.Config.CountTeamKills)
            return attackerTeam != victimTeam;

        return true;
    }

    private static bool IsScpRole(RoleTypeId role) =>
        role is RoleTypeId.Scp049 or RoleTypeId.Scp079 or RoleTypeId.Scp096 or RoleTypeId.Scp106 or RoleTypeId.Scp173 or RoleTypeId.Scp939;

    private static bool IsAliveScpRole(RoleTypeId role) => IsScpRole(role) && role != RoleTypeId.Scp0492;

    private static bool IsScpItem(ItemType type)
    {
        return type == ItemType.SCP018 || type == ItemType.SCP207 || type == ItemType.SCP2176 || 
               type == ItemType.SCP244a || type == ItemType.SCP244b || type == ItemType.SCP268 || 
               type == ItemType.SCP330 || type == ItemType.SCP500 || type == ItemType.SCP1576 || 
               type == ItemType.SCP1853 || type == ItemType.AntiSCP207;
    }

    // --- Message Building ---

    private static string BuildMessage()
    {
        if (MvpSystem.Singleton?.Config is null || !MvpSystem.Singleton.Config.IsEnabled)
            return null;

        UpdateFinalRolesSnapshot();

        var lines = new List<string>();

#if EXILED
        var tr = MvpSystem.Singleton.Translation;
        lines.Add($"<size=32>{tr?.Header ?? "Resultados de la ronda:"}</size>\n");
#else
        lines.Add("<size=32>Resultados de la ronda:</size>\n");
#endif

        var topKillsByRole = TopKillsByRole();
        if (topKillsByRole.winner != null)
#if EXILED
            lines.Add($"<size=22>{(tr?.MostKillsAsRole ?? "Más kills como {role}: {name} - {kills} kills")
                .Replace("{role}", GetRoleColoredName(topKillsByRole.role))
                .Replace("{name}", ColorizeName(topKillsByRole.winner))
                .Replace("{kills}", topKillsByRole.kills.ToString())}</size>");
#else
            lines.Add($"<size=22>{"Más kills como {role}: {name} - {kills} kills"
                .Replace("{role}", GetRoleColoredName(topKillsByRole.role))
                .Replace("{name}", ColorizeName(topKillsByRole.winner))
                .Replace("{kills}", topKillsByRole.kills.ToString())}</size>");
#endif
        else
#if EXILED
            lines.Add($"<size=22>{tr?.NoKillsAsRole ?? "No hubo kills en esta ronda"}</size>");
#else
            lines.Add("<size=22>No hubo kills en esta ronda</size>");
#endif

        var topScpKills = TopInt(s => s.ScpsKilled);
        if (topScpKills != null)
#if EXILED
            lines.Add($"<size=22>{(tr?.MostScpsKilled ?? "Más <color=red>SCPs</color> eliminados: {name} - {count} <color=red>SCPs</color>")
                .Replace("{name}", ColorizeName(topScpKills))
                .Replace("{count}", topScpKills.ScpsKilled.ToString())}</size>");
#else
            lines.Add($"<size=22>{"Más <color=red>SCPs</color> eliminados: {name} - {count} <color=red>SCPs</color>"
                .Replace("{name}", ColorizeName(topScpKills))
                .Replace("{count}", topScpKills.ScpsKilled.ToString())}</size>");
#endif
        else
#if EXILED
            lines.Add($"<size=22>{tr?.NoScpsKilled ?? "No se eliminaron <color=red>SCPs</color>"}</size>");
#else
            lines.Add("<size=22>No se eliminaron <color=red>SCPs</color></size>");
#endif

        var topScpKiller = TopInt(s => s.KillsAsScp);
        if (topScpKiller != null)
#if EXILED
            lines.Add($"<size=22>{(tr?.ScpWithMostKills ?? "<color=red>SCP</color> con más kills: {scp} ({name}) - {kills} kills")
                .Replace("{scp}", GetRoleColoredName(topScpKiller.ScpRole))
                .Replace("{name}", ColorizeName(topScpKiller))
                .Replace("{kills}", topScpKiller.KillsAsScp.ToString())}</size>");
#else
            lines.Add($"<size=22>{"<color=red>SCP</color> con más kills: {scp} ({name}) - {kills} kills"
                .Replace("{scp}", GetRoleColoredName(topScpKiller.ScpRole))
                .Replace("{name}", ColorizeName(topScpKiller))
                .Replace("{kills}", topScpKiller.KillsAsScp.ToString())}</size>");
#endif
        else
#if EXILED
            lines.Add($"<size=22>{tr?.NoScpWithMostKills ?? "Ningún <color=red>SCP</color> registró kills"}</size>");
#else
            lines.Add("<size=22>Ningún <color=red>SCP</color> registró kills</size>");
#endif

        var topScpItems = TopInt(s => s.ScpItemsHeld);
        if (topScpItems != null)
#if EXILED
            lines.Add($"<size=22>{(tr?.MostScpItems ?? "Más objetos <color=red>SCP</color>: {name} - {count} objetos")
                .Replace("{name}", ColorizeName(topScpItems))
                .Replace("{count}", topScpItems.ScpItemsHeld.ToString())}</size>");
#else
            lines.Add($"<size=22>{"Más objetos <color=red>SCP</color>: {name} - {count} objetos"
                .Replace("{name}", ColorizeName(topScpItems))
                .Replace("{count}", topScpItems.ScpItemsHeld.ToString())}</size>");
#endif
        else
#if EXILED
            lines.Add($"<size=22>{tr?.NoScpItems ?? "No se recogieron objetos <color=red>SCP</color>"}</size>");
#else
            lines.Add("<size=22>No se recogieron objetos <color=red>SCP</color></size>");
#endif

        var firstEscape = FirstEscape();
        if (firstEscape != null)
#if EXILED
            lines.Add($"<size=22>{(tr?.FirstEscape ?? "Primero en escapar como {role}: {name}")
                .Replace("{role}", GetRoleColoredName(firstEscape.FirstEscapeRole))
                .Replace("{name}", ColorizeName(firstEscape))}</size>");
#else
            lines.Add($"<size=22>{"Primero en escapar como {role}: {name}"
                .Replace("{role}", GetRoleColoredName(firstEscape.FirstEscapeRole))
                .Replace("{name}", ColorizeName(firstEscape))}</size>");
#endif
        else
#if EXILED
            lines.Add($"<size=22>{tr?.NoEscape ?? "Nadie escapó de la Instalacion"}</size>");
#else
            lines.Add("<size=22>Nadie escapó de la Instalacion</size>");
#endif

        return string.Join("\n", lines);
    }

    // --- UI / Hints ---

    private static IEnumerator<float> LobbyDisplayCoroutine()
    {
        while (true)
        {
#if EXILED
            if (!Round.IsLobby) break;
#else
            if (RoundSummary.RoundInProgress()) break;
#endif
            if (_lobbyHintShown)
                ShowLobbyHintToAll();

            yield return Timing.WaitForSeconds(LobbyHintRefreshSeconds);
        }
    }

    private static void ShowLobbyHintToAll()
    {
        if (string.IsNullOrEmpty(PendingLobbyMessage))
            return;

#if EXILED
        if (!_lobbyHintPrewarmed)
        {
            string warm = $"<color=#00000000><align=left><voffset=0><indent=-100>{PendingLobbyMessage}</indent></voffset></align></color>";
            foreach (var p in ExiledPlayer.List)
                p.ShowHint(warm, 0.1f);
            _lobbyHintPrewarmed = true;
        }

        string msg = $"<align=left><voffset=0><indent=-100>{PendingLobbyMessage}</indent></voffset></align>";
        foreach (var p in ExiledPlayer.List)
            p.ShowHint(msg, LobbyHintDurationSeconds);
#else
        if (!_lobbyHintPrewarmed)
        {
            string warm = $"<color=#00000000><align=left><voffset=0><indent=-100>{PendingLobbyMessage}</indent></voffset></align></color>";
            foreach (var p in Player.ReadyList)
                p.SendHint(warm, 0.1f);
            _lobbyHintPrewarmed = true;
        }

        string msg = $"<align=left><voffset=0><indent=-100>{PendingLobbyMessage}</indent></voffset></align>";
        foreach (var p in Player.ReadyList)
            p.SendHint(msg, LobbyHintDurationSeconds);
#endif
    }

#if EXILED
    private static void TryShowLobbyHintToPlayer(ExiledPlayer player)
    {
        if (player == null)
            return;

        if (string.IsNullOrEmpty(PendingLobbyMessage))
            PendingLobbyMessage = BuildMessage();

        if (string.IsNullOrEmpty(PendingLobbyMessage))
            return;

        player.ShowHint($"<align=left><voffset=0><indent=-100>{PendingLobbyMessage}</indent></voffset></align>", LobbyHintDurationSeconds);
    }
#else
    private static void TryShowLobbyHintToPlayer(Player player)
    {
        if (player == null)
            return;

        if (string.IsNullOrEmpty(PendingLobbyMessage))
            PendingLobbyMessage = BuildMessage();

        if (string.IsNullOrEmpty(PendingLobbyMessage))
            return;

        player.SendHint($"<align=left><voffset=0><indent=-100>{PendingLobbyMessage}</indent></voffset></align>", LobbyHintDurationSeconds);
    }
#endif

    private static void ClearLobbyHintFromAll()
    {
#if EXILED
        foreach (var p in ExiledPlayer.List)
        {
            if (p is null || p.IsHost)
                continue;

            p.ShowHint(string.Empty, 0.1f);
        }
#else
        foreach (var p in Player.ReadyList)
            p.SendHint(string.Empty, 0.1f);
#endif
    }

    private static void ClearLobbyHintFromPlayer(
#if EXILED
        ExiledPlayer p
#else
        Player p
#endif
    )
    {
        if (p is null)
            return;

#if EXILED
        if (p.IsHost)
            return;

        p.ShowHint(string.Empty, 0.1f);
#else
        p.SendHint(string.Empty, 0.1f);
#endif
    }

    private static string ColorizeName(Stats stats)
    {
        if (stats == null) return "Unknown";

        RoleTypeId role = stats.LastRole;
        PlayerRoles.Team team = stats.LastTeam;

        string color = role switch
        {
            RoleTypeId.ClassD => "#ff9900",
            RoleTypeId.Scientist => "#ffd200",
            _ => team switch
            {
                PlayerRoles.Team.ChaosInsurgency => "#00ff00",
                PlayerRoles.Team.FoundationForces => "#4aa3ff",
                PlayerRoles.Team.SCPs => "#ff0000",
                _ => "#ffffff"
            }
        };

        return $"<color={color}>{EscapeRichText(stats.Name)}</color>";
    }

    private static string EscapeRichText(string s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        return s.Replace("<", "＜").Replace(">", "＞");
    }

    private static string GetRoleColoredName(RoleTypeId role)
    {
        string label = role switch
        {
            RoleTypeId.Scp173 => "SCP-173",
            RoleTypeId.Scp106 => "SCP-106",
            RoleTypeId.Scp049 => "SCP-049",
            RoleTypeId.Scp079 => "SCP-079",
            RoleTypeId.Scp096 => "SCP-096",
            RoleTypeId.Scp939 => "SCP-939",
            RoleTypeId.Scp0492 => "SCP-049-2",
            RoleTypeId.ClassD => "Clase D",
            RoleTypeId.Scientist => "Cientifico",
            RoleTypeId.FacilityGuard => "Guardia",
            RoleTypeId.NtfPrivate or RoleTypeId.NtfSergeant or RoleTypeId.NtfSpecialist or RoleTypeId.NtfCaptain => "NTF",
            RoleTypeId.ChaosConscript or RoleTypeId.ChaosRifleman or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor => "Caos",
            _ => role.ToString()
        };

        string color = role switch
        {
            RoleTypeId.Scp173 or RoleTypeId.Scp106 or RoleTypeId.Scp049 or RoleTypeId.Scp079 or RoleTypeId.Scp096 or RoleTypeId.Scp939 or RoleTypeId.Scp0492 => "#ff0000",
            RoleTypeId.ClassD => "#ff9900",
            RoleTypeId.Scientist => "#ffd200",
            RoleTypeId.FacilityGuard => "#4aa3ff",
            RoleTypeId.NtfPrivate or RoleTypeId.NtfSergeant or RoleTypeId.NtfCaptain or RoleTypeId.NtfSpecialist => "#4aa3ff",
            RoleTypeId.ChaosConscript or RoleTypeId.ChaosRifleman or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor => "#00ff00",
            _ => "#ffffff"
        };

        return $"<color={color}>{label}</color>";
    }

    // --- Stats Management ---

    private static void UpdateFinalRolesSnapshot()
    {
#if EXILED
        foreach (var s in PlayerStats.Values)
        {
            var p = ExiledPlayer.List.FirstOrDefault(x => x.UserId == s.UserId);
            if (p != null)
            {
                s.LastRole = p.Role.Type;
                s.LastTeam = p.Role.Team;
            }
        }
#else
        foreach (var s in PlayerStats.Values)
        {
            var p = Player.ReadyList.FirstOrDefault(x => x.UserId == s.UserId);
            if (p != null)
            {
                s.LastRole = p.Role;
                s.LastTeam = p.Team;
            }
        }
#endif
    }

    private static Stats TopInt(Func<Stats, int> selector)
    {
        int best = 0;
        Stats winner = null;

        foreach (var s in PlayerStats.Values)
        {
            int v = selector(s);
            if (v <= 0)
                continue;

            if (winner is null || v > best || (v == best && string.CompareOrdinal(s.UserId, winner.UserId) < 0))
            {
                best = v;
                winner = s;
            }
        }

        return winner;
    }

    private static Stats FirstEscape()
    {
        float best = float.MaxValue;
        Stats winner = null;

        foreach (var s in PlayerStats.Values)
        {
            if (s.FirstEscapeTime < 0f)
                continue;

            if (winner is null || s.FirstEscapeTime < best || (Mathf.Abs(s.FirstEscapeTime - best) < 0.01f && string.CompareOrdinal(s.UserId, winner.UserId) < 0))
            {
                best = s.FirstEscapeTime;
                winner = s;
            }
        }

        return winner;
    }

    private static (Stats winner, RoleTypeId role, int kills) TopKillsByRole()
    {
        int best = 0;
        Stats winner = null;
        RoleTypeId bestRole = RoleTypeId.None;

        foreach (var s in PlayerStats.Values)
        {
            foreach (var kv in s.KillsByRole)
            {
                if (kv.Key is RoleTypeId.Spectator or RoleTypeId.None)
                    continue;

                int v = kv.Value;
                if (v <= 0)
                    continue;

                if (winner is null || v > best || (v == best && string.CompareOrdinal(s.UserId, winner.UserId) < 0))
                {
                    best = v;
                    winner = s;
                    bestRole = kv.Key;
                }
            }
        }

        return (winner, bestRole, best);
    }

    private static void Increment(Dictionary<RoleTypeId, int> map, RoleTypeId key)
    {
        if (!map.TryGetValue(key, out int v))
            v = 0;
        map[key] = v + 1;
    }

    private static void Upsert(int id, string userId, string nickname)
    {
        if (!PlayerStats.TryGetValue(id, out var stats))
        {
            PlayerStats[id] = new Stats(userId ?? $"dummy-{id}", nickname ?? "Dummy");
        }
        else
        {
            if (userId != null) stats.UserId = userId;
            if (nickname != null) stats.Name = nickname;
        }
    }

    private sealed class Stats
    {
        public Stats(string userId, string name)
        {
            UserId = userId;
            Name = name;
        }

        public string UserId;
        public string Name;

        public Dictionary<RoleTypeId, int> KillsByRole { get; } = new();
        public int ScpsKilled;

        public int KillsAsScp;
        public RoleTypeId ScpRole = RoleTypeId.None;

        public int ScpItemsHeld;

        public float FirstEscapeTime = -1f;
        public RoleTypeId FirstEscapeRole = RoleTypeId.None;
        public RoleTypeId LastRole = RoleTypeId.None;
        public PlayerRoles.Team LastTeam = PlayerRoles.Team.OtherAlive;
    }
}
