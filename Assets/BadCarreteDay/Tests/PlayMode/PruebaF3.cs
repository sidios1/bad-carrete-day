using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Recorre el Nivel 1 completo (greybox) de principio a fin revisando la mision,
// la plata, el perro, el ascensor y la victoria.
// OJO: teletransporta al jugador, asi que NO prueba si los saltos son alcanzables
// (eso se prueba jugando).
public class PruebaF3
{
    GameObject jugador, sistemas;

    // Marca para el juego: son pruebas automaticas (no hay presentaciones de camara)
    [SetUp]
    public void MarcarPruebas()
    {
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    string Texto(string nombre) { return GameObject.Find(nombre).GetComponent<TMP_Text>().text; }

    IEnumerator Ir(Vector3 pos, float espera = 0.4f)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = pos;
        cc.enabled = true;
        yield return new WaitForSeconds(espera);
    }

    // Posicion de un objeto del nivel (asi la prueba no depende de coordenadas fijas)
    Vector3 Pos(string objeto, float arriba = 0.2f)
    {
        GameObject obj = GameObject.Find(objeto);
        Assert.IsNotNull(obj, "No existe/activo: " + objeto);
        return obj.transform.position + Vector3.up * arriba;
    }

    void Interactuar(string objeto)
    {
        GameObject obj = GameObject.Find(objeto);
        Assert.IsNotNull(obj, "No existe/activo: " + objeto);
        obj.SendMessage("Interactuar", new InputAction.CallbackContext());
    }

    IEnumerator Hablar(string npc)
    {
        GameObject.Find(npc).SendMessage("IniciarDialogo");
        // Algunos personajes hacen algo antes de hablar (Don Lucho termina de mear): esperamos la burbuja
        for (float t = 0f; t < 5f && GameObject.Find("PanelDialogo") == null; t += Time.deltaTime)
            yield return null;
        Assert.IsNotNull(GameObject.Find("PanelDialogo"), "No empezo la conversacion con " + npc);
        sistemas.SendMessage("TerminarDialogo");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Nivel1_DePrincipioAFin()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(0.5f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        sistemas = GameObject.Find("Sistemas");

        // 0) Primero el agua. Con caña Don Lucho no te pesca (y el vino no esta a la venta)
        StringAssert.Contains("agua", Texto("TextoMision"));
        yield return Hablar("NPC_DonLucho");
        StringAssert.Contains("agua", Texto("TextoMision"), "Con caña Don Lucho no deberia dar la mision");

        // 1) Plata con caña: basureros + la señora Nelly = $1.000 (justo el agua)
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name.StartsWith("Basurero_") && t.position.z < 55f) t.SendMessage("Revisar");
        yield return Hablar("NPC_SraNelly");
        Assert.AreEqual("$1.000", Texto("TextoDinero"));
        GameObject.Find("Producto_Agua").SendMessage("Comprar");
        yield return null;
        Assert.AreEqual("$0", Texto("TextoDinero"));
        StringAssert.Contains("meando", Texto("TextoMision"));

        // 2) Ahora si: Don Lucho
        yield return Hablar("NPC_DonLucho");
        StringAssert.Contains("vino", Texto("TextoMision"));

        // 3) Juntar la plata escondida de abajo (zonas 1 y 2): alcanza para vino + completo
        var monedas = new System.Collections.Generic.List<Vector3>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if ((t.name.StartsWith("Gamba_") || t.name.StartsWith("Quina_") || t.name.StartsWith("Luka_")) && t.position.z < 55f)
                monedas.Add(t.position);
        foreach (Vector3 m in monedas)
            yield return Ir(m - Vector3.up * 0.6f, 0.25f);
        Assert.AreEqual("$4.800", Texto("TextoDinero"));

        // 4) Comprar el vino ($2.000) y llevarselo
        GameObject.Find("Producto_Vino").SendMessage("Comprar");
        yield return null;
        Assert.AreEqual("$2.800", Texto("TextoDinero"));
        StringAssert.Contains("Llévale", Texto("TextoMision"));
        yield return Hablar("NPC_DonLucho");
        StringAssert.Contains("completo", Texto("TextoMision"));

        // 5) Comprar el completo
        GameObject.Find("Producto_Completo").SendMessage("Comprar");
        yield return null;
        Assert.AreEqual("$800", Texto("TextoDinero"));

        // 6) Darle el completo al Toby: se va al plato y se abre la escalera
        Vector3 tobyAntes = GameObject.Find("NPC_Toby").transform.position;
        yield return Ir(Pos("Pegatina_Completo", 0.1f));
        Interactuar("Pegatina_Completo");
        yield return new WaitForSeconds(3f);
        Assert.Greater(Vector3.Distance(tobyAntes, GameObject.Find("NPC_Toby").transform.position), 3f, "Toby no se movio");
        Assert.IsNull(GameObject.Find("Barrera_Toby"), "La barrera sigue activa");
        StringAssert.Contains("escalera", Texto("TextoMision"));

        // 6) Llegar al techo A
        yield return Ir(Pos("Llegada_TechoA"));
        StringAssert.Contains("ascensor", Texto("TextoMision"));

        // 7) Ascensor: debe SUBIR al jugador hasta la cima (y 16)
        yield return Ir(Pos("Ascensor", 0.5f));
        Interactuar("Pegatina_Palanca");
        yield return new WaitForSeconds(6f);
        Assert.Greater(jugador.transform.position.y, 15f, "El ascensor no subio al jugador");
        StringAssert.Contains("cumas", Texto("TextoMision"));

        // 8) Escoba (secreto, no es mision): te la pasa la señora Gladys al fondo de la calle alta
        yield return Ir(Pos("NPC_SraGladys") + Vector3.right * 1.5f);
        yield return Hablar("NPC_SraGladys");
        Assert.IsNotNull(GameObject.Find("Palo_Escoba_Visual"), "La escoba no aparecio en la mano");
        StringAssert.Contains("cumas", Texto("TextoMision"));

        // 9) Los 3 cumas (se simula el daño) -> se abre la reja
        Assert.IsNotNull(GameObject.Find("Reja_Pasaje"));
        foreach (string cuma in new[] { "Cuma_James", "Cuma_David", "Cuma_James_Rojo" })
            GameObject.Find(cuma).SendMessage("CalcularDanio", 999f);
        yield return null;
        Assert.IsNull(GameObject.Find("Reja_Pasaje"), "La reja no se abrio");
        StringAssert.Contains("Julio", Texto("TextoMision"));

        // 10) Cancha: Julio habla, empieza la pelea, fase 2 con refuerzos, derrota, telefono
        yield return Ir(Pos("Llegada_Cancha"));
        StringAssert.Contains("Vence a Julio", Texto("TextoMision"));
        yield return Ir(Pos("Encuentro_Julio"));
        Assert.IsNotNull(GameObject.Find("PanelDialogo"), "Julio no empezo a hablar");
        sistemas.SendMessage("TerminarDialogo");
        yield return null;
        Assert.IsNotNull(GameObject.Find("PanelJefe"), "No aparecio la barra del jefe");

        GameObject julio = GameObject.Find("Julio_ElPastero");
        julio.SendMessage("CalcularDanio", 220f);  // 400 -> 180 (menos de la mitad): fase 2, Julio enojado
        yield return new WaitForSeconds(0.2f);
        Assert.IsNull(GameObject.Find("Refuerzo_David"), "Julio no tiene secuaces");
        Assert.IsNotNull(GameObject.Find("PanelJefe"), "La barra del jefe deberia seguir");

        julio.SendMessage("CalcularDanio", 999f);
        yield return new WaitForSeconds(0.2f);
        Assert.IsNull(GameObject.Find("PanelJefe"), "La barra del jefe sigue visible");
        Assert.AreEqual("Julio", Texto("TextoNombre"), "No empezo el dialogo de derrota");
        sistemas.SendMessage("TerminarDialogo");
        yield return null;
        StringAssert.Contains("teléfono", Texto("TextoMision"));
        yield return Ir(Pos("Telefono"));

        // 11) Victoria -> Fin
        yield return new WaitForSeconds(4f);
        Assert.AreEqual("Fin", SceneManager.GetActiveScene().name);
        Assert.IsNotNull(GameObject.Find("PanelVictoria"));
        // El telefono muestra la nota y despues el "Continuara"
        yield return new WaitForSeconds(5.5f);
        Assert.IsNotNull(GameObject.Find("Notificacion"), "No aparecio la notificacion del Portal UCSC");
        Assert.IsNotNull(GameObject.Find("Continuara"), "No aparecio el Continuara");
    }

    [UnityTest]
    public IEnumerator NPC_TeMiraAlHablarle()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(0.5f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        sistemas = GameObject.Find("Sistemas");

        // Doña Marta esta detras del meson mirando a la plaza; abro su tienda desde un costado de la ventanita
        yield return Ir(new Vector3(-7.0f, 0.1f, 42.5f)); // dentro de la zona de la ventanita, en diagonal
        GameObject marta = GameObject.Find("NPC_DonaMarta");
        Vector3 haciaJugador = jugador.transform.position - marta.transform.position;
        haciaJugador.y = 0f;
        float antes = Vector3.Angle(marta.transform.forward, haciaJugador);
        Assert.Greater(antes, 30f, "Marta ya estaba mirando al jugador: la prueba no sirve");

        // [E] en la ventanita: la tienda se abre al tiro (sin conversacion) y Marta te mira
        GameObject.Find("Punto_HablarMarta").GetComponentInChildren<UnityEngine.Collider>().gameObject.SendMessage("Interactuar", new InputAction.CallbackContext());
        yield return null;
        Assert.IsNotNull(GameObject.Find("PanelTienda"), "La tienda no se abrio al tiro");
        Assert.IsNull(GameObject.Find("PanelDialogo"), "No deberia haber conversacion antes de la tienda");
        yield return new WaitForSeconds(1.5f);
        float despues = Vector3.Angle(marta.transform.forward, haciaJugador);
        Assert.Less(despues, 15f, "Marta no se dio vuelta para mirar al jugador");
        sistemas.SendMessage("Cerrar");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Muerte_PierdeVida_YReapareceEnCheckpoint()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(0.5f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        Assert.AreEqual("Vidas: 3", Texto("TextoVidas"));

        yield return Ir(new Vector3(0f, 0.1f, 20f));
        jugador.SendMessage("CalcularDanio", 500f);
        yield return new WaitForSeconds(3f);

        Assert.AreEqual("Nivel1_Cerro", SceneManager.GetActiveScene().name, "No deberia ser Game Over con vidas");
        Assert.AreEqual("Vidas: 2", Texto("TextoVidas"));
        Assert.AreEqual("100", Texto("TextoSalud"));
        Assert.Less(Vector3.Distance(jugador.transform.position, new Vector3(0f, 0.1f, 1f)), 2f, "No reaparecio en el inicio");
    }

    [UnityTest]
    public IEnumerator PaloDeEscoba_DerrotaAUnCuma()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(0.5f);
        jugador = GameObject.FindGameObjectWithTag("Player");

        // La señora Gladys le pasa el palo
        sistemas = GameObject.Find("Sistemas");
        jugador.SendMessage("TomarAgua");
        yield return Ir(Pos("NPC_SraGladys") + Vector3.right * 1.5f);
        yield return Hablar("NPC_SraGladys");

        // Pararse frente a un cuma (50 de salud) y "pegar" 2 veces (25 c/u) con los
        // mismos Animation Events que usa la animacion de ataque (ControladorJugador de clase)
        // Solo James (si no, los golpes se reparten entre los 3 cumas: DemoArma pega a uno por ataque)
        GameObject.Find("Cuma_David").SetActive(false);
        GameObject.Find("Cuma_James_Rojo").SetActive(false);
        GameObject cuma = GameObject.Find("Cuma_James");
        yield return Ir(cuma.transform.position + new Vector3(0f, 0.2f, -1f));
        jugador.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        Assert.IsNotNull(GameObject.Find("Palo_Escoba_Visual"), "No se ve el palo en la mano");
        // 2 ataques REALES: la animacion Ataque01 dispara ActivaArma/DesactivaArma
        Animator anim = jugador.GetComponent<Animator>();
        for (int i = 0; i < 2; i++)
        {
            anim.SetTrigger("ataque");
            yield return new WaitForSeconds(1.3f);
        }
        yield return new WaitForSeconds(3.5f);
        Assert.IsTrue(cuma == null, "El palo no derroto al cuma (revisar posicion/tamaño del palo)");
    }
}
