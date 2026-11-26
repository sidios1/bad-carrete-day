using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// No se puede salir del mapa: desde puntos del borde, Benja corre y salta hacia afuera
// con teclado REAL y no debe pasar el limite. Y si igual sale, vuelve al checkpoint sin perder vida.
public class PruebaB_Limites : InputTestFixture
{
    Keyboard teclado;
    GameObject jugador;

    // Borra lo que use el input (jugador, EventSystem) para que no choque con la proxima prueba
    static void Limpiar()
    {
        foreach (PlayerInput pi in Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            Object.DestroyImmediate(pi.gameObject);
        foreach (UnityEngine.EventSystems.EventSystem es in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None))
            Object.DestroyImmediate(es.gameObject);
        // Las acciones que quedaron prendidas de otra prueba apuntan al input viejo: se apagan
        foreach (InputAction accion in InputSystem.ListEnabledActions())
            accion.Disable();
    }

    public override void TearDown()
    {
        Limpiar();
        base.TearDown();
    }

    public override void Setup()
    {
        Limpiar();
        base.Setup();
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    IEnumerator Ir(Vector3 pos)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = pos;
        cc.enabled = true;
        yield return new WaitForSeconds(0.3f);
    }

    // Borde: desde donde, con que tecla (la camara mira hacia +z) y hasta donde puede llegar
    struct Borde
    {
        public string nombre; public Vector3 desde; public char tecla; public float limite;
        public Borde(string n, Vector3 d, char t, float l) { nombre = n; desde = d; tecla = t; limite = l; }
    }

    // WASD mueven respecto de la camara: elegimos la tecla que mas apunta hacia "direccion"
    KeyControl TeclaHacia(Vector3 direccion)
    {
        Vector3 f = Camera.main.transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        KeyControl mejor = teclado.wKey; float max = Vector3.Dot(f, direccion);
        if (Vector3.Dot(-f, direccion) > max) { max = Vector3.Dot(-f, direccion); mejor = teclado.sKey; }
        if (Vector3.Dot(r, direccion) > max) { max = Vector3.Dot(r, direccion); mejor = teclado.dKey; }
        if (Vector3.Dot(-r, direccion) > max) { mejor = teclado.aKey; }
        return mejor;
    }

    [UnityTest]
    public IEnumerator CorriendoYSaltando_NoSeSaleDelMapa()
    {
        teclado = InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>(); // el esquema de control es teclado + mouse
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        jugador.SendMessage("TomarAgua"); // sin caña: corre y salta al maximo

        Borde[] bordes = {
            new Borde("mirador de la plaza -> bahia", new Vector3(12.5f, 0.1f, 44f), 'd', 14.2f),
            new Borde("techo del contenedor -> barranco", new Vector3(10.1f, 2.8f, 16f), 'd', 12.6f),
            new Borde("suelo del sitio eriazo -> barranco", new Vector3(11.4f, 0.1f, 18.5f), 'd', 12.6f),
            new Borde("mirador de la plaza -> bahia", new Vector3(12.5f, 0.1f, 44f), 'd', 14.2f),
            new Borde("balcon -> edificio", new Vector3(8.8f, 3.6f, 38.7f), 'd', 15.8f),
            new Borde("calle alta -> cerro oeste", new Vector3(-7.5f, 4.1f, 92f), 'a', -10.4f),
            new Borde("techo A -> ladera", new Vector3(6.3f, 8.1f, 85f), 'd', 9.2f),
            new Borde("techo B -> ladera", new Vector3(6.3f, 10.6f, 98f), 'd', 9.2f),
            new Borde("anden -> ladera", new Vector3(6.5f, 10.6f, 106f), 'd', 9.2f),
            new Borde("estacion de arriba -> barranco", new Vector3(10f, 16.1f, 117.5f), 'd', 16.1f),
            new Borde("cancha -> cerro oeste (escalando la reja)", new Vector3(-10.4f, 16.1f, 150f), 'a', -16.1f),
            new Borde("cancha -> bahia (escalando la reja)", new Vector3(10.4f, 16.1f, 152f), 'd', 16.1f),
            new Borde("pasaje -> calle sin salida", new Vector3(0f, 0.1f, -4f), 's', -6.8f),
            new Borde("cancha -> cerro norte", new Vector3(3f, 16.1f, 161f), 'w', 172.2f),
        };

        string fallas = "";
        foreach (Borde b in bordes)
        {
            // Cada borde parte con el nivel recien cargado (sin nada arrastrado del borde anterior)
            SceneManager.LoadScene("Nivel1_Cerro");
            yield return new WaitForSeconds(1f);
            jugador = GameObject.FindGameObjectWithTag("Player");
            jugador.SendMessage("TomarAgua");
            yield return Ir(b.desde);
            // La direccion hacia afuera (en el mundo), convertida a "palanca" segun hacia donde mira la camara.
            // Se escribe directo en las entradas del jugador (mover + correr + saltar), como si tuviera un control.
            Vector3 afuera = b.tecla == 'd' ? Vector3.right : b.tecla == 'a' ? Vector3.left : b.tecla == 'w' ? Vector3.forward : Vector3.back;
            Component entradas = jugador.GetComponent("StarterAssetsInputs");
            System.Type tipo = entradas.GetType();
            float peor = b.tecla == 'a' || b.tecla == 's' ? 999f : -999f;
            float proximoSalto = 0f;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                Vector3 f = Camera.main.transform.forward; f.y = 0f; f.Normalize();
                Vector3 r = Vector3.Cross(Vector3.up, f);
                tipo.GetField("move").SetValue(entradas, new Vector2(Vector3.Dot(afuera, r), Vector3.Dot(afuera, f)).normalized);
                tipo.GetField("sprint").SetValue(entradas, true);
                if (t >= proximoSalto) { tipo.GetField("jump").SetValue(entradas, true); proximoSalto = t + 0.5f; }
                Vector3 p = jugador.transform.position;
                float valor = (b.tecla == 'w' || b.tecla == 's') ? p.z : p.x;
                peor = (b.tecla == 'a' || b.tecla == 's') ? Mathf.Min(peor, valor) : Mathf.Max(peor, valor);
                yield return null;
            }
            tipo.GetField("move").SetValue(entradas, Vector2.zero);
            tipo.GetField("sprint").SetValue(entradas, false);
            Debug.Log("[LIMITES-DBG] " + b.nombre + " desde " + b.desde + " ahora " + jugador.transform.position.ToString("F2"));
            bool paso = (b.tecla == 'a' || b.tecla == 's') ? peor < b.limite : peor > b.limite;
            Debug.Log("[LIMITES] " + b.nombre + ": llego a " + peor.ToString("F2") + " (limite " + b.limite + ")" + (paso ? " ❌" : " ✓"));
            if (paso) fallas += "\n - " + b.nombre + " (llego a " + peor.ToString("F2") + ")";
            yield return new WaitForSeconds(0.6f); // si se salio, la red de seguridad lo devuelve
        }
        Assert.IsEmpty(fallas, "Se salio del mapa en:" + fallas);
    }

    [UnityTest]
    public IEnumerator FueraDelMapa_VuelveAlCheckpoint_SinPerderVida()
    {
        teclado = InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>(); // el esquema de control es teclado + mouse
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        string vidas = GameObject.Find("TextoVidas").GetComponent<TMP_Text>().text;

        yield return Ir(new Vector3(40f, -12f, 20f)); // en la ladera, fuera del mapa
        yield return new WaitForSeconds(1.5f);
        Assert.Less(Vector3.Distance(jugador.transform.position, new Vector3(0f, 0.1f, 1f)), 2f, "No volvio al checkpoint");
        Assert.AreEqual(vidas, GameObject.Find("TextoVidas").GetComponent<TMP_Text>().text, "Perdio una vida");
    }

    // Bajo el ascensor: con la cabina arriba (o a sus costados) no queda un hoyo por donde salirse
    [UnityTest]
    public IEnumerator FosoDelAscensor_NoSeSaleDelMapa()
    {
        teclado = InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>();
        string fallas = "";
        Vector3[] desde = { new Vector3(3.2f, 10.7f, 109.5f), new Vector3(6f, 10.7f, 109.5f), new Vector3(8.2f, 10.7f, 109.5f) };
        foreach (Vector3 inicio in desde)
        {
            SceneManager.LoadScene("Nivel1_Cerro");
            yield return new WaitForSeconds(1f);
            jugador = GameObject.FindGameObjectWithTag("Player");
            jugador.SendMessage("TomarAgua");
            // La cabina se va para arriba sin Benja
            GameObject.Find("Ascensor").SendMessage("Activar");
            yield return new WaitForSeconds(3.5f);
            yield return Ir(inicio);
            Component entradas = jugador.GetComponent("StarterAssetsInputs");
            System.Type tipo = entradas.GetType();
            float minY = 99f;
            for (float t = 0f; t < 2.5f; t += Time.deltaTime)
            {
                Vector3 f = Camera.main.transform.forward; f.y = 0f; f.Normalize();
                Vector3 r = Vector3.Cross(Vector3.up, f);
                tipo.GetField("move").SetValue(entradas, new Vector2(Vector3.Dot(Vector3.forward, r), Vector3.Dot(Vector3.forward, f)).normalized);
                minY = Mathf.Min(minY, jugador.transform.position.y);
                yield return null;
            }
            tipo.GetField("move").SetValue(entradas, Vector2.zero);
            yield return new WaitForSeconds(1.5f);
            Vector3 p = jugador.transform.position;
            Debug.Log("[LIMITES] foso del ascensor desde " + inicio + ": bajo hasta y=" + minY.ToString("F2") + ", termina en " + p.ToString("F2"));
            if (minY < 9.5f) fallas += "\n - desde " + inicio + " cayo hasta y=" + minY.ToString("F2") + " (termina en " + p.ToString("F2") + ")";
        }
        Assert.IsEmpty(fallas, "Se cae por el foso del ascensor:" + fallas);
    }
}
