using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace RagdollCleaner
{
    public class EventHandlers
    {
        private readonly Config _config;
        private readonly Translation _translation;
        private readonly List<CoroutineHandle> _coroutines = new List<CoroutineHandle>();
        private bool _cleanupMessageShown;

        public EventHandlers(Config config, Translation translation)
        {
            _config = config;
            _translation = translation;
            _cleanupMessageShown = false;
        }

        public void OnSpawnedRagdoll(SpawnedRagdollEventArgs ev)
        {
            if (ev.Player == null) return;

            // Verificar si superamos el umbral de cuerpos totales
            if (Ragdoll.List.Count < _config.BodyThreshold)
            {
                Log.Debug($"Cuerpo de {ev.Player.Nickname} spawneado. Total: {Ragdoll.List.Count}. Umbral ({_config.BodyThreshold}) no alcanzado.");
                _cleanupMessageShown = false;
                return;
            }

            float duration = ev.Player.IsScp ? _config.ScpBodyDuration : _config.HumanBodyDuration;

            if (duration < 0) return;

            // Mostrar el mensaje HUD
            if (!_cleanupMessageShown)
            {
                ShowCleanupMessage(duration);
                _cleanupMessageShown = true;
            }

            Timing.RunCoroutine(CleanRagdoll(ev.Ragdoll, duration));
        }

        public void ResetCleanupMessage()
        {
            _cleanupMessageShown = false;
        }

        private void ShowCleanupMessage(float duration)
        {
            TimeSpan time = TimeSpan.FromSeconds(duration);
            string timeStr = $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
            
            string msg = _translation.CleanupMessage.Replace("{duration}", timeStr);
            string styled = $"<align=right><size=70%><line-height=85%><voffset={_config.HudVOffsetEm}em>{msg}</voffset></line-height></size></align>";

            foreach (Player p in Player.List)
            {
                p.ShowHint(styled, _config.MsgDuration);
            }
        }

        private IEnumerator<float> CleanRagdoll(Ragdoll ragdoll, float delay)
        {
            yield return Timing.WaitForSeconds(delay);

            if (ragdoll != null && ragdoll.GameObject != null)
            {
                Log.Debug($"Eliminando cuerpo de {ragdoll.Nickname} tras {delay} segundos.");
                ragdoll.Destroy();
            }
        }
    }
}
