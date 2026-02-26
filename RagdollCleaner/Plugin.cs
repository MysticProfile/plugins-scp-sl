using System;
using Exiled.API.Features;
using ServerEvents = Exiled.Events.Handlers.Server;
using PlayerEvents = Exiled.Events.Handlers.Player;

namespace RagdollCleaner
{
    public class Plugin : Plugin<Config, Translation>
    {
        public override string Name => "RagdollCleaner";
        public override string Author => "Mystic";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);

        public static Plugin Instance;
        private EventHandlers _eventHandlers;

        public override void OnEnabled()
        {
            Instance = this;
            _eventHandlers = new EventHandlers(Config, Translation);

            PlayerEvents.SpawnedRagdoll += _eventHandlers.OnSpawnedRagdoll;
            ServerEvents.RoundStarted += _eventHandlers.ResetCleanupMessage;
            ServerEvents.RestartingRound += _eventHandlers.ResetCleanupMessage;

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            PlayerEvents.SpawnedRagdoll -= _eventHandlers.OnSpawnedRagdoll;
            ServerEvents.RoundStarted -= _eventHandlers.ResetCleanupMessage;
            ServerEvents.RestartingRound -= _eventHandlers.ResetCleanupMessage;

            _eventHandlers = null;
            Instance = null;

            base.OnDisabled();
        }
    }
}
