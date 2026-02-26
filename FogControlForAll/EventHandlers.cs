using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;

namespace FogControlForAll
{
    public class EventHandlers
    {
        // --- State ---
        private readonly Config _config;

        public EventHandlers(Config config)
        {
            _config = config;
        }

        // --- Registration ---

        public void Register()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
        }

        public void Unregister()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
        }

        // --- Player Events ---

        public void OnSpawned(SpawnedEventArgs ev)
        {
            if (_config == null || !_config.IsEnabled || ev?.Player == null)
                return;

            ApplyFog(ev.Player, "spawned");
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (_config == null || !_config.IsEnabled || ev?.Player == null)
                return;

            if (ev.NewRole != RoleTypeId.Scp0492)
                return;

            ApplyFog(ev.Player, "zombie");
        }

        // --- Logic Helpers ---

        private void ApplyFog(Player player, string context)
        {
            try
            {
                byte intensity = _config.FogIntensity;
                
                // FogControl uses 'Intensity' to encode FogType (+1)
                player.EnableEffect(EffectType.FogControl, intensity, 0f, false);

                if (_config.Debug)
                    Log.Debug($"[FogControlForAll] Applied FogControl intensity={intensity} to {player.Nickname} ({context}).");
            }
            catch (Exception e)
            {
                Log.Error($"[FogControlForAll] Error applying FogControl in {context}: {e}");
            }
        }
    }
}
