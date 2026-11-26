using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Conversaciones estilo Conker: el texto sale en una BURBUJA sobre la cabeza de quien habla,
// letra por letra y con un murmullo (Voz). Los garabatos censurados (*) suenan con un pitido.
// Al empezar, Benja mira al personaje y la camara se acerca (CamaraDialogo).
// Para no perderse la historia: no se puede pasar de linea hasta que termine de escribirse.
// Avanza con la accion Interaccion (E). Hay UNO por escena.
// Una linea que empieza con "Benja: " la dice Benja (la burbuja sale sobre el).
public class ControladorDialogo : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Burbuja de conversacion (tiene el componente Burbuja)")]
    public GameObject panelDialogo;
    public TMP_Text textoNombre;
    public TMP_Text textoDialogo;
    public Burbuja burbuja;

    [Header("Camara y jugador")]
    public CamaraDialogo camaraDialogo;
    public Transform jugador;

    [Header("Input")]
    public InputActionReference inputAvanzar;

    [Header("Configuracion")]
    [Tooltip("Letras por segundo al escribir")]
    public float velocidadEscritura = 30f;
    [Tooltip("Segundos que hay que esperar, despues de escrita la linea, para poder pasar")]
    public float esperaMinima = 0.4f;
    [Tooltip("Pitido para los garabatos censurados con *")]
    public AudioClip sonidoCensura;

    // PROPIEDAD: avisa cuando empieza y termina un dialogo (para bloquear al jugador)
    [SerializeField]
    private bool _dialogoActivo = false;
    public bool dialogoActivo
    {
        get { return _dialogoActivo; }
        set
        {
            if(value != _dialogoActivo)
            {
                _dialogoActivo = value;
                if(_dialogoActivo == true)
                    eventos.alComenzar.Invoke();
                else
                    eventos.alTerminar.Invoke();
            }
        }
    }

    [Header("Configuracion Eventos")]
    public Eventos eventos;

    private DialogoNPC npcActual;
    private int indiceLinea = 0;
    private bool escribiendo = false;
    private float listoDesde = 0f;
    private int frameInicio = -1;
    private int frameFin = -1;

    void Start()
    {
        if(panelDialogo != null)
            panelDialogo.SetActive(false);
    }

    void OnEnable()
    {
        inputAvanzar.action.started += Avanzar;
    }

    void OnDisable()
    {
        inputAvanzar.action.started -= Avanzar;
    }

    // METODOS

    public void ComenzarDialogo(DialogoNPC npc)
    {
        if(dialogoActivo == true) return;
        if(Time.timeScale == 0f) return;              // en pausa no
        if(ControladorTienda.abierta == true) return; // con la tienda abierta no
        // La misma tecla E que cierra un dialogo no debe volver a abrirlo
        if(Time.frameCount == frameFin) return;

        npcActual = npc;
        indiceLinea = 0;
        // Guardamos el frame: la misma tecla E que inicia el dialogo no debe saltarse la primera linea
        frameInicio = Time.frameCount;

        if(camaraDialogo != null) camaraDialogo.Enfocar(npc.transform);
        panelDialogo.SetActive(true);
        dialogoActivo = true;

        MostrarLinea();
    }

    private void Avanzar(InputAction.CallbackContext context)
    {
        if(dialogoActivo == false) return;
        if(Time.frameCount == frameInicio) return;
        if(Time.timeScale == 0f) return;

        // Para no perderse la historia: no se salta la linea mientras se escribe
        if(escribiendo == true) return;
        if(Time.time < listoDesde) return;

        indiceLinea++;
        if(indiceLinea >= npcActual.ConversacionActual().lineas.Length)
            TerminarDialogo();
        else
            MostrarLinea();
    }

    private void MostrarLinea()
    {
        StopAllCoroutines();

        // ¿Quien dice esta linea? Por defecto el personaje; si empieza con "Benja: ", Benja.
        string linea = LineaActual();
        string nombre = npcActual.nombrePersonaje;
        Transform quien = npcActual.transform;
        if(linea.StartsWith("Benja: ") && jugador != null)
        {
            linea = linea.Substring(7);
            nombre = "Benja";
            quien = jugador;
        }
        Voz voz = quien.GetComponent<Voz>();
        textoNombre.text = nombre;
        if(burbuja != null) burbuja.Seguir(quien, voz != null ? voz.alturaCabeza : 2f);

        StartCoroutine(_EscribirLinea(linea, voz));
    }

    private string LineaActual()
    {
        return npcActual.ConversacionActual().lineas[indiceLinea];
    }

    IEnumerator _EscribirLinea(string linea, Voz voz)
    {
        escribiendo = true;
        if(burbuja != null && burbuja.indicadorAvanzar != null) burbuja.indicadorAvanzar.SetActive(false);
        textoDialogo.text = "";

        int letras = 0;
        char anterior = ' ';
        foreach(char letra in linea)
        {
            textoDialogo.text += letra;
            if(voz != null)
            {
                if(letra == '*')
                {
                    // Garabato censurado: un solo pitido por palabra
                    if(anterior != '*') voz.Sonar(sonidoCensura);
                }
                else if(char.IsLetter(letra))
                {
                    letras++;
                    if(letras % voz.letrasPorSilaba == 1) voz.Silaba();
                }
            }
            anterior = letra;
            yield return new WaitForSeconds(1f / velocidadEscritura);
        }

        escribiendo = false;
        listoDesde = Time.time + esperaMinima;
        yield return new WaitForSeconds(esperaMinima);
        if(burbuja != null && burbuja.indicadorAvanzar != null) burbuja.indicadorAvanzar.SetActive(true);
    }

    private void TerminarDialogo()
    {
        StopAllCoroutines();
        escribiendo = false;
        panelDialogo.SetActive(false);
        if(camaraDialogo != null) camaraDialogo.Soltar();
        dialogoActivo = false;
        frameFin = Time.frameCount;

        DialogoNPC npcQueHablo = npcActual;
        npcActual = null;
        if(npcQueHablo != null) npcQueHablo.TerminoDialogo();
    }

    // DEFINICION DE DATOS
    [System.Serializable]
    public struct Eventos
    {
        public UnityEvent alComenzar;
        public UnityEvent alTerminar;
    }
}
