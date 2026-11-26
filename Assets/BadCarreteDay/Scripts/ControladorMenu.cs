using UnityEngine;
using UnityEngine.SceneManagement;

// Menu principal: Jugar (va a la Intro), Creditos y Salir.
// Usa ControladorCargaEscena (clase) para cargar con pantalla de carga.
public class ControladorMenu : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorCargaEscena cargaEscena;
    public GameObject panelCreditos;

    [Header("Escena al presionar Jugar")]
    public string rutaEscenaJugar = "Assets/BadCarreteDay/Escenas/Intro.unity";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if(panelCreditos != null) panelCreditos.SetActive(false);
    }

    public void Jugar()
    {
        int indice = SceneUtility.GetBuildIndexByScenePath(rutaEscenaJugar);
        if(indice < 0) indice = 1;
        cargaEscena.CargaEscena(indice);
    }

    public void MostrarCreditos(bool mostrar)
    {
        if(panelCreditos != null) panelCreditos.SetActive(mostrar);
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
