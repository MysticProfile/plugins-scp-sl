using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using System;
using System.Collections.Generic;

namespace Scp1853And207Explode
{
    public sealed class EventHandlers
    {
        private readonly Dictionary<string, float> _lastTrigger = new Dictionary<string, float>();

        public void Register()
        {
            Exiled.Events.Handlers.Player.UsedItem += OnUsedItem;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
        }

        public void Unregister()
        {
            Exiled.Events.Handlers.Player.UsedItem -= OnUsedItem;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;

            _lastTrigger.Clear();
        }

        private void OnWaitingForPlayers()
        {
            _lastTrigger.Clear();
        }

        private void OnLeft(LeftEventArgs ev)
        {
            if (ev?.Player is null)
                return;

            string id = GetId(ev.Player);
            if (!string.IsNullOrEmpty(id))
                _lastTrigger.Remove(id);
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (ev?.Player is null)
                return;

            string id = GetId(ev.Player);
            if (!string.IsNullOrEmpty(id))
                _lastTrigger.Remove(id);
        }

        private void OnUsedItem(UsedItemEventArgs ev)
        {
            if (Plugin.Instance?.Config is null || !Plugin.Instance.Config.IsEnabled)
                return;

            if (ev?.Player is null || ev.Item is null)
                return;

            if (ev.Item.Type != ItemType.SCP207 && ev.Item.Type != ItemType.SCP1853)
                return;

            Player p = ev.Player;

            bool has207 = p.TryGetEffect(EffectType.Scp207, out var e207) && e207 != null && e207.IsEnabled;
            bool has1853 = p.TryGetEffect(EffectType.Scp1853, out var e1853) && e1853 != null && e1853.IsEnabled;

            if (!has207 || !has1853)
                return;

            TryExplode(p);
        }

        private void TryExplode(Player player)
        {
            if (player is null || !player.IsAlive)
                return;

            string id = GetId(player);
            if (string.IsNullOrEmpty(id))
                id = player.Id.ToString();

            float now = UnityEngine.Time.time;
            float cd = Plugin.Instance.Config.CooldownSeconds;
            if (cd < 0f)
                cd = 0f;

            if (_lastTrigger.TryGetValue(id, out float last) && (now - last) < cd)
                return;

            _lastTrigger[id] = now;

            if (Plugin.Instance.Config.SpawnGrenadeExplosion)
            {
                try
                {
                    ExplosiveGrenade grenade = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE);
                    grenade.FuseTime = Plugin.Instance.Config.GrenadeFuseTime;
                    grenade.MaxRadius = Plugin.Instance.Config.GrenadeMaxRadius;
                    grenade.SpawnActive(player.Position, player);
                }
                catch (Exception e)
                {
                    if (Plugin.Instance.Config.Debug)
                        Log.Error($"[Scp1853And207Explode] Failed to spawn grenade: {e}");
                }
            }

            float dmg = Plugin.Instance.Config.ExtraLethalDamage;
            if (dmg <= 0f)
                dmg = 500f;

            player.Hurt(dmg, DamageType.Explosion);

            if (Plugin.Instance.Config.Debug)
                Log.Debug($"[Scp1853And207Explode] {player.Nickname} had Scp207 + Scp1853 -> exploded");
        }

        private static string GetId(Player player)
        {
            return player?.UserId ?? player?.Id.ToString();
        }
    }
}
