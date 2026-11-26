using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Reglas generales del nivel: victoria, derrota (Game Over), pausa,
// checkpoints y caidas al vacio. Hay UNO por escena de nivel.
public class ControladorJuego : MonoBehaviour
{
    // Datos que sobreviven al cambio de escena (para la pantalla de Fin)
    public enum Resultado { ninguno, victoria, derrota }
    public static Resultado ultimoResultado = Resultado.ninguno;
    public static int indiceUltimoNivel = -1;

    [Header("Escenas")]
    public ControladorCargaEscena cargaEscena;
    [Tooltip("Ruta de la escena Fin (se busca su indice al iniciar; asi no se rompe si cambia el orden del Build)")]
    public string rutaEscenaFin = "Assets/BadCarreteDay/Escenas/Fin.unity";
    [Tooltip("Indice (Build Settings) de la escena Fin (se calcula solo desde la ruta)")]
    public int indiceEscenaFin = 0;
    [Tooltip("Segundos de espera antes de ir a la pantalla de fin")]
    public float esperaAntesDeFin = 2.5f;

    [Header("Jugador")]
    public ThirdPersonController controladorMovimiento;
    public DemoControladorEstadisticas estadisticasJugador;
    public Animator animatorJugador;
    [Tooltip("Colgarse de bordes: si esta colgado, al desbloquear no se reactiva el movimiento")]
    public AgarreBorde agarre;

    [Header("Vidas (como las colas de ardilla del original)")]
    public int vidas = 3;
    [Tooltip("Segundos tirado en el suelo antes de reaparecer")]
    public float esperaReaparecer = 2f;

    [Header("Caidas y checkpoints")]
    public Transform checkpointActual;
    public float danioPorCaida = 25f;

    [Header("Pausa")]
    public InputActionReference inputPausa;
    [Tooltip("Entradas del jugador (se limpian al pausar: ni camara ni salto guardado)")]
    public StarterAssetsInputs entradas;
    public GameObject panelPausa;
    public GameObject panelConfiguracion;

    [Header("Configuracion Eventos")]
    public Eventos eventos;

    private bool juegoTerminado = false;
    private bool enPausa = false;

    void Start()
    {
        eventos.alCambiarVidas.Invoke(vidas);
        Time.timeScale = 1f;
        indiceUltimoNivel = SceneManager.GetActiveScene().buildIndex;
        if(string.IsNullOrEmpty(rutaEscenaFin) == false)
        {
            int indice = SceneUtility.GetBuildIndexByScenePath(rutaEscenaFin);
            if(indice >= 0) indiceEscenaFin = indice;
        }
        if(panelPausa != null) panelPausa.SetActive(false);
    }

    void OnEnable()
    {
        if(inputPausa != null)
            inputPausa.action.started += AlPresionarPausa;
    }

    void OnDisable()
    {
        if(inputPausa != null)
            inputPausa.action.started -= AlPresionarPausa;
    }

    // ===== VICTORIA / DERROTA =====

    public void Victoria()
    {
        if(juegoTerminado == true) return;
        juegoTerminado = true;
        ultimoResultado = Resultado.victoria;
        BloquearJugador(true);
        eventos.alGanar.Invoke();
        StartCoroutine(_IrAFin());
    }

    // Conectar a DemoControladorEstadisticas.eventos.alMorir del jugador:
    // pierde una vida y reaparece en el checkpoint; sin vidas -> Game Over
    public void JugadorMurio()
    {
        if(juegoTerminado == true) return;

        if(agarre != null) agarre.Soltar(); // si muere colgado, primero se suelta
        vidas--;
        eventos.alCambiarVidas.Invoke(vidas);

        if(vidas <= 0) GameOver();
        else StartCoroutine(_Reaparecer());
    }

    IEnumerator _Reaparecer()
    {
        BloquearJugador(true);
        if(animatorJugador != null) animatorJugador.SetTrigger("muerte");

        yield return new WaitForSeconds(esperaReaparecer);

        ReaparecerEnCheckpoint();

        // Salud llena y vivo otra vez
        estadisticasJugador.estadisticasActuales.salud = estadisticasJugador.estadisticasBase.salud;
        estadisticasJugador.estaVivo = true;

        // Reinicia el Animator (sale de la animacion de muerte)
        if(animatorJugador != null)
        {
            animatorJugador.Rebind();
            animatorJugador.Update(0f);
        }
        BloquearJugador(false);
        eventos.alReaparecer.Invoke();
    }

    public void GameOver()
    {
        if(juegoTerminado == true) return;
        juegoTerminado = true;
        ultimoResultado = Resultado.derrota;
        BloquearJugador(true);
        if(animatorJugador != null)
            animatorJugador.SetTrigger("muerte");
        eventos.alPerder.Invoke();
        StartCoroutine(_IrAFin());
    }

    IEnumerator _IrAFin()
    {
        yield return new WaitForSeconds(esperaAntesDeFin);
        cargaEscena.CargaEscena(indiceEscenaFin);
    }

    // ===== BLOQUEO DEL JUGADOR (dialogos, muerte, victoria) =====

    public void BloquearJugador(bool bloquear)
    {
        if(controladorMovimiento == null) return;

        // Colgado de un borde: el movimiento lo maneja AgarreBorde (si no, se caeria del borde)
        if(bloquear == false && agarre != null && (agarre.colgado || agarre.subiendo)) return;
        controladorMovimiento.enabled = !bloquear;

        // Sin esto el personaje queda "caminando en el lugar"
        if(bloquear == true && animatorJugador != null)
            animatorJugador.SetFloat("Speed", 0f);
    }

    // ===== CAIDAS Y CHECKPOINTS =====

    // Conectar a ControladorLugarObjetivo.alLlegarJugador de cada checkpoint
    public void SetCheckpoint(Transform nuevoCheckpoint)
    {
        checkpointActual = nuevoCheckpoint;
        Debug.Log("Checkpoint: " + nuevoCheckpoint.name);
    }

    // Conectar a la zona de vacio (ControladorColisionTrigger) bajo el nivel
    public void CaerAlVacio()
    {
        if(juegoTerminado == true) return;

        estadisticasJugador.CalcularDanio(danioPorCaida);
        if(estadisticasJugador.estaVivo == true)
            ReaparecerEnCheckpoint();
    }

    public void ReaparecerEnCheckpoint()
    {
        if(checkpointActual == null) return;

        // Hay que apagar el CharacterController para poder mover al jugador a mano
        CharacterController cc = controladorMovimiento.GetComponent<CharacterController>();
        cc.enabled = false;
        controladorMovimiento.transform.position = checkpointActual.position;
        controladorMovimiento.transform.rotation = checkpointActual.rotation;
        cc.enabled = true;
    }

    // ===== PAUSA =====

    private void AlPresionarPausa(InputAction.CallbackContext context)
    {
        if(juegoTerminado == true) return;
        // Esc cierra la tienda, no pausa
        if(ControladorTienda.abierta == true || Time.frameCount == ControladorTienda.frameCierre) return;

        if(enPausa == true) Reanudar();
        else Pausar();
    }

    private bool movimientoAntesDePausa = true;

    public void Pausar()
    {
        enPausa = true;
        Time.timeScale = 0f;
        // En pausa la camara no gira y no se guarda ningun salto
        if(controladorMovimiento != null)
        {
            movimientoAntesDePausa = controladorMovimiento.enabled;
            controladorMovimiento.enabled = false;
        }
        LimpiarEntradas(false);
        if(panelPausa != null) panelPausa.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Reanudar()
    {
        enPausa = false;
        Time.timeScale = 1f;
        if(controladorMovimiento != null) controladorMovimiento.enabled = movimientoAntesDePausa;
        LimpiarEntradas(true);
        if(panelPausa != null) panelPausa.SetActive(false);
        if(panelConfiguracion != null) panelConfiguracion.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Borra lo que el jugador apreto (salto, mirar, moverse) para que no "se guarde" en la pausa
    void LimpiarEntradas(bool mirarConMouse)
    {
        if(entradas == null) return;
        entradas.jump = false;
        entradas.planeo = false;
        entradas.look = Vector2.zero;
        entradas.move = Vector2.zero;
        entradas.cursorInputForLook = mirarConMouse;
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        cargaEscena.CargaEscena(0);
    }

    // DEFINICION DE DATOS
    [System.Serializable]
    public struct Eventos
    {
        public UnityEvent alGanar;
        public UnityEvent alPerder;
        public UnityEvent<int> alCambiarVidas;
        public UnityEvent alReaparecer;
    }
}
