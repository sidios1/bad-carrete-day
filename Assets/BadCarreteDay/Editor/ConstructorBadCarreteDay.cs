using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Herramienta de EDITOR (no va en el juego compilado).
// Arma automaticamente assets y escenas de prueba para que el proyecto
// se pueda reconstruir igual en cualquier computador.
// Se usa desde el menu "Bad Carrete Day" de Unity o por linea de comandos:
//   Unity -batchmode -projectPath ... -executeMethod ConstructorBadCarreteDay.ConstruirF0 -quit
public static class ConstructorBadCarreteDay
{
    // RUTAS
    const string rutaAnimatorStarter = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
    public const string rutaAnimatorJugador = "Assets/BadCarreteDay/Animaciones/AnimatorJugador.controller";
    const string rutaAnimacionAtaque = "Assets/Animaciones/Personajes/Combate/Ataque01.anim";
    public const string rutaPrefabJugador = "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/NestedParentArmature_Unpack.prefab";
    const string rutaInputs = "Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions";
    const string rutaEscenaPrueba = "Assets/BadCarreteDay/Escenas/Prueba_F0.unity";

    [MenuItem("Bad Carrete Day/F0 - Construir todo (Animator + escena de prueba)")]
    public static void ConstruirF0()
    {
        CrearAnimatorJugador();
        CrearEscenaPrueba();
        Debug.Log("[BadCarreteDay] F0 construida ✅");
    }

    // Copia el Animator de Starter Assets y le agrega la misma estructura
    // que tenia el Animator de clase (StarterAssetsThirdPerson DEMO):
    // parametros de idle random + trigger "ataque" desde Any State.
    [MenuItem("Bad Carrete Day/F0 - Crear Animator del jugador")]
    public static void CrearAnimatorJugador()
    {
        CrearCarpeta("Assets/BadCarreteDay/Animaciones");

        // Se reconstruye limpio copiando el de Starter Assets ENCIMA del archivo existente.
        // (No se borra el asset: asi conserva su GUID y los prefabs no pierden la referencia)
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimatorJugador) == null)
        {
            AssetDatabase.CopyAsset(rutaAnimatorStarter, rutaAnimatorJugador);
        }
        else
        {
            System.IO.File.Copy(rutaAnimatorStarter, rutaAnimatorJugador, true);
            AssetDatabase.ImportAsset(rutaAnimatorJugador, ImportAssetOptions.ForceUpdate);
        }

        AnimatorController animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimatorJugador);

        // Parametros que usan los scripts de clase
        AgregarParametro(animator, "tiempoIdle", AnimatorControllerParameterType.Float);   // ControladorRandomIdle
        AgregarParametro(animator, "estaIdle", AnimatorControllerParameterType.Bool);      // ControladorRandomIdle
        AgregarParametro(animator, "randomIdle", AnimatorControllerParameterType.Int);     // ControladorRandomIdle
        AgregarParametro(animator, "interrumpeIdle", AnimatorControllerParameterType.Trigger); // ControladorRandomIdle
        AgregarParametro(animator, "ataque", AnimatorControllerParameterType.Trigger);     // ControladorJugador
        AgregarParametro(animator, "Planeo", AnimatorControllerParameterType.Bool);        // ThirdPersonController (planeo)

        AnimatorStateMachine maquina = animator.layers[0].stateMachine;

        // IDLES RANDOM DE CAÑA (misma logica que el Animator de clase):
        // ControladorRandomIdle pone estaIdle = true y un randomIdle al azar.
        // randomIdle 1 = bostezo, 2 = hipo de curado. (0 = no hace nada)
        string[] clipsIdle = { "Yawn", "DrunkIdleHipo" };
        for (int i = 0; i < clipsIdle.Length; i++)
        {
            AnimationClip clip = BuscarClip(clipsIdle[i]);
            if (clip == null) continue;

            AnimatorState estadoIdle = maquina.AddState("IdleRandom_" + clipsIdle[i], new Vector3(500f, 400f + i * 70f, 0f));
            estadoIdle.motion = clip;

            // Idle normal -> idle random
            AnimatorStateTransition entrar = maquina.defaultState.AddTransition(estadoIdle);
            entrar.AddCondition(AnimatorConditionMode.If, 0f, "estaIdle");
            entrar.AddCondition(AnimatorConditionMode.Equals, i + 1, "randomIdle");
            entrar.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            entrar.hasExitTime = false;
            entrar.duration = 0.25f;

            // Vuelve solo al terminar el clip...
            AnimatorStateTransition terminar = estadoIdle.AddTransition(maquina.defaultState);
            terminar.hasExitTime = true;
            terminar.exitTime = 0.95f;
            terminar.duration = 0.25f;

            // ... o si el jugador se empieza a mover (interrumpeIdle)
            AnimatorStateTransition interrumpir = estadoIdle.AddTransition(maquina.defaultState);
            interrumpir.AddCondition(AnimatorConditionMode.If, 0f, "interrumpeIdle");
            interrumpir.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            interrumpir.hasExitTime = false;
            interrumpir.duration = 0.15f;
        }

        // Estado de ataque con la animacion de clase (trae los Animation Events
        // ActivaArma / DesactivaArma que llaman a ControladorJugador)
        AnimationClip clipAtaque = AssetDatabase.LoadAssetAtPath<AnimationClip>(rutaAnimacionAtaque);
        AnimatorState estadoAtaque = maquina.AddState("Ataque01", new Vector3(500f, 300f, 0f));
        estadoAtaque.motion = clipAtaque;

        // Any State -> Ataque01 cuando se dispara el trigger "ataque" (igual que en clase)
        AnimatorStateTransition haciaAtaque = maquina.AddAnyStateTransition(estadoAtaque);
        haciaAtaque.AddCondition(AnimatorConditionMode.If, 0f, "ataque");
        haciaAtaque.duration = 0.1f;
        haciaAtaque.hasExitTime = false;
        haciaAtaque.canTransitionToSelf = false;

        // Al terminar el ataque vuelve al estado por defecto (Idle Walk Run Blend)
        AnimatorStateTransition volver = estadoAtaque.AddTransition(maquina.defaultState);
        volver.hasExitTime = true;
        volver.exitTime = 0.9f;
        volver.duration = 0.15f;

        // MUERTE: Any State -> Dying con el trigger "muerte" (lo dispara ControladorJuego.GameOver)
        AgregarParametro(animator, "muerte", AnimatorControllerParameterType.Trigger);
        AnimationClip clipMuerte = BuscarClip("Dying");
        if (clipMuerte != null)
        {
            AnimatorState estadoMuerte = maquina.AddState("Dying", new Vector3(750f, 300f, 0f));
            estadoMuerte.motion = clipMuerte;
            AnimatorStateTransition haciaMuerte = maquina.AddAnyStateTransition(estadoMuerte);
            haciaMuerte.AddCondition(AnimatorConditionMode.If, 0f, "muerte");
            haciaMuerte.duration = 0.1f;
            haciaMuerte.canTransitionToSelf = false;
            // Sin transicion de salida: queda tirado en el suelo
        }

        // GOLPEADO: Any State -> HitReaction con el trigger "golpeado" y vuelve
        AgregarParametro(animator, "golpeado", AnimatorControllerParameterType.Trigger);
        AnimationClip clipGolpe = BuscarClip("HitReaction");
        if (clipGolpe != null)
        {
            AnimatorState estadoGolpe = maquina.AddState("HitReaction", new Vector3(750f, 400f, 0f));
            estadoGolpe.motion = clipGolpe;
            estadoGolpe.speed = 1.5f;
            AnimatorStateTransition haciaGolpe = maquina.AddAnyStateTransition(estadoGolpe);
            haciaGolpe.AddCondition(AnimatorConditionMode.If, 0f, "golpeado");
            haciaGolpe.duration = 0.05f;
            haciaGolpe.canTransitionToSelf = false;
            AnimatorStateTransition volverGolpe = estadoGolpe.AddTransition(maquina.defaultState);
            volverGolpe.hasExitTime = true;
            volverGolpe.exitTime = 0.8f;
            volverGolpe.duration = 0.15f;
        }

        // Estados de Starter Assets que se usan para conectar los nuevos
        AnimatorState locomocion = maquina.defaultState;
        AnimatorState saltoInicio = EstadoPorNombre(maquina, "JumpStart");
        AnimatorState enElAire = EstadoPorNombre(maquina, "InAir");
        AnimatorState aterrizaje = EstadoPorNombre(maquina, "JumpLand");

        // El arma se apaga SIEMPRE al salir del ataque (aunque lo corten)
        estadoAtaque.AddStateMachineBehaviour<ApagarArmaAlSalir>();

        // CAÑA (Resaca): con "Curado" usa un blend tree propio sobre Speed:
        // quieto = DrunkIdle (se tambalea), caminando a 1 m/s = DrunkWalk acelerado para que los pies no patinen
        AgregarParametro(animator, "Curado", AnimatorControllerParameterType.Bool);
        AnimationClip clipCuradoQuieto = BuscarClip("DrunkIdle");
        AnimationClip clipCuradoCamina = BuscarClip("DrunkWalk");
        AnimatorState curado = null;
        if (clipCuradoQuieto != null && clipCuradoCamina != null)
        {
            BlendTree arbol;
            curado = animator.CreateBlendTreeInController("Curado", out arbol, 0);
            curado.speedParameterActive = false;
            arbol.blendParameter = "Speed";
            arbol.useAutomaticThresholds = false;
            float velocidadClip = VelocidadDelClip(clipCuradoCamina, 0.68f);
            arbol.AddChild(clipCuradoQuieto, 0f);
            arbol.AddChild(clipCuradoCamina, 1f);
            ChildMotion[] hijos = arbol.children;
            hijos[1].timeScale = 1f / velocidadClip; // a 1 m/s el clip avanza lo mismo que el jugador
            arbol.children = hijos;
            Debug.Log("[Animator] DrunkWalk avanza " + velocidadClip.ToString("F2") + " m/s -> se acelera x" + (1f / velocidadClip).ToString("F2"));

            Transicion(locomocion, curado, 0.2f, ("Curado", true), ("Grounded", true));
            if (saltoInicio != null) Transicion(curado, saltoInicio, 0.07f, ("Jump", true));
            if (enElAire != null) Transicion(curado, enElAire, 0.1f, ("FreeFall", true));
            Transicion(curado, locomocion, 0.25f, ("Curado", false));
            // Al aterrizar con caña vuelve directo a "Curado" (sin pasar por la caminata normal)
            if (aterrizaje != null)
            {
                AnimatorStateTransition t = aterrizaje.AddTransition(curado);
                t.AddCondition(AnimatorConditionMode.If, 0f, "Curado");
                t.hasExitTime = true; t.exitTime = 0.4f; t.duration = 0.3f;
                // que quede primero (tiene prioridad sobre la vuelta a locomocion normal)
                AnimatorStateTransition[] lista = aterrizaje.transitions;
                System.Array.Copy(lista, 0, lista, 1, lista.Length - 1);
                lista[0] = t;
                aterrizaje.transitions = lista;
            }
        }

        // COLGARSE DEL BORDE Y SUBIR (AgarreBorde): bool "Colgado" y trigger "Subir".
        // Se entra solo desde el salto o la caida (no desde "Any State": asi un golpe o un ataque
        // no lo devuelven al borde).
        AgregarParametro(animator, "Colgado", AnimatorControllerParameterType.Bool);
        AgregarParametro(animator, "Subir", AnimatorControllerParameterType.Trigger);
        AnimationClip clipColgado = BuscarClip("BracedHang");
        AnimationClip clipSubir = BuscarClip("BracedHangToCrouch");
        if (clipColgado != null && clipSubir != null)
        {
            AnimatorState colgado = maquina.AddState("Colgado", new Vector3(1000f, 300f, 0f));
            colgado.motion = clipColgado;
            AnimatorState subir = maquina.AddState("SubirBorde", new Vector3(1000f, 400f, 0f));
            subir.motion = clipSubir; // a su velocidad natural (AgarreBorde espera lo que dura el clip)

            foreach (AnimatorState desde in new[] { saltoInicio, enElAire, aterrizaje, locomocion, curado })
                if (desde != null) Transicion(desde, colgado, 0.15f, ("Colgado", true));
            // Primero la de subir (tiene prioridad), despues la de soltarse (cae)
            Transicion(colgado, subir, 0.15f, ("Subir", true));
            Transicion(colgado, enElAire != null ? enElAire : locomocion, 0.15f, ("Colgado", false));
            AnimatorStateTransition terminarSubida = subir.AddTransition(locomocion);
            terminarSubida.hasExitTime = true;
            terminarSubida.exitTime = 0.97f;
            terminarSubida.duration = 0.1f;
        }

        // Capa "Escoba": con la escoba en la mano, Benja cierra el PUÑO (los dedos de la guardia de pelea);
        // el brazo sigue abajo, normal. El peso lo maneja AtaqueJugador (0 sin escoba, al pegar o colgado).
        string rutaMascara = "Assets/BadCarreteDay/Animaciones/Mascara_Puno.mask";
        AssetDatabase.DeleteAsset("Assets/BadCarreteDay/Animaciones/Mascara_Brazos.mask");
        AssetDatabase.DeleteAsset(rutaMascara);
        AvatarMask mascara = new AvatarMask();
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mascara.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
        foreach (AvatarMaskBodyPart parte in new[] { AvatarMaskBodyPart.RightFingers })
            mascara.SetHumanoidBodyPartActive(parte, true);
        AssetDatabase.CreateAsset(mascara, rutaMascara);
        AnimatorStateMachine maquinaEscoba = new AnimatorStateMachine { name = "Escoba", hideFlags = HideFlags.HideInHierarchy };
        AssetDatabase.AddObjectToAsset(maquinaEscoba, animator);
        AnimatorState guardia = maquinaEscoba.AddState("Guardia");
        guardia.motion = BuscarClip("FightingIdle");
        maquinaEscoba.defaultState = guardia;
        animator.AddLayer(new AnimatorControllerLayer { name = "Escoba", defaultWeight = 0f, avatarMask = mascara, blendingMode = AnimatorLayerBlendingMode.Override, stateMachine = maquinaEscoba });

        EditorUtility.SetDirty(animator);
        AssetDatabase.SaveAssets();
    }

    // Escena simple para probar: suelo, escalones, luz y el jugador de Starter Assets
    // con el Animator del juego y el ControladorJugador de clase.
    [MenuItem("Bad Carrete Day/F0 - Crear escena de prueba")]
    public static void CrearEscenaPrueba()
    {
        CrearCarpeta("Assets/BadCarreteDay/Escenas");

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Suelo
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        suelo.name = "Suelo";
        suelo.transform.position = new Vector3(0f, -0.5f, 0f);
        suelo.transform.localScale = new Vector3(40f, 1f, 40f);

        // Escalones y plataformas para probar el salto
        for (int i = 0; i < 4; i++)
        {
            GameObject escalon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            escalon.name = "Escalon_" + i;
            escalon.transform.position = new Vector3(4f, 0.25f + i * 0.5f, 3f + i * 1.5f);
            escalon.transform.localScale = new Vector3(3f, 0.5f + i * 1f, 1.5f);
        }

        // Jugador (prefab de Starter Assets que ya trae la camara Cinemachine conectada)
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabJugador);
        GameObject grupoJugador = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        // La escena por defecto trae una Main Camera: la quitamos porque el prefab trae la suya
        foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.transform.root != grupoJugador.transform)
                Object.DestroyImmediate(cam.gameObject);
        }

        ConfigurarJugador(grupoJugador);

        EditorSceneManager.SaveScene(escena, rutaEscenaPrueba);
        AgregarEscenaABuild(rutaEscenaPrueba);
    }

    // Asigna el Animator del juego y agrega ControladorJugador (script de clase)
    static void ConfigurarJugador(GameObject grupoJugador)
    {
        Animator animator = grupoJugador.GetComponentInChildren<Animator>();
        GameObject jugador = animator.gameObject;

        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimatorJugador);

        ControladorJugador controlador = jugador.GetComponent<ControladorJugador>();
        if (controlador == null)
            controlador = jugador.AddComponent<ControladorJugador>();

        controlador.inputAtaque = BuscarInputActionReference("Ataque");
    }

    // ===== Utilidades (publicas para usarlas desde ConstructorF1, etc.) =====

    // AddComponent + deja creadas todas sus UnityEvents (si no, quedan en null
    // hasta que Unity serializa el objeto y no se les pueden agregar listeners)
    public static T Agregar<T>(GameObject obj) where T : Component
    {
        T componente = obj.AddComponent<T>();
        InicializarEventos(componente);
        return componente;
    }

    public static void InicializarEventos(object objeto, int profundidad = 0)
    {
        if (objeto == null || profundidad > 3) return;
        var banderas = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        foreach (var campo in objeto.GetType().GetFields(banderas))
        {
            System.Type tipo = campo.FieldType;
            bool serializable = campo.IsPublic || campo.IsDefined(typeof(SerializeField), false);
            if (!serializable) continue;

            if (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(tipo))
            {
                if (campo.GetValue(objeto) == null)
                    campo.SetValue(objeto, System.Activator.CreateInstance(tipo));
            }
            // Structs serializables (ej: "Eventos" de los scripts de clase): se inicializan por dentro
            else if (tipo.IsValueType && !tipo.IsPrimitive && !tipo.IsEnum && tipo.IsDefined(typeof(System.SerializableAttribute), false))
            {
                object valor = campo.GetValue(objeto);
                InicializarEventos(valor, profundidad + 1);
                campo.SetValue(objeto, valor);
            }
        }
    }

    // Busca un clip de animacion importado desde Assets/BadCarreteDay/Arte/Animaciones/<nombre>.fbx
    static AnimatorState EstadoPorNombre(AnimatorStateMachine maquina, string nombre)
    {
        foreach (ChildAnimatorState e in maquina.states)
            if (e.state.name == nombre) return e.state;
        return null;
    }

    // Transicion sin tiempo de salida con condiciones de bools/triggers (true = If, false = IfNot)
    static void Transicion(AnimatorState desde, AnimatorState hacia, float duracion, params (string, bool)[] condiciones)
    {
        AnimatorStateTransition t = desde.AddTransition(hacia);
        foreach ((string parametro, bool valor) in condiciones)
            t.AddCondition(valor ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parametro);
        t.hasExitTime = false;
        t.duration = duracion;
    }

    // Cuantos metros por segundo avanza un clip (su movimiento de raiz)
    public static float VelocidadDelClip(AnimationClip clip, float siNoSeSabe)
    {
        float v = new Vector3(clip.averageSpeed.x, 0f, clip.averageSpeed.z).magnitude;
        return v > 0.1f ? v : siNoSeSabe;
    }

    public static AnimationClip BuscarClip(string nombre)
    {
        string ruta = "Assets/BadCarreteDay/Arte/Animaciones/" + nombre + ".fbx";
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(ruta))
        {
            AnimationClip clip = obj as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }

    public static InputActionReference BuscarInputActionReference(string nombreAccion)
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(rutaInputs))
        {
            InputActionReference referencia = obj as InputActionReference;
            if (referencia != null && referencia.action != null && referencia.action.name == nombreAccion)
                return referencia;
        }
        Debug.LogError("[BadCarreteDay] No encontre la accion de input: " + nombreAccion);
        return null;
    }

    public static void AgregarParametro(AnimatorController animator, string nombre, AnimatorControllerParameterType tipo)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == nombre) return;
        animator.AddParameter(nombre, tipo);
    }

    public static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace("\\", "/");
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(ruta));
    }

    public static void AgregarEscenaABuild(string ruta)
    {
        var escenas = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene e in escenas)
            if (e.path == ruta) return;
        escenas.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = escenas.ToArray();
    }
}
