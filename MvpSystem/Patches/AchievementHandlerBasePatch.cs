using System;
using Achievements;
using HarmonyLib;
using Mirror;

namespace MvpSystem.Patches;

#if !EXILED
using LabApi.Features.Console;
using LabApi.Features.Wrappers;

[HarmonyPatch(typeof(AchievementHandlerBase), nameof(AchievementHandlerBase.ServerAchieve))]
public class AchievementHandlerBasePatch
{
    public static bool Prefix(NetworkConnection conn, AchievementName targetAchievement)
    {
        conn.Send(new AchievementManager.AchievementMessage
        {
            AchievementId = (byte)targetAchievement
        });

        try
        {
            EventHandler.OnPlayerAchieve(Player.Get(conn.identity), targetAchievement);
        }
        catch (Exception ex)
        {
            Logger.Error("MVP.Patches.AchievementHandlerBasePatch Error: " + ex);
        }

        return false;
    }
}
#endif