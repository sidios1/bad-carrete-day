using TMPro;
using UnityEngine;

// Globo de texto (de comic) que sigue la cabeza de un personaje en la pantalla.
// Va en un Canvas de pantalla; cada frame se ubica sobre el "objetivo".
public class Burbuja : MonoBehaviour
{
    public TMP_Text texto;
    public TMP_Text nombre;
    [Tooltip("Indicador \"[E]\" (solo en conversaciones)")]
    public GameObject indicadorAvanzar;

    [HideInInspector] public Vector3 puntoCabeza;   // posicion en el mundo sobre la cabeza
    [HideInInspector] public Transform sigue;       // personaje al que sigue (se mueve con el)
    [HideInInspector] public float altura = 2.05f;

    private RectTransform rt;
    private RectTransform canvasRT;

    void Awake()
    {
        rt = (RectTransform)transform;
        canvasRT = (RectTransform)GetComponentInParent<Canvas>().transform;
    }

    public void Seguir(Transform quien, float alturaCabeza)
    {
        sigue = quien;
        altura = alturaCabeza;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if(cam == null || sigue == null) return;
        Vector3 mundo = sigue.position + Vector3.up * (altura + 0.25f);
        Vector3 pantalla = cam.WorldToScreenPoint(mundo);
        // Si el personaje queda detras de la camara, la burbuja se pega abajo
        if(pantalla.z < 0f) pantalla = new Vector3(Screen.width / 2f, Screen.height * 0.2f, 0f);

        Vector2 local;
        Canvas canvas = canvasRT.GetComponent<Canvas>();
        Camera camaraUI = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, pantalla, camaraUI, out local);
        // Que no se salga de la pantalla
        Vector2 medio = canvasRT.rect.size / 2f;
        float ancho = rt.rect.width / 2f + 20f;
        local.x = Mathf.Clamp(local.x, -medio.x + ancho, medio.x - ancho);
        local.y = Mathf.Clamp(local.y, -medio.y + 40f, medio.y - rt.rect.height - 140f);
        rt.anchoredPosition = local;
    }
}
