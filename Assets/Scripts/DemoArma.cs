using UnityEngine;

public class DemoArma : DemoItem
{
    [Header("Estadisticas arma")]
    public Tipo tipo = Tipo.espada_corta;
    
    public float danio = 10f;
    public bool danioActivo = false;
    // Objetivo del golpe del arma
    public DemoObjetivo objetivo;


    // [Bad Carrete Day] objetivos ya golpeados en ESTE golpe (cada uno recibe un golpe por ataque,
    // pero el palo le pega a TODOS los que esten en la zona, no solo al primero)
    private System.Collections.Generic.HashSet<DemoObjetivo> golpeadosEsteGolpe = new System.Collections.Generic.HashSet<DemoObjetivo>();

    void OnTriggerEnter(Collider other)
    {
        // Si no esta activo el daño del arma, termino la deteccion
        if(danioActivo == false) return;
        Golpear(other);
    }

    // [Bad Carrete Day] si el enemigo ya estaba pegado al arma cuando empieza el golpe,
    // OnTriggerEnter no se vuelve a llamar: lo detectamos aqui
    void OnTriggerStay(Collider other)
    {
        if(danioActivo == true) Golpear(other);
    }

    // [Bad Carrete Day] el golpe en si
    void Golpear(Collider other)
    {
        // Solo cuerpos solidos (no otras zonas de golpe: si no, un puño que toca la zona
        // del palo contaria como golpe al que tiene el palo)
        if(other.isTrigger) return;
        // Si el elemento que golpee tiene un componente de Objetivo..
        DemoObjetivo golpeado = other.GetComponentInParent<DemoObjetivo>();
        if(golpeado == null) return;
        // no golpear a quien lleva el arma (mismo objeto raiz)
        if(golpeado.transform.root == this.transform.root) return;
        // un golpe por ataque a cada objetivo
        if(golpeadosEsteGolpe.Contains(golpeado)) return;
        golpeadosEsteGolpe.Add(golpeado);

        objetivo = golpeado;
        // primero el daño y DESPUES el golpe (el setter invoca el evento con danioRecibido)
        objetivo.danioRecibido = danio;
        objetivo.esGolpeado = true;
        // y lo dejamos listo de inmediato: asi el proximo golpe (de esta u otra arma) tambien cuenta
        objetivo.esGolpeado = false;
    }

    public void SetDanioActivo(bool valor)
    {
        danioActivo = valor;

        // [Bad Carrete Day] cada ataque parte de cero
        golpeadosEsteGolpe.Clear();
        objetivo = null;

        // [Bad Carrete Day] al empezar el golpe revisamos a los que YA estaban dentro de la zona
        if(valor == true)
        {
            Collider zona = GetComponent<Collider>();
            if(zona != null && zona.enabled && gameObject.activeInHierarchy)
                foreach(Collider c in Physics.OverlapBox(zona.bounds.center, zona.bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    Golpear(c);
        }
    }


    public enum Tipo{daga, espada_corta, espada_larga, lanza, mazo, hacha1Mano, hacha2Manos}
}
