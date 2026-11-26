using UnityEngine;
using UnityEngine.Events;

// Guarda lo que dice un personaje. Puede tener varias conversaciones
// (ej: la primera vez que hablas con el viejo y despues de traerle el vino).
// Se conecta: DemoControlInteraccion.alInteractuar -> IniciarDialogo()
public class DialogoNPC : MonoBehaviour
{
    [Header("Personaje")]
    public string nombrePersonaje = "Viejo curado";

    [Header("Conversaciones")]
    public Conversacion[] conversaciones;
    [Tooltip("Cual conversacion se usa al hablarle")]
    public int conversacionActual = 0;

    [Header("Con caña (antes de que Benja se tome el agua)")]
    [Tooltip("Conversacion que se usa mientras Benja anda con caña (-1 = habla normal)")]
    public int conversacionConCana = -1;
    [Tooltip("Con caña, ¿se da vuelta para contestar? (Don Lucho no: sigue en lo suyo)")]
    public bool girarseConCana = true;
    public Resaca resaca;

    [Header("Referencias")]
    public ControladorDialogo controladorDialogo;

    [Header("Antes de la primera conversacion (ej: Don Lucho termina de mear)")]
    [Tooltip("Sonido que suena en loop mientras espera (ej: el chorro). Se corta al hablarle.")]
    public AudioSource sonidoEnEspera;
    [Tooltip("Sonidos que suenan uno tras otro antes de hablar por primera vez")]
    public AudioClip[] sonidosAntes;
    [Tooltip("Segundos de pausa despues de los sonidos (aqui se da vuelta)")]
    public float esperaAntes = 0.6f;

    private bool yaHablo = false;
    private bool preparando = false;
    private int conversacionEnCurso = 0; // la que se esta hablando ahora (puede ser la de caña)

    void Start()
    {
        if(controladorDialogo == null)
            controladorDialogo = FindFirstObjectByType<ControladorDialogo>();
    }

    public void IniciarDialogo()
    {
        if(conversaciones == null || conversaciones.Length == 0) return;
        if(preparando == true) return;
        // Con caña no se le entiende nada a Benja: el personaje contesta otra cosa
        // (y si estaba haciendo algo, como Don Lucho meando, ni se da vuelta)
        if(conversacionConCana >= 0 && resaca != null && resaca.conCana)
        {
            conversacionEnCurso = conversacionConCana;
            controladorDialogo.ComenzarDialogo(this);
            if(girarseConCana)
                SendMessage("EmpezarConversacion", SendMessageOptions.DontRequireReceiver);
            return;
        }
        conversacionEnCurso = conversacionActual;
        if(yaHablo == false && sonidosAntes != null && sonidosAntes.Length > 0)
        {
            StartCoroutine(_PrepararPrimeraVez());
            return;
        }
        Hablar();
    }

    void Hablar()
    {
        yaHablo = true;
        controladorDialogo.ComenzarDialogo(this);
        // Aviso a MirarJugador (si el personaje lo tiene) para que se de vuelta
        SendMessage("EmpezarConversacion", SendMessageOptions.DontRequireReceiver);
    }

    // Primera vez: se corta el sonido de espera, suenan los sonidos, se da vuelta y recien habla
    System.Collections.IEnumerator _PrepararPrimeraVez()
    {
        preparando = true;
        if(sonidoEnEspera != null) sonidoEnEspera.Stop();
        AudioSource fuente = GetComponent<AudioSource>();
        if(fuente == null) fuente = gameObject.AddComponent<AudioSource>();
        foreach(AudioClip clip in sonidosAntes)
        {
            if(clip == null) continue;
            fuente.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
        }
        // Se da vuelta (MirarJugador) y espera un poquito antes de hablar
        SendMessage("EmpezarConversacion", SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(esperaAntes);
        preparando = false;
        Hablar();
    }

    // Lo llama ControladorDialogo cuando se lee la ultima linea
    public void TerminoDialogo()
    {
        SendMessage("TerminarConversacion", SendMessageOptions.DontRequireReceiver);
        conversaciones[conversacionEnCurso].alTerminar.Invoke();
    }

    // Para cambiar lo que dice el personaje desde un UnityEvent (ej: al completar un objetivo)
    public void CambiarConversacion(int indice)
    {
        if(indice < 0 || indice >= conversaciones.Length) return;
        conversacionActual = indice;
    }

    // Cambia segun en cual conversacion esta. Ej: "0>5,1>2" = si esta en la 0 pasa a la 5;
    // si esta en la 1 pasa a la 2 (sirve para saltarse pasos si Benja ya trae lo que le piden)
    public void CambiarSegun(string reglas)
    {
        foreach(string regla in reglas.Split(','))
        {
            string[] partes = regla.Split('>');
            if(partes.Length == 2 && int.Parse(partes[0]) == conversacionActual)
            {
                CambiarConversacion(int.Parse(partes[1]));
                return;
            }
        }
    }

    public Conversacion ConversacionActual()
    {
        return conversaciones[conversacionEnCurso];
    }

    // DEFINICION DE DATOS
    [System.Serializable]
    public class Conversacion
    {
        [TextArea(2, 4)]
        public string[] lineas;
        public UnityEvent alTerminar = new UnityEvent();
    }
}
