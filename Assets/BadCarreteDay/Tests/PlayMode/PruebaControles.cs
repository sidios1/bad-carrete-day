using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Simula teclado y mouse REALES (InputTestFixture del Input System) y mide:
//  - WASD debe mover a Benja (y su modelo con el)
//  - El mouse debe girar la camara
public class PruebaControles : InputTestFixture
{
    // Antes de que InputTestFixture reinicie el sistema de input, eliminamos cualquier
    // PlayerInput que haya quedado de otra prueba (si no, falla al cambiar de escena)
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
    }

    [UnityTest]
    public IEnumerator WASD_MueveABenja_Y_MouseGiraLaCamara()
    {
        Keyboard teclado = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();

        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);

        // Solo debe haber UNA camara (los FBX de Mixamo traian una camara escondida bajo el suelo)
        Assert.AreEqual(1, Camera.allCamerasCount, "Hay mas de una camara en el nivel: " +
            string.Join(", ", System.Array.ConvertAll(Camera.allCameras, c => c.name)));

        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        Transform caderas = null;
        foreach (Transform t in jugador.GetComponentsInChildren<Transform>())
            if (t.name.EndsWith("Hips")) { caderas = t; break; }
        Camera cam = Camera.main;
        Animator anim = jugador.GetComponent<Animator>();

        Vector3 jugador0 = jugador.transform.position;
        Vector3 caderas0 = caderas.position;
        Vector3 camara0 = cam.transform.position;
        Debug.Log("[CTRL] esquema=" + jugador.GetComponent<PlayerInput>().currentControlScheme
                  + " dispositivos=" + string.Join(",", System.Array.ConvertAll(jugador.GetComponent<PlayerInput>().devices.ToArray(), d => d.name)));

        // --- W por 1 segundo ---
        Press(teclado.wKey);
        yield return new WaitForSeconds(1f);
        float velocidadAnim = anim.GetFloat("Speed");
        Release(teclado.wKey);
        yield return new WaitForSeconds(0.3f);

        float movJugador = Vector3.Distance(jugador0, jugador.transform.position);
        float movCaderas = Vector3.Distance(caderas0, caderas.position);
        float movCamara = Vector3.Distance(camara0, cam.transform.position);
        Debug.Log("[CTRL] W: jugador se movio " + movJugador.ToString("F2") + " m · modelo (caderas) " + movCaderas.ToString("F2")
                  + " m · camara " + movCamara.ToString("F2") + " m · Speed animator=" + velocidadAnim.ToString("F2"));

        // --- Mouse a la derecha por 20 frames ---
        float yaw0 = cam.transform.eulerAngles.y;
        Vector3 jugadorAntesMouse = jugador.transform.position;
        for (int i = 0; i < 20; i++)
        {
            Set(mouse.delta, new Vector2(25f, 0f));
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
        float giro = Mathf.Abs(Mathf.DeltaAngle(yaw0, cam.transform.eulerAngles.y));
        Debug.Log("[CTRL] Mouse: camara giro " + giro.ToString("F1") + " grados · jugador se movio " + Vector3.Distance(jugadorAntesMouse, jugador.transform.position).ToString("F2") + " m");

        // Limpieza: el PlayerInput debe apagarse ANTES de que InputTestFixture reinicie el
        // sistema de input (si no, falla al cargar la escena de la siguiente prueba)
        foreach (PlayerInput pi in Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            Object.Destroy(pi.gameObject);
        yield return null;

        // Con caña camina a 1 m/s (en 1 s, contando la aceleracion, avanza ~1 m)
        Assert.Greater(movJugador, 0.6f, "WASD no movio a Benja");
        Assert.Greater(movCaderas, 0.6f, "El modelo de Benja no siguio al jugador");
        Assert.Greater(giro, 5f, "El mouse no giro la camara");
    }
}
