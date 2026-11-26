using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Herramienta de EDITOR para la Fase 2 (sistemas de juego):
//  - Agrega salud (DemoControladorEstadisticas) y DemoObjetivo al prefab del jugador
//  - Crea la escena Prueba_F2: HUD, dialogo con el viejo, lucas, pegatina ¡Aquí!,
//    trampa, curacion, caida al vacio, pausa y mision
//  - Crea la escena Fin (minima, se termina en la F6)
// Todo se conecta con UnityEvents, igual que en clase, pero cableado por codigo.
public static class ConstructorF2
{
    const string rutaPrefabJugadorAj = "Assets/BadCarreteDay/Prefabs/JugadorAj.prefab";
    const string rutaBarraSalud = "Assets/Prefabs/UI/BarraSalud.prefab";
    const string rutaEscenaPrueba = "Assets/BadCarreteDay/Escenas/Prueba_F2.unity";
    const string rutaEscenaFin = "Assets/BadCarreteDay/Escenas/Fin.unity";
    const string rutaAnimatorViejo = "Assets/BadCarreteDay/Animaciones/AnimatorNPC_Viejo.controller";

    [MenuItem("Bad Carrete Day/Reconstruir todo")]
    public static void ReconstruirTodo()
    {
        ConstructorF5.PrepararAssets(); // fuente, cielo, post-proceso, audio
        ConstructorSonidos.Generar();   // murmullos, censura, cierre, chorro, tienda
        ConstructorBadCarreteDay.ConstruirF0();
        ConstructorF1.ConstruirF1();
        ConstruirF2();
        ConstructorF3.ConstruirF3();
        ConstructorF6.ConstruirF6();
    }

    [MenuItem("Bad Carrete Day/F2 - Construir todo")]
    public static void ConstruirF2()
    {
        ConstructorBadCarreteDay.CrearAnimatorJugador(); // agrega el estado de muerte
        ActualizarPrefabJugador();
        CrearAnimatorViejo();
        CrearEscenaFin();          // primero Fin, para conocer su indice en Build Settings
        CrearEscenaPrueba();
        Debug.Log("[BadCarreteDay] F2 construida ✅");
    }

    // ===== PREFAB DEL JUGADOR: salud + recibir golpes =====

    public static void ActualizarPrefabJugador()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefabJugadorAj);
        GameObject jugador = raiz.GetComponentInChildren<Animator>().gameObject;

        DemoControladorEstadisticas stats = jugador.GetComponent<DemoControladorEstadisticas>();
        if (stats == null) stats = ConstructorBadCarreteDay.Agregar<DemoControladorEstadisticas>(jugador);
        stats.estadisticasBase.salud = 100f;
        stats.estadisticasBase.ataque = 10f;

        // DemoObjetivo (clase): cuando un arma enemiga lo golpea -> CalcularDanio
        DemoObjetivo objetivo = jugador.GetComponent<DemoObjetivo>();
        if (objetivo == null)
        {
            objetivo = ConstructorBadCarreteDay.Agregar<DemoObjetivo>(jugador);
            UnityEventTools.AddPersistentListener(objetivo.alSerGolpeado, stats.CalcularDanio);
        }

        // Al recibir un golpe: daño + reaccion (ReaccionGolpe no corta el golpe propio ni el colgado)
        ReaccionGolpe reaccion = jugador.GetComponent<ReaccionGolpe>();
        if (reaccion == null) reaccion = ConstructorBadCarreteDay.Agregar<ReaccionGolpe>(jugador);
        reaccion.animator = jugador.GetComponent<Animator>();
        for (int i = objetivo.alSerGolpeado.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(objetivo.alSerGolpeado, i);
        UnityEventTools.AddPersistentListener(objetivo.alSerGolpeado, stats.CalcularDanio);
        UnityEventTools.AddPersistentListener(objetivo.alSerGolpeado, reaccion.Golpeado);

        // Reglas del golpe de Benja (solo en el suelo, sin dialogo, apunta al enemigo cercano)
        AtaqueJugador ataque = jugador.GetComponent<AtaqueJugador>();
        if (ataque == null) ataque = ConstructorBadCarreteDay.Agregar<AtaqueJugador>(jugador);
        ataque.movimiento = jugador.GetComponent<StarterAssets.ThirdPersonController>();
        ataque.animator = jugador.GetComponent<Animator>();
        ataque.estadisticas = stats;

        // PALO DE ESCOBA: arma de clase (DemoArma) en la mano derecha, APAGADA hasta recogerlo en el nivel
        CrearPaloEscoba(jugador);

        // Polerón-hélice que aparece al planear (F5)
        ConstructorF5.AgregarPoleronHelice(jugador);

        // BasicRigidBodyPush (Starter Assets): permite empujar objetos con Rigidbody (bidon del nivel)
        BasicRigidBodyPush empuje = jugador.GetComponent<BasicRigidBodyPush>();
        if (empuje != null)
        {
            empuje.canPush = true;
            empuje.pushLayers = LayerMask.GetMask("Default");
            empuje.strength = 2f;
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefabJugadorAj);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    public const string nombrePalo = "Palo_Escoba_Arma";       // zona de golpe (DemoArma) frente a Benja
    public const string nombrePaloVisual = "Palo_Escoba_Visual"; // el palo que se ve en la mano

    // La escoba la ubica EscobaEnMano en cada cuadro (dentro del puño); aqui solo se arma
    public const float largoEscoba = 1.25f;

    // Escoba (Poly by Google, CC-BY): el extremo del mango queda en el origen del padre y las cerdas
    // hacia +Z. El largo se da en metros del mundo (se compensa la escala del padre).
    public static GameObject ModeloEscoba(Transform padre, float largo)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorF5.rutaEscoba);
        GameObject escoba;
        if (asset != null) escoba = (GameObject)PrefabUtility.InstantiatePrefab(asset, padre);
        else { escoba = GameObject.CreatePrimitive(PrimitiveType.Cube); escoba.transform.SetParent(padre, false); }
        escoba.name = "Escoba";
        foreach (Collider c in escoba.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        // En el modelo, el mango esta arriba (+Y) y las cerdas abajo: -Y pasa a ser +Z
        escoba.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        escoba.transform.localScale = Vector3.one;
        escoba.transform.localPosition = Vector3.zero;
        Bounds b = LimitesEn(escoba, padre);
        escoba.transform.localScale = Vector3.one * (largo / padre.lossyScale.z) / Mathf.Max(b.size.z, 0.0001f);
        // El extremo del mango (lo mas atras en Z) al origen, centrado en X/Y
        b = LimitesEn(escoba, padre);
        escoba.transform.localPosition -= new Vector3(b.center.x, b.center.y, b.min.z);
        return escoba;
    }

    // Caja de las mallas de un objeto en el espacio local de "espacio"
    static Bounds LimitesEn(GameObject obj, Transform espacio)
    {
        bool primero = true;
        Bounds r = new Bounds();
        foreach (MeshFilter f in obj.GetComponentsInChildren<MeshFilter>())
        {
            Bounds m = f.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = espacio.InverseTransformPoint(f.transform.TransformPoint(esquina));
                if (primero) { r = new Bounds(p, Vector3.zero); primero = false; } else r.Encapsulate(p);
            }
        }
        return r;
    }

    static void CrearPaloEscoba(GameObject jugador)
    {
        foreach (string viejo in new[] { nombrePalo, nombrePaloVisual })
            foreach (Transform t in jugador.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == viejo) Object.DestroyImmediate(t.gameObject);

        // 1) VISUAL: la escoba en la mano derecha, agarrada del extremo del mango como una espada
        Transform mano = null;
        foreach (Transform t in jugador.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith("RightHand")) { mano = t; break; }
        GameObject visual = new GameObject(nombrePaloVisual);
        visual.transform.SetParent(mano != null ? mano : jugador.transform, false);
        GameObject escoba = ModeloEscoba(visual.transform, largoEscoba);
        // la mano agarra a 8 cm del extremo del mango
        escoba.transform.localPosition += Vector3.back * (0.08f / Mathf.Max(visual.transform.lossyScale.z, 0.0001f));
        visual.SetActive(false);

        // 2) DAÑO: zona de golpe delante de Benja con DemoArma (clase).
        //    ControladorJugador la prende/apaga con los Animation Events de Ataque01.
        GameObject golpe = new GameObject(nombrePalo);
        golpe.transform.SetParent(jugador.transform, false);
        BoxCollider col = golpe.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 1f, 0.9f);
        col.size = new Vector3(1.4f, 1.4f, 1.4f);
        Rigidbody rb = golpe.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        DemoArma arma = ConstructorBadCarreteDay.Agregar<DemoArma>(golpe);
        arma.descripcion = "Peor es na'.";
        arma.tipo = DemoArma.Tipo.lanza;
        arma.danio = 25f;

        jugador.GetComponent<ControladorJugador>().armaActual = arma;
        // La zona de golpe siempre esta: sin escoba pega a puño limpio (menos daño)
        AtaqueJugador ataque = jugador.GetComponent<AtaqueJugador>();
        ataque.arma = arma;
        ataque.escobaVisual = visual;
        // La escoba sigue al puño real (huesos de los dedos) en todas las animaciones
        EscobaEnMano enMano = jugador.GetComponent<EscobaEnMano>();
        if (enMano == null) enMano = ConstructorBadCarreteDay.Agregar<EscobaEnMano>(jugador);
        enMano.animator = jugador.GetComponent<Animator>();
        enMano.escoba = visual.transform;
        arma.nombre = "Puño";
        arma.danio = ataque.danioPunio;
    }

    static void CrearAnimatorViejo()
    {
        AssetDatabase.DeleteAsset(rutaAnimatorViejo);
        AnimatorController.CreateAnimatorControllerAtPathWithClip(rutaAnimatorViejo, ConstructorBadCarreteDay.BuscarClip("DrunkIdle"));
    }

    // ===== ESCENA DE PRUEBA =====

    [MenuItem("Bad Carrete Day/F2 - Crear escena de prueba")]
    public static void CrearEscenaPrueba()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Object.DestroyImmediate(GameObject.Find("Main Camera"));

        // --- Mundo ---
        Cubo("Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), Material("Gris_Suelo", new Color(0.55f, 0.55f, 0.6f)));
        for (int i = 0; i < 3; i++)
            Cubo("Escalon_" + i, new Vector3(6f, 0.5f + i * 0.5f, -2f + i * 1.5f), new Vector3(2.5f, 1f + i * 1f, 1.5f), null);

        Base b = CrearBase(new Vector3(0f, 0.1f, -4f));
        ControladorJuego juego = b.juego; ControladorDinero dinero = b.dinero; ControladorMision mision = b.mision;
        ControladorHUD hud = b.hud; DemoControladorEstadisticas stats = b.stats;

        juego.vidas = 1; // escena de prueba: Game Over a la primera muerte

        // --- Mision de prueba (al completarla: VICTORIA) ---
        mision.tituloMision = "Prueba F2";
        mision.objetivos.Add(new ControladorMision.Objetivo { descripcion = "Habla con el viejo curado [E]" });
        mision.objetivos.Add(new ControladorMision.Objetivo { descripcion = "Junta 3 lucas ($300)" });
        mision.objetivos.Add(new ControladorMision.Objetivo { descripcion = "Pisa la pegatina ¡AQUÍ! y presiona [E]" });
        UnityEventTools.AddPersistentListener(mision.eventos.alCompletarMision, juego.Victoria);

        // --- NPC: viejo curado con dialogo ---
        CrearViejo(hud, mision);

        // --- Lucas: 3 monedas de $100 (una arriba de los escalones) ---
        dinero.montoObjetivo = 300;
        UnityEventTools.AddIntPersistentListener(dinero.eventos.alAlcanzarMontoObjetivo, mision.CompletarObjetivo, 1);
        CrearMoneda(new Vector3(-3f, 0.8f, 0f), dinero);
        CrearMoneda(new Vector3(-5f, 0.8f, 4f), dinero);
        CrearMoneda(new Vector3(6f, 3.8f, 1f), dinero);

        // --- Pegatina ¡AQUÍ! (accion contextual): hace aparecer una plataforma ---
        GameObject plataformaOculta = Cubo("Plataforma_Aparece", new Vector3(-8f, 1f, -6f), new Vector3(3f, 0.4f, 3f), Material("Verde_Aparece", new Color(0.3f, 0.9f, 0.5f)));
        plataformaOculta.SetActive(false);
        CrearPegatina(new Vector3(-8f, 0.02f, -2f), hud, mision, plataformaOculta);

        // --- Trampa (prueba de daño / Game Over): 34 de daño cada vez que la pisas ---
        GameObject trampa = Cubo("Trampa_Clavos", new Vector3(3f, 0.05f, -8f), new Vector3(3f, 0.1f, 3f), Material("Rojo_Trampa", new Color(0.9f, 0.15f, 0.15f)));
        ControladorColisionTrigger zonaTrampa = Trigger(trampa);
        // El cubo es muy delgado: el trigger se alarga hacia arriba (1 m) para detectar los pies
        BoxCollider colTrampa = trampa.GetComponent<BoxCollider>();
        colTrampa.size = new Vector3(1f, 10f, 1f);
        colTrampa.center = new Vector3(0f, 5f, 0f);
        UnityEventTools.AddFloatPersistentListener(zonaTrampa.onTriggerEnterEvent, stats.CalcularDanio, 34f);

        // --- Agua mineral (curacion +25) ---
        GameObject agua = Cubo("Agua_Mineral", new Vector3(0f, 0.5f, -10f), new Vector3(0.4f, 0.8f, 0.4f), Material("Celeste_Agua", new Color(0.4f, 0.8f, 1f)));
        ControladorColisionTrigger zonaAgua = Trigger(agua);
        UnityEventTools.AddFloatPersistentListener(zonaAgua.onTriggerEnterEvent, stats.Curar, 25f);
        UnityEventTools.AddBoolPersistentListener(zonaAgua.onTriggerEnterEvent, agua.SetActive, false);

        // --- Vacio bajo el nivel: daño + vuelta al checkpoint ---
        GameObject vacio = new GameObject("Zona_Vacio");
        vacio.transform.position = new Vector3(0f, -12f, 0f);
        BoxCollider colVacio = vacio.AddComponent<BoxCollider>();
        colVacio.size = new Vector3(200f, 2f, 200f);
        ControladorColisionTrigger zonaVacio = Trigger(vacio);
        UnityEventTools.AddVoidPersistentListener(zonaVacio.onTriggerEnterEvent, juego.CaerAlVacio);

        EditorSceneManager.SaveScene(escena, rutaEscenaPrueba);
        ConstructorBadCarreteDay.AgregarEscenaABuild(rutaEscenaPrueba);
    }

    // ===== BASE COMUN DE UN NIVEL: jugador + sistemas + HUD + conexiones =====

    public class Base
    {
        public GameObject checkpoint;
        public Animator animJugador;
        public DemoControladorEstadisticas stats;
        public ControladorJuego juego;
        public ControladorDinero dinero;
        public ControladorMision mision;
        public ControladorDialogo dialogo;
        public ControladorHUD hud;
        public ControladorBurbujas burbujas;
        public ControladorTienda tienda;
        public GameObject camaraPresentacion;
    }

    public static Base CrearBase(Vector3 posicionInicio)
    {
        Base b = new Base();
        GameObject checkpoint = new GameObject("Checkpoint_Inicio");
        checkpoint.transform.position = posicionInicio;

        // --- Jugador ---
        GameObject grupoJugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabJugadorAj));
        Animator animJugador = grupoJugador.GetComponentInChildren<Animator>();
        animJugador.transform.position = checkpoint.transform.position;
        DemoControladorEstadisticas stats = animJugador.GetComponent<DemoControladorEstadisticas>();

        // --- Sistemas (un objeto con los controladores generales) ---
        GameObject sistemas = new GameObject("Sistemas");
        ControladorCargaEscena carga = ConstructorBadCarreteDay.Agregar<ControladorCargaEscena>(sistemas);
        ControladorJuego juego = ConstructorBadCarreteDay.Agregar<ControladorJuego>(sistemas);
        ControladorDinero dinero = ConstructorBadCarreteDay.Agregar<ControladorDinero>(sistemas);
        ControladorMision mision = ConstructorBadCarreteDay.Agregar<ControladorMision>(sistemas);
        ControladorDialogo dialogo = ConstructorBadCarreteDay.Agregar<ControladorDialogo>(sistemas);

        juego.cargaEscena = carga;
        juego.indiceEscenaFin = IndiceEnBuild(rutaEscenaFin);
        juego.controladorMovimiento = animJugador.GetComponent<StarterAssets.ThirdPersonController>();
        juego.estadisticasJugador = stats;
        juego.animatorJugador = animJugador;
        juego.checkpointActual = checkpoint.transform;
        juego.inputPausa = ConstructorBadCarreteDay.BuscarInputActionReference("Pausa");

        dialogo.inputAvanzar = ConstructorBadCarreteDay.BuscarInputActionReference("Interaccion");

        // --- HUD ---
        ControladorHUD hud = CrearHUD(dialogo, juego);
        CrearEventSystem();

        // --- Jugador: voz, colgarse de bordes, sin planeo (se desbloquea en otros niveles) ---
        GameObject jugador = animJugador.gameObject;
        StarterAssets.ThirdPersonController tpc = jugador.GetComponent<StarterAssets.ThirdPersonController>();
        StarterAssets.StarterAssetsInputs entradas = jugador.GetComponent<StarterAssets.StarterAssetsInputs>();
        tpc.PlaneoHabilitado = false;
        juego.entradas = entradas;
        AgregarVoz(jugador, "Benja", "Benja", 1f, 1.95f);
        AgarreBorde agarre = ConstructorBadCarreteDay.Agregar<AgarreBorde>(jugador);
        agarre.movimiento = tpc;
        agarre.entradas = entradas;
        agarre.animator = animJugador;
        agarre.capasAgarrables = ~((1 << ConstructorNivel1.CapaLimite()) | (1 << 2));
        // Las animaciones de colgarse y subir (AgarreBorde mide sus medidas al empezar el juego)
        agarre.clipColgado = ConstructorBadCarreteDay.BuscarClip("BracedHang");
        agarre.clipSubir = ConstructorBadCarreteDay.BuscarClip("BracedHangToCrouch");
        juego.agarre = agarre;
        jugador.GetComponent<ReaccionGolpe>().agarre = agarre;
        jugador.GetComponent<AtaqueJugador>().agarre = agarre;
        jugador.GetComponent<EscobaEnMano>().agarre = agarre;
        jugador.GetComponent<EscobaEnMano>().movimiento = tpc;
        UnityEventTools.AddStringPersistentListener(agarre.alAgarrarPrimeraVez, hud.MostrarPensamiento, "¡Me colgué del borde! [Espacio] para subir, [S] para soltarme.");

        // --- Burbujas y conversaciones con camara ---
        hud.burbujas.jugador = jugador.transform;
        dialogo.jugador = jugador.transform;
        dialogo.sonidoCensura = ConstructorSonidos.Clip("Censura");
        CamaraDialogo camDialogo = ConstructorBadCarreteDay.Agregar<CamaraDialogo>(sistemas);
        camDialogo.camara = CamaraCinemachine("CamaraDialogo", 20, 42f);
        camDialogo.jugador = jugador.transform;
        dialogo.camaraDialogo = camDialogo;
        b.camaraPresentacion = CamaraCinemachine("CamaraPresentacion", 30, 50f);
        CinemachineBrain cerebro = Object.FindFirstObjectByType<CinemachineBrain>();
        if (cerebro != null) cerebro.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.6f);

        // --- Tienda (la arma la escena que la use) ---
        ControladorTienda tienda = ConstructorBadCarreteDay.Agregar<ControladorTienda>(sistemas);
        CrearPanelTienda(hud.transform, tienda);
        tienda.dinero = dinero;
        tienda.juego = juego;
        tienda.burbujas = hud.burbujas;
        tienda.inputComprar = ConstructorBadCarreteDay.BuscarInputActionReference("Interaccion");
        tienda.inputSalir = ConstructorBadCarreteDay.BuscarInputActionReference("Pausa");
        tienda.fuente = Fuente2D(sistemas, "Sonido_Tienda");
        tienda.sonidoMover = ConstructorSonidos.Clip("Tienda_Mover");
        tienda.sonidoComprar = ConstructorSonidos.Clip("Tienda_Comprar");
        tienda.sonidoError = ConstructorSonidos.Clip("Tienda_Error");
        b.tienda = tienda;
        b.burbujas = hud.burbujas;

        // --- Conexiones generales ---
        UnityEventTools.AddPersistentListener(stats.eventos.alCambiarSalud, hud.ActualizarSalud);
        UnityEventTools.AddPersistentListener(stats.eventos.alMorir, juego.JugadorMurio);
        UnityEventTools.AddPersistentListener(juego.eventos.alCambiarVidas, hud.ActualizarVidas);
        UnityEventTools.AddPersistentListener(dinero.eventos.alCambiarDinero, hud.ActualizarDinero);
        UnityEventTools.AddPersistentListener(mision.eventos.alCambiarObjetivo, hud.ActualizarMision);
        UnityEventTools.AddBoolPersistentListener(dialogo.eventos.alComenzar, juego.BloquearJugador, true);
        UnityEventTools.AddPersistentListener(dialogo.eventos.alComenzar, hud.OcultarPrompt);
        UnityEventTools.AddBoolPersistentListener(dialogo.eventos.alTerminar, juego.BloquearJugador, false);

        b.checkpoint = checkpoint; b.animJugador = animJugador; b.stats = stats; b.juego = juego;
        b.dinero = dinero; b.mision = mision; b.dialogo = dialogo; b.hud = hud;
        return b;
    }

    // ===== PERSONAJES Y OBJETOS =====

    public static void CrearViejo(ControladorHUD hud, ControladorMision mision)
    {
        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BadCarreteDay/Arte/Personajes/PeasantMan.fbx");
        GameObject viejo = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        viejo.name = "NPC_ViejoCurado";
        viejo.transform.position = new Vector3(3f, 0f, 6f);
        viejo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Animator anim = viejo.GetComponent<Animator>();
        if (anim == null) anim = viejo.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimatorViejo);

        CapsuleCollider cuerpo = viejo.AddComponent<CapsuleCollider>();
        cuerpo.center = new Vector3(0f, 0.9f, 0f);
        cuerpo.height = 1.8f;
        cuerpo.radius = 0.35f;

        // Lo que dice (TEXTO DE PRUEBA: los dialogos finales se escriben juntos en la F3)
        DialogoNPC dialogoNPC = ConstructorBadCarreteDay.Agregar<DialogoNPC>(viejo);
        dialogoNPC.nombrePersonaje = "Viejo curado";
        dialogoNPC.conversaciones = new DialogoNPC.Conversacion[]
        {
            new DialogoNPC.Conversacion { lineas = new string[] {
                "(Texto de prueba) ¡Eh, cabro! No me mirí, que estoy ocupado...",
                "(Texto de prueba) ¿Tu teléfono? Se lo llevó El Pastero, po.",
                "(Texto de prueba) Junta unas lucas y anda a pisar esa pegatina rosada." } },
            new DialogoNPC.Conversacion { lineas = new string[] {
                "(Texto de prueba) ¿Otra vez vo'? Ya te dije todo lo que sé." } },
        };
        UnityEventTools.AddIntPersistentListener(dialogoNPC.conversaciones[0].alTerminar, mision.CompletarObjetivo, 0);
        UnityEventTools.AddIntPersistentListener(dialogoNPC.conversaciones[0].alTerminar, dialogoNPC.CambiarConversacion, 1);

        // Zona de interaccion (DemoControlInteraccion de clase)
        GameObject zona = new GameObject("ZonaInteraccion");
        zona.transform.SetParent(viejo.transform, false);
        SphereCollider esfera = zona.AddComponent<SphereCollider>();
        esfera.isTrigger = true;
        esfera.radius = 2.5f;
        esfera.center = new Vector3(0f, 1f, 0f);

        DemoControlInteraccion interaccion = ConstructorBadCarreteDay.Agregar<DemoControlInteraccion>(zona);
        interaccion.inputInteraccion = ConstructorBadCarreteDay.BuscarInputActionReference("Interaccion");
        UnityEventTools.AddStringPersistentListener(interaccion.eventos.alEntrarDeteccion, hud.MostrarPrompt, "[E] Hablar");
        UnityEventTools.AddPersistentListener(interaccion.eventos.alSalirDeteccion, hud.OcultarPrompt);
        UnityEventTools.AddPersistentListener(interaccion.eventos.alInteractuar, dialogoNPC.IniciarDialogo);
    }

    public static void CrearMoneda(Vector3 posicion, ControladorDinero dinero)
    {
        GameObject moneda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        moneda.name = "Luca_$100";
        moneda.transform.position = posicion;
        moneda.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        moneda.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
        moneda.GetComponent<Renderer>().sharedMaterial = Material("Amarillo_Luca", new Color(1f, 0.82f, 0.1f));

        // Reemplazamos el collider por uno trigger un poco mas grande (mas facil de tomar)
        Object.DestroyImmediate(moneda.GetComponent<Collider>());
        SphereCollider col = moneda.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.2f;

        Moneda script = ConstructorBadCarreteDay.Agregar<Moneda>(moneda); // script de clase (ajustado)
        script.puntos = 100;
        script.tagBuscado = "Player";
        script.controladorDinero = dinero;
    }

    public static void CrearPegatina(Vector3 posicion, ControladorHUD hud, ControladorMision mision, GameObject plataforma)
    {
        GameObject pegatina = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pegatina.name = "Pegatina_AQUI";
        pegatina.transform.position = posicion;
        pegatina.transform.localScale = new Vector3(1.6f, 0.02f, 1.6f);
        pegatina.GetComponent<Renderer>().sharedMaterial = Material("Rosado_Pegatina", new Color(1f, 0.3f, 0.7f));
        Object.DestroyImmediate(pegatina.GetComponent<Collider>());

        // Trigger alto para que detecte al jugador parado encima
        BoxCollider col = pegatina.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1f, 100f, 1f);
        col.center = new Vector3(0f, 50f, 0f);

        DemoControlInteraccion interaccion = ConstructorBadCarreteDay.Agregar<DemoControlInteraccion>(pegatina);
        interaccion.inputInteraccion = ConstructorBadCarreteDay.BuscarInputActionReference("Interaccion");
        UnityEventTools.AddStringPersistentListener(interaccion.eventos.alEntrarDeteccion, hud.MostrarPrompt, "[E] Tirar la palanca");
        UnityEventTools.AddPersistentListener(interaccion.eventos.alSalirDeteccion, hud.OcultarPrompt);
        UnityEventTools.AddBoolPersistentListener(interaccion.eventos.alInteractuar, plataforma.SetActive, true);
        UnityEventTools.AddIntPersistentListener(interaccion.eventos.alInteractuar, mision.CompletarObjetivo, 2);
        UnityEventTools.AddPersistentListener(interaccion.eventos.alInteractuar, hud.OcultarPrompt);
        UnityEventTools.AddBoolPersistentListener(interaccion.eventos.alInteractuar, pegatina.SetActive, false);
    }

    // ===== HUD (Canvas) =====

    public static ControladorHUD CrearHUD(ControladorDialogo dialogo, ControladorJuego juego)
    {
        GameObject canvas = CrearCanvas("HUD");
        ControladorHUD hud = ConstructorBadCarreteDay.Agregar<ControladorHUD>(canvas);

        // Salud: barra de clase arriba a la izquierda + numero
        GameObject barra = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(rutaBarraSalud), canvas.transform);
        RectTransform rtBarra = (RectTransform)barra.transform;
        rtBarra.anchoredPosition = new Vector2(170f, -60f);
        hud.barraSalud = barra.GetComponent<ControladorBarraSalud>();
        hud.textoSalud = Texto(canvas.transform, "TextoSalud", "100", new Vector2(0f, 1f), new Vector2(330f, -60f), new Vector2(120f, 50f), 36, TextAlignmentOptions.Left);

        hud.textoVidas = Texto(canvas.transform, "TextoVidas", "Vidas: 3", new Vector2(0f, 1f), new Vector2(130f, -120f), new Vector2(220f, 40f), 28, TextAlignmentOptions.Left);

        // Lucas arriba a la derecha
        hud.textoDinero = Texto(canvas.transform, "TextoDinero", "$0", new Vector2(1f, 1f), new Vector2(-120f, -60f), new Vector2(220f, 70f), 56, TextAlignmentOptions.Right);
        hud.textoDinero.color = new Color(1f, 0.85f, 0.2f);

        // Mision arriba al centro, en un panel oscuro con borde amarillo (se lee sobre cualquier fondo)
        GameObject panelMision = Panel(canvas.transform, "PanelMision", new Color(0.05f, 0.04f, 0.12f, 0.85f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1000f, 96f));
        Outline borde = panelMision.AddComponent<Outline>();
        borde.effectColor = new Color(1f, 0.82f, 0.2f);
        borde.effectDistance = new Vector2(3f, -3f);
        GameObject icono = Panel(panelMision.transform, "IconoMision", new Color(1f, 0.82f, 0.2f), new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(60f, 60f));
        icono.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Texto(icono.transform, "Exclamacion", "!", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f), 44, TextAlignmentOptions.Center).color = new Color(0.1f, 0.05f, 0.2f);
        Texto(panelMision.transform, "TituloMision", "MISIÓN", new Vector2(0f, 1f), new Vector2(160f, -18f), new Vector2(200f, 30f), 20, TextAlignmentOptions.Left).color = new Color(1f, 0.82f, 0.2f);
        hud.textoMision = Texto(panelMision.transform, "TextoMision", "", new Vector2(0.5f, 0.5f), new Vector2(40f, -10f), new Vector2(880f, 56f), 32, TextAlignmentOptions.Left);
        hud.textoMision.enableAutoSizing = true;
        hud.textoMision.fontSizeMin = 20f;
        hud.textoMision.fontSizeMax = 32f;
        hud.panelMision = (RectTransform)panelMision.transform;
        hud.sonidoMision = Fuente2D(canvas, "Sonido_Mision");
        hud.sonidoMision.clip = ConstructorSonidos.Clip("Mision_Nueva");

        // Titulo grande (presentacion de personajes, ej: "EL PASTERO")
        hud.textoTitulo = Texto(canvas.transform, "TextoTitulo", "", new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1600f, 160f), 120, TextAlignmentOptions.Center);
        hud.textoTitulo.color = new Color(1f, 0.35f, 0.4f);
        hud.textoTitulo.outlineWidth = 0.25f;
        hud.textoTitulo.outlineColor = new Color32(20, 5, 30, 255);

        // Prompt de accion contextual abajo al centro
        GameObject prompt = Panel(canvas.transform, "PanelPrompt", new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 260f), new Vector2(520f, 70f));
        hud.panelPrompt = prompt;
        hud.textoPrompt = Texto(prompt.transform, "TextoPrompt", "[E]", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 60f), 34, TextAlignmentOptions.Center);

        // Pensamientos de Benja (arriba del prompt, en cursiva)
        GameObject pensamiento = Panel(canvas.transform, "PanelPensamiento", new Color(1f, 1f, 1f, 0.85f), new Vector2(0.5f, 0f), new Vector2(0f, 360f), new Vector2(1000f, 70f));
        hud.panelPensamiento = pensamiento;
        hud.textoPensamiento = Texto(pensamiento.transform, "TextoPensamiento", "...", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 60f), 30, TextAlignmentOptions.Center);
        hud.textoPensamiento.color = new Color(0.15f, 0.1f, 0.25f);
        hud.textoPensamiento.fontStyle = FontStyles.Italic;

        // Conversaciones: burbuja de comic sobre la cabeza de quien habla (estilo Conker)
        Burbuja conversacion = CrearBurbuja(canvas.transform, "PanelDialogo", false, new Vector2(680f, 170f), 30);
        dialogo.panelDialogo = conversacion.gameObject;
        dialogo.textoNombre = conversacion.nombre;
        dialogo.textoNombre.name = "TextoNombre";
        dialogo.textoDialogo = conversacion.texto;
        dialogo.textoDialogo.name = "TextoDialogo";
        dialogo.burbuja = conversacion;

        // Frases sueltas (3 burbujas) y la nube de pensamiento de Benja
        ControladorBurbujas burbujas = ConstructorBadCarreteDay.Agregar<ControladorBurbujas>(canvas);
        burbujas.burbujasSueltas = new Burbuja[3];
        for (int i = 0; i < 3; i++)
            burbujas.burbujasSueltas[i] = CrearBurbuja(canvas.transform, "BurbujaSuelta_" + i, false, new Vector2(460f, 110f), 26);
        burbujas.nubePensamiento = CrearBurbuja(canvas.transform, "NubePensamiento", true, new Vector2(560f, 110f), 26);
        hud.burbujas = burbujas;

        // Fundido a negro (cuando el jugador se sale del mapa)
        GameObject fundido = Panel(canvas.transform, "Fundido", Color.black, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f));
        fundido.GetComponent<Image>().raycastTarget = false;

        // Menu de pausa
        GameObject pausa = Panel(canvas.transform, "PanelPausa", new Color(0f, 0f, 0f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f));
        juego.panelPausa = pausa;
        Texto(pausa.transform, "TextoPausa", "PAUSA", new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(600f, 120f), 90, TextAlignmentOptions.Center);
        Boton(pausa.transform, "BotonReanudar", "Seguir", new Vector2(0f, 0f), juego.Reanudar);
        GameObject panelConfig = ConstructorF6.PanelConfiguracion(canvas.transform);
        juego.panelConfiguracion = panelConfig;
        ConstructorF6.BotonBool(pausa.transform, "BotonConfiguracion", "Configuración", new Vector2(0f, -110f), panelConfig.SetActive, true);
        Boton(pausa.transform, "BotonMenu", "Menú", new Vector2(0f, -220f), juego.IrAlMenu);

        // Fuente Luckiest Guy (F5), menos en los textos largos que deben leerse facil
        ConstructorF5.AplicarFuente(canvas, "TextoDialogo", "TextoPensamiento", "TextoBurbuja", "TextoDescripcion");

        return hud;
    }

    // Globo de comic: fondo blanco redondeado con borde negro, colita (o circulitos si es "nube"),
    // nombre arriba y texto oscuro. El componente Burbuja lo ubica sobre la cabeza del personaje.
    public static Burbuja CrearBurbuja(Transform padre, string nombre, bool nube, Vector2 tamanio, float tamanioFuente)
    {
        Sprite redondo = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite circulo = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        GameObject obj = Panel(padre, nombre, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, tamanio);
        RectTransform rt = (RectTransform)obj.transform;
        rt.pivot = new Vector2(0.5f, 0f);
        Image fondo = obj.GetComponent<Image>();
        fondo.sprite = redondo;
        fondo.type = Image.Type.Sliced;
        fondo.raycastTarget = false;
        Outline borde = obj.AddComponent<Outline>();
        borde.effectColor = new Color(0.08f, 0.05f, 0.12f);
        borde.effectDistance = new Vector2(3f, -3f);

        if (nube)
        {
            // Circulitos de pensamiento hacia la cabeza
            foreach (Vector3 c in new[] { new Vector3(-30f, -26f, 26f), new Vector3(-48f, -52f, 16f) })
            {
                GameObject bolita = Panel(obj.transform, "Bolita", Color.white, new Vector2(0.5f, 0f), new Vector2(c.x, c.y), new Vector2(c.z, c.z));
                bolita.GetComponent<Image>().sprite = circulo;
                bolita.GetComponent<Image>().raycastTarget = false;
                bolita.AddComponent<Outline>().effectColor = new Color(0.08f, 0.05f, 0.12f);
            }
        }
        else
        {
            // Colita apuntando hacia abajo (cuadrado girado 45 grados)
            GameObject colita = Panel(obj.transform, "Colita", Color.white, new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(34f, 34f));
            colita.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            colita.transform.SetAsFirstSibling();
            colita.GetComponent<Image>().raycastTarget = false;
            colita.AddComponent<Outline>().effectColor = new Color(0.08f, 0.05f, 0.12f);
        }

        Burbuja b = ConstructorBadCarreteDay.Agregar<Burbuja>(obj);
        b.texto = Texto(obj.transform, "TextoBurbuja", "...", new Vector2(0.5f, 0.5f), new Vector2(0f, nube ? 0f : -6f), tamanio - new Vector2(44f, nube ? 24f : 44f), tamanioFuente, nube ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft);
        b.texto.color = new Color(0.12f, 0.08f, 0.2f);
        b.texto.enableAutoSizing = true;
        b.texto.fontSizeMin = 18f;
        b.texto.fontSizeMax = tamanioFuente;
        if (nube) b.texto.fontStyle = FontStyles.Italic;
        if (!nube)
        {
            b.nombre = Texto(obj.transform, "NombreBurbuja", "", new Vector2(0f, 1f), new Vector2(110f, 6f), new Vector2(260f, 40f), 28, TextAlignmentOptions.Left);
            b.nombre.color = new Color(1f, 0.45f, 0.75f);
            b.nombre.outlineWidth = 0.25f;
            b.nombre.outlineColor = new Color32(20, 5, 30, 255);
            TMP_Text avanzar = Texto(obj.transform, "TextoAvanzar", "[E] ▸", new Vector2(1f, 0f), new Vector2(-50f, 18f), new Vector2(90f, 30f), 22, TextAlignmentOptions.Right);
            avanzar.text = "[E] >";
            avanzar.color = new Color(0.85f, 0.25f, 0.55f);
            b.indicadorAvanzar = avanzar.gameObject;
        }
        obj.SetActive(false);
        return b;
    }

    // Tienda tipo Zelda/Pokemon: titulo, plata, 3 filas (icono, nombre, precio), descripcion y controles
    static void CrearPanelTienda(Transform canvas, ControladorTienda tienda)
    {
        GameObject panel = Panel(canvas, "PanelTienda", new Color(0.06f, 0.04f, 0.14f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(380f, 0f), new Vector2(900f, 720f));
        Outline borde = panel.AddComponent<Outline>();
        borde.effectColor = new Color(1f, 0.82f, 0.2f);
        borde.effectDistance = new Vector2(4f, -4f);
        tienda.panel = panel;
        tienda.textoTitulo = Texto(panel.transform, "TituloTienda", "ALMACÉN DOÑA MARTA", new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(860f, 70f), 46, TextAlignmentOptions.Center);
        tienda.textoTitulo.color = new Color(1f, 0.82f, 0.2f);
        tienda.textoDinero = Texto(panel.transform, "DineroTienda", "Tienes: $0", new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(860f, 40f), 28, TextAlignmentOptions.Center);
        int filas = 3;
        tienda.filas = new Button[filas];
        tienda.iconos = new Image[filas];
        tienda.nombres = new TMP_Text[filas];
        tienda.precios = new TMP_Text[filas];
        for (int i = 0; i < filas; i++)
        {
            GameObject fila = Panel(panel.transform, "Fila_" + i, new Color(1f, 1f, 1f, 0.08f), new Vector2(0.5f, 1f), new Vector2(0f, -195f - i * 115f), new Vector2(820f, 100f));
            tienda.filas[i] = fila.AddComponent<Button>();
            GameObject icono = Panel(fila.transform, "Icono", Color.white, new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(76f, 76f));
            icono.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            icono.GetComponent<Image>().type = Image.Type.Sliced;
            icono.GetComponent<Image>().raycastTarget = false;
            tienda.iconos[i] = icono.GetComponent<Image>();
            tienda.nombres[i] = Texto(fila.transform, "Nombre", "Producto", new Vector2(0f, 0.5f), new Vector2(360f, 0f), new Vector2(520f, 60f), 36, TextAlignmentOptions.Left);
            tienda.precios[i] = Texto(fila.transform, "Precio", "$0", new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(220f, 60f), 36, TextAlignmentOptions.Right);
        }
        GameObject caja = Panel(panel.transform, "CajaDescripcion", new Color(0f, 0f, 0f, 0.35f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(820f, 100f));
        tienda.textoDescripcion = Texto(caja.transform, "TextoDescripcion", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(790f, 96f), 26, TextAlignmentOptions.Center);
        Texto(panel.transform, "Controles", "[W/S] elegir   [E] comprar   [Esc] salir", new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(860f, 40f), 24, TextAlignmentOptions.Center).color = new Color(1f, 1f, 1f, 0.7f);
    }

    // Camara Cinemachine extra (dialogo, presentaciones). Apagada: se prende cuando se usa.
    static GameObject CamaraCinemachine(string nombre, int prioridad, float campoVision)
    {
        GameObject obj = new GameObject(nombre);
        CinemachineCamera cam = obj.AddComponent<CinemachineCamera>();
        cam.Priority.Enabled = true;
        cam.Priority.Value = prioridad;
        LensSettings lente = cam.Lens;
        lente.FieldOfView = campoVision;
        lente.NearClipPlane = 0.1f;
        cam.Lens = lente;
        obj.SetActive(false);
        return obj;
    }

    // Voz (murmullo) de un personaje
    public static Voz AgregarVoz(GameObject personaje, string nombre, string voz, float tono, float alturaCabeza)
    {
        Voz v = ConstructorBadCarreteDay.Agregar<Voz>(personaje);
        v.nombre = nombre;
        v.silabas = ConstructorSonidos.Silabas(voz);
        v.tono = tono;
        v.alturaCabeza = alturaCabeza;
        return v;
    }

    public static AudioSource Fuente2D(GameObject obj, string nombre)
    {
        GameObject hijo = new GameObject(nombre);
        hijo.transform.SetParent(obj.transform, false);
        AudioSource a = hijo.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 0f;
        return a;
    }

    // ===== ESCENA FIN (minima) =====

    [MenuItem("Bad Carrete Day/F2 - Crear escena Fin")]
    public static void CrearEscenaFin()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.1f, 0.06f, 0.2f);

        GameObject sistemas = new GameObject("Sistemas");
        ControladorCargaEscena carga = ConstructorBadCarreteDay.Agregar<ControladorCargaEscena>(sistemas);
        PantallaFin pantalla = ConstructorBadCarreteDay.Agregar<PantallaFin>(sistemas);
        pantalla.cargaEscena = carga;

        GameObject canvas = CrearCanvas("CanvasFin");

        GameObject victoria = Panel(canvas.transform, "PanelVictoria", new Color(0f, 0f, 0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 900f));
        Texto(victoria.transform, "Titulo", "¡RECUPERASTE EL TELÉFONO!", new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1500f, 140f), 80, TextAlignmentOptions.Center);
        Texto(victoria.transform, "Subtitulo", "(Pantalla de prueba — el final de verdad llega en la F6)", new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1500f, 60f), 30, TextAlignmentOptions.Center);
        pantalla.panelVictoria = victoria;

        GameObject derrota = Panel(canvas.transform, "PanelDerrota", new Color(0f, 0f, 0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 900f));
        Texto(derrota.transform, "Titulo", "TE DEJARON PATO", new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1500f, 140f), 90, TextAlignmentOptions.Center).color = new Color(1f, 0.3f, 0.3f);
        Texto(derrota.transform, "Subtitulo", "Game Over", new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1500f, 60f), 36, TextAlignmentOptions.Center);
        pantalla.panelDerrota = derrota;

        Boton(canvas.transform, "BotonReintentar", "Reintentar", new Vector2(0f, -80f), pantalla.Reintentar);
        Boton(canvas.transform, "BotonMenu", "Menú", new Vector2(0f, -190f), pantalla.IrAlMenu);

        CrearEventSystem();
        ConstructorF5.AplicarFuente(canvas);

        EditorSceneManager.SaveScene(escena, rutaEscenaFin);
        ConstructorBadCarreteDay.AgregarEscenaABuild(rutaEscenaFin);
    }

    // ===== UTILIDADES =====

    public static GameObject Cubo(string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        cubo.transform.position = posicion;
        cubo.transform.localScale = escala;
        if (material != null) cubo.GetComponent<Renderer>().sharedMaterial = material;
        return cubo;
    }

    // Convierte el collider del objeto en trigger y le agrega ControladorColisionTrigger (clase)
    public static ControladorColisionTrigger Trigger(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        col.isTrigger = true;
        ControladorColisionTrigger trigger = ConstructorBadCarreteDay.Agregar<ControladorColisionTrigger>(obj);
        trigger.tagEsperado = "Player";
        return trigger;
    }

    public static Material Material(string nombre, Color color)
    {
        ConstructorBadCarreteDay.CrearCarpeta("Assets/BadCarreteDay/Materiales");
        string ruta = "Assets/BadCarreteDay/Materiales/" + nombre + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static GameObject CrearCanvas(string nombre)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler escalador = obj.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        obj.AddComponent<GraphicRaycaster>();
        return obj;
    }

    public static void CrearEventSystem()
    {
        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<InputSystemUIInputModule>();
    }

    public static TMP_Text Texto(Transform padre, string nombre, string texto, Vector2 ancla, Vector2 posicion, Vector2 tamanio, float tamanioFuente, TextAlignmentOptions alineacion)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamanio;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamanioFuente;
        tmp.alignment = alineacion;
        tmp.color = Color.white;
        return tmp;
    }

    public static GameObject Panel(Transform padre, string nombre, Color color, Vector2 ancla, Vector2 posicion, Vector2 tamanio)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamanio;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0.01f;
        return obj;
    }

    public static void Boton(Transform padre, string nombre, string texto, Vector2 posicion, UnityEngine.Events.UnityAction accion)
    {
        GameObject obj = Panel(padre, nombre, new Color(1f, 0.35f, 0.7f), new Vector2(0.5f, 0.5f), posicion, new Vector2(360f, 90f));
        Button boton = obj.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(boton.onClick, accion);
        Texto(obj.transform, "Texto", texto, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 80f), 40, TextAlignmentOptions.Center);
    }

    public static int IndiceEnBuild(string ruta)
    {
        EditorBuildSettingsScene[] escenas = EditorBuildSettings.scenes;
        for (int i = 0; i < escenas.Length; i++)
            if (escenas[i].path == ruta) return i;
        return 0;
    }
}
