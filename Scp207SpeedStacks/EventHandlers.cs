using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using System.Collections.Generic;

namespace Scp207SpeedStacks
{
    public sealed class EventHandlers
    {
        private readonly Dictionary<string, int> _colasTaken = new Dictionary<string, int>();
        
        private readonly Dictionary<string, float> _lastScp207Duration = new Dictionary<string, float>();

        public void Register()
        {
            Exiled.Events.Handlers.Player.UsedItem += OnUsedItem;
            Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
        }

        public void Unregister()
        {
            Exiled.Events.Handlers.Player.UsedItem -= OnUsedItem;
            Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;

            _colasTaken.Clear();
            _lastScp207Duration.Clear();
        }

        private void OnWaitingForPlayers()
        {
            _colasTaken.Clear();

            _lastScp207Duration.Clear();
        }

        private void OnVerified(VerifiedEventArgs ev)
        {
            if (ev?.Player is null)
                return;

            string id = GetId(ev.Player);
            if (!_colasTaken.ContainsKey(id))
                _colasTaken[id] = 0;

            if (!_lastScp207Duration.ContainsKey(id))
                _lastScp207Duration[id] = 0f;
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (ev?.Player is null)
                return;

            string id = GetId(ev.Player);
            if (_colasTaken.ContainsKey(id))
                _colasTaken[id] = 0;

            if (_lastScp207Duration.ContainsKey(id))
                _lastScp207Duration[id] = 0f;

            if (ev.Player.TryGetEffect(EffectType.MovementBoost, out var effect) && effect != null && effect.IsEnabled)
                ev.Player.DisableEffect(EffectType.MovementBoost);
        }

        private void OnLeft(LeftEventArgs ev)
        {
            if (ev?.Player is null)
                return;

            string id = GetId(ev.Player);
            if (_colasTaken.ContainsKey(id))
                _colasTaken.Remove(id);

            if (_lastScp207Duration.ContainsKey(id))
                _lastScp207Duration.Remove(id);
        }

        private void OnUsedItem(UsedItemEventArgs ev)
        {
            if (Plugin.Instance?.Config is null || !Plugin.Instance.Config.IsEnabled)
                return;

            if (ev?.Player is null || ev.Item is null)
                return;

            if (ev.Item.Type != ItemType.SCP207)
                return;

            string id = GetId(ev.Player);

            if (!_colasTaken.ContainsKey(id))
                _colasTaken[id] = 0;

            if (!_lastScp207Duration.ContainsKey(id))
                _lastScp207Duration[id] = 0f;


            if (ev.Player.TryGetEffect(EffectType.Scp207, out var scp207) && scp207 != null && scp207.IsEnabled)
            {
                float duration = scp207.Duration;
                if (duration > _lastScp207Duration[id])
                    _lastScp207Duration[id] = duration;
            }

            _colasTaken[id]++;

            ApplySpeedStacks(ev.Player);
        }

        private void OnChangedItem(ChangedItemEventArgs ev)
        {
            if (Plugin.Instance?.Config is null || !Plugin.Instance.Config.IsEnabled)
                return;

            if (ev?.Player is null)
                return;

            if (ev.Item is null)
                return;

            if (ev.Item.Type != ItemType.SCP207)
                return;

            int count = GetColas(ev.Player);

            if (count <= 0)
                return;

            string msg = (Plugin.Instance?.Translation?.ColaCountHud ?? "<color=red>Colas: {count}</color>")
                .Replace("{count}", count.ToString());

            ev.Player.ShowHint(msg, 3f);
        }

        private void ApplySpeedStacks(Player player)
        {
            if (player is null)
                return;

            int count = GetColas(player);

            int extra = count - 1;
            if (extra <= 0)
            {
                if (player.TryGetEffect(EffectType.MovementBoost, out var effect) && effect != null && effect.IsEnabled)
                    player.DisableEffect(EffectType.MovementBoost);

                return;
            }

            float per = Plugin.Instance.Config.SpeedPerExtraCola;
            if (per <= 0f)
                return;

            float bonus = extra * per;

            int intensity = (int)System.Math.Round(bonus * 100f);
            if (intensity < 1)
                intensity = 1;
            if (intensity > 255)
                intensity = 255;

            player.EnableEffect(EffectType.MovementBoost, (byte)intensity, 999999f, false);
        }

        private int GetColas(Player player)
        {
            if (player is null)
                return 0;

            string id = GetId(player);
            return _colasTaken.TryGetValue(id, out int v) ? v : 0;
        }

        private static string GetId(Player player)
        {
            return player?.UserId ?? player?.Id.ToString();
        }
    }
}
