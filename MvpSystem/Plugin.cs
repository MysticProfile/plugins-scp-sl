using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Exiled.API.Features;

namespace MvpSystem
{
    public class MvpSystem : Plugin<Config, Translation>
    {
        public static MvpSystem Singleton { get; private set; }
        private readonly Harmony _harmony = new Harmony("MedveMarci.MVP");

        public override string Name => "MvpSystem";
        public override string Author => "Mystic";
        public override Version Version => new Version(1, 1, 2);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);

        public override void OnEnabled()
        {
            Singleton = this;
            _harmony.PatchAll();

            RegisterEvents();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            UnregisterEvents();
            _harmony.UnpatchAll("MedveMarci.MVP");
            Singleton = null;

            base.OnDisabled();
        }

        private void RegisterEvents()
        {
            Exiled.Events.Handlers.Player.Verified += EventHandler.OnPlayerVerified;
            Exiled.Events.Handlers.Player.PickingUpItem += EventHandler.OnPickingUpItem;
            Exiled.Events.Handlers.Player.Spawned += EventHandler.OnPlayerSpawned;
            Exiled.Events.Handlers.Player.ChangingRole += EventHandler.OnPlayerChangingRole;
            Exiled.Events.Handlers.Server.WaitingForPlayers += EventHandler.OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += EventHandler.OnRoundStart;
            Exiled.Events.Handlers.Player.Died += EventHandler.OnPlayerDied;
            Exiled.Events.Handlers.Player.Escaping += EventHandler.OnPlayerEscaping;
            Exiled.Events.Handlers.Server.RoundEnded += EventHandler.OnRoundEnded;
            Exiled.Events.Handlers.Server.RestartingRound += EventHandler.OnRoundRestarted;
        }

        private void UnregisterEvents()
        {
            Exiled.Events.Handlers.Player.Verified -= EventHandler.OnPlayerVerified;
            Exiled.Events.Handlers.Player.PickingUpItem -= EventHandler.OnPickingUpItem;
            Exiled.Events.Handlers.Player.Spawned -= EventHandler.OnPlayerSpawned;
            Exiled.Events.Handlers.Player.ChangingRole -= EventHandler.OnPlayerChangingRole;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= EventHandler.OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= EventHandler.OnRoundStart;
            Exiled.Events.Handlers.Player.Died -= EventHandler.OnPlayerDied;
            Exiled.Events.Handlers.Player.Escaping -= EventHandler.OnPlayerEscaping;
            Exiled.Events.Handlers.Server.RoundEnded -= EventHandler.OnRoundEnded;
            Exiled.Events.Handlers.Server.RestartingRound -= EventHandler.OnRoundRestarted;
        }
    }
}
