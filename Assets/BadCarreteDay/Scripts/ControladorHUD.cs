using TMPro;
using UnityEngine;

// Muestra en pantalla: salud (barra de clase), lucas, objetivo de la mision
// y el aviso de accion contextual ("[E] Hablar").
// Los demas scripts le avisan por UnityEvents; el HUD no busca nada por su cuenta.
public class ControladorHUD : MonoBehaviour
{
    [Header("Salud")]
    [Tooltip("Barra de salud de clase (usa un Animator)")]
    public ControladorBarraSalud barraSalud;
    public TMP_Text textoSalud;
    public float saludMaxima = 100f;

    [Header("Vidas")]
    public TMP_Text textoVidas;

    [Header("Lucas")]
    public TMP_Text textoDinero;

    [Header("Mision")]
    public TMP_Text textoMision;
    [Tooltip("Panel de la mision (se agranda un poco al cambiar el objetivo)")]
    public RectTransform panelMision;
    public AudioSource sonidoMision;

    [Header("Titulo grande (presentacion de personajes)")]
    public TMP_Text textoTitulo;

    [Header("Burbujas (frases y pensamientos sobre los personajes)")]
    public ControladorBurbujas burbujas;

    [Header("Accion contextual")]
    public GameObject panelPrompt;
    public TMP_Text textoPrompt;

    [Header("Pensamientos de Benja")]
    public GameObject panelPensamiento;
    public TMP_Text textoPensamiento;
    public float duracionPensamiento = 3.5f;

    void Start()
    {
        OcultarPrompt();
        if(panelPensamiento != null) panelPensamiento.SetActive(false);
        if(textoTitulo != null) textoTitulo.gameObject.SetActive(false);
    }

    // Muestra un comentario de Benja unos segundos (ej: al recoger la primera luka).
    // Si el texto empieza con "Nombre: " y ese personaje esta en la escena, la frase sale
    // en una burbuja sobre ese personaje (ej: "Doña Marta: No te alcanza, mijo.").
    public void MostrarPensamiento(string texto)
    {
        if(burbujas != null)
        {
            int dosPuntos = texto.IndexOf(": ");
            if(dosPuntos > 0)
            {
                Voz quien = Voz.Buscar(texto.Substring(0, dosPuntos));
                if(quien != null)
                {
                    burbujas.Decir(quien.transform, quien.nombre, texto.Substring(dosPuntos + 2));
                    return;
                }
            }
            burbujas.Pensar(texto);
            return;
        }
        if(panelPensamiento == null) return;
        StopAllCoroutines();
        StartCoroutine(_MostrarPensamiento(texto));
    }

    System.Collections.IEnumerator _MostrarPensamiento(string texto)
    {
        panelPensamiento.SetActive(true);
        textoPensamiento.text = texto;
        yield return new WaitForSeconds(duracionPensamiento);
        panelPensamiento.SetActive(false);
    }

    // Conectar a DemoControladorEstadisticas.eventos.alCambiarSalud
    public void ActualizarSalud(float salud)
    {
        // ControladorBarraSalud.SetearSalud01 espera la salud en escala 0-100
        if(barraSalud != null)
            barraSalud.SetearSalud01(salud / saludMaxima * 100f);
        if(textoSalud != null)
            textoSalud.text = Mathf.CeilToInt(salud).ToString();
    }

    // Conectar a ControladorJuego.eventos.alCambiarVidas
    public void ActualizarVidas(int vidas)
    {
        if(textoVidas != null)
            textoVidas.text = "Vidas: " + vidas;
    }

    // Conectar a ControladorDinero.eventos.alCambiarDinero
    public void ActualizarDinero(int dinero)
    {
        // Formato chileno: punto como separador de miles ($2.000)
        if(textoDinero != null)
            textoDinero.text = "$" + dinero.ToString("#,0").Replace(",", ".");
    }

    // Conectar a ControladorMision.eventos.alCambiarObjetivo
    public void ActualizarMision(string objetivo)
    {
        if(textoMision != null)
            textoMision.text = objetivo;
        if(panelMision != null && panelMision.gameObject.activeInHierarchy)
            StartCoroutine(_Latido());
        if(sonidoMision != null && Time.timeSinceLevelLoad > 1f) sonidoMision.Play();
    }

    // El panel de mision crece y vuelve (para que se note el objetivo nuevo)
    System.Collections.IEnumerator _Latido()
    {
        for(float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            float s = 1f + 0.12f * Mathf.Sin(t / 0.5f * Mathf.PI);
            panelMision.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        panelMision.localScale = Vector3.one;
    }

    // Titulo grande al centro (ej: "EL PASTERO" al presentar a Julio)
    public void MostrarTitulo(string titulo)
    {
        if(textoTitulo == null) return;
        StartCoroutine(_Titulo(titulo));
    }

    System.Collections.IEnumerator _Titulo(string titulo)
    {
        textoTitulo.text = titulo;
        textoTitulo.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(2.5f);
        textoTitulo.gameObject.SetActive(false);
    }

    // Conectar a DemoControlInteraccion.eventos.alEntrarDeteccion (con el texto como parametro)
    public void MostrarPrompt(string texto)
    {
        if(panelPrompt != null) panelPrompt.SetActive(true);
        if(textoPrompt != null) textoPrompt.text = texto;
    }

    // Conectar a DemoControlInteraccion.eventos.alSalirDeteccion
    public void OcultarPrompt()
    {
        if(panelPrompt != null) panelPrompt.SetActive(false);
    }
}
