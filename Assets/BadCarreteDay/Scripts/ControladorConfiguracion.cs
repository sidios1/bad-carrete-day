using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menu de Configuracion: volumen general y sensibilidad del mouse.
// Los valores se guardan en PlayerPrefs (quedan para la proxima vez que se juega).
public class ControladorConfiguracion : MonoBehaviour
{
    const string claveVolumen = "volumen";
    const string claveSensibilidad = "sensibilidad";

    [Header("Referencias UI")]
    public Slider sliderVolumen;
    public Slider sliderSensibilidad;
    public TMP_Text textoVolumen;
    public TMP_Text textoSensibilidad;

    // Se aplica solo al iniciar el juego (antes de cargar la primera escena)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AplicarGuardado()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(claveVolumen, 0.8f);
        StarterAssetsInputs.sensibilidad = PlayerPrefs.GetFloat(claveSensibilidad, 1f);
    }

    // Cada vez que se abre el panel, los sliders muestran los valores actuales
    void OnEnable()
    {
        if(sliderVolumen != null) sliderVolumen.SetValueWithoutNotify(AudioListener.volume);
        if(sliderSensibilidad != null) sliderSensibilidad.SetValueWithoutNotify(StarterAssetsInputs.sensibilidad);
        ActualizarTextos();
    }

    // Conectar a sliderVolumen.onValueChanged (0 a 1)
    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(claveVolumen, valor);
        PlayerPrefs.Save();
        ActualizarTextos();
    }

    // Conectar a sliderSensibilidad.onValueChanged (0.2 a 3)
    public void CambiarSensibilidad(float valor)
    {
        StarterAssetsInputs.sensibilidad = valor;
        PlayerPrefs.SetFloat(claveSensibilidad, valor);
        PlayerPrefs.Save();
        ActualizarTextos();
    }

    private void ActualizarTextos()
    {
        if(textoVolumen != null) textoVolumen.text = "Volumen: " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
        if(textoSensibilidad != null) textoSensibilidad.text = "Sensibilidad del mouse: " + StarterAssetsInputs.sensibilidad.ToString("F1") + "x";
    }
}
