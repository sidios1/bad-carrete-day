using System.Collections;
using UnityEngine;

// Hace parpadear una luz (el farol "medio malo" del pasaje oscuro).
public class ParpadeoLuz : MonoBehaviour
{
    public Light luz;
    [Tooltip("Segundos entre parpadeos (al azar entre estos dos)")]
    public float esperaMinima = 1.5f;
    public float esperaMaxima = 5f;

    private float intensidadNormal;

    void Start()
    {
        if(luz == null) luz = GetComponent<Light>();
        intensidadNormal = luz.intensity;
        StartCoroutine(_Parpadear());
    }

    IEnumerator _Parpadear()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(esperaMinima, esperaMaxima));

            // 2 a 4 parpadeos rapidos
            int veces = Random.Range(2, 5);
            for(int i = 0; i < veces; i++)
            {
                luz.intensity = intensidadNormal * 0.1f;
                yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
                luz.intensity = intensidadNormal;
                yield return new WaitForSeconds(Random.Range(0.04f, 0.15f));
            }
        }
    }
}
