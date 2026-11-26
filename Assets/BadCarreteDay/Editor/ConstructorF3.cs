using StarterAssets;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Herramienta de EDITOR para la Fase 3: arma el NIVEL 1 completo en greybox
// (bloques de colores; el arte final llega en la F5).
// Zonas (ver GUION_NIVEL1.md):
//   1) Pasaje del despertar (Don Lucho)      z  -2..32   y 0
//   2) Plaza del almacen (Doña Marta, Toby)  z  32..52   y 0
//   3) Escalera rota + techos (planeo)       z  52..111  y 4 a 8
//   4) Ascensor + pasaje oscuro (palo)       z 111..134  y 16
//   5) Cancha en la cima (Julio)             z 134..164  y 16
public static class ConstructorF3
{
    const string rutaEscena = "Assets/BadCarreteDay/Escenas/Nivel1_Cerro.unity";
    const string rutaAnimatorSimple = "Assets/BadCarreteDay/Animaciones/AnimatorSimple_Speed.controller";
    const string rutaAnimatorJulio = "Assets/BadCarreteDay/Animaciones/AnimatorNPC_Julio.controller";
    const string rutaAnimatorMarta = "Assets/BadCarreteDay/Animaciones/AnimatorNPC_Marta.controller";

    static Transform nivel; // padre de toda la geometria (para el NavMesh)
    static Transform enemigos; // FUERA de "nivel": sus colliders no deben hacer hoyos en el NavMesh
    const string rutaAnimatorCuma = "Assets/BadCarreteDay/Animaciones/AnimatorNPC_Cuma.controller";
    static ConstructorF2.Base b;

    [MenuItem("Bad Carrete Day/F3 - Construir Nivel 1 (greybox)")]
    public static void ConstruirF3()
    {
        CrearAnimators();
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Object.DestroyImmediate(GameObject.Find("Main Camera"));

        nivel = new GameObject("Nivel").transform;
        enemigos = new GameObject("Enemigos").transform;
        b = ConstructorF2.CrearBase(new Vector3(0f, 0.1f, 1f));
        b.mision.tituloMision = "Recupera tu teléfono";

        CrearObjetivos();
        ConstructorNivel1.Terreno(); // el cerro (terreno continuo); va fuera de "Nivel"
        CrearResaca();
        Zona1_Pasaje();
        Zona2_Almacen();
        Zona3_EscaleraYTechos();
        Zona4_AscensorYPasajeOscuro();
        Zona5_Cancha();
        CrearVacio();
        CrearLimiteMapa();
        CrearPresentaciones();
        CrearAtajos();

        // F5: modelos, props, luz de madrugada y audio (sin cambiar los colliders del greybox)
        ConstructorF5.VestirNivel(b);
        ConectarBasureros();

        // NavMesh para el perro (y en la F4 para los cumas y Julio)
        NavMeshSurface superficie = nivel.gameObject.AddComponent<NavMeshSurface>();
        superficie.collectObjects = CollectObjects.Children;
        superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        superficie.BuildNavMesh();

        UnityEventTools.AddPersistentListener(b.mision.eventos.alCompletarMision, b.juego.Victoria);

        // Revision automatica: huecos, plata alcanzable, recorrido, nada flotando, edificios bajos
        int problemas = VerificadorNivel1.Verificar();
        if (problemas > 0) throw new System.Exception("[BadCarreteDay] El Nivel 1 tiene " + problemas + " problemas (ver [Verificador] en la consola)");

        EditorSceneManager.SaveScene(escena, rutaEscena);
        ConstructorBadCarreteDay.AgregarEscenaABuild(rutaEscena);
        Debug.Log("[BadCarreteDay] F3 construida ✅ (Nivel1_Cerro)");
    }

    // ===== MISION =====
    // Indices (se usan abajo en los eventos)
    // Primero el agua: con caña Benja solo camina y habla (y nadie le entiende)
    const int OBJ_AGUA = 0, OBJ_HABLAR_LUCHO = 1, OBJ_COMPRAR_VINO = 2, OBJ_LLEVAR_VINO = 3, OBJ_COMPRAR_COMPLETO = 4,
              OBJ_DAR_COMPLETO = 5, OBJ_SUBIR_ESCALERA = 6, OBJ_ASCENSOR = 7,
              OBJ_CUMAS = 8, OBJ_LLEGAR_CANCHA = 9, OBJ_JULIO = 10, OBJ_TELEFONO = 11;

    static Resaca resaca;                     // la caña de Benja (se quita con el agua)
    static ContadorCondiciones escaleraToby;  // Toby comiendo + agua tomada = se abre la escalera

    static void CrearObjetivos()
    {
        string[] textos = {
            "Junta plata y tómate un agua en el almacén ($1.000)",
            "Habla con el viejo que está meando el muro",
            "Compra una caja de vino en el almacén ($2.000)",
            "Llévale el vino a Don Lucho",
            "Compra un completo para el Toby ($2.000)",
            "Dale el completo al Toby",
            "Sube por la escalera rota",
            "Cruza los techos y activa el ascensor",
            "Vence a los cumas del pasaje oscuro",
            "Encuentra a Julio en la cancha",
            "Vence a Julio",
            "Recupera tu teléfono",
        };
        foreach (string t in textos)
            b.mision.objetivos.Add(new ControladorMision.Objetivo { descripcion = t });
    }

    // ===== ZONA 1: PASAJE DEL DESPERTAR (y 0, z -6.5..31) =====
    // Calle angosta sin salida al sur, edificios reales a ambos lados y un sitio eriazo al este
    // (con un auto y un contenedor-bodega para subir) desde donde se ve el plan.

    static void Zona1_Pasaje()
    {
        Transform z = Grupo("Zona1_Pasaje");
        Material adoquin = Color_("Adoquin", 0.62f, 0.6f, 0.58f);
        Material vereda = Color_("Vereda", 0.58f, 0.58f, 0.6f);
        ConstructorNivel1.Suelo(z, "Suelo_Pasaje", -4.2f, 4.2f, -6.5f, 31.5f, 0f, adoquin);
        ConstructorNivel1.Caja(z, "Vereda", new Vector3(-3.5f, 0.06f, 12.25f), new Vector3(1.4f, 0.12f, 37.5f), vereda);
        ConstructorNivel1.Caja(z, "Vereda", new Vector3(3.5f, 0.06f, 2.75f), new Vector3(1.4f, 0.12f, 18.5f), vereda);
        ConstructorNivel1.Caja(z, "Vereda", new Vector3(3.5f, 0.06f, 25.75f), new Vector3(1.4f, 0.12f, 11.5f), vereda);

        // Edificios: fondo (calle sin salida), lado oeste y lado este (con el sitio eriazo en z 12..20)
        ConstructorNivel1.FilaEdificios(z, new Vector3(-14f, 0f, -6.5f), new Vector3(14f, 0f, -6.5f), Vector3.forward, false);
        ConstructorNivel1.FilaEdificios(z, new Vector3(-4.2f, 0f, -6.5f), new Vector3(-4.2f, 0f, 31f), Vector3.right, false);
        ConstructorNivel1.FilaEdificios(z, new Vector3(4.2f, 0f, -6.5f), new Vector3(4.2f, 0f, 12f), Vector3.left, false);
        ConstructorNivel1.FilaEdificios(z, new Vector3(4.2f, 0f, 20f), new Vector3(4.2f, 0f, 31f), Vector3.left, false);

        // Sitio eriazo con baranda al borde del cerro (se ve el plan y la bahia)
        ConstructorNivel1.Suelo(z, "Lote_Eriazo", 4.2f, 12.5f, 11.9f, 20.1f, 0f, Color_("Tierra_Lote", 0.42f, 0.34f, 0.26f));
        // Borde del barranco: reja CON puas (no se escala, pincha)
        ConstructorNivel1.Reja(z, new Vector3(12.4f, 0f, 11.9f), new Vector3(12.4f, 0f, 20.1f), 2.2f, true, true);
        // Entrada desde la calle: reja SIN puas de 1,8 m (se escala colgandose del borde)
        ConstructorNivel1.Reja(z, new Vector3(4.45f, 0f, 11.9f), new Vector3(4.45f, 0f, 20.1f), 1.8f, false, false);
        Pensamiento(z, new Vector3(3f, 0f, 16f), 1.8f, "Una reja sin púas... salto, me cuelgo y [Espacio] para pasar.");
        ConstructorNivel1.Limite(z, new Vector3(4.2f, 0f, 11.9f), new Vector3(12.5f, 0f, 11.9f));
        ConstructorNivel1.Limite(z, new Vector3(4.2f, 0f, 20.1f), new Vector3(12.5f, 0f, 20.1f));
        // Para subir: capo del auto (1 m) -> techo del auto (1,7 m) -> contenedor-bodega (2,6 m)
        ConstructorNivel1.topeAutoLote = ConstructorNivel1.Auto(z, "car_sedan", new Vector3(7.4f, 0f, 16.2f), 180f);
        Vector3 capoLote = ConstructorNivel1.posicionCapo;
        ConstructorNivel1.topeContenedor = ConstructorNivel1.Contenedor(z, "shipping-container-b", new Vector3(10.1f, 0f, 16.3f), 0f);

        // Plata zona 1: NADA tirado en la calle. Con caña sale de los basureros (y de la señora Nelly);
        // despues del agua, de lo alto y escondido del sitio eriazo.
        Pensamiento(z, new Vector3(0f, 0f, 1f), 2f, "Mi celular... no está. Y sin una luka pa' un agua. Igual algo encuentro en la basura...");
        TachoRevisable(z, new Vector3(3.5f, 0f, 24f), 300, "Alguien botó vuelto. Bendito sea.");
        TachoRevisable(z, new Vector3(-3.5f, 0f, 15f), 0, "Media pata de pollo... no, tanta hambre no tengo.");
        Moneda(new Vector3(7.4f, ConstructorNivel1.topeAutoLote + 0.8f, 16.2f), 500);     // techo del auto
        GameObject primeraLuka = Moneda(new Vector3(10.1f, ConstructorNivel1.topeContenedor + 0.8f, 16.3f), 1000); // techo del contenedor
        PensamientoAlTocar(primeraLuka, "Una luka arriba de un contenedor. Valpo, nunca cambies.");
        // Entre los neumaticos del rincon del sitio eriazo
        Moneda(new Vector3(11.0f, 0.8f, 12.5f), 100);
        Moneda(new Vector3(11.9f, 0.8f, 12.4f), 100);
        GameObject gambaNeumatico = Moneda(new Vector3(11.9f, 0.8f, 13.3f), 100);
        PensamientoAlTocar(gambaNeumatico, "Gambas entre los neumáticos. Aquí alguien perdió una apuesta.");

        CrearSenoraYoli(z);

        CrearDonLucho(z);
    }

    // Referencias que se conectan entre zonas
    static DialogoNPC dialogoLucho;
    static GameObject productoVino, productoCompleto, pegatinaCompleto;
    static ControladorCompra productoAguaCompra;

    static void CrearDonLucho(Transform zona)
    {
        GameObject lucho = NPC(zona, "NPC_DonLucho", "PeasantMan", "Assets/BadCarreteDay/Animaciones/AnimatorNPC_Viejo.controller",
                               new Vector3(-3.35f, 0.12f, 10.5f), -90f); // en la vereda, mirando al muro (de espaldas al pasaje)

        dialogoLucho = ConstructorBadCarreteDay.Agregar<DialogoNPC>(lucho);
        dialogoLucho.nombrePersonaje = "Don Lucho";
        ConstructorBadCarreteDay.Agregar<MirarJugador>(lucho); // al hablarle se da vuelta
        ConstructorF2.AgregarVoz(lucho, "Don Lucho", "DonLucho", 1f, 1.85f);
        // Sin sonidos antes de hablar: se da vuelta y habla al tiro
        dialogoLucho.conversaciones = new DialogoNPC.Conversacion[]
        {
            Conv("¡Ey, ey! No te acerquí tanto, cabro, que estoy regando el murito, jajajaja.",
                 "¿Tu celular? Jajaja... te jodieron, cabro.",
                 "Te lo sacó Julio, po. Pasó corriendo pa' la cancha de arriba con una mochila que no era de él.",
                 "¿Querí saber cómo llegar? Uff... un \"cajerne\" y te lo digo.",
                 "Tráeme una cajita de vino del almacén y te cuento todo. Y apúrate, que quiero ir a tomar a la plaza."),
            Conv("¿Y el vino? Sin vino no hay dato, cabro."),
            Conv("¡Ahí está! Un gatito... *glu glu glu*.",
                 "Julio se esconde en la cancha de arriba. Pero ojo con el Toby, el quiltro de la escalera: no deja pasar a nadie que no le traiga algo pa' comer.",
                 "Y si el ascensor no anda, pégale a la palanca. Siempre funciona. A veces.",
                 "Ya, ándate. Y no le digai a nadie que me viste... ¡NI QUE ME VISTE MEANDO!"),
            Conv("¿Todavía aquí? Julio está en la cancha de arriba, cabro. ¡Ándate!"),
            Conv(lineasLuchoConCana),
            Conv("¡Ey, ey! No te acerquí tanto, cabro... ¿y eso? ¿Un vinito pa' mí? ¡Ahí está! Un gatito... *glu glu glu*.",
                 "¿Tu celular? Te lo sacó Julio, po. Pasó corriendo pa' la cancha de arriba con una mochila que no era de él.",
                 "Pero ojo con el Toby, el quiltro de la escalera: no deja pasar a nadie que no le traiga algo pa' comer.",
                 "Y si el ascensor no anda, pégale a la palanca. Siempre funciona. A veces.",
                 "Ya, ándate. Y no le digai a nadie que me viste... ¡NI QUE ME VISTE MEANDO!"),
        };
        // Con caña no te pesca: sigue meando y contesta por encima del hombro
        dialogoLucho.resaca = resaca;
        dialogoLucho.conversacionConCana = 4;
        dialogoLucho.girarseConCana = false; // con caña contesta de espaldas, sin darse vuelta
        UnityEventTools.AddStringPersistentListener(dialogoLucho.conversaciones[4].alTerminar, b.hud.MostrarPensamiento, "Nadie me entiende... necesito un agua. El almacén de la plaza.");
        ZonaInteraccion(lucho.transform, Vector3.zero, 2.5f, "[E] Hablar", dialogoLucho.IniciarDialogo);

        // Al terminar la 1ra conversacion: objetivo + espera el vino (se habilita el producto en el almacen)
        DialogoNPC.Conversacion c0 = dialogoLucho.conversaciones[0];
        UnityEventTools.AddIntPersistentListener(c0.alTerminar, b.mision.CompletarObjetivo, OBJ_HABLAR_LUCHO);
        UnityEventTools.AddIntPersistentListener(c0.alTerminar, dialogoLucho.CambiarConversacion, 1);
        // Al terminar la 3ra (ya con el vino): habla del Toby -> se habilita el completo
        DialogoNPC.Conversacion c2 = dialogoLucho.conversaciones[2];
        UnityEventTools.AddIntPersistentListener(c2.alTerminar, b.mision.CompletarObjetivo, OBJ_LLEVAR_VINO);
        UnityEventTools.AddIntPersistentListener(c2.alTerminar, dialogoLucho.CambiarConversacion, 3);
        // Si llego con el vino sin haberle hablado: una sola conversacion cumple los tres pasos
        DialogoNPC.Conversacion c5 = dialogoLucho.conversaciones[5];
        UnityEventTools.AddIntPersistentListener(c5.alTerminar, b.mision.CompletarObjetivo, OBJ_HABLAR_LUCHO);
        UnityEventTools.AddIntPersistentListener(c5.alTerminar, b.mision.CompletarObjetivo, OBJ_LLEVAR_VINO);
        UnityEventTools.AddIntPersistentListener(c5.alTerminar, dialogoLucho.CambiarConversacion, 3);
    }

    // ===== SEÑORAS DEL CERRO (dialogos: aqui juntos para irlos editando) =====
    // Antes del agua Benja balbucea y no se le entiende (salvo Nelly, que igual le da plata).

    static readonly string[] lineasYoli = {
        "¡Mira cómo andai, cabro! ¡Curao a las cinco de la mañana!",
        "Mi hijo estudia en la Católica y nunca llega así.",
        "¡Sale de mi vereda, sinvergüenza c*****o!" };
    static readonly string[] lineasYoliConCana = {
        "Benja: Sheñora... ¿ushté no ha vishto... mi celu...?",
        "¿Qué? No se te entiende nada.",
        "¡Curao a las cinco de la mañana! ¡Sale de mi vereda, sinvergüenza c*****o!" };
    static readonly string[] lineasNelly = {
        "¿Qué me mirai? Yo salgo temprano a comprar el pan, no como otros...",
        "Toma, quinientos. Cómprate un agua, que das pena, mijo." };
    static readonly string[] lineasRosa = {
        "Sacudo a esta hora porque después pasa la vecina y pela.",
        "¿Y tú qué hací en la calle? Ah, no... si se te nota." };
    static readonly string[] lineasGladys = {
        "¿Vai a pasar por ese pasaje? Esos cumas te van a dejar pila'o.",
        "Toma mi escoba. Total, la nuera nunca barre." };
    static readonly string[] lineasLuchoConCana = {
        "Benja: Don... Lusho... ¿ushté vio... mi celu...?",
        "No se te entiende nada, cabro.",
        "Ya, déjame tranquilo." };

    // Modelos de las señoras (Mixamo). Si alguno no esta, se usa Martha con otro color.
    static readonly string[] modelosSenoras = { "SenoraYoli", "SenoraNelly", "SenoraRosa", "SenoraGladys" };
    static readonly Color[] tintesSenoras = { new Color(0.8f, 0.9f, 1f), new Color(1f, 0.8f, 0.85f), new Color(0.85f, 1f, 0.8f), new Color(1f, 0.95f, 0.7f) };

    static DialogoNPC Senora(Transform zona, int i, string nombre, Vector3 pos, float rotY, float tono, params DialogoNPC.Conversacion[] conversaciones)
    {
        string modelo = modelosSenoras[i];
        bool hayModelo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BadCarreteDay/Arte/Personajes/" + modelo + ".fbx") != null;
        GameObject obj = NPC(zona, "NPC_Sra" + nombre, hayModelo ? modelo : "Martha", rutaAnimatorMarta, pos, rotY);
        if (!hayModelo) Tenir(obj, "Sra" + nombre, tintesSenoras[i]);
        DialogoNPC d = ConstructorBadCarreteDay.Agregar<DialogoNPC>(obj);
        d.nombrePersonaje = "Sra. " + nombre;
        d.resaca = resaca;
        d.conversaciones = conversaciones;
        ConstructorBadCarreteDay.Agregar<MirarJugador>(obj);
        ConstructorF2.AgregarVoz(obj, d.nombrePersonaje, "Marta", tono, 1.75f);
        ZonaInteraccion(obj.transform, Vector3.zero, 2.2f, "[E] Hablar", d.IniciarDialogo);
        return d;
    }

    // Yoli: barre su vereda en el pasaje. Solo insulta.
    static void CrearSenoraYoli(Transform zona)
    {
        DialogoNPC yoli = Senora(zona, 0, "Yoli", new Vector3(3.2f, 0.12f, 4.0f), -90f, 1.1f,
                                 Conv(lineasYoli), Conv("¿Todavía aquí? ¡Fuera!"), Conv(lineasYoliConCana));
        yoli.conversacionConCana = 2;
        UnityEventTools.AddIntPersistentListener(yoli.conversaciones[0].alTerminar, yoli.CambiarConversacion, 1);
    }

    // Nelly: viene con la bolsa del pan. Se justifica y te da una quina (con o sin caña).
    static void CrearSenoraNelly(Transform zona)
    {
        DialogoNPC nelly = Senora(zona, 1, "Nelly", new Vector3(-4.5f, 0f, 33.5f), 135f, 0.95f,
                                  Conv(lineasNelly), Conv("Ya te di, mijo. No abusí."));
        UnityEventTools.AddIntPersistentListener(nelly.conversaciones[0].alTerminar, b.dinero.Sumar, 500);
        UnityEventTools.AddStringPersistentListener(nelly.conversaciones[0].alTerminar, b.hud.MostrarPensamiento, "(+$500) Gracias, tía... creo.");
        UnityEventTools.AddIntPersistentListener(nelly.conversaciones[0].alTerminar, nelly.CambiarConversacion, 1);
    }

    // Rosa: sacude una alfombra en la calle alta. Se justifica.
    static void CrearSenoraRosa(Transform zona)
    {
        DialogoNPC rosa = Senora(zona, 2, "Rosa", new Vector3(-9.0f, 4f, 84f), 90f, 1.2f,
                                 Conv(lineasRosa), Conv("Sigue no más."));
        UnityEventTools.AddIntPersistentListener(rosa.conversaciones[0].alTerminar, rosa.CambiarConversacion, 1);
    }

    // Gladys (secreto): barre al fondo de la calle alta, abajo, pasada la escalera rota.
    // Si le hablas te pasa su escoba (no es mision: se puede pelear a puño limpio).
    static void CrearSenoraGladys(Transform zona)
    {
        DialogoNPC gladys = Senora(zona, 3, "Gladys", new Vector3(-5.5f, 4f, 108.6f), 180f, 1.0f,
                                   Conv(lineasGladys), Conv("Cuídame la escoba, ¿ya?"));
        DialogoNPC.Conversacion c0 = gladys.conversaciones[0];
        Pensamiento(zona, new Vector3(-5.5f, 4f, 104.5f), 2.5f, "¿Una señora barriendo a esta hora, al fondo del pasaje? Esto es Valpo.");
        // La escoba apoyada al lado de ella (se la pasa a Benja)
        GameObject escoba = new GameObject("Palo_Escoba");
        escoba.transform.SetParent(zona, false);
        // Apoyada casi parada: el extremo del mango arriba y las cerdas en el suelo (la escoba apunta a +Z del objeto)
        escoba.transform.rotation = Quaternion.Euler(80f, 0f, 0f);
        escoba.transform.position = new Vector3(-4.85f, 4f + ConstructorF2.largoEscoba * Mathf.Sin(80f * Mathf.Deg2Rad), 108.6f);
        ConstructorF2.ModeloEscoba(escoba.transform, ConstructorF2.largoEscoba);
        UnityEventTools.AddBoolPersistentListener(c0.alTerminar, escoba.SetActive, false);
        UnityEventTools.AddPersistentListener(c0.alTerminar, b.animJugador.GetComponent<AtaqueJugador>().TomarEscoba);
        UnityEventTools.AddStringPersistentListener(c0.alTerminar, b.hud.MostrarPensamiento, "Una escoba. La voy a usar como espada... peor es na'.");
        UnityEventTools.AddIntPersistentListener(c0.alTerminar, gladys.CambiarConversacion, 1);
    }

    // ===== BASUREROS Y SECRETOS =====

    static System.Collections.Generic.List<Basurero> basureros = new System.Collections.Generic.List<Basurero>();

    // Basurero que se revisa una vez con [E] (el modelo lo pone la F5; se conecta en ConectarBasureros)
    static Basurero TachoRevisable(Transform padre, Vector3 pos, int plata, string pensamiento)
    {
        GameObject obj = new GameObject("Basurero_$" + plata);
        obj.transform.SetParent(padre, false);
        obj.transform.position = pos;
        Basurero bas = ConstructorBadCarreteDay.Agregar<Basurero>(obj);
        bas.plata = plata;
        bas.pensamiento = pensamiento;
        bas.dinero = b.dinero;
        bas.hud = b.hud;
        bas.sonido = ConstructorSonidos.Clip("Basurero");
        bas.zonaInteraccion = ZonaInteraccion(obj.transform, Vector3.zero, 1.5f, "[E] Revisar el basurero", bas.Revisar);
        basureros.Add(bas);
        return bas;
    }

    // Despues de vestir el nivel: cada basurero se sacude con su tapa (o el dumpster entero)
    static void ConectarBasureros()
    {
        foreach (Basurero bas in basureros)
        {
            Transform mejor = null;
            float distancia = 2.5f;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name != "Tacho_Tapa" && t.name != "Prop_dumpster") continue;
                Vector3 d = t.position - bas.transform.position;
                d.y = 0f;
                if (d.magnitude < distancia) { distancia = d.magnitude; mejor = t; }
            }
            if (mejor == null) Debug.LogWarning("[BadCarreteDay] Basurero sin modelo en " + bas.transform.position);
            bas.modelo = mejor;
        }
        basureros.Clear();
    }

    // Detras de la escalera rota (pegado al descanso, a nivel de la calle): una luka y el cuaderno de Benja
    static void SecretoEscaleraRota(Transform z)
    {
        Vector3 rincon = new Vector3(0.6f, 4f, 87.0f);
        GameObject luka = Moneda(rincon + new Vector3(0f, 0.8f, 0f), 1000);
        PensamientoAlTocar(luka, "¡Una luka escondida! Alguien la guardó pa' la micro... ahora es mía.");
        GameObject cuaderno = Bloque(z, "Cuaderno_UCSC", rincon + new Vector3(-0.9f, 0.03f, 0.2f), new Vector3(0.42f, 0.05f, 0.56f), Color_("Cuaderno", 0.15f, 0.35f, 0.75f));
        cuaderno.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        Object.DestroyImmediate(cuaderno.GetComponent<Collider>());
        ConstructorF7.Texto3D(z, "UCSC", cuaderno.transform.position + Vector3.up * 0.035f, 25f, 0.14f, Color.white).transform.rotation = Quaternion.Euler(90f, 25f, 0f);
        ControladorLugarObjetivo verCuaderno = LugarObjetivo(z, "Secreto_Cuaderno", cuaderno.transform.position, 0.8f, null, -1, null);
        UnityEventTools.AddStringPersistentListener(verCuaderno.alLlegarJugador, b.hud.MostrarPensamiento, "Mi cuaderno... \"Certamen de Cálculo II: lunes\". ¿Qué día es hoy?");
    }

    // Detras del kiosko: grafiti de la UCSC y una quina
    static void SecretoKiosko(Transform z)
    {
        // Rincon entre el kiosko, la medianera y el balcon (x 8,2..9,4, z 37,6..39,7)
        ConstructorF7.Texto3D(z, "UCSC\n¡VAMOS LOS DELFINES!", new Vector3(9.37f, 1.7f, 39.6f), -90f, 0.16f, new Color(0.3f, 0.75f, 1f));
        GameObject quina = Moneda(new Vector3(8.9f, 0.8f, 38.3f), 500);
        PensamientoAlTocar(quina, "Un grafiti de la U... y una quina. Esto es una señal.");
    }

    // ===== ZONA 2: PLAZA MIRADOR, ALMACEN Y TOBY (y 0, z 31..55) =====
    // Al oeste el almacen de Doña Marta (primer piso de un edificio), al este un kiosko de lata,
    // un auto y un balcon, y al noreste el mirador con baranda hacia la bahia.

    static void Zona2_Almacen()
    {
        Transform z = Grupo("Zona2_Almacen");
        ConstructorNivel1.Suelo(z, "Suelo_Plaza", -8f, 14f, 31f, 55.2f, 0f, Color_("Cemento", 0.7f, 0.68f, 0.66f));

        // Lado oeste: edificios + almacen (z 37..47) con un edificio encima
        ConstructorNivel1.FilaEdificios(z, new Vector3(-8f, 0f, 31f), new Vector3(-8f, 0f, 37f), Vector3.right, false);
        ConstructorNivel1.FilaEdificios(z, new Vector3(-8f, 0f, 47f), new Vector3(-8f, 0f, 55f), Vector3.right, false);
        CrearLocalAlmacen(z);
        CrearDonaMarta(z);

        // Lado este: edificio con balcon, kiosko de lata, auto y cajon (para subir) + mirador
        ConstructorNivel1.FilaEdificios(z, new Vector3(9.4f, 0f, 31f), new Vector3(9.4f, 0f, 41f), Vector3.left, false);
        ConstructorNivel1.Caja(z, "Balcon", new Vector3(8.8f, 3.3f, 38.7f), new Vector3(1.2f, 0.2f, 2.2f), Color_("Balcon", 0.75f, 0.72f, 0.68f));
        ConstructorNivel1.topeBalcon = 3.4f;
        ConstructorNivel1.topeKiosko = ConstructorNivel1.Kiosko(z, new Vector3(7.9f, 0f, 36f), 2.6f);
        ConstructorNivel1.topeAutoPlaza = ConstructorNivel1.Auto(z, "car_hatchback", new Vector3(5.4f, 0f, 36f), 180f);
        ConstructorNivel1.Baranda(z, new Vector3(13.9f, 0f, 41f), new Vector3(13.9f, 0f, 55.2f));

        // Al norte: edificios a ambos lados de la escalera de Toby (miran a la plaza)
        ConstructorNivel1.FilaEdificios(z, new Vector3(-8f, 0f, 55.2f), new Vector3(-2f, 0f, 55.2f), Vector3.back, false);
        ConstructorNivel1.FilaEdificios(z, new Vector3(2f, 0f, 55.2f), new Vector3(14f, 0f, 55.2f), Vector3.back, false);

        // Plata zona 2: el dumpster (con caña), detras del dumpster, y arriba (despues del agua)
        // (el dumpster esta en la esquina del mirador: ConstructorNivel1Detalles)
        TachoRevisable(z, new Vector3(11.2f, 0f, 50.8f), 200, "Huele a carrete de anoche. Familiar.");
        GameObject quinaDumpster = Moneda(new Vector3(13.38f, 0.8f, 50.8f), 500);         // escondida entre el dumpster y la baranda
        PensamientoAlTocar(quinaDumpster, "Una quina detrás del basurero. El que busca, encuentra.");
        Moneda(new Vector3(5.4f, ConstructorNivel1.topeAutoPlaza + 0.8f, 36f), 500);     // techo del auto
        Moneda(new Vector3(7.9f, ConstructorNivel1.topeKiosko + 0.8f, 36f), 500);        // techo del kiosko
        GameObject lukaBalcon = Moneda(new Vector3(8.8f, ConstructorNivel1.topeBalcon + 0.8f, 38.7f), 1000);     // balcon
        PensamientoAlTocar(lukaBalcon, "Una luka en el balcón. Perdón, vecino, es una emergencia.");
        SecretoKiosko(z);

        CrearSenoraNelly(z);

        CrearToby(z);
    }

    // Local del almacen: x -14..-8, z 37..47. Frente (x -8) con ventanita y meson; puerta cerrada.
    // Encima va un edificio de departamentos.
    static void CrearLocalAlmacen(Transform zona)
    {
        Transform a = new GameObject("Almacen").transform;
        a.SetParent(zona, false);
        Material muro = Color_("Almacen_Muro", 0.95f, 0.78f, 0.3f);
        ConstructorNivel1.Suelo(a, "Almacen_Piso", -14f, -8f, 37f, 47f, 0f, Color_("Piso_Baldosa", 0.55f, 0.25f, 0.2f));
        ConstructorNivel1.Caja(a, "Almacen_MuroFondo", new Vector3(-13.9f, 2.2f, 42f), new Vector3(0.2f, 4.4f, 10f), muro);
        ConstructorNivel1.Caja(a, "Almacen_Lado1", new Vector3(-11f, 2.2f, 37.1f), new Vector3(6f, 4.4f, 0.2f), muro);
        ConstructorNivel1.Caja(a, "Almacen_Lado2", new Vector3(-11f, 2.2f, 46.9f), new Vector3(6f, 4.4f, 0.2f), muro);
        ConstructorNivel1.Caja(a, "Almacen_FrenteBajo", new Vector3(-8.1f, 0.5f, 41f), new Vector3(0.2f, 1f, 8f), muro);
        ConstructorNivel1.Caja(a, "Almacen_FrenteAlto", new Vector3(-8.1f, 3.55f, 42f), new Vector3(0.2f, 1.7f, 10f), muro);
        ConstructorNivel1.Caja(a, "Almacen_FrentePuerta", new Vector3(-8.1f, 1.35f, 46f), new Vector3(0.2f, 2.7f, 2f), muro);
        ConstructorNivel1.Caja(a, "Almacen_Techo", new Vector3(-11f, 4.5f, 42f), new Vector3(6.4f, 0.2f, 10.4f), Color_("Almacen_Techo", 0.35f, 0.18f, 0.15f));
        ConstructorNivel1.Caja(a, "Meson", new Vector3(-7.85f, 0.5f, 41f), new Vector3(0.7f, 1f, 7.6f), Color_("Madera", 0.6f, 0.42f, 0.25f));
        // La ventanita no se puede cruzar (collider invisible arriba del meson)
        GameObject ventana = new GameObject("Ventanita");
        ventana.transform.SetParent(a, false);
        ventana.transform.position = new Vector3(-8.1f, 1.85f, 41f);
        ventana.AddComponent<BoxCollider>().size = new Vector3(0.2f, 1.7f, 8f);
        // Departamentos encima del local (sin cimientos: se apoyan en el techo del almacen)
        ConstructorNivel1.Edificio(a, "building_H", new Vector3(-8f, 4.6f, 42f), Vector3.right, false, false);
    }

    static void CrearDonaMarta(Transform zona)
    {
        GameObject marta = NPC(zona, "NPC_DonaMarta", "Martha", rutaAnimatorMarta, new Vector3(-9.3f, 0f, 41f), 90f); // DENTRO del almacen
        ConstructorBadCarreteDay.Agregar<MirarJugador>(marta).distancia = 5f; // te mira desde detras del meson
        ConstructorF2.AgregarVoz(marta, "Doña Marta", "Marta", 1f, 1.8f);
        // Se le habla frente a ELLA, en la ventanita: se abre su tienda al tiro (saluda en una burbuja)
        Transform ventanita = Punto(zona, "Punto_HablarMarta", new Vector3(-6.7f, 0f, 41f));
        GameObject zonaMarta = ZonaInteraccion(ventanita, Vector3.zero, 1.6f, "[E] Comprar donde la Doña Marta", b.tienda.Abrir);
        UnityEventTools.AddPersistentListener(zonaMarta.GetComponent<DemoControlInteraccion>().eventos.alInteractuar, b.hud.OcultarPrompt);
        b.tienda.saludo = "¿Qué vai a llevar, mijo?";

        // Productos: se compran en la tienda (no se ven en el meson). Todo esta a la venta desde el inicio:
        // si Benja llega a un evento con el producto, el evento se lo salta.
        productoVino = Producto(zona, "Producto_Vino", "Caja de vino", 2000, new Vector3(-7.85f, 1.25f, 38.6f), Color_("Vino", 0.55f, 0.1f, 0.25f),
                                "Doña Marta: ¿Pa'l Don Lucho, cierto? Ese viejo me debe medio almacén.");
        productoCompleto = Producto(zona, "Producto_Completo", "Completo", 2000, new Vector3(-7.85f, 1.25f, 41f), Color_("Completo", 0.95f, 0.75f, 0.4f),
                                    "Doña Marta: Con harta mayo. Como debe ser.");
        GameObject agua = Producto(zona, "Producto_Agua", "Agua mineral", 1000, new Vector3(-7.85f, 1.25f, 43.4f), Color_("Celeste_Agua", 0.4f, 0.8f, 1f),
                                   "Agua. Menos mal, aún me duele la cabeza.");

        // Vino: Don Lucho cambia lo que dice (si ya le hablo -> "aqui esta el vino"; si no -> todo de una)
        ControladorCompra compraVino = productoVino.GetComponent<ControladorCompra>();
        UnityEventTools.AddIntPersistentListener(compraVino.alComprar, b.mision.CompletarObjetivo, OBJ_COMPRAR_VINO);
        UnityEventTools.AddStringPersistentListener(compraVino.alComprar, dialogoLucho.CambiarSegun, "0>5,1>2");

        // Completo: el plato del Toby se habilita al comprarlo (en cualquier momento)
        UnityEventTools.AddIntPersistentListener(productoCompleto.GetComponent<ControladorCompra>().alComprar, b.mision.CompletarObjetivo, OBJ_COMPRAR_COMPLETO);

        // Agua: cura la caña (obligatoria para subir)
        ControladorCompra compraAgua = agua.GetComponent<ControladorCompra>();
        productoAguaCompra = compraAgua;
        UnityEventTools.AddFloatPersistentListener(compraAgua.alComprar, b.stats.Curar, 40f);
        UnityEventTools.AddPersistentListener(compraAgua.alComprar, resaca.TomarAgua);
        UnityEventTools.AddIntPersistentListener(compraAgua.alComprar, b.mision.CompletarObjetivo, OBJ_AGUA);

        // La tienda de Doña Marta (tipo Zelda/Pokemon)
        b.tienda.productos = new ControladorCompra[] { productoVino.GetComponent<ControladorCompra>(), productoCompleto.GetComponent<ControladorCompra>(), compraAgua };
        b.tienda.descripciones = new string[] {
            "Cajita de vino tinto. Para el Don Lucho, que no suelta el dato sin su \"gatito\".",
            "Completo italiano con harta mayo. Dicen que el Toby se ablanda con esto.",
            "Agua mineral sin gas. Para la caña: sin esto no subes ni un escalón." };
        b.tienda.coloresIcono = new Color[] { new Color(0.65f, 0.15f, 0.3f), new Color(0.95f, 0.75f, 0.4f), new Color(0.4f, 0.8f, 1f) };
        b.tienda.comentarios = new string[] { "¿Pa'l Don Lucho, cierto? Ese viejo me debe medio almacén.", "Con harta mayo. Como debe ser.", "Tómatela al tiro, mijo, que se te nota la caña." };
        b.tienda.vendedor = marta.transform;
        // No se ven en el meson
        foreach (GameObject p in new[] { productoVino, productoCompleto, agua })
            foreach (Renderer r in p.GetComponentsInChildren<Renderer>()) Object.DestroyImmediate(r);
    }

    static void CrearToby(Transform zona)
    {
        // Toby (greybox: un bloque cafe; el modelo llega en la F5). Usa MobController de clase.
        GameObject toby = new GameObject("NPC_Toby");
        toby.transform.SetParent(zona, false);
        toby.transform.position = new Vector3(0f, 0f, 53.6f); // al pie de la escalera
        toby.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cuerpo.name = "Cuerpo";
        cuerpo.transform.SetParent(toby.transform, false);
        cuerpo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        cuerpo.transform.localScale = new Vector3(0.45f, 0.5f, 0.9f);
        cuerpo.GetComponent<Renderer>().sharedMaterial = Color_("Perro", 0.55f, 0.35f, 0.2f);
        Object.DestroyImmediate(cuerpo.GetComponent<Collider>());

        NavMeshAgent agente = toby.AddComponent<NavMeshAgent>();
        agente.speed = 2.5f;
        agente.radius = 0.3f;
        agente.height = 0.7f;
        Animator anim = toby.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimatorSimple);
        MobController mob = ConstructorBadCarreteDay.Agregar<MobController>(toby);
        mob.agente = agente;
        mob.animatorMob = anim;
        mob.comportamiento = MobController.Comportamiento.APuntoObjetivo;
        MirarJugador mirarToby = ConstructorBadCarreteDay.Agregar<MirarJugador>(toby);
        mirarToby.girarCabeza = false;
        mirarToby.girarCuerpoCerca = true;
        mirarToby.distancia = 5f;

        Transform plato = new GameObject("Plato_Toby").transform;
        plato.SetParent(zona, false);
        plato.position = new Vector3(5f, 0f, 52.5f);

        // Barrera invisible en la escalera hasta que coma
        GameObject barrera = Bloque(zona, "Barrera_Toby", new Vector3(0f, 2f, 55.4f), new Vector3(4f, 4f, 0.4f), null);
        barrera.GetComponent<Renderer>().enabled = false;

        // Toby cuida la escalera: gruñe y se te lanza (PerroGuardian). Habla en burbujas.
        PerroGuardian guardian = ConstructorBadCarreteDay.Agregar<PerroGuardian>(toby);
        guardian.fuente = toby.AddComponent<AudioSource>();
        guardian.fuente.spatialBlend = 0.5f;   // se escucha de lejos (sigue siendo 3D)
        guardian.fuente.volume = 1f;
        guardian.fuente.minDistance = 6f;
        guardian.fuente.maxDistance = 40f;
        guardian.ladrido = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/BadCarreteDay/Arte/Audio/SFX/dog_barking_mono.wav");
        guardian.grunido = ConstructorSonidos.Clip("Grunido");
        guardian.burbujas = b.burbujas;
        Voz vozToby = ConstructorBadCarreteDay.Agregar<Voz>(toby);
        vozToby.nombre = "Toby";
        vozToby.alturaCabeza = 1.0f;

        // La escalera se abre con DOS condiciones: Toby comiendo y Benja sin caña (agua)
        GameObject condiciones = new GameObject("Condiciones_Escalera");
        condiciones.transform.SetParent(zona, false);
        escaleraToby = ConstructorBadCarreteDay.Agregar<ContadorCondiciones>(condiciones);
        escaleraToby.requeridas = 2;
        UnityEventTools.AddBoolPersistentListener(escaleraToby.alCumplirTodas, barrera.SetActive, false);
        UnityEventTools.AddStringPersistentListener(escaleraToby.alCumplirTodas, b.hud.MostrarPensamiento, "Toby comiendo y yo sin caña. Ahora sí: ¡pa' arriba!");
        UnityEventTools.AddPersistentListener(productoAguaCompra.alComprar, escaleraToby.Cumplir);
        // Si llega a la escalera con caña, Benja se da cuenta (el aviso vive en la barrera)
        GameObject avisoCana = new GameObject("Aviso_Cana");
        avisoCana.transform.SetParent(zona, false);
        avisoCana.transform.position = new Vector3(0f, 0f, 54.4f);
        BoxCollider zonaAviso = avisoCana.AddComponent<BoxCollider>();
        zonaAviso.isTrigger = true;
        zonaAviso.size = new Vector3(4f, 3f, 1.6f);
        zonaAviso.center = new Vector3(0f, 1.5f, 0f);
        UnityEventTools.AddBoolPersistentListener(escaleraToby.alCumplirTodas, avisoCana.SetActive, false);
        ControladorColisionTrigger aviso = ConstructorBadCarreteDay.Agregar<ControladorColisionTrigger>(avisoCana);
        aviso.tagEsperado = "Player";
        UnityEventTools.AddStringPersistentListener(aviso.onTriggerEnterEvent, b.hud.MostrarPensamiento, "Con esta caña no subo ni un escalón... necesito un agua.");

        // Pegatina ¡AQUÍ! en el PLATO (fuera de su alcance) para dejarle el completo
        pegatinaCompleto = Pegatina(zona, "Pegatina_Completo", plato.position + Vector3.up * 0.02f, "[E] Dejar el completo en el plato");
        pegatinaCompleto.SetActive(false);
        UnityEventTools.AddBoolPersistentListener(productoCompleto.GetComponent<ControladorCompra>().alComprar, pegatinaCompleto.SetActive, true);

        DemoControlInteraccion dejar = pegatinaCompleto.GetComponent<DemoControlInteraccion>();
        UnityEventTools.AddObjectPersistentListener<Transform>(dejar.eventos.alInteractuar, mob.SetTarget, plato);
        UnityEventTools.AddPersistentListener(dejar.eventos.alInteractuar, guardian.Calmarse);
        UnityEventTools.AddPersistentListener(dejar.eventos.alInteractuar, mirarToby.Dejar);
        UnityEventTools.AddPersistentListener(dejar.eventos.alInteractuar, escaleraToby.Cumplir);
        UnityEventTools.AddIntPersistentListener(dejar.eventos.alInteractuar, b.mision.CompletarObjetivo, OBJ_DAR_COMPLETO);
        UnityEventTools.AddStringPersistentListener(dejar.eventos.alInteractuar, b.hud.MostrarPensamiento, "Dos lukas en un completo pa' un perro. Mi mamá estaría orgullosa.");
        UnityEventTools.AddBoolPersistentListener(dejar.eventos.alInteractuar, pegatinaCompleto.SetActive, false);
    }

    // ===== ZONA 3: ESCALERA DE TOBY, CALLE ALTA, ESCALERA ROTA Y TECHOS (y 0 -> 4 -> 8) =====
    // La escalera sube caminando a la calle alta (y 4). Al este de la calle estan las casas del
    // "lado bajo" del cerro: sus techos (y 8) quedan 4 m sobre la calle. Se sube por la escalera
    // rota + el bidon al techo A y se PLANEA al techo B (debajo hay un patio: no es el vacio).

    static void Zona3_EscaleraYTechos()
    {
        Transform z = Grupo("Zona3_EscaleraYTechos");
        Material cemento = Color_("Cemento", 0.7f, 0.68f, 0.66f);

        // Escalera caminable de Toby: 20 escalones de 0,2 m (plaza y 0 -> calle alta y 4)
        ConstructorNivel1.Escalera(z, "Escalon", -2f, 2f, 55.2f, 63f, 0f, 4f, 20, cemento);

        // Calle alta (y 4) con edificios al oeste (el cerro sigue subiendo detras)
        ConstructorNivel1.Suelo(z, "Calle_Alta", -10f, 2f, 62.8f, 110.8f, 4f, cemento);
        ConstructorNivel1.FilaEdificios(z, new Vector3(-10f, 4f, 62.8f), new Vector3(-10f, 4f, 110.8f), Vector3.right, false);

        // Lado bajo (este): casas sobre pilotes con la entrada a nivel de la calle
        ConstructorNivel1.FilaEdificios(z, new Vector3(2f, 4f, 63f), new Vector3(2f, 4f, 80.4f), Vector3.left, true);
        // Techo A (azotea y 8) y techo B (azotea y 10,5): hueco de 3 m y el B mas alto.
        // Se cruza corriendo, saltando y COLGANDOSE del borde del techo B (sin planeo).
        Bounds techoA = CasaConAzotea(z, "Techo_A", "building_G", 85.2f, 8f);
        Bounds techoB = CasaConAzotea(z, "Techo_B", "building_E", techoA.max.z + 3f + 4.73f, 10.5f);
        // Patio entre los dos (y 4): si caes, caes aqui y vuelves a la calle
        ConstructorNivel1.Suelo(z, "Patio", 2f, techoA.max.x, techoA.max.z - 0.05f, techoB.min.z + 0.05f, 4f, cemento);
        ConstructorNivel1.Baranda(z, new Vector3(techoA.max.x, 4f, techoA.max.z), new Vector3(techoA.max.x, 4f, techoB.min.z));

        // ESCALERA ROTA: de la calle (y 4) al descanso (y 6), con escalones de 0,5 m y huecos: hay que saltar
        for (int i = 0; i < 4; i++)
            ConstructorNivel1.Caja(z, "EscalonRoto_" + i, new Vector3(0f, 4.25f + i * 0.25f, 75f + i * 1.1f), new Vector3(3f, 0.5f + i * 0.5f, 1.1f), cemento);
        // Descanso (y 6) pegado a la fachada del techo A (y 8): falta 2 m -> empujar el bidon
        ConstructorNivel1.Caja(z, "Descanso", new Vector3(0f, 5f, 82.7f), new Vector3(4f, 2f, 6.4f), cemento);
        Pensamiento(z, new Vector3(0f, 4f, 73f), 3f, "Ni las escaleras se salvan del carrete... Salto a los bordes y me cuelgo. O empujo ese bidón (camino contra él).");

        // Bidon para empujar contra la fachada y usarlo de escalon (BasicRigidBodyPush de Starter Assets)
        GameObject bidon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bidon.name = "Bidon_Empujable";
        bidon.transform.SetParent(z, false);
        bidon.transform.position = new Vector3(0.3f, 6.6f, 84.5f);
        bidon.transform.localScale = new Vector3(1.1f, 0.6f, 1.1f);
        bidon.GetComponent<Renderer>().sharedMaterial = Color_("Bidon", 0.2f, 0.45f, 0.85f);
        Rigidbody rb = bidon.AddComponent<Rigidbody>();
        rb.mass = 3f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        // Barandas del descanso: el bidon no se cae a la calle
        ConstructorNivel1.Baranda(z, new Vector3(-2f, 6f, 79.5f), new Vector3(-2f, 6f, 85.9f));
        ConstructorNivel1.Baranda(z, new Vector3(-2f, 6f, 85.9f), new Vector3(2f, 6f, 85.9f));

        CrearSenoraRosa(z);
        CrearSenoraGladys(z);
        TachoRevisable(z, new Vector3(-9.3f, 4f, 95f), 500, "Una quina entre las cáscaras de huevo. Asqueroso. Me la quedo.");
        SecretoEscaleraRota(z);

        Vector3 centroA = new Vector3(techoA.center.x, 8f, techoA.center.z);
        LugarObjetivo(z, "Llegada_TechoA", centroA + Vector3.back * 2f, 3f, b.mision, OBJ_SUBIR_ESCALERA, null);
        Checkpoint(z, "Checkpoint_TechoA", centroA + new Vector3(0f, 0.1f, -1f));

        // Anden del ascensor (y 10,5) a continuacion del techo B
        ConstructorNivel1.Caja(z, "Anden_Abajo", new Vector3((2f + techoB.max.x) / 2f, 10.35f, (techoB.max.z + 110.08f) / 2f),
                               new Vector3(techoB.max.x - 2f, 0.3f, 110.08f - techoB.max.z + 0.1f), cemento);
        // Borde este de los techos (da a la ladera): limite alto
        ConstructorNivel1.Limite(z, new Vector3(techoA.max.x, 4f, techoA.min.z), new Vector3(techoA.max.x, 4f, 110.1f));
    }

    // Casa del lado bajo con azotea a la altura pedida (la base queda mas abajo, en la ladera)
    static Bounds CasaConAzotea(Transform z, string nombre, string modelo, float centroZ, float alturaAzotea)
    {
        GameObject casa = ConstructorNivel1.Edificio(z, modelo, new Vector3(2f, 0f, centroZ), Vector3.left, false, false);
        Bounds b0 = ConstructorF5.Limites(casa);
        casa.transform.position += Vector3.up * (alturaAzotea - 0.2f - b0.max.y);
        Bounds b = ConstructorF5.Limites(casa);
        ConstructorNivel1.Caja(z, nombre, new Vector3(b.center.x, alturaAzotea - 0.1f, b.center.z), new Vector3(b.size.x, 0.2f, b.size.z), Color_("Azotea", 0.62f, 0.6f, 0.58f));
        return b;
    }

    // ===== ZONA 4: ASCENSOR + PASAJE OSCURO =====

    static void Zona4_AscensorYPasajeOscuro()
    {
        Transform z = Grupo("Zona4_PasajeOscuro");

        // Cima del cerro (y 16) y la cara empinada del cerro (muro de piedra inclinado)
        ConstructorNivel1.Suelo(z, "Cerro_Alto", -16f, 16f, 115f, 172f, 16f, Color_("Tierra", 0.5f, 0.42f, 0.35f));
        float angulo = Mathf.Atan2(4.4f, 12f) * Mathf.Rad2Deg;
        GameObject cara = ConstructorNivel1.MuroPiedra(z, "Cara_Cerro", Vector3.zero, new Vector3(34f, 12.8f, 3f));
        cara.transform.rotation = Quaternion.Euler(angulo, 0f, 0f);
        cara.transform.position = new Vector3(1f, 10f, 112.9f) + cara.transform.rotation * Vector3.forward * 1.5f;
        // No se puede trepar la cara del cerro: muro invisible delante (calle) y a los lados del anden
        ConstructorNivel1.Limite(z, new Vector3(-16f, 4f, 110.6f), new Vector3(2f, 4f, 110.6f), 13f);
        ConstructorNivel1.Limite(z, new Vector3(2f, 10.5f, 113.2f), new Vector3(4.4f, 10.5f, 113.2f), 3.4f);
        ConstructorNivel1.Limite(z, new Vector3(7.6f, 10.5f, 113.2f), new Vector3(10f, 10.5f, 113.2f), 3.4f);
        ConstructorNivel1.Limite(z, new Vector3(9f, 10.5f, 104f), new Vector3(9f, 10.5f, 113.2f), 3.4f);

        // Ascensor (funicular): cabina que sube por la cara del cerro del anden (y 8) a la cima (y 16)
        GameObject ascensor = Bloque(z, "Ascensor", new Vector3(6f, 10.3f, 111.58f), new Vector3(3f, 0.4f, 3f), Color_("Ascensor", 0.9f, 0.3f, 0.3f));
        ascensor.isStatic = false;
        ascensor.AddComponent<Rigidbody>().isKinematic = true;
        BoxCollider sensor = ascensor.AddComponent<BoxCollider>();
        sensor.isTrigger = true;
        sensor.center = new Vector3(0f, 3f, 0f);
        sensor.size = new Vector3(1f, 6f, 1f);
        PlataformaMovil plataforma = ConstructorBadCarreteDay.Agregar<PlataformaMovil>(ascensor);
        plataforma.puntoInicio = Punto(z, "Ascensor_Abajo", new Vector3(6f, 10.3f, 111.58f));
        plataforma.puntoFin = Punto(z, "Ascensor_Arriba", new Vector3(6f, 15.8f, 113.6f));
        plataforma.velocidad = 2f;
        UnityEventTools.AddStringPersistentListener(plataforma.alLlegarAlFin, b.hud.MostrarPensamiento, "...funcionó. Igual que yo: a duras penas.");

        // Foso del ascensor: cuando la cabina sube no queda un hoyo (ni a los costados de la cabina)
        Material cementoFoso = Color_("Cemento", 0.7f, 0.68f, 0.66f);
        ConstructorNivel1.Caja(z, "Foso_Ascensor", new Vector3(6f, 9.95f, 111.64f), new Vector3(3f, 0.3f, 3.2f), cementoFoso);
        ConstructorNivel1.Caja(z, "Anden_LadoOeste", new Vector3(3.25f, 10.35f, 111.64f), new Vector3(2.5f, 0.3f, 3.2f), cementoFoso);
        ConstructorNivel1.Caja(z, "Anden_LadoEste", new Vector3(8.2f, 10.35f, 111.64f), new Vector3(1.4f, 0.3f, 3.2f), cementoFoso);

        // Palanca = pegatina ¡AQUÍ! sobre el ascensor (se mueve con el)
        GameObject palanca = Pegatina(ascensor.transform, "Pegatina_Palanca", Vector3.zero, "[E] Tirar la palanca");
        palanca.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        palanca.transform.localScale = new Vector3(0.45f, 0.05f, 0.45f);
        DemoControlInteraccion tirar = palanca.GetComponent<DemoControlInteraccion>();
        UnityEventTools.AddPersistentListener(tirar.eventos.alInteractuar, plataforma.Activar);
        UnityEventTools.AddIntPersistentListener(tirar.eventos.alInteractuar, b.mision.CompletarObjetivo, OBJ_ASCENSOR);
        UnityEventTools.AddStringPersistentListener(tirar.eventos.alInteractuar, b.hud.MostrarPensamiento, "Ya po, ascensor... coopera.");
        UnityEventTools.AddBoolPersistentListener(tirar.eventos.alInteractuar, palanca.SetActive, false);

        // Pasaje oscuro (y 16): callejon entre dos filas de casas. Solo se puede entrar por el.
        Checkpoint(z, "Checkpoint_Cima", new Vector3(6f, 16.1f, 116.5f));
        ConstructorNivel1.FilaEdificios(z, new Vector3(-3f, 16f, 118.2f), new Vector3(-3f, 16f, 135.5f), Vector3.right, false);
        ConstructorNivel1.FilaEdificios(z, new Vector3(3f, 16f, 118.2f), new Vector3(3f, 16f, 135.5f), Vector3.left, false);
        ConstructorNivel1.Limite(z, new Vector3(-16f, 16f, 118.2f), new Vector3(-3f, 16f, 118.2f));
        ConstructorNivel1.Limite(z, new Vector3(3f, 16f, 118.2f), new Vector3(16f, 16f, 118.2f));
        ConstructorNivel1.Limite(z, new Vector3(-16f, 16f, 115f), new Vector3(-16f, 16f, 172f));
        ConstructorNivel1.Limite(z, new Vector3(16f, 16f, 115f), new Vector3(16f, 16f, 172f));
        ConstructorNivel1.Limite(z, new Vector3(-16f, 16f, 115f), new Vector3(2f, 16f, 115f));
        // Borde este junto a la estacion de arriba: reja con puas
        ConstructorNivel1.Reja(z, new Vector3(15.4f, 16f, 115.2f), new Vector3(15.4f, 16f, 136.6f), 2.2f, true, false);

        CrearCumasDelPasaje(z);

    }

    // ===== ZONA 5: CANCHA (JULIO) =====

    static void Zona5_Cancha()
    {
        Transform z = Grupo("Zona5_Cancha");
        Bloque(z, "Cancha_Tierra", new Vector3(0f, 16.02f, 150f), new Vector3(24f, 0.04f, 26f), Color_("Cancha", 0.75f, 0.55f, 0.35f));
        // Casas alrededor: al oeste el cerro sigue subiendo; al este se dejan huecos para ver la bahia
        ConstructorNivel1.FilaEdificios(z, new Vector3(-13.3f, 16f, 136.8f), new Vector3(-13.3f, 16f, 171f), Vector3.right, false);
        ConstructorNivel1.FilaEdificios(z, new Vector3(13.3f, 16f, 136.8f), new Vector3(13.3f, 16f, 146f), Vector3.left, true);
        ConstructorNivel1.FilaEdificios(z, new Vector3(13.3f, 16f, 157f), new Vector3(13.3f, 16f, 171f), Vector3.left, true);
        ConstructorNivel1.FilaEdificios(z, new Vector3(-13.3f, 16f, 165f), new Vector3(13.3f, 16f, 165f), Vector3.back, false);
        CrearRejaCancha(z);
        Bloque(z, "Arco_Poste_Izq", new Vector3(-2.5f, 17f, 162f), new Vector3(0.2f, 2f, 0.2f), Color_("Blanco", 0.95f, 0.95f, 0.95f));
        Bloque(z, "Arco_Poste_Der", new Vector3(2.5f, 17f, 162f), new Vector3(0.2f, 2f, 0.2f), Color_("Blanco", 0.95f, 0.95f, 0.95f));
        Bloque(z, "Arco_Travesano", new Vector3(0f, 18f, 162f), new Vector3(5.2f, 0.2f, 0.2f), Color_("Blanco", 0.95f, 0.95f, 0.95f));

        Checkpoint(z, "Checkpoint_Cancha", new Vector3(0f, 16.1f, 136f));
        LugarObjetivo(z, "Llegada_Cancha", new Vector3(0f, 16f, 140f), 4f, b.mision, OBJ_LLEGAR_CANCHA, null);

        CrearPeleaJulio(z);
    }

    // Reja de la cancha (con collider): entrada abierta frente al pasaje oscuro (x -3..3)
    static void CrearRejaCancha(Transform z)
    {
        // Reja SIN puas (3,2 m): se puede escalar colgandose del borde. Detras hay casas y limites.
        float y = 16f, alto = 3.2f;
        ConstructorNivel1.Reja(z, new Vector3(-16f, y, 136.8f), new Vector3(-3f, y, 136.8f), alto, false, false);
        ConstructorNivel1.Reja(z, new Vector3(3f, y, 136.8f), new Vector3(16f, y, 136.8f), alto, false, false);
        ConstructorNivel1.Reja(z, new Vector3(-12.4f, y, 136.8f), new Vector3(-12.4f, y, 163.6f), alto, false, false);
        ConstructorNivel1.Reja(z, new Vector3(12.4f, y, 136.8f), new Vector3(12.4f, y, 163.6f), alto, false, false);
        ConstructorNivel1.Reja(z, new Vector3(-12.4f, y, 163.6f), new Vector3(12.4f, y, 163.6f), alto, false, false);
    }

    // Material de malla romboidal con transparencia recortada (alpha clip), visible por ambos lados
    static Texture2D texturaMalla;
    public static Material MaterialMalla(float largo)
    {
        if (texturaMalla == null)
        {
            int n = 64;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Dos diagonales = rombos; el alambre ocupa ~3 pixeles
                    int d1 = Mathf.Abs((x + y) % n - n / 2), d2 = Mathf.Abs((x - y + n) % n - n / 2);
                    bool alambre = d1 > n / 2 - 3 || d2 > n / 2 - 3;
                    t.SetPixel(x, y, alambre ? new Color(0.55f, 0.58f, 0.6f, 1f) : new Color(0f, 0f, 0f, 0f));
                }
            t.Apply();
            texturaMalla = ConstructorF7.GuardarTextura("Malla_Rombos", t, true);
        }
        Material m = ConstructorF2.Material("Malla_" + Mathf.RoundToInt(largo), Color.white);
        m.SetTexture("_BaseMap", texturaMalla);
        m.SetTextureScale("_BaseMap", new Vector2(largo / 0.6f, 3.1f / 0.6f));
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f); // se ve de los dos lados
        m.doubleSidedGI = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ===== CAÑA, LIMITES DEL MAPA Y PRESENTACIONES =====

    // La caña de Benja: lento y sin correr hasta tomarse el agua
    static void CrearResaca()
    {
        GameObject jugador = b.animJugador.gameObject;
        resaca = ConstructorBadCarreteDay.Agregar<Resaca>(jugador);
        resaca.movimiento = jugador.GetComponent<ThirdPersonController>();
        resaca.entradas = jugador.GetComponent<StarterAssetsInputs>();
        resaca.agarre = jugador.GetComponent<AgarreBorde>();
        resaca.animator = b.animJugador;
        resaca.hud = b.hud;
        // Vista con caña: borrosa, bordes oscuros y colores apagados (encima del post-proceso normal)
        GameObject vista = new GameObject("PostProceso_Cana");
        Volume volumen = vista.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 10f;
        volumen.sharedProfile = PerfilCana();
        resaca.vistaCana = volumen;
    }

    // Zonas jugables: si el jugador sale de todas, vuelve al checkpoint sin perder vida
    static void CrearLimiteMapa()
    {
        LimiteMapa limite = ConstructorBadCarreteDay.Agregar<LimiteMapa>(GameObject.Find("Sistemas"));
        limite.jugador = b.animJugador.transform;
        limite.juego = b.juego;
        limite.hud = b.hud;
        GameObject fundido = GameObject.Find("Fundido");
        if (fundido != null) limite.fundido = fundido.GetComponent<UnityEngine.UI.Image>();
        limite.zonas = new Bounds[] {
            Caja(-4.3f, 4.3f, -2f, 12f, -6.6f, 31.6f),     // pasaje
            Caja(4.2f, 12.5f, -2f, 12f, 11.8f, 20.2f),     // sitio eriazo
            Caja(-8.1f, 14f, -2f, 12f, 30.9f, 55.3f),      // plaza
            Caja(-2.1f, 2.1f, -2f, 14f, 55f, 63.2f),       // escalera de Toby
            Caja(-10.1f, 2.1f, 2f, 18f, 62.6f, 110.8f),    // calle alta
            Caja(1.9f, 9.3f, 2f, 20f, 80f, 93f),           // techo A y patio
            Caja(1.9f, 9.3f, 9.5f, 20f, 92.9f, 113.4f),    // techo B y anden (si se cae por debajo, vuelve)
            Caja(4f, 8f, 9.5f, 22f, 108f, 116f),           // ascensor
            Caja(-16.1f, 16.1f, 14f, 32f, 114.9f, 172.1f), // cima: pasaje oscuro y cancha
        };
    }

    static Bounds Caja(float x0, float x1, float y0, float y1, float z0, float z1)
    {
        return new Bounds(new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f), new Vector3(x1 - x0, y1 - y0, z1 - z0));
    }

    // Presentaciones de zona estilo Conker: la primera vez, la camara muestra el objetivo y Benja comenta
    static void CrearPresentaciones()
    {
        Transform z = Grupo("Presentaciones");
        Presentacion(z, "Plaza", new Vector3(0f, 0f, 33f), 2.5f, new Vector3(4f, 5f, 31.5f), new Vector3(-8f, 2f, 41f),
                     "Un almacén abierto a las 5 de la mañana... esto es Valpo.");
        Presentacion(z, "Toby", new Vector3(1f, 0f, 47f), 3f, new Vector3(3.5f, 2.4f, 47.5f), new Vector3(0f, 0.6f, 53.6f),
                     "Ese quiltro no deja pasar a nadie... ni me le acerco sin algo pa' darle.");
        Presentacion(z, "EscaleraRota", new Vector3(-4f, 4f, 69f), 3f, new Vector3(-7f, 7.5f, 70f), new Vector3(0f, 5.5f, 81f),
                     "La escalera está rota... pero me puedo colgar de los bordes.");
        Presentacion(z, "Techos", new Vector3(5.4f, 8f, 84f), 2.5f, new Vector3(5.4f, 11.5f, 82.5f), new Vector3(5.4f, 10f, 97f),
                     "El otro techo está más alto: corro (Shift), salto y me cuelgo del borde.");
        Presentacion(z, "Ascensor", new Vector3(5.4f, 10.5f, 107.5f), 2.5f, new Vector3(3f, 12.5f, 104.5f), new Vector3(6f, 13f, 113f),
                     "Un ascensor... ojalá ande. Tiene una palanca.");
        Presentacion(z, "PasajeOscuro", new Vector3(6f, 16f, 116.8f), 2.5f, new Vector3(0f, 18.6f, 118f), new Vector3(0f, 17f, 129f),
                     "Puros cumas... ojalá tuviera algo pa' defenderme.");
        Presentacion(z, "Cancha", new Vector3(0f, 16f, 137.6f), 2f, new Vector3(0f, 19f, 137.5f), new Vector3(0f, 17f, 152f),
                     "Ahí está Julio... ¡con MI celular!");
    }

    // Atajos de prueba (F1..F7): solo en el editor y builds de desarrollo
    static void CrearAtajos()
    {
        AtajosDesarrollo atajos = ConstructorBadCarreteDay.Agregar<AtajosDesarrollo>(GameObject.Find("Sistemas"));
        atajos.dinero = b.dinero;
        atajos.mision = b.mision;
        atajos.hud = b.hud;
        atajos.jugador = b.animJugador.transform;
        atajos.compraAgua = productoAguaCompra;
        atajos.compraVino = productoVino.GetComponent<ControladorCompra>();
        atajos.compraCompleto = productoCompleto.GetComponent<ControladorCompra>();
        atajos.dialogoLucho = dialogoLucho;
        atajos.dejarCompleto = pegatinaCompleto.GetComponent<DemoControlInteraccion>();
        atajos.ataque = b.animJugador.GetComponent<AtaqueJugador>();
        atajos.objetivoTechoA = OBJ_SUBIR_ESCALERA;
        atajos.objetivoAscensor = OBJ_ASCENSOR;

        // Botones en el menu de pausa (columna a la derecha): "Ir a..." cada punto de la mision
        GameObject panel = ConstructorF2.Panel(b.juego.panelPausa.transform, "PanelAtajos", new Color(0.08f, 0.05f, 0.18f, 0.9f),
                                               new Vector2(0.5f, 0.5f), new Vector2(560f, 20f), new Vector2(380f, 700f));
        ConstructorF2.Texto(panel.transform, "TituloAtajos", "IR A (PRUEBAS)", new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(360f, 50f), 32, TMPro.TextAlignmentOptions.Center);
        string[] puntos = { "Inicio", "Con agua", "Vino entregado", "Toby comiendo", "Techo A", "Cima (cumas)", "Cancha (Julio)" };
        for (int i = 0; i < puntos.Length; i++)
        {
            GameObject boton = ConstructorF2.Panel(panel.transform, "BotonAtajo_" + (i + 1), new Color(1f, 0.35f, 0.7f), new Vector2(0.5f, 1f),
                                                   new Vector2(0f, -110f - i * 80f), new Vector2(330f, 66f));
            UnityEngine.UI.Button b2 = boton.AddComponent<UnityEngine.UI.Button>();
            UnityEventTools.AddIntPersistentListener(b2.onClick, atajos.IrA, i + 1);
            ConstructorF2.Texto(boton.transform, "Texto", (i + 1) + ". " + puntos[i], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 60f), 30, TMPro.TextAlignmentOptions.Center);
        }
        atajos.panelAtajos = panel;
    }

    static void Presentacion(Transform padre, string nombre, Vector3 disparador, float radio, Vector3 camara, Vector3 mirar, string comentario)
    {
        ControladorLugarObjetivo lugar = LugarObjetivo(padre, "Presentacion_" + nombre, disparador, radio, null, -1, null);
        PresentacionZona p = ConstructorBadCarreteDay.Agregar<PresentacionZona>(lugar.gameObject);
        p.camara = b.camaraPresentacion;
        p.posicionCamara = camara;
        p.mirarA = mirar;
        p.comentario = comentario;
        p.juego = b.juego;
        p.hud = b.hud;
        UnityEventTools.AddPersistentListener(lugar.alLlegarJugador, p.Presentar);
    }

    // Perfil de la vista con caña (se guarda como asset)
    static VolumeProfile PerfilCana()
    {
        string ruta = "Assets/BadCarreteDay/Materiales/PostProceso_Cana.asset";
        AssetDatabase.DeleteAsset(ruta);
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(perfil, ruta);
        DepthOfField desenfoque = perfil.Add<DepthOfField>(true);
        desenfoque.mode.Override(DepthOfFieldMode.Gaussian);
        desenfoque.gaussianStart.Override(4.5f);   // Benja (a ~3,5 m de la camara) se ve; lo de mas alla, borroso
        desenfoque.gaussianEnd.Override(14f);
        desenfoque.gaussianMaxRadius.Override(1.5f);
        desenfoque.highQualitySampling.Override(true);
        Vignette vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(0.55f);
        vineta.smoothness.Override(0.6f);
        vineta.color.Override(new Color(0.05f, 0.02f, 0.05f));
        ColorAdjustments color = perfil.Add<ColorAdjustments>(true);
        color.saturation.Override(-45f);
        color.postExposure.Override(-0.15f);
        foreach (VolumeComponent c in perfil.components)
            AssetDatabase.AddObjectToAsset(c, perfil);
        AssetDatabase.SaveAssets();
        return perfil;
    }

    static void CrearVacio()
    {
        GameObject vacio = new GameObject("Zona_Vacio");
        vacio.transform.position = new Vector3(0f, -15f, 80f);
        BoxCollider col = vacio.AddComponent<BoxCollider>();
        col.size = new Vector3(400f, 2f, 400f);
        ControladorColisionTrigger zona = ConstructorF2.Trigger(vacio);
        UnityEventTools.AddVoidPersistentListener(zona.onTriggerEnterEvent, b.juego.CaerAlVacio);
    }

    // ===== COMBATE (F4) =====

    static void CrearCumasDelPasaje(Transform zona)
    {
        ContadorEnemigos contador = ConstructorBadCarreteDay.Agregar<ContadorEnemigos>(new GameObject("Contador_Cumas"));
        contador.transform.SetParent(zona, false);
        contador.totalEnemigos = 3;

        string[] alDetectar = { "¡Oe, oe! ¿Y vo' quién soi?", "¡Pasa las lukas, aw*****o!", "¡Esta es nuestra pobla, perkin!" };
        string[] alGolpe = { "¡Ay, mi jockey!", "¡Te cociste!" };
        ControladorEnemigo[] cumas = {
            Enemigo("Cuma_James", "James", new Vector3(-1f, 16f, 125f), 50f, 8f, alDetectar, alGolpe, Color.white),
            Enemigo("Cuma_David", "David", new Vector3(1f, 16f, 128f), 50f, 8f, alDetectar, alGolpe, Color.white),
            Enemigo("Cuma_James_Rojo", "James", new Vector3(0f, 16f, 131f), 50f, 8f, alDetectar, alGolpe, new Color(1f, 0.55f, 0.55f)),
        };

        // Al entrar al pasaje te ven (repetible: si reapareces, vuelven a buscarte)
        GameObject zonaAviso = new GameObject("Zona_AvisoCumas");
        zonaAviso.transform.SetParent(zona, false);
        zonaAviso.transform.position = new Vector3(0f, 16f, 124.5f); // empieza despues del palo de escoba
        BoxCollider col = zonaAviso.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(6f, 4f, 6f);
        col.center = new Vector3(0f, 2f, 0f);
        ControladorLugarObjetivo aviso = ConstructorBadCarreteDay.Agregar<ControladorLugarObjetivo>(zonaAviso);
        aviso.empiezaStart = true;
        aviso.tipoObjetivo = ControladorLugarObjetivo.TipoObjetivo.repetible;

        foreach (ControladorEnemigo c in cumas)
        {
            UnityEventTools.AddPersistentListener(aviso.alLlegarJugador, c.Activar);
            UnityEventTools.AddPersistentListener(c.alDerrotado, contador.EnemigoDerrotado);
        }

        // Reja al final del pasaje: se abre cuando caen los 3
        GameObject reja = Bloque(zona, "Reja_Pasaje", new Vector3(0f, 18f, 135.8f), new Vector3(6.2f, 4f, 0.3f), Color_("Reja", 0.25f, 0.25f, 0.28f));
        UnityEventTools.AddBoolPersistentListener(contador.alTodosDerrotados, reja.SetActive, false);
        UnityEventTools.AddIntPersistentListener(contador.alTodosDerrotados, b.mision.CompletarObjetivo, OBJ_CUMAS);
        UnityEventTools.AddStringPersistentListener(contador.alTodosDerrotados, b.hud.MostrarPensamiento, "La reja quedó abierta. Julio... voy por ti.");
    }

    static void CrearPeleaJulio(Transform zona)
    {
        // Julio: TANQUE trasnochado. 400 de salud, lento, golpe pesado (20); de frente no siente el dolor
        // y en la espalda tiene un cuchillo clavado (ahi el daño x3)
        string[] seRie = { "Jajaja... ¿eso es todo?", "Jejeje... hazme cosquillas.", "*risa* No siento ni la cara, compadre.", "¿Me pegaste? Jajaja." };
        ControladorEnemigo julio = Enemigo("Julio_ElPastero", "Mremireh", new Vector3(0f, 16f, 152f), 400f, 20f, null, seRie, Color.white);
        julio.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        julio.transform.localScale = Vector3.one * 1.2f;
        julio.nombre = "Julio";
        julio.destruirAlMorir = false; // queda tirado y habla
        julio.puntoDebilEspalda = true;
        julio.multiplicadorEspalda = 3f;
        julio.sienteDolor = false;
        julio.giroAlPegar = 0f; // el giro de Julio lo maneja ConductaJulio (lento)
        julio.frasesEspalda = new[] { "¡AAAH! ¡EL CUCHILLO NO, C*****O!", "¡Ay! ¡Ahí no, ahí no!", "¡¿Quién te dijo lo del cuchillo?!" };
        julio.mob.agente.speed = 1.6f;
        julio.mob.agente.angularSpeed = 55f;   // gira lento: se le puede rodear (ConductaJulio limita todo giro a 55°/s)
        julio.mob.agente.stoppingDistance = 1.6f;
        julio.mob.toleranciaDist = 2.2f;
        julio.GetComponent<Animator>().runtimeAnimatorController = CrearAnimatorJulio();
        JulioVivo(julio.gameObject);
        GameObject cuchillo = CuchilloEnLaEspalda(julio.gameObject);

        ConductaJulio conducta = ConstructorBadCarreteDay.Agregar<ConductaJulio>(julio.gameObject);
        conducta.enemigo = julio;
        conducta.animator = julio.GetComponent<Animator>();
        conducta.agente = julio.mob.agente;
        conducta.jugador = b.animJugador.transform;
        conducta.hud = b.hud;
        conducta.voz = julio.gameObject.AddComponent<AudioSource>();
        conducta.voz.spatialBlend = 0.7f;
        conducta.voz.minDistance = 4f;
        conducta.voz.maxDistance = 40f;
        conducta.rugido = ConstructorSonidos.Clip("Julio_Rugido");
        conducta.quejido = ConstructorSonidos.Clip("Julio_Quejido");
        // Respiracion agitada (siempre)
        AudioSource respiracion = julio.gameObject.AddComponent<AudioSource>();
        respiracion.clip = ConstructorSonidos.Clip("Julio_Respiracion");
        respiracion.loop = true;
        respiracion.playOnAwake = true;
        respiracion.spatialBlend = 1f;
        respiracion.minDistance = 2f;
        respiracion.maxDistance = 14f;
        respiracion.volume = 0.6f;
        UnityEventTools.AddPersistentListener(julio.alGolpeEspalda, conducta.GolpeEnLaEspalda);
        UnityEventTools.AddPersistentListener(julio.alGolpeFrente, conducta.GolpeDeFrente);
        UnityEventTools.AddPersistentListener(julio.alDerrotado, conducta.Terminar);

        DialogoNPC dialogoJulio = ConstructorBadCarreteDay.Agregar<DialogoNPC>(julio.gameObject);
        dialogoJulio.nombrePersonaje = "Julio";
        MirarJugador mirarJulio = ConstructorBadCarreteDay.Agregar<MirarJugador>(julio.gameObject);
        mirarJulio.volverAlTerminar = false; // despues pelea (lo gira el NavMeshAgent)
        mirarJulio.distancia = 8f;
        mirarJulio.girarCuerpoAlHablar = true; // solo al hablar (antes de la pelea); en pelea lo limita ConductaJulio
        dialogoJulio.conversaciones = new DialogoNPC.Conversacion[]
        {
            Conv("Mira quién llegó... el pao Q* del celular.",
                 "¿Esto buscai? Ni siquiera tiene batería, compadre.",
                 "Si lo querí, vai a tener que quitármelo. Y te aviso: no he dormido en TRES días. Estoy imparable."),
            Conv("Ya, ya, ya... llévatelo. Igual tenía puras fotos tuyas bailando arriba de una mesa.",
                 "Benja: ...¿Arriba de una mesa?"),
        };

        // Al acercarse: titulo "EL PASTERO" y empieza a hablar solo
        ControladorLugarObjetivo encuentro = LugarObjetivo(zona, "Encuentro_Julio", new Vector3(0f, 16f, 146f), 4f, null, -1, dialogoJulio.IniciarDialogo);
        UnityEventTools.AddStringPersistentListener(encuentro.alLlegarJugador, b.hud.MostrarTitulo, "EL PASTERO");

        // Barra de salud del jefe en el HUD
        GameObject panelJefe = ConstructorF2.Panel(b.hud.transform, "PanelJefe", new Color(0f, 0f, 0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(400f, 110f));
        ConstructorF2.Texto(panelJefe.transform, "TituloJefe", "EL PASTERO", new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(400f, 40f), 30, TMPro.TextAlignmentOptions.Center).color = new Color(1f, 0.4f, 0.4f);
        GameObject barra = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/BarraSalud.prefab"), panelJefe.transform);
        RectTransform rtBarra = (RectTransform)barra.transform;
        rtBarra.anchorMin = rtBarra.anchorMax = new Vector2(0.5f, 0.5f);
        rtBarra.anchoredPosition = new Vector2(20f, -20f);

        // Fases del jefe
        ControladorFasesJefe fases = ConstructorBadCarreteDay.Agregar<ControladorFasesJefe>(julio.gameObject);
        fases.jefe = julio;
        fases.saludMaxima = 400f;
        fases.multiplicadorVelocidad = 1f; // la velocidad de la fase 2 la maneja ConductaJulio
        fases.panelBarra = panelJefe;
        fases.barra = barra.GetComponent<ControladorBarraSalud>();
        fases.refuerzos = new ControladorEnemigo[0]; // sin secuaces: la fase 2 es Julio enojado
        UnityEventTools.AddPersistentListener(fases.alEmpezarPelea, conducta.EmpezarPelea);
        UnityEventTools.AddStringPersistentListener(fases.alEmpezarPelea, b.hud.MostrarPensamiento, "¡Tiene un cuchillo clavado en la espalda! Si le pego ahí...");
        UnityEventTools.AddPersistentListener(fases.alFase2, conducta.EmpezarFase2);
        UnityEventTools.AddStringPersistentListener(fases.alFase2, b.hud.MostrarPensamiento, "Julio: ¡¿Y vo' qué me mirai?! ¡TE VOY A REVENTAR!");

        // Flujo: dialogo -> pelea -> derrotado -> dialogo final -> telefono
        UnityEventTools.AddPersistentListener(dialogoJulio.conversaciones[0].alTerminar, fases.EmpezarPelea);
        UnityEventTools.AddPersistentListener(julio.estadisticas.eventos.alCambiarSalud, fases.ActualizarSalud);
        UnityEventTools.AddPersistentListener(julio.alDerrotado, fases.TerminarPelea);
        UnityEventTools.AddIntPersistentListener(julio.alDerrotado, b.mision.CompletarObjetivo, OBJ_JULIO);
        UnityEventTools.AddIntPersistentListener(julio.alDerrotado, dialogoJulio.CambiarConversacion, 1);
        UnityEventTools.AddPersistentListener(julio.alDerrotado, dialogoJulio.IniciarDialogo);

        GameObject telefono = Bloque(zona, "Telefono", new Vector3(1.5f, 16.4f, 150f), new Vector3(0.35f, 0.05f, 0.6f), Color_("Telefono", 0.1f, 0.1f, 0.12f));
        telefono.isStatic = false;
        Collider colTel = telefono.GetComponent<Collider>();
        colTel.isTrigger = true;
        ((BoxCollider)colTel).size = new Vector3(4f, 30f, 3f);
        telefono.SetActive(false);
        UnityEventTools.AddBoolPersistentListener(dialogoJulio.conversaciones[1].alTerminar, telefono.SetActive, true);
        UnityEventTools.AddStringPersistentListener(dialogoJulio.conversaciones[1].alTerminar, b.hud.MostrarPensamiento, "¡Mi teléfono! Ahí está, en el suelo.");
        ControladorColisionTrigger tomarTel = ConstructorF2.Trigger(telefono);
        UnityEventTools.AddIntPersistentListener(tomarTel.onTriggerEnterEvent, b.mision.CompletarObjetivo, OBJ_TELEFONO);
        UnityEventTools.AddBoolPersistentListener(tomarTel.onTriggerEnterEvent, telefono.SetActive, false);
    }

    // ===== JULIO: ANIMATOR, ASPECTO Y CUCHILLO =====

    // Animacion de Julio: la de Mixamo (Arte/Animaciones/Julio) si esta; si no, una que ya tenemos
    static AnimationClip ClipJulio(string mixamo, string siNoEsta)
    {
        AnimationClip c = ConstructorBadCarreteDay.BuscarClip("Julio/" + mixamo);
        if (c == null) c = ConstructorBadCarreteDay.BuscarClip(siNoEsta);
        if (c == null) c = ClipStarter(siNoEsta);
        return c;
    }

    // Animator propio de Julio (tanque): quieto encorvado, caminar pesado, embestida, golpe lento,
    // rugido, risa (no siente el dolor), aturdido (cuchillo) y muerte
    static RuntimeAnimatorController CrearAnimatorJulio()
    {
        AssetDatabase.DeleteAsset(rutaAnimatorJulio);
        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(rutaAnimatorJulio);
        foreach (string p in new[] { "Speed" }) c.AddParameter(p, AnimatorControllerParameterType.Float);
        c.AddParameter("atacando", AnimatorControllerParameterType.Bool);
        foreach (string p in new[] { "golpeado", "muerte", "rugir", "reir" }) c.AddParameter(p, AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine m = c.layers[0].stateMachine;

        BlendTree arbol;
        AnimatorState loco = c.CreateBlendTreeInController("Locomocion", out arbol, 0);
        arbol.blendParameter = "Speed";
        AnimationClip quieto = ClipJulio("Mutant Idle", "DrunkIdle");
        AnimationClip caminar = ClipJulio("Mutant Walking", "DrunkWalk");
        AnimationClip correr = ClipJulio("Mutant Run", "Locomotion--Run_N");
        arbol.AddChild(quieto, 0f);
        arbol.AddChild(caminar, 1.6f);
        arbol.AddChild(correr, 6.5f);
        ChildMotion[] hijos = arbol.children;
        hijos[1].timeScale = Mathf.Clamp(1.6f / ConstructorBadCarreteDay.VelocidadDelClip(caminar, 1.6f), 0.6f, 1.8f);
        hijos[2].timeScale = Mathf.Clamp(6.5f / ConstructorBadCarreteDay.VelocidadDelClip(correr, 6.5f), 0.7f, 1.6f);
        arbol.children = hijos;

        AnimatorState atacar = m.AddState("Atacando");
        atacar.motion = ClipJulio("Mutant Punch", "Punching");
        atacar.speed = ConstructorBadCarreteDay.BuscarClip("Julio/Mutant Punch") != null ? 1f : 0.6f; // golpe lento: se ve venir
        atacar.AddStateMachineBehaviour<ApagarArmaAlSalir>();
        AnimatorStateTransition t1 = loco.AddTransition(atacar);
        t1.AddCondition(AnimatorConditionMode.If, 0f, "atacando");
        t1.hasExitTime = false; t1.duration = 0.15f;
        AnimatorStateTransition t2 = atacar.AddTransition(loco);
        t2.AddCondition(AnimatorConditionMode.IfNot, 0f, "atacando");
        t2.hasExitTime = false; t2.duration = 0.2f;

        // Rugir y reir: desde la locomocion; vuelven solos
        foreach (string[] e in new[] { new[] { "Rugir", "rugir", "Mutant Roaring", "Taunt" }, new[] { "Reir", "reir", "Laughing", "Taunt" } })
        {
            AnimatorState st = m.AddState(e[0]);
            st.motion = ClipJulio(e[2], e[3]);
            if (e[0] == "Reir") st.speed = 1.4f;
            AnimatorStateTransition ida = loco.AddTransition(st);
            ida.AddCondition(AnimatorConditionMode.If, 0f, e[1]);
            ida.hasExitTime = false; ida.duration = 0.15f;
            AnimatorStateTransition vuelta = st.AddTransition(loco);
            vuelta.hasExitTime = true; vuelta.exitTime = e[0] == "Reir" ? 0.6f : 0.9f; vuelta.duration = 0.2f;
            AnimatorStateTransition aPegar = st.AddTransition(atacar);
            aPegar.AddCondition(AnimatorConditionMode.If, 0f, "atacando");
            aPegar.hasExitTime = false; aPegar.duration = 0.15f;
        }

        // Golpe en el cuchillo: se tambalea
        AnimatorState golpe = m.AddState("HitReaction");
        golpe.motion = ConstructorBadCarreteDay.BuscarClip("HitReaction");
        AnimatorStateTransition t3 = m.AddAnyStateTransition(golpe);
        t3.AddCondition(AnimatorConditionMode.If, 0f, "golpeado");
        t3.duration = 0.05f; t3.canTransitionToSelf = false;
        AnimatorStateTransition t4 = golpe.AddTransition(loco);
        t4.hasExitTime = true; t4.exitTime = 0.85f; t4.duration = 0.15f;

        AnimatorState muerte = m.AddState("Dying");
        muerte.motion = ConstructorBadCarreteDay.BuscarClip("Dying");
        AnimatorStateTransition t5 = m.AddAnyStateTransition(muerte);
        t5.AddCondition(AnimatorConditionMode.If, 0f, "muerte");
        t5.duration = 0.1f; t5.canTransitionToSelf = false;

        m.defaultState = loco;
        AnimatorControllerLayer[] capas = c.layers;
        capas[0].iKPass = true; // MirarJugador gira la cabeza
        c.layers = capas;
        AssetDatabase.SaveAssets();
        return c;
    }

    // Piel viva (no zombie): la textura del modelo trae piel verde, heridas y sangre. Se procesa por
    // codigo: verde -> piel trigueña palida; heridas/sangre -> manchas y moretones apagados (ojeras, mugre)
    static void JulioVivo(GameObject julio)
    {
        string rutaOriginal = "Assets/BadCarreteDay/Arte/Personajes/Texturas_Mremireh/mremireh_body__diffuse.png";
        string rutaViva = "Assets/BadCarreteDay/Arte/Personajes/Texturas_Mremireh/julio_vivo_diffuse.png";
        {
            Texture2D t = new Texture2D(2, 2);
            t.LoadImage(System.IO.File.ReadAllBytes(rutaOriginal));
            Color[] px = t.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                float h, sat, v;
                Color.RGBToHSV(px[i], out h, out sat, out v);
                float grados = h * 360f;
                if (sat > 0.12f && grados > 50f && grados < 175f)           // piel verde -> trigueña palida (clara: de noche se ve)
                    px[i] = Color.HSVToRGB(20f / 360f, Mathf.Clamp(sat * 1.2f, 0.4f, 0.55f), Mathf.Clamp01(v * 1.25f + 0.15f));
                else if (sat > 0.4f && (grados < 18f || grados > 330f))    // heridas y sangre -> moretones apagados
                    px[i] = Color.HSVToRGB(14f / 360f, 0.3f, Mathf.Clamp01(v * 0.8f + 0.08f));
                else if (sat < 0.12f && v > 0.75f)                         // blanco de los ojos -> rosado irritado
                    px[i] = Color.HSVToRGB(355f / 360f, 0.22f, v);
                px[i].a = 1f;
            }
            t.SetPixels(px);
            System.IO.File.WriteAllBytes(rutaViva, t.EncodeToPNG());
            AssetDatabase.ImportAsset(rutaViva);
        }
        Texture2D viva = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaViva);
        foreach (Renderer r in julio.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || !mats[i].name.Contains("body")) continue;
                string ruta = "Assets/BadCarreteDay/Materiales/Tintes/Julio_Vivo_Cuerpo.mat";
                ConstructorBadCarreteDay.CrearCarpeta("Assets/BadCarreteDay/Materiales/Tintes");
                AssetDatabase.DeleteAsset(ruta);
                Material vivo = new Material(mats[i]);
                vivo.SetTexture("_BaseMap", viva);
                if (vivo.HasProperty("_BaseColor")) vivo.SetColor("_BaseColor", Color.white);
                // Un poco de luz propia con su misma textura: la piel se lee trigueña aunque la luz sea azul
                vivo.EnableKeyword("_EMISSION");
                vivo.SetTexture("_EmissionMap", viva);
                vivo.SetColor("_EmissionColor", new Color(0.22f, 0.2f, 0.18f));
                vivo.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                AssetDatabase.CreateAsset(vivo, ruta);
                mats[i] = vivo;
            }
            r.sharedMaterials = mats;
        }
    }

    // Cuchillo (Quaternius, CC0) clavado en la espalda, en diagonal hacia abajo; la hoja con un brillo suave
    static GameObject CuchilloEnLaEspalda(GameObject julio)
    {
        Animator anim = julio.GetComponent<Animator>();
        Transform pecho = anim.GetBoneTransform(HumanBodyBones.Chest);
        if (pecho == null) pecho = julio.transform;
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BadCarreteDay/Arte/Props/Cuchillo.obj");
        GameObject soporte = new GameObject("Cuchillo_Espalda");
        soporte.transform.SetParent(pecho, false);
        if (asset == null) return soporte;
        GameObject cuchillo = (GameObject)PrefabUtility.InstantiatePrefab(asset, soporte.transform);
        cuchillo.name = "Cuchillo";
        // Mango -> punta (la hoja es la pieza gris clara)
        // El .obj se importa como UNA malla con 4 materiales: la hoja es el material "Cuchillo_2" y el mango "Cuchillo_0"
        MeshFilter malla = cuchillo.GetComponentInChildren<MeshFilter>();
        MeshRenderer rend = cuchillo.GetComponentInChildren<MeshRenderer>();
        Vector3 centroHoja = Vector3.zero, centroMango = Vector3.zero;
        int iHoja = -1;
        if (malla != null && rend != null)
            for (int i = 0; i < rend.sharedMaterials.Length && i < malla.sharedMesh.subMeshCount; i++)
            {
                Vector3 c = malla.transform.TransformPoint(malla.sharedMesh.GetSubMesh(i).bounds.center);
                if (rend.sharedMaterials[i].name.EndsWith("_2")) { centroHoja = c; iHoja = i; }
                if (rend.sharedMaterials[i].name.EndsWith("_0")) centroMango = c;
            }
        Bounds total = ConstructorF5.Limites(cuchillo);
        float largo = Mathf.Max(total.size.x, total.size.y, total.size.z);
        cuchillo.transform.localScale *= 0.55f / Mathf.Max(largo, 0.0001f) / Mathf.Max(pecho.lossyScale.x, 0.0001f);
        // (la hoja del modelo queda del lado contrario al centro de la pieza "_2": se verifico con capturas)
        Vector3 haciaPunta = iHoja >= 0 ? (centroMango - centroHoja).normalized : Vector3.forward;
        // La punta entra al cuerpo: hacia adelante de Julio y un poco hacia abajo
        Vector3 dentro = (julio.transform.forward + Vector3.down * 0.45f).normalized;
        cuchillo.transform.rotation = Quaternion.FromToRotation(haciaPunta, dentro) * cuchillo.transform.rotation;
        Vector3 espalda = pecho.position - julio.transform.forward * 0.16f * julio.transform.lossyScale.x + Vector3.up * 0.04f;
        for (int intento = 0; intento < 2 && iHoja >= 0; intento++)
        {
            // El punto entre el mango y la hoja queda justo en la espalda: la hoja adentro, el mango afuera
            Vector3 hojaMundo = malla.transform.TransformPoint(malla.sharedMesh.GetSubMesh(iHoja).bounds.center);
            Vector3 mangoMundo = Vector3.zero;
            for (int k = 0; k < rend.sharedMaterials.Length && k < malla.sharedMesh.subMeshCount; k++)
                if (rend.sharedMaterials[k].name.EndsWith("_0")) mangoMundo = malla.transform.TransformPoint(malla.sharedMesh.GetSubMesh(k).bounds.center);
            Vector3 union = Vector3.Lerp(mangoMundo, hojaMundo, 0.3f);
            cuchillo.transform.position += espalda - union;
            hojaMundo = malla.transform.TransformPoint(malla.sharedMesh.GetSubMesh(iHoja).bounds.center);
            // ¿La hoja quedo adentro (hacia el frente de Julio desde la espalda)? Si no, se da vuelta
            if (Vector3.Dot(hojaMundo - espalda, julio.transform.forward) > 0f) break;
            cuchillo.transform.RotateAround(espalda, julio.transform.right, 180f);
        }
        // Brillo suave en la hoja (que se note)
        if (iHoja >= 0)
        {
            Material[] mats = rend.sharedMaterials;
            mats[iHoja] = ConstructorF7.MaterialEmisivo("Cuchillo_Hoja", new Color(0.8f, 0.82f, 0.86f), 0.7f);
            for (int k = 0; k < mats.Length; k++)
                if (mats[k].name.EndsWith("_0")) mats[k] = ConstructorF7.MaterialEmisivo("Cuchillo_Mango", new Color(0.62f, 0.4f, 0.22f), 0.35f); // mango de madera que se vea
            rend.sharedMaterials = mats;
        }
        // Mancha oscura en la polera donde entra el cuchillo
        GameObject mancha = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mancha.name = "Mancha_Cuchillo";
        Object.DestroyImmediate(mancha.GetComponent<Collider>());
        mancha.GetComponent<Renderer>().sharedMaterial = ConstructorF2.Material("Mancha_Sangre_Seca", new Color(0.28f, 0.05f, 0.05f));
        mancha.transform.SetParent(soporte.transform, true);
        mancha.transform.position = pecho.position - julio.transform.forward * 0.155f * julio.transform.lossyScale.x + Vector3.up * 0.04f;
        mancha.transform.rotation = Quaternion.LookRotation(Vector3.up, -julio.transform.forward) * Quaternion.Euler(90f, 0f, 0f);
        mancha.transform.rotation = Quaternion.FromToRotation(Vector3.up, -julio.transform.forward);
        mancha.transform.localScale = Vector3.Scale(new Vector3(0.12f, 0.003f, 0.16f), new Vector3(1f / mancha.transform.lossyScale.x * mancha.transform.localScale.x, 1f / mancha.transform.lossyScale.y * mancha.transform.localScale.y, 1f / mancha.transform.lossyScale.z * mancha.transform.localScale.z));
        foreach (Collider col in cuchillo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        return soporte;
    }

    // Enemigo listo para pelear: modelo Mixamo + scripts de clase (MobController, estadisticas,
    // DemoObjetivo, DemoArma en el puño) + ControladorEnemigo que los une
    static ControladorEnemigo Enemigo(string nombre, string modelo, Vector3 pos, float salud, float danio,
                                      string[] alDetectar, string[] alGolpe, Color tinte)
    {
        GameObject obj = NPC(enemigos, nombre, modelo, rutaAnimatorCuma, pos, 180f);
        ConstructorF2.AgregarVoz(obj, modelo == "Mremireh" ? "Julio" : "Cuma", modelo == "Mremireh" ? "Julio" : "Cuma", Random.Range(0.9f, 1.12f), 1.95f);
        if (tinte != Color.white) Tenir(obj, nombre, tinte);
        obj.GetComponent<Animator>().applyRootMotion = false; // lo mueve el NavMeshAgent, no la animacion

        NavMeshAgent agente = obj.AddComponent<NavMeshAgent>();
        agente.speed = 3.2f;
        agente.stoppingDistance = 1.1f;
        agente.radius = 0.35f;
        agente.angularSpeed = 360f;

        DemoControladorEstadisticas stats = ConstructorBadCarreteDay.Agregar<DemoControladorEstadisticas>(obj);
        stats.estadisticasBase.salud = salud;
        DemoObjetivo objetivo = ConstructorBadCarreteDay.Agregar<DemoObjetivo>(obj);

        MobController mob = ConstructorBadCarreteDay.Agregar<MobController>(obj);
        mob.agente = agente;
        mob.animatorMob = obj.GetComponent<Animator>();
        mob.comportamiento = MobController.Comportamiento.Hostil;
        mob.toleranciaDist = 2.0f; // sigue pegando hasta que te alejas 2 m (no al dar un paso)

        // Puño: DemoArma (clase) en la mano derecha, prendido por Animation Events del golpe
        Transform mano = null;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>())
            if (t.name.EndsWith("RightHand")) { mano = t; break; }
        GameObject punio = new GameObject("Punio");
        punio.transform.SetParent(mano != null ? mano : obj.transform, false);
        SphereCollider colPunio = punio.AddComponent<SphereCollider>();
        colPunio.isTrigger = true;
        colPunio.radius = 0.25f;
        Rigidbody rb = punio.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        DemoArma arma = ConstructorBadCarreteDay.Agregar<DemoArma>(punio);
        arma.nombre = "Puño";
        arma.danio = danio;

        ControladorEnemigo enemigo = ConstructorBadCarreteDay.Agregar<ControladorEnemigo>(obj);
        enemigo.mob = mob;
        enemigo.estadisticas = stats;
        enemigo.animator = obj.GetComponent<Animator>();
        enemigo.arma = arma;
        enemigo.jugador = b.animJugador.transform;
        enemigo.hud = b.hud;
        enemigo.frasesAlDetectar = alDetectar;
        enemigo.frasesAlRecibirGolpe = alGolpe;

        UnityEventTools.AddPersistentListener(objetivo.alSerGolpeado, enemigo.RecibirGolpe);
        UnityEventTools.AddPersistentListener(stats.eventos.alMorir, enemigo.Morir);
        return enemigo;
    }

    // Copia los materiales del modelo con un tinte (para diferenciar cumas con el mismo modelo)
    static void Tenir(GameObject obj, string nombre, Color tinte)
    {
        ConstructorBadCarreteDay.CrearCarpeta("Assets/BadCarreteDay/Materiales/Tintes");
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string ruta = "Assets/BadCarreteDay/Materiales/Tintes/" + nombre + "_" + r.name + "_" + i + ".mat";
                AssetDatabase.DeleteAsset(ruta);
                Material copia = new Material(mats[i]);
                if (copia.HasProperty("_BaseColor")) copia.SetColor("_BaseColor", copia.GetColor("_BaseColor") * tinte);
                AssetDatabase.CreateAsset(copia, ruta);
                mats[i] = copia;
            }
            r.sharedMaterials = mats;
        }
    }

    static GameObject BuscarEnJugador(string nombre)
    {
        foreach (Transform t in b.animJugador.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t.gameObject;
        Debug.LogError("[BadCarreteDay] No encontre en el jugador: " + nombre);
        return null;
    }

    // ===== PIEZAS REUTILIZABLES =====

    static Transform Grupo(string nombre)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(nivel, false);
        return t;
    }

    static GameObject Bloque(Transform padre, string nombre, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject cubo = ConstructorF2.Cubo(nombre, pos, escala, mat);
        cubo.transform.SetParent(padre, true);
        cubo.isStatic = true;
        return cubo;
    }

    static Material Color_(string nombre, float r, float g, float bl)
    {
        return ConstructorF2.Material(nombre, new Color(r, g, bl));
    }

    // Paleta pastel saturada de casas de cerro
    static Material ColorCasa(int i)
    {
        Color[] paleta = {
            new Color(1f, 0.55f, 0.7f), new Color(0.35f, 0.85f, 0.85f), new Color(1f, 0.85f, 0.35f),
            new Color(0.7f, 0.55f, 0.95f), new Color(1f, 0.6f, 0.35f), new Color(0.55f, 0.9f, 0.6f),
        };
        int k = i % paleta.Length;
        return ConstructorF2.Material("Casa_" + k, paleta[k]);
    }

    static Transform Punto(Transform padre, string nombre, Vector3 pos)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.position = pos;
        return t;
    }

    // Gamba ($100, plateada chica), quina ($500, dorada grande) o luka ($1.000, billete verde)
    static GameObject Moneda(Vector3 pos, int valor)
    {
        GameObject obj;
        if (valor >= 1000)
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(0.7f, 0.35f, 0.03f);
            obj.GetComponent<Renderer>().sharedMaterial = ConstructorF2.Material("Luka_Verde", new Color(0.35f, 0.75f, 0.4f));
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            float r = valor >= 500 ? 0.55f : 0.38f;
            obj.transform.localScale = new Vector3(r, 0.03f, r);
            obj.GetComponent<Renderer>().sharedMaterial = valor >= 500
                ? ConstructorF2.Material("Quina_Dorada", new Color(1f, 0.8f, 0.15f))
                : ConstructorF2.Material("Gamba_Plateada", new Color(0.8f, 0.82f, 0.86f));
        }
        obj.name = valor >= 1000 ? "Luka_$1000" : (valor >= 500 ? "Quina_$500" : "Gamba_$100");
        obj.transform.position = pos;
        obj.transform.SetParent(nivel, true);

        Object.DestroyImmediate(obj.GetComponent<Collider>());
        SphereCollider col = obj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = valor >= 1000 ? 1.2f : 1.5f / obj.transform.localScale.x * 0.5f;

        Moneda m = ConstructorBadCarreteDay.Agregar<Moneda>(obj);
        m.puntos = valor;
        m.tagBuscado = "Player";
        m.controladorDinero = b.dinero;
        return obj;
    }

    static void PensamientoAlTocar(GameObject obj, string texto)
    {
        ControladorColisionTrigger t = ConstructorBadCarreteDay.Agregar<ControladorColisionTrigger>(obj);
        t.tagEsperado = "Player";
        UnityEventTools.AddStringPersistentListener(t.onTriggerEnterEvent, b.hud.MostrarPensamiento, texto);
    }

    // Trigger que muestra un pensamiento de Benja una sola vez
    static void Pensamiento(Transform padre, Vector3 pos, float radio, string texto)
    {
        LugarObjetivoConTexto(padre, "Pensamiento", pos, radio, texto);
    }

    static void LugarObjetivoConTexto(Transform padre, string nombre, Vector3 pos, float radio, string texto)
    {
        ControladorLugarObjetivo lugar = LugarObjetivo(padre, nombre, pos, radio, null, -1, null);
        UnityEventTools.AddStringPersistentListener(lugar.alLlegarJugador, b.hud.MostrarPensamiento, texto);
    }

    // ControladorLugarObjetivo de clase: al llegar el jugador completa un objetivo y/o llama una accion
    static ControladorLugarObjetivo LugarObjetivo(Transform padre, string nombre, Vector3 pos, float radio,
                                                  ControladorMision mision, int objetivo, UnityEngine.Events.UnityAction accion)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(padre, false);
        obj.transform.position = pos;
        BoxCollider col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(radio * 2f, 4f, radio * 2f);
        col.center = new Vector3(0f, 2f, 0f);

        ControladorLugarObjetivo lugar = ConstructorBadCarreteDay.Agregar<ControladorLugarObjetivo>(obj);
        lugar.empiezaStart = true; // necesario: asi espera al jugador desde el inicio
        lugar.tipoObjetivo = ControladorLugarObjetivo.TipoObjetivo.unaEjecucion;
        if (mision != null && objetivo >= 0)
            UnityEventTools.AddIntPersistentListener(lugar.alLlegarJugador, mision.CompletarObjetivo, objetivo);
        if (accion != null)
            UnityEventTools.AddPersistentListener(lugar.alLlegarJugador, accion);
        return lugar;
    }

    static void Checkpoint(Transform padre, string nombre, Vector3 pos)
    {
        ControladorLugarObjetivo lugar = LugarObjetivo(padre, nombre, pos, 3f, null, -1, null);
        UnityEventTools.AddObjectPersistentListener<Transform>(lugar.alLlegarJugador, b.juego.SetCheckpoint, lugar.transform);
    }

    static GameObject NPC(Transform padre, string nombre, string modelo, string rutaAnimator, Vector3 pos, float rotY)
    {
        GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BadCarreteDay/Arte/Personajes/" + modelo + ".fbx"));
        PrefabUtility.UnpackPrefabInstance(npc, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        npc.name = nombre;
        npc.transform.SetParent(padre, false);
        npc.transform.position = pos;
        npc.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        Animator anim = npc.GetComponent<Animator>();
        if (anim == null) anim = npc.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimator);
        CapsuleCollider cuerpo = npc.AddComponent<CapsuleCollider>();
        cuerpo.center = new Vector3(0f, 0.9f, 0f);
        cuerpo.height = 1.8f;
        cuerpo.radius = 0.35f;
        return npc;
    }

    static DialogoNPC.Conversacion Conv(params string[] lineas)
    {
        return new DialogoNPC.Conversacion { lineas = lineas };
    }

    static GameObject ZonaInteraccion(Transform npc, Vector3 offset, float radio, string prompt, UnityEngine.Events.UnityAction accion)
    {
        GameObject zona = new GameObject("ZonaInteraccion");
        zona.transform.SetParent(npc, false);
        zona.transform.localPosition = offset;
        SphereCollider esfera = zona.AddComponent<SphereCollider>();
        esfera.isTrigger = true;
        esfera.radius = radio;
        esfera.center = new Vector3(0f, 1f, 0f);
        DemoControlInteraccion inter = ConstructorBadCarreteDay.Agregar<DemoControlInteraccion>(zona);
        inter.inputInteraccion = ConstructorBadCarreteDay.BuscarInputActionReference("Interaccion");
        UnityEventTools.AddStringPersistentListener(inter.eventos.alEntrarDeteccion, b.hud.MostrarPrompt, prompt);
        UnityEventTools.AddPersistentListener(inter.eventos.alSalirDeteccion, b.hud.OcultarPrompt);
        UnityEventTools.AddPersistentListener(inter.eventos.alInteractuar, accion);
        return zona;
    }

    // Pegatina ¡AQUÍ! (accion contextual). Devuelve el objeto con su DemoControlInteraccion.
    static GameObject Pegatina(Transform padre, string nombre, Vector3 pos, string prompt)
    {
        GameObject pegatina = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pegatina.name = nombre;
        pegatina.transform.SetParent(padre, false);
        pegatina.transform.position = pos;
        pegatina.transform.localScale = new Vector3(1.6f, 0.02f, 1.6f);
        pegatina.GetComponent<Renderer>().sharedMaterial = ConstructorF2.Material("Rosado_Pegatina", new Color(1f, 0.3f, 0.7f));
        Object.DestroyImmediate(pegatina.GetComponent<Collider>());
        BoxCollider col = pegatina.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1f, 100f, 1f);
        col.center = new Vector3(0f, 50f, 0f);

        DemoControlInteraccion inter = ConstructorBadCarreteDay.Agregar<DemoControlInteraccion>(pegatina);
        inter.inputInteraccion = ConstructorBadCarreteDay.BuscarInputActionReference("Interaccion");
        UnityEventTools.AddStringPersistentListener(inter.eventos.alEntrarDeteccion, b.hud.MostrarPrompt, prompt);
        UnityEventTools.AddPersistentListener(inter.eventos.alSalirDeteccion, b.hud.OcultarPrompt);
        UnityEventTools.AddPersistentListener(inter.eventos.alInteractuar, b.hud.OcultarPrompt);
        return pegatina;
    }

    // Producto del almacen: se ve sobre el meson y se compra en la tienda de Doña Marta.
    // (Los comentarios de Marta los dice la tienda; aqui solo queda lo que piensa Benja)
    static GameObject Producto(Transform padre, string nombre, string producto, int precio, Vector3 pos, Material mat, string alComprarTexto)
    {
        GameObject obj = Bloque(padre, nombre, pos, new Vector3(0.45f, 0.5f, 0.45f), mat);
        obj.isStatic = false;
        ControladorCompra compra = ConstructorBadCarreteDay.Agregar<ControladorCompra>(obj);
        compra.nombreProducto = producto;
        compra.precio = precio;
        compra.unaSolaVez = true;
        compra.controladorDinero = b.dinero;
        if (alComprarTexto.StartsWith("Doña Marta:") == false)
            UnityEventTools.AddStringPersistentListener(compra.alComprar, b.hud.MostrarPensamiento, alComprarTexto);
        UnityEventTools.AddBoolPersistentListener(compra.alComprar, obj.SetActive, false);
        return obj;
    }

    // ===== ANIMATORS DE NPCs =====

    // Idle "normal" de Starter Assets (para Doña Marta, que no esta curada)
    static AnimationClip ClipIdleStarter() { return ClipStarter("Stand--Idle"); }

    static AnimationClip ClipStarter(string archivo)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath("Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/" + archivo + ".anim.fbx"))
            if (o is AnimationClip && !o.name.StartsWith("__preview__")) return (AnimationClip)o;
        return null;
    }

    // Animator de enemigos (cumas y Julio) con los parametros que usan MobController y ControladorEnemigo:
    // Speed (caminar/correr), atacando (golpear), golpeado (reaccion) y muerte
    static void CrearAnimatorCuma()
    {
        AssetDatabase.DeleteAsset(rutaAnimatorCuma);
        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(rutaAnimatorCuma);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("atacando", AnimatorControllerParameterType.Bool);
        c.AddParameter("golpeado", AnimatorControllerParameterType.Trigger);
        c.AddParameter("muerte", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine m = c.layers[0].stateMachine;

        BlendTree arbol;
        AnimatorState loco = c.CreateBlendTreeInController("Locomocion", out arbol, 0);
        arbol.blendParameter = "Speed";
        arbol.AddChild(ConstructorBadCarreteDay.BuscarClip("FightingIdle"), 0f);
        arbol.AddChild(ClipStarter("Locomotion--Walk_N"), 1.8f);
        arbol.AddChild(ClipStarter("Locomotion--Run_N"), 4.5f);

        AnimatorState atacar = m.AddState("Atacando");
        atacar.motion = ConstructorBadCarreteDay.BuscarClip("Punching");
        atacar.AddStateMachineBehaviour<ApagarArmaAlSalir>(); // el puño se apaga aunque corten el golpe
        AnimatorStateTransition t1 = loco.AddTransition(atacar);
        t1.AddCondition(AnimatorConditionMode.If, 0f, "atacando");
        t1.hasExitTime = false; t1.duration = 0.1f;
        AnimatorStateTransition t2 = atacar.AddTransition(loco);
        t2.AddCondition(AnimatorConditionMode.IfNot, 0f, "atacando");
        t2.hasExitTime = false; t2.duration = 0.15f;

        AnimatorState golpe = m.AddState("HitReaction");
        golpe.motion = ConstructorBadCarreteDay.BuscarClip("HitReaction");
        golpe.speed = 1.5f;
        AnimatorStateTransition t3 = m.AddAnyStateTransition(golpe);
        t3.AddCondition(AnimatorConditionMode.If, 0f, "golpeado");
        t3.duration = 0.05f; t3.canTransitionToSelf = false;
        AnimatorStateTransition t4 = golpe.AddTransition(loco);
        t4.hasExitTime = true; t4.exitTime = 0.8f; t4.duration = 0.15f;

        AnimatorState muerte = m.AddState("Dying");
        muerte.motion = ConstructorBadCarreteDay.BuscarClip("Dying");
        AnimatorStateTransition t5 = m.AddAnyStateTransition(muerte);
        t5.AddCondition(AnimatorConditionMode.If, 0f, "muerte");
        t5.duration = 0.1f; t5.canTransitionToSelf = false;

        m.defaultState = loco;
        AssetDatabase.SaveAssets();
    }

    // Activa "IK Pass" en la capa base (para que MirarJugador pueda girar la cabeza)
    static void ActivarIK(string ruta)
    {
        AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
        if (c == null) return;
        AnimatorControllerLayer[] capas = c.layers;
        capas[0].iKPass = true;
        c.layers = capas;
        EditorUtility.SetDirty(c);
    }

    static void CrearAnimators()
    {
        CrearAnimatorCuma();
        // Controller minimo con parametro "Speed" (lo exige MobController) para el perro greybox
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimatorSimple) == null)
        {
            AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(rutaAnimatorSimple);
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("atacando", AnimatorControllerParameterType.Bool);
        }
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimatorJulio) == null)
            AnimatorController.CreateAnimatorControllerAtPathWithClip(rutaAnimatorJulio, ConstructorBadCarreteDay.BuscarClip("FightingIdle"));
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaAnimatorMarta) == null)
            AnimatorController.CreateAnimatorControllerAtPathWithClip(rutaAnimatorMarta, ClipIdleStarter());
        foreach (string ruta in new[] { rutaAnimatorCuma, rutaAnimatorJulio, rutaAnimatorMarta, "Assets/BadCarreteDay/Animaciones/AnimatorNPC_Viejo.controller" })
            ActivarIK(ruta);
        AssetDatabase.SaveAssets();
    }
}
