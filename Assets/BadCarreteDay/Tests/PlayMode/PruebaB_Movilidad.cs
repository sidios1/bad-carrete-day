using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Movilidad con teclado REAL (InputTestFixture):
//  - Colgarse del borde de una reja sin puas y pasar al otro lado con Saltar
//  - La reja con puas hace daño y empuja
//  - En pausa la camara no gira y el salto no queda guardado
public class PruebaB_Movilidad : InputTestFixture
{
    Keyboard teclado;
    Mouse mouse;
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

    IEnumerator Cargar()
    {
        teclado = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
    }

    IEnumerator Ir(Vector3 pos)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = pos;
        cc.enabled = true;
        yield return new WaitForSeconds(0.4f);
    }

    string Texto(string nombre) { return GameObject.Find(nombre).GetComponent<TMP_Text>().text; }

    [UnityTest]
    public IEnumerator SeCuelgaDeLaReja_YPasaConSaltar()
    {
        yield return Cargar();
        jugador.SendMessage("TomarAgua"); // con caña no se salta ni se cuelga
        // Frente a la reja sin puas del sitio eriazo (x 4,45), mirando hacia ella (+x = tecla D)
        yield return Ir(new Vector3(3.0f, 0.1f, 16f));
        Animator anim = jugador.GetComponent<Animator>();
        Press(teclado.dKey);
        yield return new WaitForSeconds(0.7f); // gira hacia la reja y camina
        PressAndRelease(teclado.spaceKey);
        bool colgo = false;
        for (float t = 0f; t < 1.5f && !colgo; t += Time.deltaTime) { colgo = anim.GetBool("Colgado"); yield return null; }
        Release(teclado.dKey);
        Assert.IsTrue(colgo, "Benja no se colgo del borde de la reja");
        yield return new WaitForSeconds(0.3f);

        // Saltar: sube y pasa al otro lado
        PressAndRelease(teclado.spaceKey);
        yield return new WaitForSeconds(2f);
        Assert.Greater(jugador.transform.position.x, 4.5f, "No paso la reja al subir");
        Assert.IsFalse(anim.GetBool("Colgado"), "Quedo colgado");
    }

    [UnityTest]
    public IEnumerator RejaConPuas_HaceDanio_YEmpuja()
    {
        yield return Cargar();
        Assert.AreEqual("100", Texto("TextoSalud"));
        // Pegado a la reja con puas del borde del barranco (x 12,4)
        yield return Ir(new Vector3(12.05f, 0.1f, 12.5f));
        yield return new WaitForSeconds(0.6f);
        Assert.AreEqual("85", Texto("TextoSalud"), "Las puas no hicieron 15 de daño");
        Assert.Less(jugador.transform.position.x, 11.95f, "Las puas no lo empujaron hacia atras");
    }

    [UnityTest]
    public IEnumerator Pausa_SinCamara_NiSaltoGuardado()
    {
        yield return Cargar();
        Transform objetivoCamara = GameObject.Find("PlayerCameraRoot").transform;
        float y0 = jugador.transform.position.y;

        PressAndRelease(teclado.escapeKey);
        yield return null;
        Assert.AreEqual(0f, Time.timeScale, "No se pauso");
        Quaternion antes = objetivoCamara.rotation;
        // Mover el mouse y apretar saltar en pausa
        for (int i = 0; i < 10; i++) { Set(mouse.delta, new Vector2(40f, 15f)); yield return null; }
        Set(mouse.delta, Vector2.zero);
        PressAndRelease(teclado.spaceKey);
        yield return null;
        Assert.Less(Quaternion.Angle(antes, objetivoCamara.rotation), 0.5f, "La camara giro en pausa");

        PressAndRelease(teclado.escapeKey);
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(1f, Time.timeScale, "No se reanudo");
        Assert.Less(jugador.transform.position.y - y0, 0.2f, "Al volver de la pausa salto solo (salto guardado)");
    }
}
