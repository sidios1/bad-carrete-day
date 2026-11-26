# Descargas de Mixamo para la Fase 1 — Bad Carrete Day

Entra a https://www.mixamo.com con tu cuenta Adobe **personal**. Las cuentas de la universidad (Enterprise/Federated) no sirven.

## 1. Personajes
Ruta: pestaña **Characters** → busca el nombre → **DOWNLOAD** con estas opciones:
- Format: **FBX for Unity (.fbx)**
- Pose: **T-pose**

| Archivo a guardar | Personaje (nombre exacto en Mixamo) | Rol |
|---|---|---|
| `Aj.fbx` | **Aj** | Protagonista (el estudiante) |
| `PeasantMan.fbx` | **Peasant Man** | Viejo curado |
| `Mremireh.fbx` | **Mremireh O Desbiens** | El Pastero |
| `James.fbx` | **James** | Cuma 1 |
| `David.fbx` | **David** | Cuma 2 |
| `Martha.fbx` | **Martha** | Señora del almacén |

Guárdalos en: `Assets/BadCarreteDay/Arte/Personajes/`

## 2. Animaciones
Antes de buscar animaciones, deja seleccionado a **Aj** como personaje en el visor. Después: pestaña **Animations** → busca → **DOWNLOAD** con estas opciones:
- Format: **FBX for Unity (.fbx)**
- Skin: **Without Skin**
- Frames per Second: **30**

Caminar, correr y saltar **no** hay que bajarlos: los Starter Assets ya traen esas animaciones y sirven para cualquier personaje Humanoid.

| Archivo a guardar | Buscar en Mixamo | Para qué |
|---|---|---|
| `Yawn.fbx` | **Yawn** ("Big Yawn While Standing") | Idle random del protagonista (caña) |
| `DrunkIdleHipo.fbx` | **Drunk Idle Variation** ("Drunk Idle Variation Hiccup") | Idle random del protagonista (hipo) |
| `DrunkIdle.fbx` | **Drunk Idle** ("Drunken Basic Idle") | Viejo curado |
| `DrunkWalk.fbx` | **Drunk Walk** ("Drunk Walk Forward") | Viejo curado / jugador mareado |
| `GolpePalo.fbx` | **Heavy Weapon Swing** ("Two Handed Heavy Weapon Swing") | Golpe con el palo de escoba |
| `HitReaction.fbx` | **Hit Reaction** (el primero de la lista) | Recibir golpe (todos) |
| `Dying.fbx` | **Dying** (el primero de la lista) | Muerte (todos) |
| `Victory.fbx` | **Victory** | Ganarle a El Pastero |
| `FightingIdle.fbx` | **Fighting Idle** | Cumas y El Pastero en guardia |
| `Punching.fbx` | **Punching** | Ataque de cumas y El Pastero |
| `Taunt.fbx` | **Taunt** ("Taunting Throwing Arms Back") | Provocación de El Pastero |
| `PullingLever.fbx` | **Pulling Lever** ("Pulling A Lever Down") | Acción contextual: palanca del ascensor |
| `PickingUp.fbx` | **Picking Up** ("Picking Up An Object") | Acción contextual: recoger objetos |

Guárdalas en: `Assets/BadCarreteDay/Arte/Animaciones/`

Cuando estén todas, avísame y yo configuro la importación como Humanoid, el avatar y el Animator.
