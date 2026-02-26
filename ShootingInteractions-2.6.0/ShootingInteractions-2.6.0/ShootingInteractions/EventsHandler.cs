#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using DoorLockType = Exiled.API.Enums.DoorLockType;
using Exiled.Events.EventArgs.Player;
using PlayerShotWeaponEventArgs = Exiled.Events.EventArgs.Player.ShotEventArgs;
#else
using LabApi.Features.Wrappers;
using Firearm = LabApi.Features.Wrappers.FirearmItem;
using LabApi.Events.Arguments.PlayerEvents;
#endif
using Interactables.Interobjects;
using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables.Scp244;
using InventorySystem.Items.Firearms.Modules;
using MEC;
using Mirror;
using ShootingInteractions.Configuration;
using ShootingInteractions.Configuration.Bases;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Footprinting;
using ElevatorDoor = Interactables.Interobjects.ElevatorDoor;
using InteractableCollider = Interactables.InteractableCollider;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Scp2176Projectile = InventorySystem.Items.ThrowableProjectiles.Scp2176Projectile;
using CheckpointDoor = Interactables.Interobjects.CheckpointDoor;
using BasicDoor = Interactables.Interobjects.BasicDoor;
using Locker = MapGeneration.Distributors.Locker;
using ExperimentalWeaponLocker = MapGeneration.Distributors.ExperimentalWeaponLocker;
using LockerChamber = MapGeneration.Distributors.LockerChamber;
using TimedGrenadePickup = InventorySystem.Items.ThrowableProjectiles.TimedGrenadePickup;
using ThrowableItem = InventorySystem.Items.ThrowableProjectiles.ThrowableItem;

namespace ShootingInteractions
{
    internal sealed class EventsHandler
    {
        // --- State ---
        private static Config Config => Plugin.Instance.Config;
        public static List<GameObject> BlacklistedObjects = new List<GameObject>();

        // --- Player Events ---

        public void OnShot(PlayerShotWeaponEventArgs args)
        {
#if EXILED
            Vector3 origin = args.Player.CameraTransform.position;
            Vector3 direction = Config.AccurateBullets ? (args.RaycastHit.point - origin).normalized : args.Player.CameraTransform.forward;
            Firearm firearm = args.Firearm;
#else
            Vector3 origin = args.Player.Camera.position;
            Vector3 direction = args.Player.Camera.forward;
            FirearmItem firearm = args.FirearmItem;
#endif

            int ignoredLayers = (1 << 1) | (1 << 13) | (1 << 28) | (1 << 29);
            if (!Config.InteractWithSurfaceGateBridge)
                ignoredLayers |= (1 << 16);

            if (!Physics.Raycast(origin, direction, out RaycastHit raycastHit, 70f, ~ignoredLayers))
                return;

            GameObject hitObj = raycastHit.transform.gameObject;
            if (BlacklistedObjects.Contains(hitObj))
                return;

            bool isDoorButton = hitObj.GetComponentInParent<BasicDoorButton>() != null;
            bool isElevatorButton = hitObj.GetComponentInParent<ElevatorPanel>() != null;
            bool suppressHitMarker = isDoorButton || isElevatorButton;

            if (Interact(args.Player, hitObj, firearm, direction))
            {
                bool showHitMarker = !suppressHitMarker ||
                    (isDoorButton && Config.DoorButtonsShowHitMarker) ||
                    (isElevatorButton && Config.ElevatorButtonsShowHitMarker);

                if (showHitMarker)
                {
#if EXILED
                    args.Player.ShowHitMarker();
#else
                    args.Player.SendHitMarker();
#endif
                }

                BlacklistedObjects.Add(hitObj);
                Timing.CallDelayed(Time.smoothDeltaTime, () => BlacklistedObjects.Remove(hitObj));
            }
        }

        // --- Interaction Logic ---

        public static bool Interact(Player player, GameObject gameObject, Firearm firearm, Vector3 direction)
        {
            float penetration = 0;
            foreach (ModuleBase moduleBase in firearm.Base.Modules)
                if (moduleBase is HitscanHitregModuleBase hitscanHitregModuleBase)
                    penetration = hitscanHitregModuleBase.BasePenetration;

#if EXILED
            bool isBypassEnabled = player.IsBypassModeEnabled;
#else
            bool isBypassEnabled = player.IsBypassEnabled;
#endif

            // Doors & Buttons
            if (gameObject.GetComponentInParent<BasicDoorButton>() is BasicDoorButton button)
                return HandleDoorInteraction(player, button, penetration, isBypassEnabled);

            // Lockers
            if (gameObject.GetComponentInParent<Locker>() is Locker locker)
                return HandleLockerInteraction(player, gameObject, locker, penetration, isBypassEnabled);

            // Elevators
            if (gameObject.GetComponentInParent<ElevatorPanel>() is ElevatorPanel panel)
                return HandleElevatorInteraction(player, panel, penetration, isBypassEnabled);

            // Grenades
            if (gameObject.GetComponentInParent<TimedGrenadePickup>() is TimedGrenadePickup grenadePickup)
                return HandleGrenadeInteraction(player, grenadePickup, penetration, direction);

            // SCP-018
            if (gameObject.GetComponentInParent<TimeGrenade>() is TimeGrenade scp018 && gameObject.name.Contains("Scp018Projectile"))
                return HandleScp018Interaction(player, scp018, penetration, direction);

            // SCP-244
            if (gameObject.GetComponentInParent<Scp244DeployablePickup>() is Scp244DeployablePickup scp244)
                return HandleScp244Interaction(scp244, penetration);

            // SCP-2176
            if (gameObject.GetComponentInParent<Scp2176Projectile>() is Scp2176Projectile projectile)
                return HandleScp2176Interaction(projectile, penetration);

            // Alpha Warhead Panels
            if (gameObject.GetComponentInParent<AlphaWarheadNukesitePanel>() is AlphaWarheadNukesitePanel warheadAlpha && gameObject.name.Contains("cancel"))
                return HandleWarheadCancel(player, penetration);

            if (gameObject.GetComponentInParent<InteractableCollider>() != null && gameObject.name.Contains("Button"))
                return HandleWarheadStart(player, penetration);

            return false;
        }

        // --- Private Logic Helpers ---

        private static bool HandleDoorInteraction(Player player, BasicDoorButton button, float penetration, bool isBypassEnabled)
        {
            Door door = Door.Get(button.GetComponentInParent<DoorVariant>());
            DoorVariant doorVariant = button.GetComponentInParent<DoorVariant>();

            DoorsInteraction doorInteractionConfig = doorVariant switch
            {
                PryableDoor => Config.Gates,
                CheckpointDoor => Config.Checkpoints,
                BasicDoor => Config.Doors,
                _ => new DoorsInteraction { IsEnabled = false }
            };

            if (!doorInteractionConfig.IsEnabled || (doorInteractionConfig.MinimumPenetration / 100 >= penetration) || door == null || door.Base.IsMoving)
                return false;

            if (doorVariant.NetworkActiveLocks > 0 && !isBypassEnabled)
                return false;

            if (doorVariant is CheckpointDoor && doorVariant.NetworkTargetState)
                return false;

            float cooldown = 0f;
            if (doorVariant is BasicDoor interactableDoor)
            {
                if (interactableDoor._remainingAnimCooldown >= 0.1f) return false;
                cooldown = interactableDoor._remainingAnimCooldown - 0.35f;
                if (doorVariant is PryableDoor && !doorVariant.NetworkTargetState) cooldown -= 0.35f;
            }

            bool shouldLock = !door.IsLocked && Random.Range(1, 101) <= doorInteractionConfig.LockChance;

            if (shouldLock && !doorInteractionConfig.MoveBeforeLocking && !door.IsLocked)
            {
#if EXILED
                door.ChangeLock(DoorLockType.Isolation);
                if (doorInteractionConfig.LockDuration > 0)
                    Timing.CallDelayed(doorInteractionConfig.LockDuration, () => door.ChangeLock(DoorLockType.None));
#else
                door.Lock(DoorLockReason.Isolation, true);
                if (doorInteractionConfig.LockDuration > 0)
                    Timing.CallDelayed(doorInteractionConfig.LockDuration, () => door.Lock(DoorLockReason.Isolation, false));
#endif
                if (!isBypassEnabled) return false;
            }

            if (doorVariant.RequiredPermissions.RequiredPermissions != DoorPermissionFlags.None && !isBypassEnabled && (!doorInteractionConfig.RemoteKeycard || !HasPermission(player.ReferenceHub, doorVariant)))
            {
                door.Base.PermissionsDenied(null, 0);
                return false;
            }

            doorVariant.NetworkTargetState = !doorVariant.NetworkTargetState;

            if (shouldLock && doorInteractionConfig.MoveBeforeLocking)
            {
                Timing.CallDelayed(cooldown, () =>
                {
#if EXILED
                    door.ChangeLock(DoorLockType.Isolation);
                    if (doorInteractionConfig.LockDuration > 0)
                        Timing.CallDelayed(doorInteractionConfig.LockDuration, () => door.ChangeLock(DoorLockType.None));
#else
                    door.Lock(DoorLockReason.Isolation, true);
                    if (doorInteractionConfig.LockDuration > 0)
                        Timing.CallDelayed(doorInteractionConfig.LockDuration, () => door.Lock(DoorLockReason.Isolation, false));
#endif
                });
            }

            return true;
        }

        private static bool HandleLockerInteraction(Player player, GameObject gameObject, Locker locker, float penetration, bool isBypassEnabled)
        {
            LockersInteraction lockerInteractionConfig = gameObject.name switch
            {
                "Collider Keypad" => Config.BulletproofLockers,
                "Collider Door" => Config.BulletproofLockers,
                "Door" => Config.WeaponGridLockers,
                "EWL_CenterDoor" => Config.ExperimentalWeaponLockers,
                "Collider Lid" => Config.Scp127Container,
                "Collider" => Config.RifleRackLockers,
                _ => new LockersInteraction() { IsEnabled = false }
            };

            if (!lockerInteractionConfig.IsEnabled || (lockerInteractionConfig.MinimumPenetration / 100 >= penetration))
                return false;

            if (lockerInteractionConfig is BulletproofLockersInteraction bulletProof && gameObject.name == "Collider Door" && bulletProof.OnlyKeypad)
                return false;

            if (gameObject.GetComponentInParent<LockerChamber>() is LockerChamber chamber)
            {
                if (!chamber.CanInteract) return false;
                if (!isBypassEnabled && (!lockerInteractionConfig.RemoteKeycard || !HasPermission(player.ReferenceHub, chamber)))
                {
                    locker.RpcPlayDenied((byte)locker.Chambers.ToList().IndexOf(chamber), chamber.RequiredPermissions);
                    return false;
                }
                chamber.SetDoor(!chamber.IsOpen, locker._grantedBeep);
                locker.RefreshOpenedSyncvar();
                return true;
            }

            return false;
        }

        private static bool HandleElevatorInteraction(Player player, ElevatorPanel panel, float penetration, bool isBypassEnabled)
        {
            ElevatorsInteraction elevatorInteractionConfig = Config.Elevators;
#if EXILED
            Lift elevator = Lift.Get(panel.AssignedChamber);
            bool isLocked = elevator.IsLocked;
            bool isMoving = elevator.IsMoving;
#else
            Elevator elevator = Elevator.Get(panel.AssignedChamber);
            bool isLocked = elevator.AnyDoorLockedReason <= 0 ? elevator.AllDoorsLockedReason > 0 : true;
            bool isMoving = (uint)elevator.Base.CurSequence - 2 <= 1u;
#endif

            if (!elevatorInteractionConfig.IsEnabled || (elevatorInteractionConfig.MinimumPenetration / 100 >= penetration) || panel.AssignedChamber == null || elevator == null || isMoving || !elevator.Base.IsReady)
                return false;

            if (isLocked && !isBypassEnabled) return false;
            if (!ElevatorDoor.AllElevatorDoors.TryGetValue(panel.AssignedChamber.AssignedGroup, out List<ElevatorDoor> list)) return false;

            bool shoudLock = !isLocked && Random.Range(1, 101) <= elevatorInteractionConfig.LockChance;

            if (shoudLock && !elevatorInteractionConfig.MoveBeforeLocking)
            {
                foreach (ElevatorDoor door in list) door.ServerChangeLock(DoorLockReason.Isolation, true);
                if (elevatorInteractionConfig.LockDuration > 0)
                    Timing.CallDelayed(elevatorInteractionConfig.LockDuration, () => { foreach (ElevatorDoor door in list) { door.NetworkActiveLocks = 0; } });
                if (!isBypassEnabled) return false;
            }

#if EXILED
            elevator.TryStart(panel.AssignedChamber.NextLevel);
#else
            elevator.SetDestination(panel.AssignedChamber.NextLevel);
#endif

            if (shoudLock && elevatorInteractionConfig.MoveBeforeLocking)
            {
                foreach (ElevatorDoor door in list) door.ServerChangeLock(DoorLockReason.Isolation, true);
                if (elevatorInteractionConfig.LockDuration > 0)
                    Timing.CallDelayed(elevatorInteractionConfig.LockDuration + elevator.Base._animationTime + elevator.Base._rotationTime + elevator.Base._doorOpenTime + elevator.Base._doorCloseTime, () =>
                    { foreach (ElevatorDoor door in list) { door.NetworkActiveLocks = 0; } });
            }

            return true;
        }

        private static bool HandleGrenadeInteraction(Player player, TimedGrenadePickup grenadePickup, float penetration, Vector3 direction)
        {
            if (Plugin.GetCustomItem != null && (bool)Plugin.GetCustomItem.Invoke(null, new[] { Pickup.Get(grenadePickup), null }))
            {
#if EXILED
                if (!Config.CustomGrenades.IsEnabled || (Config.CustomGrenades.MinimumPenetration >= penetration / 100)) return false;
                grenadePickup.PreviousOwner = new Footprint(player.ReferenceHub);
                grenadePickup._replaceNextFrame = true;
                return true;
#endif
                return false;
            }

            TimedProjectilesInteraction config = grenadePickup.Info.ItemId switch
            {
                ItemType.GrenadeHE => Config.FragGrenades,
                ItemType.GrenadeFlash => Config.Flashbangs,
                _ => new TimedProjectilesInteraction() { IsEnabled = false }
            };

            if (!config.IsEnabled || config.MinimumPenetration >= penetration / 100) return false;
            if (!InventoryItemLoader.AvailableItems.TryGetValue(grenadePickup.Info.ItemId, out ItemBase grenadeBase) || grenadeBase is not ThrowableItem throwable) return false;

            ThrownProjectile projectile = Object.Instantiate(throwable.Projectile);
            if (projectile.PhysicsModule is PickupStandardPhysics physics && grenadePickup.PhysicsModule is PickupStandardPhysics pickupPhysics)
            {
                physics.Rb.position = pickupPhysics.Rb.position;
                physics.Rb.rotation = pickupPhysics.Rb.rotation;
                physics.Rb.AddForce(direction * (config.AdditionalVelocity ? config.VelocityForce : 1) * (config.ScaleWithPenetration ? penetration * config.VelocityPenetrationMultiplier : 1));
            }

            grenadePickup.Info.Locked = true;
            projectile.NetworkInfo = grenadePickup.Info;
            grenadePickup.PreviousOwner = new Footprint(player.ReferenceHub);
            projectile.PreviousOwner = new Footprint(player.ReferenceHub);
            NetworkServer.Spawn(projectile.gameObject);

            if (Random.Range(1, 101) <= config.CustomFuseTimeChance)
                (projectile as TimeGrenade)._fuseTime = Mathf.Max(Time.smoothDeltaTime * 2, config.CustomFuseTimeDuration);

            projectile.ServerActivate();
            grenadePickup.DestroySelf();
            return true;
        }

        private static bool HandleScp018Interaction(Player player, TimeGrenade scp018, float penetration, Vector3 direction)
        {
            TimedProjectilesInteraction config = Config.Scp018;
            if (!config.IsEnabled || config.MinimumPenetration >= penetration / 100) return false;
            if (!InventoryItemLoader.AvailableItems.TryGetValue(scp018.Info.ItemId, out ItemBase baseItem) || baseItem is not ThrowableItem throwable) return false;

            ThrownProjectile projectile = Object.Instantiate(throwable.Projectile);
            if (projectile.PhysicsModule is PickupStandardPhysics physics)
            {
                physics.Rb.position = scp018.Position;
                physics.Rb.rotation = scp018.Rotation;
                physics.Rb.AddForce(direction * (config.AdditionalVelocity ? config.VelocityForce : 1) * (config.ScaleWithPenetration ? penetration * config.VelocityPenetrationMultiplier : 1));
            }

            scp018.Info.Locked = true;
            projectile.NetworkInfo = scp018.Info;
            scp018.PreviousOwner = new Footprint(player.ReferenceHub);
            projectile.PreviousOwner = new Footprint(player.ReferenceHub);

            if (Random.Range(1, 101) <= config.CustomFuseTimeChance)
                (projectile as TimeGrenade)._fuseTime = Mathf.Max(Time.smoothDeltaTime * 2, config.CustomFuseTimeDuration);

            projectile.ServerActivate();
            return true;
        }

        private static bool HandleScp244Interaction(Scp244DeployablePickup scp244, float penetration)
        {
            if (!Config.Scp244.IsEnabled || (Config.Scp244.MinimumPenetration >= penetration / 100)) return false;
            scp244.State = Scp244State.Destroyed;
            return true;
        }

        private static bool HandleScp2176Interaction(Scp2176Projectile projectile, float penetration)
        {
            if (!Config.Scp2176.IsEnabled || (Config.Scp2176.MinimumPenetration >= penetration / 100)) return false;
            projectile.ServerImmediatelyShatter();
            return true;
        }

        private static bool HandleWarheadCancel(Player player, float penetration)
        {
            if (!Config.NukeCancelButton.IsEnabled || (Config.NukeCancelButton.MinimumPenetration >= penetration / 100)) return false;
            AlphaWarheadController.Singleton?.CancelDetonation(player.ReferenceHub);
            return true;
        }

        private static bool HandleWarheadStart(Player player, float penetration)
        {
#if EXILED
            if (player.CurrentRoom.Type != RoomType.Surface) return false;
            bool isAuth = Warhead.IsKeycardActivated;
            bool canStart = Warhead.CanBeStarted;
#else
            if (player.Room.Zone != MapGeneration.FacilityZone.Surface) return false;
            bool isAuth = Warhead.IsAuthorized;
            bool canStart = Warhead.IsDetonationInProgress && !Warhead.IsDetonated && Warhead.BaseController?.CooldownEndTime <= NetworkTime.time;
#endif
            if (!Config.NukeCancelButton.IsEnabled || (Config.NukeCancelButton.MinimumPenetration >= penetration / 100) || !isAuth || !canStart || !Warhead.LeverStatus)
                return false;

            Warhead.Start();
            return true;
        }

        private static bool HasPermission(ReferenceHub player, IDoorPermissionRequester permissionFlags)
        {
#if EXILED
            return Player.Get(player).Items.Any(item => item is Keycard keycard && item.Base is IDoorPermissionProvider itemProvider && permissionFlags.PermissionsPolicy.CheckPermissions(itemProvider.GetPermissions(permissionFlags)));
#else
            return Player.Get(player).Items.Any(item => item is KeycardItem keycard && item.Base is IDoorPermissionProvider itemProvider && permissionFlags.PermissionsPolicy.CheckPermissions(itemProvider.GetPermissions(permissionFlags)));
#endif
        }
    }
}
