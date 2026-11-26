using System.Collections;
using UnityEngine;

// Reja con puas: si la tocas te hace daño y te empuja hacia atras. No se puede escalar
// (su collider va en la capa "Limite", que AgarreBorde ignora).
// Este script va en un trigger un poco mas grande que la reja.
public class RejaPuas : MonoBehaviour
{
    public float danio = 15f;
    public float fuerzaEmpuje = 6f;
    public float duracionEmpuje = 0.25f;
    public string frase = "¡Auch! Con púas no, po.";

    private float proximo = 0f;
    private ControladorHUD hud;

    void Start()
    {
        hud = FindFirstObjectByType<ControladorHUD>();
    }

    void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Player") == false || Time.time < proximo) return;
        proximo = Time.time + 1f;

        DemoControladorEstadisticas stats = other.GetComponent<DemoControladorEstadisticas>();
        if(stats != null) stats.CalcularDanio(danio);
        if(hud != null) hud.MostrarPensamiento(frase);

        // Empuja hacia el lado de la reja donde esta el jugador
        Vector3 hacia = other.transform.position - transform.position;
        float lado = Mathf.Sign(Vector3.Dot(hacia, transform.forward));
        StartCoroutine(_Empujar(other.GetComponent<CharacterController>(), transform.forward * lado));
    }

    IEnumerator _Empujar(CharacterController cc, Vector3 direccion)
    {
        if(cc == null) yield break;
        for(float t = 0f; t < duracionEmpuje; t += Time.deltaTime)
        {
            if(cc.enabled) cc.Move((direccion * fuerzaEmpuje + Vector3.up * 2f) * Time.deltaTime);
            yield return null;
        }
    }
}
