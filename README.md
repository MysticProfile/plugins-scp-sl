# Plugins SCP:SL (EXILED) - Mystic

Colección de plugins para **SCP: Secret Laboratory** (principalmente **EXILED**).

## Nota importante (créditos / forks)

Además, algunos plugins de este repo están **inspirados** en ideas o comportamientos vistos en otros plugins de la comunidad.

En este repo hay algunos proyectos que **NO son originalmente míos**, pero están **editados/modificados por mí** para que se comporten como me gusta o para adaptarlos a mi servidor:

- RespawnTimer
- ShootingInteractions
- TextChat
- CandyUtilities

El resto de proyectos del repo son plugins hechos por mí (o re-trabajados fuertemente) según mis necesidades.

## Disclaimer

Si a alguien le afecta o molesta que haya publicado estos plugins (especialmente los forks/ediciones), le pido disculpas de antemano. Mi intención no es apropiarme del trabajo ajeno ni causar problemas, simplemente compartí mis modificaciones personales por si a alguien le fueran útiles. Si algún autor original desea que retire algo, puede contactarme y lo haré sin problema.

## Advertencia

Es posible que algunos plugins no funcionen correctamente en todas las configuraciones o versiones del juego/EXILED. Cuando los probé funcionaban, pero no garantizo que funcionen en todos los casos. Úsalos bajo tu propia responsabilidad.

## Lista de plugins

### AntiAFK
Evita comportamientos AFK (anti-idle) y maneja eventos para detectar actividad.

### AntiSpawnKill
Protección anti-spawnkill (proyecto compatible con EXILED/LabAPI según configuración).

### ContainmentBreachTimers
Timers/avisos relacionados a eventos importantes del round (decont, pocket, etc.).

### FogControlForAll
Control de niebla (FogControl) aplicado a jugadores según condiciones configurables.

### LastTeamAlive
Lógica para detectar/forzar final de ronda cuando queda un solo equipo en pie.

### MvpSystem
Sistema MVP (parches Harmony + lógica de tracking de performance durante la ronda).

### RagdollCleaner
Limpieza de ragdolls con reglas configurables para mantener el mapa limpio y mejorar performance.

### RespawnTimer
**No es originalmente mío**. Fork/edición personal.

Carpetas relacionadas:
- `RespawnTimer default`
- `RespawnTimer-1.2.1`

### RoleEscapeHandler
Convierte roles al escapar:
- **Facility Guard** o **Scientist** al escapar pasan a **NTF Specialist**.
- **Class-D** al escapar pasa a **Chaos Repressor**.
- Si el **Guard** está esposado y siendo escoltado por **Chaos**, se comporta como escape de Class-D y pasa a **Chaos Repressor**.

### Scp1853And207Explode
Si un jugador tiene simultáneamente los efectos de **SCP-1853** y **SCP-207**, al consumir el segundo item se produce una “explosión” (granada opcional + daño letal configurable).

### Scp207SpeedStacks
Sistema de “stacks” de velocidad al tomar múltiples **SCP-207**.

### Scp914VeryFineEffects
Efectos adicionales aplicados al usar SCP-914 en **Very Fine**.

### ScpContainAnnouncer
Anuncios/avisos cuando se contienen SCPs.

### ShootingInteractions
**No es originalmente mío**. Fork/edición personal.

Carpeta relacionada:
- `ShootingInteractions-2.6.0`

### TeamShotsAndKills
Tracking/estadísticas de disparos y kills por equipo (incluye modo EXILED/LabAPI según build).

### TextChat
**No es originalmente mío**. Fork/edición personal.

Carpeta relacionada:
- `TextChat-1.2.1`

### TutorialNoScpEffect
Ajustes para el rol Tutorial (por ejemplo, evitar efectos/condiciones tipo SCP).

### CandyUtilities
**No es originalmente mío**. Fork/edición personal.

Carpeta relacionada:
- `CandyUtilitiesv2 mystic scpsl`

## Build

Compilación típica (ejemplo):

```bash
dotnet build .\NOMBRE_PROYECTO\NOMBRE_PROYECTO.csproj -c EXILED
```

La DLL generada suele salir en:

- `NOMBRE_PROYECTO/bin/EXILED/net48/`  

## Instalación

Copiar la DLL al servidor en:

- `EXILED/Plugins/`

y reiniciar/reload.
