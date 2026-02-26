#if EXILED
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#endif
using AntiSpawnKill.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AntiSpawnKill
{
    internal sealed class EventsHandler
    {
        private static Config Config => Plugin.Instance.Config;

        private readonly Dictionary<uint, float> immunityEndByNetId = new();

#if !EXILED
        private Delegate spawnedDelegate;
        private Delegate hurtingDelegate;
        private Delegate leftDelegate;
#endif

#if EXILED
        public void OnSpawned(SpawnedEventArgs ev)
        {
            if (!Config.IsEnabled)
                return;

            if (ev?.Player is null)
                return;

            SetImmunity(ev.Player);
        }

        public void OnHurting(HurtingEventArgs ev)
        {
            if (!Config.IsEnabled)
                return;

            if (ev?.Player is null || ev.DamageHandler is null)
                return;

            if (!IsImmune(ev.Player.NetId))
                return;

            if (Config.AllowScpDamageDuringImmunity && ev.Attacker is not null && ev.Attacker.IsScp)
                return;

            ev.IsAllowed = false;
        }

        public void OnLeft(LeftEventArgs ev)
        {
            if (ev?.Player is null)
                return;

            immunityEndByNetId.Remove(ev.Player.NetId);
        }
#else
        public void RegisterLabApiEvents()
        {
            if (!Config.IsEnabled)
                return;

            TryHookPlayerEvent("Spawning", nameof(OnLabApiSpawned), ref spawnedDelegate);
            TryHookPlayerEvent("Spawned", nameof(OnLabApiSpawned), ref spawnedDelegate);
            TryHookPlayerEvent("Joined", nameof(OnLabApiSpawned), ref spawnedDelegate);
            TryHookPlayerEvent("ChangingRole", nameof(OnLabApiSpawned), ref spawnedDelegate);
            TryHookPlayerEvent("ChangedRole", nameof(OnLabApiSpawned), ref spawnedDelegate);
            TryHookPlayerEvent("Hurting", nameof(OnLabApiHurting), ref hurtingDelegate);
            TryHookPlayerEvent("Hurt", nameof(OnLabApiHurting), ref hurtingDelegate);
            TryHookPlayerEvent("Left", nameof(OnLabApiLeft), ref leftDelegate);
        }

        public void UnregisterLabApiEvents()
        {
            TryUnhookPlayerEvent("Spawning", spawnedDelegate);
            TryUnhookPlayerEvent("Spawned", spawnedDelegate);
            TryUnhookPlayerEvent("Joined", spawnedDelegate);
            TryUnhookPlayerEvent("ChangingRole", spawnedDelegate);
            TryUnhookPlayerEvent("ChangedRole", spawnedDelegate);
            TryUnhookPlayerEvent("Hurting", hurtingDelegate);
            TryUnhookPlayerEvent("Hurt", hurtingDelegate);
            TryUnhookPlayerEvent("Left", leftDelegate);

            spawnedDelegate = null;
            hurtingDelegate = null;
            leftDelegate = null;
        }

        private void OnLabApiSpawned(object ev)
        {
            try
            {
                object player = GetPropertyValue(ev, "Player");
                if (player is null)
                    return;

                SetImmunity(player);
            }
            catch (Exception)
            {
            }
        }

        private void OnLabApiHurting(object ev)
        {
            try
            {
                object player = GetPropertyValue(ev, "Player");
                if (player is null)
                    return;

                uint netId = GetNetId(player);
                if (!IsImmune(netId))
                    return;

                if (Config.AllowScpDamageDuringImmunity)
                {
                    object attacker = GetPropertyValue(ev, "Attacker");
                    if (attacker is not null && IsScp(attacker))
                        return;
                }

                object damageHandler = GetPropertyValue(ev, "DamageHandler") ?? GetPropertyValue(ev, "Handler") ?? GetPropertyValue(ev, "DamageHandlerBase");
                if (damageHandler is null)
                    return;

                SetPropertyValue(ev, "IsAllowed", false);
                SetPropertyValue(ev, "IsPermitted", false);
                SetPropertyValue(ev, "Allowed", false);
            }
            catch (Exception)
            {
            }
        }

        private void OnLabApiLeft(object ev)
        {
            try
            {
                object player = GetPropertyValue(ev, "Player");
                if (player is null)
                    return;

                uint netId = GetNetId(player);
                if (netId == 0)
                    return;

                immunityEndByNetId.Remove(netId);
            }
            catch (Exception)
            {
            }
        }

        private void TryHookPlayerEvent(string eventName, string handlerMethodName, ref Delegate backingDelegate)
        {
            if (backingDelegate is not null)
                return;

            Type playerEventsType = Type.GetType("LabApi.Events.Handlers.PlayerEvents, LabApi", throwOnError: false);
            if (playerEventsType is null)
                return;

            EventInfo evInfo = playerEventsType.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
            if (evInfo is null)
                return;

            MethodInfo method = GetType().GetMethod(handlerMethodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method is null)
                return;

            backingDelegate = Delegate.CreateDelegate(evInfo.EventHandlerType, this, method);
            evInfo.AddEventHandler(null, backingDelegate);
        }

        private void TryUnhookPlayerEvent(string eventName, Delegate del)
        {
            if (del is null)
                return;

            Type playerEventsType = Type.GetType("LabApi.Events.Handlers.PlayerEvents, LabApi", throwOnError: false);
            if (playerEventsType is null)
                return;

            EventInfo evInfo = playerEventsType.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
            if (evInfo is null)
                return;

            evInfo.RemoveEventHandler(null, del);
        }
#endif

        private void SetImmunity(object player)
        {
            uint netId = GetNetId(player);
            if (netId == 0)
                return;

            float duration = Mathf.Max(0f, Config.ImmunitySeconds);
            immunityEndByNetId[netId] = Time.time + duration;
        }

#if EXILED
        private void SetImmunity(Player player) => SetImmunity((object)player);
#endif

        private uint GetNetId(object player)
        {
            try
            {
#if EXILED
                if (player is Player exiledPlayer)
                    return exiledPlayer.NetId;
#endif
                object val = GetPropertyValue(player, "NetId") ?? GetPropertyValue(player, "NetworkId") ?? GetPropertyValue(player, "Id");
                if (val is uint u)
                    return u;
                if (val is int i && i >= 0)
                    return (uint)i;

                object rh = GetPropertyValue(player, "ReferenceHub");
                object rhNetId = GetPropertyValue(rh, "netId") ?? GetPropertyValue(rh, "NetId");
                if (rhNetId is uint u2)
                    return u2;
                if (rhNetId is int i2 && i2 >= 0)
                    return (uint)i2;
            }
            catch (Exception)
            {
            }

            return 0;
        }

        private bool IsImmune(uint netId)
        {
            if (netId == 0)
                return false;

            if (!immunityEndByNetId.TryGetValue(netId, out float end))
                return false;

            if (Time.time <= end)
                return true;

            immunityEndByNetId.Remove(netId);
            return false;
        }

#if !EXILED
        private bool IsScp(object player)
        {
            try
            {
                object isScp = GetPropertyValue(player, "IsScp");
                if (isScp is bool b)
                    return b;

                object roleObj = GetPropertyValue(player, "Role") ?? GetPropertyValue(player, "RoleType") ?? GetPropertyValue(player, "RoleId");
                if (roleObj is not null)
                {
                    string roleStr = roleObj.ToString();
                    if (!string.IsNullOrEmpty(roleStr) && roleStr.StartsWith("Scp", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }
#endif

        private static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj is null)
                return null;

            PropertyInfo prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }

        private static void SetPropertyValue(object obj, string propertyName, object value)
        {
            if (obj is null)
                return;

            PropertyInfo prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite)
                return;

            prop.SetValue(obj, value);
        }
    }
}
