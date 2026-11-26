using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Herramienta: reproduce el camino del jugador (Menu -> Jugar -> Intro -> Nivel) y saca
// capturas desde la camara real + lista que colliders hay entre la camara y Benja.
public class PruebaVistaDesdeMenu : InputTestFixture
{
    public override void Setup()
    {
        foreach (PlayerInput pi in Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            Object.DestroyImmediate(pi.gameObject);
        base.Setup();
    }

    [UnityTest, Explicit("Herramienta de diagnostico de camara")]
    public IEnumerator DesdeMenu_HastaNivel()
    {
        string carpeta = Path.Combine(Application.dataPath, "../Capturas/DesdeMenu");
        Directory.CreateDirectory(carpeta);
        Keyboard teclado = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();

        SceneManager.LoadScene("MenuInicio");
        yield return new WaitForSeconds(1f);
        GameObject.Find("Sistemas").SendMessage("Jugar");
        yield return new WaitForSeconds(4f);
        // Intro: avanzar las 4 viñetas con Espacio (como el jugador)
        for (int i = 0; i < 4; i++)
        {
            PressAndRelease(teclado.spaceKey);
            yield return new WaitForSeconds(0.4f);
        }
        yield return new WaitForSeconds(5f);
        Debug.Log("[MENU] escena actual: " + SceneManager.GetActiveScene().name);
        foreach (Camera c in Camera.allCameras)
            Debug.Log("[MENU] CAMARA '" + c.name + "' padre=" + (c.transform.parent ? c.transform.parent.name : "-") + " depth=" + c.depth
                      + " pos=" + c.transform.position.ToString("F2") + " clear=" + c.clearFlags + " escena=" + c.gameObject.scene.name
                      + " componentes=" + string.Join(",", System.Array.ConvertAll(c.GetComponents<Component>(), x => x.GetType().Name)));

        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        yield return Foto(carpeta, "1_llegada", jugador);
        for (int i = 0; i < 30; i++) { Set(mouse.delta, new Vector2(12f, 6f)); yield return null; }
        yield return new WaitForSeconds(0.5f);
        yield return Foto(carpeta, "2_mouse", jugador);
        Press(teclado.wKey);
        yield return new WaitForSeconds(1.5f);
        Release(teclado.wKey);
        yield return new WaitForSeconds(0.5f);
        yield return Foto(carpeta, "3_caminar", jugador);

        foreach (PlayerInput pi in Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            Object.Destroy(pi.gameObject);
        yield return null;
        Assert.Pass();
    }

    IEnumerator Foto(string carpeta, string nombre, GameObject jugador)
    {
        yield return null;
        Camera cam = Camera.main;
        Transform raiz = jugador.transform.Find("PlayerCameraRoot");
        Vector3 desde = cam.transform.position;
        Vector3 hasta = raiz.position;
        string choques = "";
        foreach (RaycastHit h in Physics.RaycastAll(desde, (hasta - desde).normalized, Vector3.Distance(desde, hasta), ~0, QueryTriggerInteraction.Collide))
            choques += h.collider.name + "(" + (h.collider.isTrigger ? "trigger" : "solido") + "," + h.distance.ToString("F2") + ") ";
        Debug.Log("[MENU] " + nombre + " camaras=" + Camera.allCamerasCount + " main=" + cam.name + " pos=" + desde.ToString("F2")
                  + " rot=" + cam.transform.eulerAngles.ToString("F0") + " jugador=" + jugador.transform.position.ToString("F2")
                  + " distancia=" + Vector3.Distance(desde, hasta).ToString("F2") + " entre medio: " + choques);
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
