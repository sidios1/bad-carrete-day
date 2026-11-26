using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Herramienta (no corre sola): fotos de momentos de juego con la interfaz
// (burbujas, camara de dialogo, tienda, pensamientos, Toby) para revisarlas.
public class PruebaCapturasJuego
{
    string carpeta;
    RenderTexture rt;

    [UnityTest, Explicit("Herramienta de capturas")]
    public IEnumerator MomentosDeJuego()
    {
        carpeta = Path.Combine(Application.dataPath, "../Capturas/Juego");
        Directory.CreateDirectory(carpeta);
        if (GameObject.Find("PruebasAutomaticas") == null) Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1.5f);
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        GameObject sistemas = GameObject.Find("Sistemas");

        // Todo se dibuja en una textura 1280x720 (la camara y la interfaz)
        Camera cam = Camera.main;
        rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        Canvas hud = GameObject.Find("HUD").GetComponent<Canvas>();
        hud.renderMode = RenderMode.ScreenSpaceCamera;
        hud.worldCamera = cam;
        hud.planeDistance = 0.5f;

        // 1) Don Lucho: termina de mear, se da vuelta y habla (camara de dialogo + burbuja)
        yield return Ir(jugador, new Vector3(-2f, 0.1f, 9.6f));
        GameObject.Find("NPC_DonLucho").SendMessage("IniciarDialogo");
        yield return new WaitForSeconds(4.5f);
        yield return Foto(cam, "20_conversacion_don_lucho");
        sistemas.SendMessage("TerminarDialogo");
        yield return new WaitForSeconds(1f);

        // 2) Pensamiento de Benja (nube)
        GameObject.Find("HUD").SendMessage("MostrarPensamiento", "Cien pesos. Algo es algo.");
        yield return new WaitForSeconds(0.6f);
        yield return Foto(cam, "21_pensamiento");

        // 3) Doña Marta y su tienda (con plata para que se vean precios)
        GameObject.Find("Sistemas").SendMessage("Sumar", 3000);
        yield return Ir(jugador, new Vector3(-6.6f, 0.1f, 41f));
        GameObject.Find("Punto_HablarMarta").GetComponentInChildren<Collider>().gameObject.SendMessage("Interactuar", new UnityEngine.InputSystem.InputAction.CallbackContext());
        yield return new WaitForSeconds(1.2f);
        yield return Foto(cam, "23_tienda");
        sistemas.SendMessage("Cerrar");
        yield return new WaitForSeconds(1f);

        // 4) Toby gruñendo y empujando
        yield return Ir(jugador, new Vector3(1.2f, 0.1f, 50f));
        yield return new WaitForSeconds(1.2f);
        yield return Foto(cam, "24_toby");

        // 5) Reja sin puas del sitio eriazo y reja con puas al fondo
        yield return Ir(jugador, new Vector3(2f, 0.1f, 15f));
        jugador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        yield return new WaitForSeconds(1f);
        yield return Foto(cam, "25_rejas_sitio_eriazo");

        // 6) Mision en el HUD
        yield return Ir(jugador, new Vector3(0f, 0.1f, 3f));
        yield return new WaitForSeconds(1f);
        yield return Foto(cam, "26_hud_mision");
        // 7) Secuencias cuadro a cuadro: caminar con caña, y colgarse + subir al kiosko
        hud.enabled = false; // sin interfaz en las secuencias (el HUD sigue funcionando)
        Component entradas = jugador.GetComponent("StarterAssetsInputs");
        System.Type tipo = entradas.GetType();
        yield return Ir(jugador, new Vector3(0f, 0.1f, 6f));
        tipo.GetField("move").SetValue(entradas, new Vector2(1f, 0f));
        yield return new WaitForSeconds(0.8f);
        for (int i = 0; i < 6; i++) { yield return Foto(cam, "30_cana_" + i); yield return new WaitForSeconds(0.15f); }
        tipo.GetField("move").SetValue(entradas, Vector2.zero);

        jugador.SendMessage("TomarAgua");
        yield return Ir(jugador, new Vector3(7.9f, 0.1f, 33.2f));
        tipo.GetField("move").SetValue(entradas, new Vector2(0f, 1f));
        yield return new WaitForSeconds(0.4f);
        tipo.GetField("jump").SetValue(entradas, true);
        for (int i = 0; i < 6; i++) { yield return Foto(cam, "31_agarre_" + i); yield return new WaitForSeconds(0.08f); }
        tipo.GetField("move").SetValue(entradas, Vector2.zero);
        yield return new WaitForSeconds(0.5f);
        // Vista de costado (la camara se maneja a mano un momento)
        Behaviour cerebro = (Behaviour)cam.GetComponent("CinemachineBrain");
        cerebro.enabled = false;
        cam.transform.position = new Vector3(3.6f, 3.2f, 30.8f);
        cam.transform.LookAt(new Vector3(7.9f, 1.6f, 34.6f));
        yield return Foto(cam, "33_colgado_lado");
        tipo.GetField("jump").SetValue(entradas, true);
        for (int i = 0; i < 10; i++) { yield return Foto(cam, "34_subida_lado_" + i); yield return new WaitForSeconds(0.12f); }
        cerebro.enabled = true;
        Assert.Pass();
    }

    // Caña, señoras, basureros, secretos y foso del ascensor
    [UnityTest, Explicit("Herramienta de capturas")]
    public IEnumerator CanaSenorasYSecretos()
    {
        carpeta = Path.Combine(Application.dataPath, "../Capturas/Juego");
        Directory.CreateDirectory(carpeta);
        if (GameObject.Find("PruebasAutomaticas") == null) Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1.5f);
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        GameObject sistemas = GameObject.Find("Sistemas");
        Camera cam = Camera.main;
        rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        Canvas hud = GameObject.Find("HUD").GetComponent<Canvas>();
        hud.renderMode = RenderMode.ScreenSpaceCamera;
        hud.worldCamera = cam;
        hud.planeDistance = 0.5f;

        // Con caña (vista borrosa) en el pasaje
        yield return Ir(jugador, new Vector3(0f, 0.1f, 1.5f));
        yield return new WaitForSeconds(1f);
        yield return Foto(cam, "40_cana_vista");
        // Don Lucho con caña
        yield return Ir(jugador, new Vector3(-2f, 0.1f, 9.6f));
        GameObject.Find("NPC_DonLucho").SendMessage("IniciarDialogo");
        yield return new WaitForSeconds(2.5f);
        yield return Foto(cam, "41_lucho_con_cana");
        sistemas.SendMessage("TerminarDialogo");
        yield return new WaitForSeconds(1f);
        // Yoli
        yield return Ir(jugador, new Vector3(1.6f, 0.1f, 4f));
        GameObject.Find("NPC_SraYoli").SendMessage("IniciarDialogo");
        yield return new WaitForSeconds(3f);
        yield return Foto(cam, "42_yoli");
        sistemas.SendMessage("TerminarDialogo");
        yield return new WaitForSeconds(1f);
        // Basurero del pasaje
        yield return Ir(jugador, new Vector3(2.4f, 0.1f, 24f));
        jugador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        yield return new WaitForSeconds(0.6f);
        yield return Foto(cam, "43_basurero_prompt");
        GameObject.Find("Basurero_$300").SendMessage("Revisar");
        yield return new WaitForSeconds(0.25f);
        yield return Foto(cam, "44_basurero_revisado");
        // Nelly
        yield return Ir(jugador, new Vector3(-3f, 0.1f, 32f));
        GameObject.Find("NPC_SraNelly").SendMessage("IniciarDialogo");
        yield return new WaitForSeconds(3f);
        yield return Foto(cam, "45_nelly");
        sistemas.SendMessage("TerminarDialogo");
        yield return new WaitForSeconds(1f);
        // Dumpster del mirador
        yield return Ir(jugador, new Vector3(9.8f, 0.1f, 50.8f));
        jugador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        yield return new WaitForSeconds(0.8f);
        yield return Foto(cam, "46_dumpster");

        // Sin caña: misma vista del pasaje
        jugador.SendMessage("TomarAgua");
        yield return Ir(jugador, new Vector3(0f, 0.1f, 1.5f));
        jugador.transform.rotation = Quaternion.identity;
        yield return new WaitForSeconds(2.6f);
        yield return Foto(cam, "47_sin_cana_vista");
        // Rosa y Gladys
        yield return Ir(jugador, new Vector3(-7f, 4.1f, 84f));
        GameObject.Find("NPC_SraRosa").SendMessage("IniciarDialogo");
        yield return new WaitForSeconds(3f);
        yield return Foto(cam, "48_rosa");
        sistemas.SendMessage("TerminarDialogo");
        yield return new WaitForSeconds(1f);
        yield return Ir(jugador, new Vector3(0.4f, 16.1f, 117f));
        GameObject.Find("NPC_SraGladys").SendMessage("IniciarDialogo");
        yield return new WaitForSeconds(3f);
        yield return Foto(cam, "49_gladys");
        sistemas.SendMessage("TerminarDialogo");
        yield return new WaitForSeconds(1f);

        // Vistas fijas (sin HUD): secretos y foso
        hud.enabled = false;
        Behaviour cerebro = (Behaviour)cam.GetComponent("CinemachineBrain");
        cerebro.enabled = false;
        cam.transform.position = new Vector3(6.2f, 1.7f, 39.6f);
        cam.transform.LookAt(new Vector3(9.4f, 1.2f, 38.6f));
        yield return Foto(cam, "50_secreto_kiosko");
        cam.transform.position = new Vector3(-3.5f, 5.9f, 88.5f);
        cam.transform.LookAt(new Vector3(0.3f, 4.3f, 87f));
        yield return Foto(cam, "51_secreto_escalera_rota");
        cam.transform.position = new Vector3(-4f, 6.5f, 83f);
        cam.transform.LookAt(new Vector3(0f, 4.5f, 87f));
        yield return Foto(cam, "52_secreto_escalera_desde_calle");
        GameObject.Find("Ascensor").SendMessage("Activar");
        yield return new WaitForSeconds(4f);
        cam.transform.position = new Vector3(5f, 13f, 105f);
        cam.transform.LookAt(new Vector3(6f, 10f, 111.6f));
        yield return Foto(cam, "53_foso_ascensor");
        cerebro.enabled = true;
        Assert.Pass();
    }

    // Toby, la escoba en la mano (varios agarres), Gladys y el almacen
    [UnityTest, Explicit("Herramienta de capturas")]
    public IEnumerator TobyEscobaAlmacen()
    {
        carpeta = Path.Combine(Application.dataPath, "../Capturas/Juego");
        Directory.CreateDirectory(carpeta);
        if (GameObject.Find("PruebasAutomaticas") == null) Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1.5f);
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        GameObject sistemas = GameObject.Find("Sistemas");
        Camera cam = Camera.main;
        rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        Canvas hud = GameObject.Find("HUD").GetComponent<Canvas>();
        hud.renderMode = RenderMode.ScreenSpaceCamera;
        hud.worldCamera = cam;
        hud.planeDistance = 0.5f;

        // Menu de pausa con los botones de atajos
        sistemas.SendMessage("Pausar");
        yield return Foto(cam, "69_pausa_atajos");
        sistemas.SendMessage("Reanudar");
        yield return null;

        // Almacen con caña y la tienda abierta
        yield return Ir(jugador, new Vector3(-6.6f, 0.1f, 41f));
        jugador.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        GameObject.Find("Punto_HablarMarta").GetComponentInChildren<Collider>().gameObject.SendMessage("Interactuar", new UnityEngine.InputSystem.InputAction.CallbackContext());
        yield return new WaitForSeconds(1.5f);
        yield return Foto(cam, "60_tienda_con_cana");
        sistemas.SendMessage("Cerrar");
        jugador.SendMessage("TomarAgua");
        yield return new WaitForSeconds(2.5f);

        Behaviour cerebro = (Behaviour)cam.GetComponent("CinemachineBrain");
        hud.enabled = false;
        cerebro.enabled = false;
        cam.transform.position = new Vector3(-3.5f, 1.8f, 41f);
        cam.transform.LookAt(new Vector3(-11f, 1.3f, 41.5f));
        yield return Foto(cam, "61_almacen_frente");
        cam.transform.position = new Vector3(-7.2f, 1.7f, 39f);
        cam.transform.LookAt(new Vector3(-13f, 1.2f, 43f));
        yield return Foto(cam, "62_almacen_interior");

        // Toby de cerca
        cam.transform.position = new Vector3(1.6f, 1.2f, 50.8f);
        cam.transform.LookAt(new Vector3(0f, 0.6f, 53.6f));
        yield return Foto(cam, "63_toby");
        cam.transform.position = new Vector3(0f, 0.9f, 51.2f);
        cam.transform.LookAt(new Vector3(0f, 0.7f, 53.6f));
        yield return Foto(cam, "64_toby_frente");

        // Gladys al fondo de la calle alta
        cam.transform.position = new Vector3(-5f, 5.7f, 104.5f);
        cam.transform.LookAt(new Vector3(-5.2f, 4.9f, 108.6f));
        yield return Foto(cam, "65_gladys");

        // Escoba en la mano: agarres de prueba (Benja de costado, en un lugar abierto)
        GameObject.Find("NPC_SraGladys").SendMessage("IniciarDialogo");
        yield return null;
        sistemas.SendMessage("TerminarDialogo");
        yield return null;
        yield return Ir(jugador, new Vector3(0f, 0.1f, 20f));
        jugador.transform.rotation = Quaternion.identity;
        yield return new WaitForSeconds(1f);
        Transform agarre = GameObject.Find("Palo_Escoba_Visual").transform;
        yield return Ir(jugador, new Vector3(0f, 0.1f, 20f));
        jugador.transform.rotation = Quaternion.identity;
        cam.transform.position = jugador.transform.position + new Vector3(3.2f, 1.3f, 0.6f);
        cam.transform.LookAt(jugador.transform.position + Vector3.up * 1.1f);
        // Golpe con el agarre actual del constructor (frames)
        Animator anim = jugador.GetComponent<Animator>();
        cam.transform.position = jugador.transform.position + new Vector3(3.2f, 1.3f, 0.6f);
        cam.transform.LookAt(jugador.transform.position + Vector3.up * 1.1f);
        anim.SetTrigger("ataque");
        for (int f = 0; f < 7; f++) { yield return new WaitForSeconds(0.15f); yield return Foto(cam, "68_golpe_" + f); }
        cerebro.enabled = true;
        Assert.Pass();
    }

    // Julio: de frente, de espalda (cuchillo), la cara, rugido, golpe y embestida
    [UnityTest, Explicit("Herramienta de capturas")]
    public IEnumerator JulioCapturas()
    {
        carpeta = Path.Combine(Application.dataPath, "../Capturas/Juego/Julio");
        Directory.CreateDirectory(carpeta);
        if (GameObject.Find("PruebasAutomaticas") == null) Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1.5f);
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        jugador.SendMessage("TomarAgua");
        jugador.SendMessage("TomarEscoba");
        GameObject julio = GameObject.Find("Julio_ElPastero");
        Camera cam = Camera.main;
        rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        GameObject.Find("HUD").GetComponent<Canvas>().enabled = false;
        ((Behaviour)cam.GetComponent("CinemachineBrain")).enabled = false;
        yield return Ir(jugador, new Vector3(0f, 16.1f, 145f));
        Vector3 p = julio.transform.position;
        yield return new WaitForSeconds(1f);
        cam.transform.position = p + new Vector3(0.6f, 1.7f, -3.2f); cam.transform.LookAt(p + Vector3.up * 1.2f);
        yield return Foto(cam, "70_julio_frente");
        cam.transform.position = p + new Vector3(0.2f, 2.0f, -1.1f); cam.transform.LookAt(p + Vector3.up * 1.9f);
        yield return Foto(cam, "71_julio_cara");
        cam.transform.position = p + new Vector3(-1.2f, 2.0f, 2.6f); cam.transform.LookAt(p + Vector3.up * 1.3f);
        yield return Foto(cam, "72_julio_espalda_cuchillo");
        cam.transform.position = p + new Vector3(-0.4f, 1.9f, 1.0f); cam.transform.LookAt(p + Vector3.up * 1.5f);
        yield return Foto(cam, "73_cuchillo_cerca");
        // Rugido (empieza la pelea) y golpe
        julio.SendMessage("EmpezarPelea");
        cam.transform.position = p + new Vector3(2.8f, 1.6f, -2.2f); cam.transform.LookAt(p + Vector3.up * 1.1f);
        for (int f = 0; f < 4; f++) { yield return new WaitForSeconds(0.3f); yield return Foto(cam, "74_rugido_" + f); }
        yield return Ir(jugador, p + new Vector3(0f, 0.1f, -1.8f));
        for (int f = 0; f < 8; f++)
        {
            yield return new WaitForSeconds(0.25f);
            Vector3 q = julio.transform.position;
            cam.transform.position = q + new Vector3(3.2f, 1.6f, -0.5f); cam.transform.LookAt(q + Vector3.up * 1.1f);
            yield return Foto(cam, "75_golpe_" + f);
        }
        // Fase 2: embestida
        yield return Ir(jugador, julio.transform.position + new Vector3(0f, 0.1f, -9f));
        julio.SendMessage("CalcularDanio", 220f);
        for (int f = 0; f < 14; f++)
        {
            yield return new WaitForSeconds(0.25f);
            Vector3 q = julio.transform.position;
            cam.transform.position = q + new Vector3(4.5f, 2f, -1f); cam.transform.LookAt(q + Vector3.up * 1.1f);
            yield return Foto(cam, "76_enojado_" + f);
        }
        Assert.Pass();
    }

    IEnumerator Ir(GameObject jugador, Vector3 pos)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = pos;
        cc.enabled = true;
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator Foto(Camera cam, string nombre)
    {
        yield return null;
        yield return null;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(carpeta, nombre + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        Object.Destroy(tex);
    }
}
