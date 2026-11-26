using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Julio "El Pastero" (jefe tanque):
//  - por la espalda (cuchillo) el golpe hace x3; de frente no se interrumpe (no siente el dolor)
//  - es lento; en la fase 2 (enojado) no hay secuaces, embiste y despues queda agotado
public class PruebaG_Julio
{
    GameObject jugador, julio;

    [SetUp]
    public void MarcarPruebas()
    {
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    IEnumerator Preparar()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        julio = GameObject.Find("Julio_ElPastero");
        jugador.SendMessage("TomarAgua");
        jugador.SendMessage("TomarEscoba");
    }

    IEnumerator Ir(Vector3 pos, Vector3 mirarA)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = pos;
        Vector3 d = mirarA - pos; d.y = 0f;
        jugador.transform.rotation = Quaternion.LookRotation(d);
        cc.enabled = true;
        yield return new WaitForSeconds(0.4f);
    }

    float Salud()
    {
        Component stats = julio.GetComponent("DemoControladorEstadisticas");
        object actuales = stats.GetType().GetField("estadisticasActuales").GetValue(stats);
        return (float)actuales.GetType().GetField("salud").GetValue(actuales);
    }

    object Conducta(string propiedad)
    {
        Component c = julio.GetComponent("ConductaJulio");
        return c.GetType().GetProperty(propiedad).GetValue(c);
    }

    [UnityTest]
    public IEnumerator PorLaEspalda_DueleTriple_DeFrente_SeRie()
    {
        // Por la espalda (Julio mira hacia -z; su espalda da a +z)
        yield return Preparar();
        Vector3 p = julio.transform.position;
        float s0 = Salud();
        yield return Ir(p + new Vector3(0f, 0.1f, 1.1f), p);
        jugador.SendMessage("Atacar", new InputAction.CallbackContext());
        yield return new WaitForSeconds(1.5f);
        float espalda = s0 - Salud();
        Debug.Log("[JULIO] golpe en la espalda: " + espalda);
        Assert.AreEqual(75f, espalda, 0.01f, "Por la espalda deberia quitar 25 x 3");

        // De frente
        yield return Preparar();
        p = julio.transform.position;
        s0 = Salud();
        yield return Ir(p + new Vector3(0f, 0.1f, -1.1f), p);
        jugador.SendMessage("Atacar", new InputAction.CallbackContext());
        bool seTambaleo = false;
        Animator anim = julio.GetComponent<Animator>();
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("HitReaction")) seTambaleo = true;
            yield return null;
        }
        float frente = s0 - Salud();
        Debug.Log("[JULIO] golpe de frente: " + frente + " · se tambaleo: " + seTambaleo);
        Assert.AreEqual(25f, frente, 0.01f, "De frente deberia quitar 25");
        Assert.IsFalse(seTambaleo, "De frente no deberia interrumpirse (no siente el dolor)");
    }

    [UnityTest]
    public IEnumerator Tanque_Lento_YEnojadoEmbisteSinSecuaces()
    {
        yield return Preparar();
        Vector3 p = julio.transform.position;
        yield return Ir(p + new Vector3(0f, 0.1f, -9f), p);
        julio.SendMessage("EmpezarPelea");
        float maxVel = 0f;
        UnityEngine.AI.NavMeshAgent agente = julio.GetComponent<UnityEngine.AI.NavMeshAgent>();
        for (float t = 0f; t < 2.5f; t += Time.deltaTime) { maxVel = Mathf.Max(maxVel, agente.velocity.magnitude); yield return null; }
        Debug.Log("[JULIO] velocidad caminando: " + maxVel.ToString("F2"));
        Assert.Less(maxVel, 2.0f, "Julio deberia ser lento (tanque)");

        // Fase 2: enojado, sin secuaces, embiste y queda agotado
        yield return Ir(julio.transform.position + new Vector3(0f, 0.1f, -9f), julio.transform.position);
        julio.SendMessage("CalcularDanio", 220f);
        bool agotado = false;
        float maxVelEmbestida = 0f;
        for (float t = 0f; t < 10f && !agotado; t += Time.deltaTime)
        {
            maxVelEmbestida = Mathf.Max(maxVelEmbestida, agente.velocity.magnitude);
            if ((int)Conducta("embestidas") > 0 && (bool)Conducta("embistiendo") == false && agente.isStopped) agotado = true;
            yield return null;
        }
        Debug.Log("[JULIO] embestidas: " + Conducta("embestidas") + " · velocidad max embistiendo: " + maxVelEmbestida.ToString("F2") + " · quedo agotado: " + agotado);
        Assert.IsNull(GameObject.Find("Refuerzo_David"), "Julio no tiene secuaces");
        Assert.Greater((int)Conducta("embestidas"), 0, "En la fase 2 deberia embestir");
        Assert.Greater(maxVelEmbestida, 4f, "La embestida deberia ser rapida");
        Assert.IsTrue(agotado, "Despues de embestir deberia quedar agotado");
    }

    // Corriendo en circulo alrededor de Julio (a 5,3 m/s, como con Shift) se llega a su espalda
    [UnityTest]
    public IEnumerator CorriendoAlrededor_SeLlegaASuEspalda()
    {
        yield return Preparar();
        Vector3 p = julio.transform.position;
        yield return Ir(p + new Vector3(0f, 0.1f, -2.3f), p);
        // Como en el juego: al acercarse habla; al terminar la conversacion empieza la pelea
        if (GameObject.Find("PanelDialogo") != null) GameObject.Find("Sistemas").SendMessage("TerminarDialogo");
        yield return null;
        julio.SendMessage("EmpezarPelea");
        yield return new WaitForSeconds(1.5f);
        CharacterController cc = jugador.GetComponent<CharacterController>();
        float mejor = 180f, tiempoDetras = 0f, giroMax = 0f;
        // Como el juego real: 60 cuadros por segundo (en pruebas corre a miles y los giros por cuadro
        // son tan chicos que Quaternion.RotateTowards los trata como 0 y no los limita)
        Time.captureDeltaTime = 1f / 60f;
        // La rotacion se mide al FINAL del cuadro (despues de todos los scripts), que es lo que se ve
        MedidorGiro medidor = julio.AddComponent<MedidorGiro>();
        yield return null;
        Quaternion antes = Quaternion.Euler(0f, medidor.yaw, 0f);
        float ventana = 0f;
        for (float t = 0f; t < 6f; t += Time.deltaTime)
        {
            Vector3 centro = julio.transform.position;
            Vector3 radio = jugador.transform.position - centro; radio.y = 0f;
            Vector3 tangente = Vector3.Cross(Vector3.up, radio.normalized);
            Vector3 correccion = radio.normalized * (2.3f - radio.magnitude) * 4f;
            cc.Move((tangente * 5.3f + correccion) * Time.deltaTime);
            yield return null;
            Vector3 haciaJugador = jugador.transform.position - julio.transform.position; haciaJugador.y = 0f;
            float a = Vector3.Angle(-julio.transform.forward, haciaJugador);
            mejor = Mathf.Min(mejor, a);
            if (a < 60f) tiempoDetras += Time.deltaTime;
            // giro medido en ventanas de 0,25 s (por cuadro el angulo es tan chico que se redondea a 0)
            ventana += Time.deltaTime;
            if (ventana >= 0.25f)
            {
                giroMax = Mathf.Max(giroMax, Mathf.Abs(Mathf.DeltaAngle(antes.eulerAngles.y, medidor.yaw)) / ventana);
                antes = Quaternion.Euler(0f, medidor.yaw, 0f);
                ventana = 0f;
            }
        }
        Time.captureDeltaTime = 0f;
        Debug.Log("[JULIO] rodeandolo: mejor angulo a su espalda " + mejor.ToString("F0") + "° · segundos detras " + tiempoDetras.ToString("F1") + " · giro max " + giroMax.ToString("F0") + "°/s");
        Assert.Less(mejor, 30f, "Rodeandolo corriendo no se llega a su espalda");
        Assert.Greater(tiempoDetras, 0.8f, "Se queda muy poco tiempo de espaldas");
        Assert.Less(giroMax, 70f, "Julio gira demasiado rapido");
    }
}

// Guarda la rotacion de un objeto al final del cuadro (despues de los LateUpdate del juego)
[DefaultExecutionOrder(10000)]
public class MedidorGiro : MonoBehaviour
{
    public float yaw;
    void LateUpdate() { yaw = transform.eulerAngles.y; }
}
