using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using PlayerRoles;
using Respawning;
using Respawning.Waves;

namespace RespawnTimer.API.Features;

public partial class TimerView
{
    public static readonly Dictionary<string, TimerView> CachedTimers = new();

    internal TimerView()
    {
    }

    public static void AddTimer(string name)
    {
        if (CachedTimers.ContainsKey(name))
            return;

        CachedTimers.Add(name, new TimerView());
    }

    public static bool TryGetTimerForPlayer(Player player, out TimerView timerView)
    {
        var groupName = !ServerStatic.PermissionsHandler.Members.TryGetValue(player.UserId, out var str) ? null : str;

        // Check by group name
        if (groupName is not null && RespawnTimer.Singleton.Config.Timers.TryGetValue(groupName, out var timerName))
        {
            if (CachedTimers.TryGetValue(timerName, out timerView))
                return true;
        }

        // Check by user id
        if (RespawnTimer.Singleton.Config.Timers.TryGetValue(player.UserId, out timerName))
        {
            if (CachedTimers.TryGetValue(timerName, out timerView))
                return true;
        }

        // Use fallback default timer
        if (RespawnTimer.Singleton.Config.Timers.TryGetValue("default", out timerName))
        {
            if (CachedTimers.TryGetValue(timerName, out timerView))
                return true;
        }

        // Default fallback does not exist
        timerView = null!;
        return false;
    }

    public string GetText()
    {
        var translation = RespawnTimer.Singleton.Translation;
        var spectators = Player.List.Count(x => x.Role.Team == Team.Dead && !x.IsOverwatchEnabled);
        var roundTime = Round.ElapsedTime;

        var (ci, ntf, mci, mntf) = GetSpawnCounters();

        var roundLine = translation.RoundTime
            .Replace("{round_minutes}", roundTime.Minutes.ToString("D2"))
            .Replace("{round_seconds}", roundTime.Seconds.ToString("D2"));

        var spawnLine = translation.SpawnCounters
            .Replace("{ci}", ci)
            .Replace("{ntf}", ntf)
            .Replace("{mci}", mci)
            .Replace("{mntf}", mntf);

        var spectatorsLine = translation.SpectatorsCount.Replace("{spectators_num}", spectators.ToString());
        if (translation.SpectatorsXOffset != 0)
            spectatorsLine = spectatorsLine.Replace("<align=center>", $"<align=center><pos={translation.SpectatorsXOffset}>").Replace("</align>", "</pos></align>");

        return $"{roundLine}\n{spawnLine}\n{spectatorsLine}\n{translation.DiscordLink}";
    }

    private static (string ci, string ntf, string mci, string mntf) GetSpawnCounters()
    {
        static string Format(TimeBasedWave wave)
        {
            if (wave == null)
                return "--:--";

            var total = (int)wave.Timer.TimeLeft;
            if (total < 0)
                total = 0;
            return $"{total / 60:D2}:{total % 60:D2}";
        }

        var waves = WaveManager.Waves.OfType<TimeBasedWave>().ToList();
        var ntf = waves.FirstOrDefault(w => w is NtfSpawnWave);
        var ci = waves.FirstOrDefault(w => w is ChaosSpawnWave);
        var mntf = waves.FirstOrDefault(w => w is NtfMiniWave);
        var mci = waves.FirstOrDefault(w => w is ChaosMiniWave);

        return (Format(ci), Format(ntf), Format(mci), Format(mntf));
    }
}