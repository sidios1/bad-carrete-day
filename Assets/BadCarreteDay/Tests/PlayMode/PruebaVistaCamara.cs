using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Juega unos segundos con teclado/mouse simulados y guarda capturas desde la camara REAL del juego
// (carpeta Capturas/Juego) + posiciones en el log, para revisar donde queda la camara.
public class PruebaVistaCamara : InputTestFixture
{
    // Antes de que InputTestFixture reinicie el sistema de input, eliminamos cualquier
    // PlayerInput que haya quedado de otra prueba (si no, falla al cambiar de escena)
    public override void Setup()
    {
        foreach (PlayerInput pi in Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            Object.DestroyImmediate(pi.gameObject);
        base.Setup();
    }

    string carpeta;

    [UnityTest, Explicit("Herramienta: saca capturas de la camara (correr a mano desde Test Runner)")]
    public IEnumerator Capturas_DesdeLaCamaraDelJuego()
    {
        carpeta = Path.Combine(Application.dataPath, "../Capturas/Juego");
        Directory.CreateDirectory(carpeta);
        Keyboard teclado = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();

        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1.5f);
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");

        yield return Foto("1_inicio", jugador);

        Press(teclado.wKey);
        yield return new WaitForSeconds(1.5f);
        Release(teclado.wKey);
        yield return new WaitForSeconds(0.5f);
        yield return Foto("2_tras_caminar", jugador);

        for (int i = 0; i < 30; i++) { Set(mouse.delta, new Vector2(20f, 0f)); yield return null; }
        yield return new WaitForSeconds(0.5f);
        yield return Foto("3_tras_mouse_derecha", jugador);

        for (int i = 0; i < 20; i++) { Set(mouse.delta, new Vector2(0f, 15f)); yield return null; }
        yield return new WaitForSeconds(0.5f);
        yield return Foto("4_tras_mouse_arriba", jugador);

        foreach (PlayerInput pi in Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            Object.Destroy(pi.gameObject);
        yield return null;
        Assert.Pass();
    }

    IEnumerator Foto(string nombre, GameObject jugador)
    {
        yield return null; // (WaitForEndOfFrame no avanza en modo consola)
        Camera cam = Camera.main;
        Transform raiz = jugador.transform.Find("PlayerCameraRoot");
        Debug.Log("[VISTA] " + nombre + " camara=" + cam.transform.position.ToString("F2") + " rot=" + cam.transform.eulerAngles.ToString("F0")
                  + " jugador=" + jugador.transform.position.ToString("F2") + " PlayerCameraRoot=" + (raiz ? raiz.position.ToString("F2") : "NULL")
                  + " fov=" + cam.fieldOfView.ToString("F0") + " near=" + cam.nearClipPlane);
        RenderTexture rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(carpeta, nombre + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.Destroy(rt);
        Object.Destroy(tex);
    }
}
