using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Herramienta de EDITOR para la Fase 1:
//  1) Configura la importacion de los FBX de Mixamo como Humanoid
//  2) Crea el prefab del jugador (Starter Assets + modelo de Aj)
//  3) Crea una escena de prueba con plataformas para probar salto y planeo
// Uso: menu "Bad Carrete Day/F1 - Construir todo" o por linea de comandos:
//   Unity -batchmode -projectPath ... -executeMethod ConstructorF1.ConstruirF1 -quit
public static class ConstructorF1
{
    const string carpetaPersonajes = "Assets/BadCarreteDay/Arte/Personajes";
    const string carpetaAnimaciones = "Assets/BadCarreteDay/Arte/Animaciones";
    const string rutaModeloAj = carpetaPersonajes + "/Aj.fbx";
    const string rutaPrefabJugadorAj = "Assets/BadCarreteDay/Prefabs/JugadorAj.prefab";
    const string rutaEscenaPrueba = "Assets/BadCarreteDay/Escenas/Prueba_F1.unity";

    // Animaciones que se repiten en loop (el resto se reproduce una vez)
    static readonly string[] animacionesEnLoop = { "DrunkIdle", "DrunkWalk", "FightingIdle", "Punching" , "BracedHang",
                                                    "Mutant Idle", "Mutant Walking", "Mutant Run", "Mutant Punch" }; // las de Julio (Arte/Animaciones/Julio)

    [MenuItem("Bad Carrete Day/F1 - Construir todo")]
    public static void ConstruirF1()
    {
        ConfigurarImportaciones();
        ConstructorBadCarreteDay.CrearAnimatorJugador(); // se reconstruye con los idles de caña y el parametro Planeo
        CrearPrefabJugadorAj();
        CrearEscenaPrueba();
        Debug.Log("[BadCarreteDay] F1 construida ✅");
    }

    // ===== 1) IMPORTACION =====

    [MenuItem("Bad Carrete Day/F1 - Configurar importacion Mixamo")]
    public static void ConfigurarImportaciones()
    {
        // Personajes: Humanoid con su propio avatar
        foreach (string ruta in BuscarFBX(carpetaPersonajes))
        {
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            // Mixamo trae una CAMARA ("bottom") dentro del FBX: si se importa, viaja con el
            // personaje bajo el suelo y se dibuja encima de la camara del juego
            imp.importCameras = false;
            imp.importLights = false;
            imp.importAnimation = false;
            imp.SaveAndReimport();

            // Mixamo trae las texturas EMBEBIDAS en el FBX: hay que extraerlas a una carpeta
            // para que los materiales las encuentren (si no, el personaje sale blanco)
            string carpetaTex = carpetaPersonajes + "/Texturas_" + System.IO.Path.GetFileNameWithoutExtension(ruta);
            if (!AssetDatabase.IsValidFolder(carpetaTex))
            {
                AssetDatabase.CreateFolder(carpetaPersonajes, "Texturas_" + System.IO.Path.GetFileNameWithoutExtension(ruta));
                imp.ExtractTextures(carpetaTex);
                AssetDatabase.Refresh();
                imp.SaveAndReimport();
            }
            EnlazarMateriales(ruta, carpetaTex);
        }

        // Avatar de Aj: las animaciones se bajaron sobre su esqueleto
        Avatar avatarAj = BuscarAvatar(rutaModeloAj);

        // Animaciones: Humanoid copiando el avatar de Aj, con el clip renombrado
        foreach (string ruta in BuscarFBX(carpetaAnimaciones))
        {
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar = avatarAj;
            imp.importAnimation = true;

            string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
            bool enLoop = System.Array.IndexOf(animacionesEnLoop, nombre) >= 0;

            ModelImporterClipAnimation[] clips = imp.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = nombre;
                clip.loopTime = enLoop;
                // "Bake Into Pose": el personaje se mueve con el CharacterController / NavMesh,
                // no con el movimiento grabado en la animacion
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                // Colgado y subir: el cuerpo queda donde esta en la animacion (altura y avance
                // originales), asi la subida la hace la animacion y el script solo la acompaña
                // (las dos animaciones se centran en su centro de masa: asi colgado y subir parten
                // alineados, aunque en Mixamo esten grabadas desde puntos distintos)
                if (nombre.StartsWith("BracedHang"))
                {
                    clip.keepOriginalPositionY = false;
                    clip.keepOriginalPositionXZ = false;
                    clip.heightFromFeet = false;
                }
                // Colgado quieto: se recorta el comienzo (el salto para agarrarse); asi el loop no salta
                if (nombre == "BracedHang")
                    clip.firstFrame = Mathf.Max(clip.firstFrame, 16f);
                // Caminar curado: el clip AVANZA (no es "in place"). Ese avance no se hornea en la pose:
                // queda como movimiento de raiz, que se ignora (al jugador lo mueve el CharacterController)
                if (nombre == "DrunkWalk")
                    clip.lockRootPositionXZ = false;
                // Subir el borde: la subida (alto y avance) queda como movimiento de raiz; durante la
                // subida el Animator mueve a Benja con ella (cuerpo y personaje suben juntos)
                if (nombre == "BracedHangToCrouch")
                {
                    clip.lockRootHeightY = false;
                    clip.lockRootPositionXZ = false;
                }

                // Golpe de los enemigos: igual que Ataque01 de clase, prende y apaga el arma
                // con Animation Events (los recibe ControladorEnemigo). Tiempos normalizados 0-1.
                // Golpe pesado de Julio (Mixamo "Mutant Punch"): mismos eventos que el golpe de los cumas
                if (nombre == "Mutant Punch")
                {
                    clip.events = new AnimationEvent[]
                    {
                        new AnimationEvent { time = 0.45f, functionName = "ActivaArma" },
                        new AnimationEvent { time = 0.7f, functionName = "DesactivaArma" },
                    };
                }
                if (nombre == "Punching")
                {
                    clip.events = new AnimationEvent[]
                    {
                        new AnimationEvent { time = 0.35f, functionName = "ActivaArma" },
                        new AnimationEvent { time = 0.65f, functionName = "DesactivaArma" },
                    };
                }
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }
    }

    // Crea un material URP por cada material del FBX con la textura de color que le
    // corresponde (por nombre) y lo "remapea" en el importador.
    static void EnlazarMateriales(string rutaFBX, string carpetaTex)
    {
        ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(rutaFBX);
        string personaje = System.IO.Path.GetFileNameWithoutExtension(rutaFBX);
        string carpetaMat = carpetaPersonajes + "/Materiales_" + personaje;
        if (!AssetDatabase.IsValidFolder(carpetaMat)) AssetDatabase.CreateFolder(carpetaPersonajes, "Materiales_" + personaje);

        // Texturas de color disponibles
        List<Texture2D> difusas = new List<Texture2D>();
        Texture2D cara = null;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { carpetaTex }))
        {
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
            string n = t.name.ToLower();
            if (n.Contains("diffuse")) difusas.Add(t);
            if (n.Contains("facial")) cara = t;
        }
        if (difusas.Count == 0) return;

        bool huboCambios = false;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(rutaFBX))
        {
            Material original = o as Material;
            if (original == null) continue;
            string nm = original.name.ToLower();

            // Elegir la textura: cara (Aj), zapatos, pelo/pestañas (textura 1002) o cuerpo (1001)
            Texture2D tex = difusas[0];
            bool esDetalle = nm.Contains("eyelash") || nm.Contains("lash") || nm.Contains("hair") || nm.Contains("brow");
            if (cara != null && (nm.Contains("eye") || nm.Contains("brow") || nm.Contains("mouth"))) tex = cara;
            else foreach (Texture2D d in difusas)
            {
                string dn = d.name.ToLower();
                if (nm.Contains("shoe") && dn.Contains("shoe")) tex = d;
                else if (!nm.Contains("shoe") && dn.Contains("body")) tex = d;
                if ((esDetalle || nm.EndsWith("1")) && dn.Contains("1002")) tex = d;
            }

            string rutaMat = carpetaMat + "/" + original.name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, rutaMat);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.15f);
            // Pestañas, cejas y pelo usan transparencia recortada
            bool recorte = esDetalle || tex == cara;
            mat.SetFloat("_AlphaClip", recorte ? 1f : 0f);
            mat.SetFloat("_Cutoff", 0.4f);
            if (recorte) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(mat);

            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), mat);
            huboCambios = true;
        }
        if (huboCambios) imp.SaveAndReimport();
    }

    // ===== 2) PREFAB DEL JUGADOR =====

    // Toma el prefab de Starter Assets (jugador + camaras ya conectadas),
    // le cambia el modelo por el de Aj y le agrega los scripts de clase.
    [MenuItem("Bad Carrete Day/F1 - Crear prefab JugadorAj")]
    public static void CrearPrefabJugadorAj()
    {
        ConstructorBadCarreteDay.CrearCarpeta("Assets/BadCarreteDay/Prefabs");

        GameObject prefabStarter = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorBadCarreteDay.rutaPrefabJugador);
        GameObject grupo = (GameObject)PrefabUtility.InstantiatePrefab(prefabStarter);
        PrefabUtility.UnpackPrefabInstance(grupo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        grupo.name = "JugadorAj";

        // Quitar la UI tactil (joysticks de celular) y su EventSystem: cada escena trae el suyo
        foreach (string nombreUI in new[] { "UI_EventSystem", "UI_Canvas_StarterAssetsInputs_Joysticks" })
        {
            Transform ui = grupo.transform.Find(nombreUI);
            if (ui != null) Object.DestroyImmediate(ui.gameObject);
        }

        Animator animator = grupo.GetComponentInChildren<Animator>();
        Transform jugador = animator.transform;

        // CAMARA: el prefab de Starter Assets guarda el objetivo con el formato de Cinemachine 2
        // y en Cinemachine 3 queda VACIO -> la camara no seguia a Benja ni giraba con el mouse.
        // La conectamos a PlayerCameraRoot (el punto que gira ThirdPersonController con el mouse).
        Unity.Cinemachine.CinemachineCamera camaraSeguimiento = grupo.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true);
        Transform raizCamara = jugador.Find("PlayerCameraRoot");
        camaraSeguimiento.Follow = raizCamara;

        // Camara en tercera persona TIPO FORTNITE: sobre el hombro derecho, Benja a la
        // izquierda de la pantalla, cerca y a la altura del hombro.
        Unity.Cinemachine.LensSettings lente = camaraSeguimiento.Lens;
        lente.FieldOfView = 60f;
        lente.NearClipPlane = 0.1f;
        camaraSeguimiento.Lens = lente;
        Unity.Cinemachine.CinemachineThirdPersonFollow seguir = camaraSeguimiento.GetComponent<Unity.Cinemachine.CinemachineThirdPersonFollow>();
        seguir.CameraDistance = 3.3f;                          // cerca, como Fortnite
        seguir.ShoulderOffset = new Vector3(0.75f, 0.25f, 0f); // sobre el hombro derecho
        seguir.VerticalArmLength = 0.2f;
        seguir.CameraSide = 1f;                                // hombro derecho
        seguir.Damping = new Vector3(0.05f, 0.15f, 0.2f);      // responde rapido

        // Limites de inclinacion del ThirdPersonController (campos publicos de Starter Assets)
        StarterAssets.ThirdPersonController tpc = jugador.GetComponent<StarterAssets.ThirdPersonController>();
        tpc.TopClamp = 65f;             // mirar hacia abajo desde arriba
        tpc.BottomClamp = -25f;         // mirar hacia arriba sin meterse al suelo
        tpc.CameraAngleOverride = 0f;

        // Quitar el modelo y esqueleto del robot de Starter Assets
        Object.DestroyImmediate(jugador.Find("Geometry").gameObject);
        Object.DestroyImmediate(jugador.Find("Skeleton").gameObject);

        // Poner el modelo de Aj: sus hijos (malla + huesos) quedan directo bajo el jugador,
        // asi las rutas de los huesos calzan con el avatar de Aj
        GameObject modeloAj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(rutaModeloAj));
        PrefabUtility.UnpackPrefabInstance(modeloAj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        List<Transform> hijos = new List<Transform>();
        foreach (Transform hijo in modeloAj.transform) hijos.Add(hijo);
        foreach (Transform hijo in hijos) hijo.SetParent(jugador, false);
        Object.DestroyImmediate(modeloAj);

        // Animator: avatar de Aj + Animator del juego
        animator.avatar = BuscarAvatar(rutaModeloAj);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorBadCarreteDay.rutaAnimatorJugador);
        animator.applyRootMotion = false;

        // Scripts de clase
        ControladorJugador controlador = jugador.gameObject.AddComponent<ControladorJugador>();
        controlador.inputAtaque = ConstructorBadCarreteDay.BuscarInputActionReference("Ataque");

        ControladorRandomIdle randomIdle = jugador.gameObject.AddComponent<ControladorRandomIdle>();
        randomIdle.animatorPersonaje = animator;
        randomIdle.maxRandom = 2; // 0 = nada, 1 = bostezo, 2 = hipo

        PrefabUtility.SaveAsPrefabAsset(grupo, rutaPrefabJugadorAj);
        Object.DestroyImmediate(grupo);
    }

    // ===== 3) ESCENA DE PRUEBA =====

    [MenuItem("Bad Carrete Day/F1 - Crear escena de prueba")]
    public static void CrearEscenaPrueba()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // La escena por defecto trae Main Camera: el prefab del jugador trae la suya
        Object.DestroyImmediate(GameObject.Find("Main Camera"));

        Cubo("Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f));

        // Escalones para saltar
        for (int i = 0; i < 4; i++)
            Cubo("Escalon_" + i, new Vector3(4f, 0.25f + i * 0.5f, 3f + i * 1.5f), new Vector3(3f, 0.5f + i * 1f, 1.5f));

        // Torre alta + plataforma lejana: solo se alcanza PLANEANDO
        Cubo("Torre", new Vector3(-6f, 3f, 6f), new Vector3(3f, 6f, 3f));
        Cubo("Rampa_Torre", new Vector3(-6f, 1.5f, 1.5f), new Vector3(2f, 0.3f, 7f)).transform.rotation = Quaternion.Euler(-40f, 0f, 0f);
        Cubo("Plataforma_Lejana", new Vector3(-6f, 2f, 16f), new Vector3(3f, 0.5f, 3f));

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabJugadorAj);
        PrefabUtility.InstantiatePrefab(prefab);

        EditorSceneManager.SaveScene(escena, rutaEscenaPrueba);
        ConstructorBadCarreteDay.AgregarEscenaABuild(rutaEscenaPrueba);
    }

    // ===== Utilidades =====

    static GameObject Cubo(string nombre, Vector3 posicion, Vector3 escala)
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        cubo.transform.position = posicion;
        cubo.transform.localScale = escala;
        return cubo;
    }

    static List<string> BuscarFBX(string carpeta)
    {
        List<string> rutas = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { carpeta }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (ruta.ToLower().EndsWith(".fbx")) rutas.Add(ruta);
        }
        return rutas;
    }

    static Avatar BuscarAvatar(string rutaModelo)
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(rutaModelo))
            if (obj is Avatar) return (Avatar)obj;
        Debug.LogError("[BadCarreteDay] No encontre avatar en " + rutaModelo);
        return null;
    }
}
