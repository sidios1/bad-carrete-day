using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Intro tipo historieta: muestra viñetas (encabezado + texto + imagen opcional)
// y avanza con Espacio/E/clic. Esc salta la intro. Al final carga el nivel.
public class SecuenciaIntro : MonoBehaviour
{
    [System.Serializable]
    public class Vineta
    {
        public string encabezado;          // ej: "Concepción, 18:00"
        [TextArea(2, 4)]
        public string texto;
        public Sprite imagen;              // opcional
        public Color colorFondo = Color.black;
    }

    [Header("Viñetas")]
    public Vineta[] vinetas;

    [Header("Referencias UI")]
    public Image fondo;
    public Image imagen;
    public TMP_Text textoEncabezado;
    public TMP_Text textoVineta;
    public TMP_Text textoContador;

    [Header("Input")]
    public InputActionReference inputAvanzar;
    public InputActionReference inputAvanzarAlt;
    public InputActionReference inputSaltar;

    [Header("Siguiente escena")]
    public ControladorCargaEscena cargaEscena;
    public string rutaEscenaSiguiente = "Assets/BadCarreteDay/Escenas/Nivel1_Cerro.unity";

    private int indice = 0;
    private bool terminada = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Mostrar(0);
    }

    void OnEnable()
    {
        // En esta escena no hay PlayerInput: hay que habilitar las acciones a mano
        Suscribir(inputAvanzar, Avanzar);
        Suscribir(inputAvanzarAlt, Avanzar);
        Suscribir(inputSaltar, Saltar);
    }

    void OnDisable()
    {
        if(inputAvanzar != null) inputAvanzar.action.started -= Avanzar;
        if(inputAvanzarAlt != null) inputAvanzarAlt.action.started -= Avanzar;
        if(inputSaltar != null) inputSaltar.action.started -= Saltar;
    }

    private void Suscribir(InputActionReference referencia, System.Action<InputAction.CallbackContext> metodo)
    {
        if(referencia == null) return;
        referencia.action.Enable();
        referencia.action.started += metodo;
    }

    private void Avanzar(InputAction.CallbackContext context) { Siguiente(); }
    private void Saltar(InputAction.CallbackContext context) { Terminar(); }

    // Tambien lo llama el boton "Siguiente" (clic)
    public void Siguiente()
    {
        if(terminada == true) return;
        indice++;
        if(indice >= vinetas.Length) Terminar();
        else Mostrar(indice);
    }

    private void Mostrar(int i)
    {
        Vineta v = vinetas[i];
        fondo.color = v.colorFondo;
        textoEncabezado.text = v.encabezado;
        textoVineta.text = v.texto;
        imagen.gameObject.SetActive(v.imagen != null);
        imagen.sprite = v.imagen;
        if(textoContador != null) textoContador.text = (i + 1) + " / " + vinetas.Length;
    }

    public void Terminar()
    {
        if(terminada == true) return;
        terminada = true;
        int siguiente = SceneUtility.GetBuildIndexByScenePath(rutaEscenaSiguiente);
        cargaEscena.CargaEscena(siguiente >= 0 ? siguiente : 2);
    }
}
