# Bad Carrete Day — Nivel 1: "El Pastero"

Juego de plataformas y aventura 3D para la clase de Desarrollo de Videojuegos (UCSC 2026). Está inspirado en la jugabilidad de Conker's Bad Fur Day, con personajes, mundo e historia originales. La historia está en `GUION_NIVEL1.md`.

## Cómo jugar
- **Abrir:** Unity **6000.3.21f1**. Abre la escena `Assets/BadCarreteDay/Escenas/MenuInicio` y presiona Play.
- **Build para Mac:** `Builds/Mac/BadCarreteDay.app`.
- **Controles:** WASD mover · Shift correr · Espacio saltar (salta, suelta y vuelve a mantener en el aire: **planear con el polerón**) · Clic izquierdo atacar · E interactuar (pegatinas ¡AQUÍ!, hablar, comprar) · Esc pausa.

## Escenas (Build Settings)
| # | Escena | Qué es |
|---|---|---|
| 0 | MenuInicio | Menú: Jugar, Créditos, Salir |
| 1 | Intro | 4 viñetas de la historia |
| 2 | Nivel1_Cerro | Nivel completo (5 zonas, jefe Julio "El Pastero") |
| 3 | Fin | Victoria (teléfono + "Continuará") o Game Over |
| 4-6 | Prueba_F2/F1/F0 | Escenas de prueba que usan las pruebas automáticas (no van en el build) |

## Herramientas (menú **Bad Carrete Day** en Unity)
Las escenas y prefabs se **generan por código** (`Assets/BadCarreteDay/Editor/Constructor*.cs`), así que el proyecto se puede reconstruir igual en cualquier computador.
- **Reconstruir todo:** regenera el Animator, el prefab del jugador y todas las escenas.
- **F6 - Compilar juego (Mac):** genera el build.
- **Sacar capturas del Nivel 1:** guarda imágenes en `Capturas/`.
- **Pruebas automáticas:** *Window › General › Test Runner › PlayMode* (7 pruebas: HUD, diálogo, plata, vidas, combate, nivel completo, menú/intro/fin).

> ⚠ Si editas una escena a mano y después usas "Reconstruir todo", tus cambios en esa escena se pierden. Para cambios permanentes, edita los constructores.

## Base de código de clase (repo ClaseUCSC2026)
**Se usan tal cual:** `ControladorCargaEscena`, `ControladorJugador`, `DemoItem`, `DemoInventario`, `DemoObjetivo`, `ControladorBarraSalud`, `ControladorLugarObjetivo`, `ControladorColisionTrigger`, `ControladorRandomIdle`, `ControladorBolaPeso` y Starter Assets.

**Cambios puntuales** (marcados con `// [Bad Carrete Day]`):
| Archivo | Cambio |
|---|---|
| `ThirdPersonController.cs` | Planeo (campos `PlaneoHabilitado`, `VelocidadCaidaPlaneo`, `DuracionPlaneo`, `Planeando`) |
| `StarterAssetsInputs.cs` | `planeo` (mantener Saltar) |
| `StarterAssets.inputactions` | Acciones `Interaccion` (E) y `Pausa` (Esc) |
| `DemoControlInteraccion.cs` | Solo interactúa si el jugador está dentro del trigger (`tagEsperado`, `jugadorDentro`) |
| `Moneda.cs` | Suma la plata, no cuenta doble, se destruye y el animator es opcional |
| `DemoControladorEstadisticas.cs` | `Curar(float)` |
| `DemoArma.cs` | Arreglos: el primer golpe hacía 0 de daño, NullReference al salir, se golpeaba a sí mismo, un golpe por ataque |
| `MobController.cs` | `Detener()` |

**Scripts nuevos** (`Assets/BadCarreteDay/Scripts`): diálogo, misión, dinero, compra, HUD, juego (vidas, checkpoints, pausa), plataforma móvil, enemigo, fases del jefe, contador de enemigos, menú, intro, fin, girar, visual del planeo, configuración, `Flotar` (barcos) y `ParpadeoLuz` (farol del pasaje oscuro).

**Jugabilidad estilo Conker:** diálogos en burbujas con murmullo y pitido de censura (`ControladorDialogo`, `Burbuja`, `ControladorBurbujas`, `Voz`), cámara de conversación (`CamaraDialogo`), tienda tipo Zelda/Pokémon (`ControladorTienda`), Toby guardián (`PerroGuardian`), caña hasta tomar agua (`Resaca`), colgarse de bordes y escalar rejas (`AgarreBorde`, animaciones Mixamo "Braced Hang" y "Braced Hang To Crouch"), rejas con púas (`RejaPuas`), límites del mapa con red de seguridad (`LimiteMapa`), presentaciones de zona (`PresentacionZona`). Los sonidos (murmullos, censura, cierre, chorro, tienda) se generan por código en `Editor/ConstructorSonidos.cs`. El planeo quedó programado pero apagado (se desbloquea en otro nivel). Las animaciones de colgarse y subir se miden solas al empezar (`AgarreBorde.MedirAnimaciones`): Benja queda con las manos en el borde y la subida la mueve la propia animación (movimiento de raíz). Con caña usa un blend tree "Curado" (DrunkIdle/DrunkWalk acelerado a 1 m/s). Los golpes: `DemoArma` pega a todos los enemigos de la zona (uno por golpe a cada uno), `ApagarArmaAlSalir` apaga el arma aunque corten el golpe, `ReaccionGolpe` no corta tu golpe si te pegan, y `AtaqueJugador` solo deja pegar en el suelo y apunta al enemigo cercano.

**El mundo del Nivel 1 (un cerro real):** `Editor/ConstructorNivel1.cs` arma un `Terrain` continuo (al oeste el cerro sube, al este baja al plan y la bahía), edificios KayKit con **escala uniforme** y collider propio, cimientos o pilotes hasta el terreno, calles, escaleras caminables (escalones de 0,2 m), barandas y límites. `Editor/ConstructorNivel1Detalles.cs` pone los detalles de cada zona sobre superficies reales. `Editor/VerificadorNivel1.cs` revisa en cada construcción que no haya huecos, que toda la plata se alcance (subidas de máximo 1 m), que el recorrido esté conectado, que nada flote y que no haya edificios de menos de 5 m; si algo falla, la construcción se detiene. Los NPC miran al jugador con `Scripts/MirarJugador.cs` (giro de cabeza con IK y de cuerpo al hablarles). Las viñetas 1–3 de la intro se fotografían desde `Escenas/Vinetas.unity` (`Editor/ConstructorVinetas.cs`).

## Assets y licencias
Están en **Créditos**, dentro del juego. Personajes y animaciones de Mixamo (Adobe). Entorno de KayKit (City Builder, Restaurant y Furniture Bits) y Kenney (City Kit Suburban, Roads, Industrial y Cube Pets), todos CC0. Golpes de Kenney Impact Sounds (CC0). Música de Zane Little Music y Almitory (CC0) y jingles de Kenney (CC0). Ladrido de Brandon Morris (CC0). Fuente Luckiest Guy (Apache 2.0). Cada pack trae su `License.txt` en `Assets/BadCarreteDay/Arte/`.

## Pendientes conocidos
- **Barcos de la bahía:** hechos con primitivas (el Kenney Watercraft Kit no alcanzó a descargarse).
- **Ojeras de Julio:** sin pintar (habría que editar su textura).
- **Balance y alcance de saltos:** ajustar jugando.

## Caña primero, plata escondida, señoras y secretos
- **Agua = primera misión.** Con caña Benja solo camina (1 m/s) y habla: no salta, no se cuelga, no corre ni pega, y ve borroso (Volume `PostProceso_Cana`: desenfoque, viñeta, colores apagados; se aclara en 2 s al tomar agua). Script: `Resaca.cs`.
- **Con caña nadie te entiende:** `DialogoNPC.conversacionConCana`. Don Lucho sigue meando de espaldas ("No se te entiende nada, cabro." / "Ya, déjame tranquilo.").
- **Economía:** sin monedas tiradas en la calle. Plata en basureros revisables una vez (`Basurero.cs`, [E]), en la señora Nelly ($500) y en rincones altos/escondidos. Con caña se junta $2.000 caminando; en las zonas 1 y 2 hay $5.800 (se necesitan $5.000). `VerificadorNivel1` revisa ambas cuentas.
- **Señoras** (textos juntos al inicio de la sección "SEÑORAS" de `ConstructorF3.cs`): Yoli (pasaje, insulta), Nelly (plaza, da $500), Rosa (calle alta, se justifica), Gladys (cima, te da el palo de escoba). Por ahora usan el modelo de Martha teñido; si se agregan `Arte/Personajes/SenoraYoli.fbx` (etc.) se usan solos.
- **Secretos:** detrás de la escalera rota (luka + cuaderno UCSC "Certamen de Cálculo II: lunes") y en el rincón del kiosko (grafiti "UCSC ¡VAMOS LOS DELFINES!" + quina).
- **Arreglos:** foso del ascensor cerrado (antes se caía a y≈6,9 y quedaba atrapado); el dumpster de la plaza estaba metido dentro de un edificio (ahora en la esquina del mirador); el post-proceso de la cámara del juego no se guardaba (faltaba `RecordPrefabInstancePropertyModifications`): ahora sí hay bloom/viñeta en el juego real.

## Toby bravo, escoba-espada, Don Lucho sin "locomotora", almacén rápido
- **Toby:** perro animado "Dog" de Quaternius (CC0, `Arte/Animales/Toby_Dog.fbx`), 1,2 m, pelaje oscuro y ojos rojos. Ladra fuerte y seguido desde 10 m (`PerroGuardian`) y se calla al comer.
- **Escoba:** modelo "Broom" de Poly by Google (CC-BY 3.0, `Arte/Props/Escoba.obj`, ver `CREDITOS.txt`). Benja la lleva al hombro como espada y al pegar la baja en tajo (`ConstructorF2.rotacionAgarre`).
- **La escoba es un secreto:** la Sra. Gladys te la da al fondo de la calle alta, abajo, pasada la escalera rota. Sin ella se pelea a puño limpio (10 de daño; con escoba 25). Ya no es un objetivo de la misión.
- **Don Lucho:** antes de hablar solo suena el cierre (se quitó el fin del chorro).
- **Almacén:** repisas con mercadería, refri de bebidas, registradora. Los productos ya no están en el mesón. [E] abre la tienda al tiro (Marta saluda en una burbuja). Todo se puede comprar desde el inicio; si llevas el vino antes de hablar con Don Lucho, una sola conversación cumple los tres pasos (`DialogoNPC.CambiarSegun`). Con caña, al hablar o comprar la vista se aclara.
- **Escoba en el puño (`EscobaEnMano`):** en cada cuadro el mango pasa por el centro del puño real, en la dirección meñique→índice (las cerdas salen por el lado del pulgar), con los dedos siempre cerrados (capa "Escoba" del Animator, solo dedos). Fuera del golpe la muñeca la deja en la línea del antebrazo inclinada hacia adelante, y si así chocara con el cuerpo prueba otras direcciones; corriendo va casi horizontal al frente; nunca se entierra. Colgado, subiendo un borde, conversando, comprando o muerto, va a la espalda. Con escoba no bosteza. Auditoría: `PruebaG_AuditoriaEscoba` (explícita, con fotos en `Capturas/Juego/Auditoria`); verificación automática: `PruebaG_Escoba`.

## Atajos de prueba (solo editor / build de desarrollo)
En el menú de pausa (Esc) hay una columna "IR A (PRUEBAS)" con un botón por punto (también sirven las teclas F1..F7):
F1 inicio · F2 con agua · F3 vino entregado y completo comprado (frente al Toby) · F4 Toby comiendo (calle alta) · F5 techo A · F6 cima, antes de los cumas (con escoba) · F7 cancha, antes de Julio.
Cada tecla recarga el nivel y reproduce los eventos reales hasta ese punto (`Scripts/AtajosDesarrollo.cs`). En un build normal (no "Development") no hacen nada.

## Julio "El Pastero": jefe tanque
- 400 de salud, lento (1,6 m/s), golpe pesado de 20 que empuja. **Gira como máximo 55°/s** (22°/s mientras pega, nada mientras ruge, se ríe o está agotado): corriendo en círculo con Shift se le llega a la espalda. El giro lo maneja solo `ConductaJulio` (el NavMeshAgent no lo rota y `MirarJugador` se apaga al empezar la pelea), con un tope final en `LateUpdate`. Las embestidas van en línea recta: si te haces a un lado, sigue de largo.
- **Cuchillo en la espalda** (Quaternius, CC0, `Arte/Props/Cuchillo.obj`): golpe por la espalda = daño ×3, se queja y queda aturdido 0,6 s. De frente no siente dolor: no se interrumpe y se ríe.
- **Fase 2 (50%)** sin secuaces: ruge, "¡JULIO SE ENOJÓ!", embestidas rápidas cada 4–7 s y después queda agotado 1,6 s (la oportunidad).
- Aspecto trasnochado: piel trigueña pálida (textura procesada por código desde la de Mremireh), ojos irritados, encorvado, tirones de cabeza, respiración agitada (sonido generado).
- Animator propio (`AnimatorNPC_Julio`). Si se agregan clips de Mixamo en `Arte/Animaciones/Julio/` ("Mutant Idle", "Mutant Walking", "Mutant Run", "Mutant Punch", "Mutant Roaring", "Laughing") se usan solos; hoy usa los que ya había.
- Script: `ConductaJulio.cs`. Prueba: `PruebaG_Julio`.
