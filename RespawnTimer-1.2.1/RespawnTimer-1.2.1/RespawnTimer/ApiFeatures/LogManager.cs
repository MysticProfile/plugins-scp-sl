using System;
using System.Collections.Generic;
using Exiled.API.Features;
using NorthwoodLib.Pools;
using RespawnTimer.API.Features;

namespace RespawnTimer.ApiFeatures;

internal static class LogManager
{
    private static readonly List<LogEntry> History = [];
    private static bool DebugEnabled => RespawnTimer.Singleton.Config?.Debug ?? false;

    public static void Debug(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Debug", message));
        if (!DebugEnabled)
            return;

        Log.Debug($"[{RespawnTimer.Singleton.Name}] {message}");
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Info", message));
        Log.Info($"[{RespawnTimer.Singleton.Name}] {message}");
    }

    public static void Warn(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Warn", message));
        Log.Warn($"[{RespawnTimer.Singleton.Name}] {message}");
    }

    public static void Error(string message, ConsoleColor color = ConsoleColor.Red)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Error", message));
        Log.Error($"[{RespawnTimer.Singleton.Name}] {message}");
    }

    public static (string logResult, bool success) GetLogHistory()
    {
        var stringBuilder = StringBuilderPool.Shared.Rent();
        foreach (var log in History)
            stringBuilder.AppendLine(
                $"[{DateTimeOffset.FromUnixTimeMilliseconds(log.Timestamp):yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

        return (StringBuilderPool.Shared.ToStringReturn(stringBuilder), true);
    }

    private class LogEntry(long timestamp, string level, string message)
    {
        public long Timestamp { get; } = timestamp;
        public string Level { get; } = level;
        public string Message { get; } = message;
    }
}