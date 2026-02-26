using Exiled.API.Features;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using UserSettings.ServerSpecific;

namespace RespawnTimer
{
    public class RespawnTimer : Plugin<Configs.Config, Configs.Translation>
    {
        public static RespawnTimer Singleton { get; private set; }
        public static string RespawnTimerDirectoryPath { get; private set; }

        public override string Name => "RespawnTimer";
        public override string Author => "Mystic";
        public override Version Version => new Version(1, 2, 1);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);

        private EventHandler _eventHandler;

        public override void OnEnabled()
        {
            Singleton = this;
            _eventHandler = new EventHandler();

            RespawnTimerDirectoryPath = Path.Combine(Paths.Configs, "RespawnTimer");
            if (!Directory.Exists(RespawnTimerDirectoryPath))
            {
                Directory.CreateDirectory(RespawnTimerDirectoryPath);
            }

            RegisterEvents();
            SetupSettings();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            UnregisterEvents();
            _eventHandler = null;
            Singleton = null;

            base.OnDisabled();
        }

        private void RegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += _eventHandler.OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += _eventHandler.OnRoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole += EventHandler.OnRoleChanging;
            Exiled.Events.Handlers.Player.Verified += EventHandler.OnVerified;
            Exiled.Events.Handlers.Player.Left += EventHandler.OnLeft;
            Exiled.Events.Handlers.Server.RespawningTeam += EventHandler.OnRespawningTeam;
        }

        private void UnregisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= _eventHandler.OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= _eventHandler.OnRoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole -= EventHandler.OnRoleChanging;
            Exiled.Events.Handlers.Player.Verified -= EventHandler.OnVerified;
            Exiled.Events.Handlers.Player.Left -= EventHandler.OnLeft;
            Exiled.Events.Handlers.Server.RespawningTeam -= EventHandler.OnRespawningTeam;
        }

        private void SetupSettings()
        {
            ServerSpecificSettingBase[] setting =
            [
                new SSGroupHeader("RespawnTimer"),
                new SSTwoButtonsSetting(1, "Timers", "Show", "Hide", false, "Toggle RespawnTimer for yourself.")
            ];

            if (ServerSpecificSettingsSync.DefinedSettings == null || ServerSpecificSettingsSync.DefinedSettings.Length == 0)
            {
                ServerSpecificSettingsSync.DefinedSettings = setting;
            }
            else
            {
                var newSettings = new List<ServerSpecificSettingBase>(ServerSpecificSettingsSync.DefinedSettings);
                newSettings.AddRange(setting);
                ServerSpecificSettingsSync.DefinedSettings = newSettings.ToArray();
            }

            ServerSpecificSettingsSync.SendToAll();
        }

        public override void OnReloaded()
        {
            if (Config?.Timers == null || Config.Timers.Count == 0)
                return;

            API.Features.TimerView.CachedTimers.Clear();
            foreach (var name in Config.Timers.Values)
                API.Features.TimerView.AddTimer(name);
        }
    }
}
