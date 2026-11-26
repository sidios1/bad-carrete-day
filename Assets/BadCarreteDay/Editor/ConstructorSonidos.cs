using System.IO;
using UnityEditor;
using UnityEngine;

// Herramienta de EDITOR — Genera por codigo los sonidos originales del juego (.wav):
//  - Murmullos de dialogo por personaje (silabas con vocales: estilo Conker / Animal Crossing)
//  - Pitido de censura, cierre de pantalon, chorro (loop) y fin del chorro
//  - Gruñido de Toby y sonidos de la tienda (mover, comprar, error) y de mision nueva
// Sin descargas ni licencias: todo es sintesis simple (ondas + ruido).
public static class ConstructorSonidos
{
    public const string carpeta = "Assets/BadCarreteDay/Arte/Audio/Generados/";
    const int frecuencia = 22050;

    // Voces: tono base (Hz), velocidad de silaba (s), aspereza (ruido 0..1)
    public static readonly string[] voces = { "Benja", "DonLucho", "Marta", "Julio", "Cuma" };
    static readonly float[] tonos = { 175f, 112f, 255f, 138f, 160f };
    static readonly float[] largos = { 0.085f, 0.11f, 0.07f, 0.09f, 0.08f };
    static readonly float[] asperezas = { 0.05f, 0.18f, 0.03f, 0.22f, 0.12f };

    [MenuItem("Bad Carrete Day/Generar sonidos")]
    public static void Generar()
    {
        Directory.CreateDirectory(carpeta);
        Random.InitState(2026);
        for (int v = 0; v < voces.Length; v++)
            for (int i = 0; i < 6; i++)
                Guardar("Voz_" + voces[v] + "_" + i, Silaba(tonos[v], largos[v], asperezas[v], i));
        Guardar("Censura", Pitido(1000f, 0.32f));
        Guardar("Cierre", Cierre());
        Guardar("Chorro", Chorro(2f, false));
        Guardar("FinChorro", Chorro(1.3f, true));
        Guardar("Grunido", Grunido());
        Guardar("Tienda_Mover", Pitido(1250f, 0.04f));
        Guardar("Tienda_Comprar", DosTonos(880f, 1320f));
        Guardar("Tienda_Error", Zumbido());
        Guardar("Mision_Nueva", DosTonos(660f, 990f));
        Guardar("Basurero", SonidoBasurero());
        Guardar("Julio_Respiracion", Respiracion());
        Guardar("Julio_Quejido", Quejido());
        Guardar("Julio_Rugido", Rugido());
        AssetDatabase.Refresh();
        Debug.Log("[Sonidos] generados en " + carpeta);
    }

    public static AudioClip Clip(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(carpeta + nombre + ".wav");
    }

    public static AudioClip[] Silabas(string voz)
    {
        AudioClip[] c = new AudioClip[6];
        for (int i = 0; i < 6; i++) c[i] = Clip("Voz_" + voz + "_" + i);
        return c;
    }

    // ---------- Sintesis ----------

    // Silaba: pulso glotal con armonicos filtrados por dos formantes de una vocal (a, e, i, o, u)
    static float[] Silaba(float f0, float largo, float aspereza, int vocal)
    {
        float[,] formantes = { { 750f, 1200f }, { 500f, 1800f }, { 320f, 2300f }, { 520f, 900f }, { 350f, 750f }, { 650f, 1500f } };
        float f1 = formantes[vocal % 6, 0], f2 = formantes[vocal % 6, 1];
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        float glide = Random.Range(-0.12f, 0.12f);
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            float f = f0 * (1f + glide * t / largo);
            float valor = 0f;
            for (int h = 1; h * f < 4000f; h++)
            {
                float fh = h * f;
                // resonancia de los dos formantes
                float peso = Mathf.Exp(-Mathf.Pow((fh - f1) / 180f, 2f)) + 0.6f * Mathf.Exp(-Mathf.Pow((fh - f2) / 260f, 2f)) + 0.05f / h;
                valor += peso * Mathf.Sin(2f * Mathf.PI * fh * t);
            }
            valor += aspereza * (Random.value * 2f - 1f);
            float env = Mathf.Min(1f, t / 0.012f) * Mathf.Pow(1f - t / largo, 1.5f);
            s[k] = valor * env * 0.35f;
        }
        return Normalizar(s, 0.8f);
    }

    static float[] Pitido(float f, float largo)
    {
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            float env = Mathf.Min(1f, t / 0.005f) * Mathf.Min(1f, (largo - t) / 0.01f);
            s[k] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.6f;
        }
        return s;
    }

    static float[] DosTonos(float a, float b)
    {
        float[] x = Pitido(a, 0.08f), y = Pitido(b, 0.14f);
        float[] s = new float[x.Length + y.Length];
        x.CopyTo(s, 0); y.CopyTo(s, x.Length);
        return s;
    }

    static float[] Zumbido()
    {
        int n = (int)(0.22f * frecuencia);
        float[] s = new float[n];
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            s[k] = (Mathf.Repeat(t * 140f, 1f) * 2f - 1f) * 0.35f * Mathf.Min(1f, (0.22f - t) / 0.03f);
        }
        return s;
    }

    // Cierre del pantalon: rafaga de "clics" cada vez mas rapidos
    static float[] Cierre()
    {
        float largo = 0.55f;
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        float t = 0.02f;
        while (t < largo - 0.01f)
        {
            int inicio = (int)(t * frecuencia);
            for (int k = 0; k < 60 && inicio + k < n; k++)
                s[inicio + k] += (Random.value * 2f - 1f) * Mathf.Exp(-k / 12f) * 0.8f;
            t += Mathf.Lerp(0.022f, 0.009f, t / largo);
        }
        // un poco mas agudo: diferencia entre muestras (filtro pasa-altos)
        for (int k = n - 1; k > 0; k--) s[k] = s[k] - 0.7f * s[k - 1];
        return Normalizar(s, 0.7f);
    }

    // Chorro: ruido filtrado con burbujeo. "termina" = se va cortando con goteos al final
    static float[] Chorro(float largo, bool termina)
    {
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        float filtrado = 0f;
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            filtrado += 0.25f * ((Random.value * 2f - 1f) - filtrado);
            float burbujeo = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 7f * t + Mathf.Sin(t * 13f));
            float env = 1f;
            if (termina) env = Mathf.Clamp01(1f - t / (largo * 0.7f));
            else env = Mathf.Min(1f, Mathf.Min(t, largo - t) / 0.05f); // loop sin clic
            s[k] = filtrado * burbujeo * env;
        }
        if (termina) // tres goteos
            foreach (float g in new[] { 0.95f, 1.08f, 1.22f })
            {
                int inicio = (int)(g * frecuencia);
                for (int k = 0; k < 900 && inicio + k < n; k++)
                    s[inicio + k] += Mathf.Sin(2f * Mathf.PI * (900f + k * 0.8f) * k / frecuencia) * Mathf.Exp(-k / 220f) * 0.5f;
            }
        return Normalizar(s, 0.6f);
    }

    // Basurero: golpe de tapa metalica (tono que resuena) + bolsas que crujen
    static float[] SonidoBasurero()
    {
        float largo = 0.8f;
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            // tapa: dos parciales metalicos que se apagan rapido
            float tapa = (Mathf.Sin(2f * Mathf.PI * 410f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 1130f * t)) * Mathf.Exp(-t * 9f);
            // bolsas: ruido a rafagas despues del golpe
            float bolsas = t > 0.12f ? (Random.value * 2f - 1f) * 0.35f * Mathf.Abs(Mathf.Sin(t * 37f)) * Mathf.Exp(-(t - 0.12f) * 4f) : 0f;
            s[k] = tapa + bolsas;
        }
        return Normalizar(s, 0.7f);
    }

    // Respiracion agitada (loop): inspirar/espirar rapido, ruido filtrado
    static float[] Respiracion()
    {
        float largo = 2.4f;
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        float filtrado = 0f;
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            filtrado += 0.18f * ((Random.value * 2f - 1f) - filtrado);
            float ciclo = Mathf.Repeat(t / 0.6f, 1f);           // 4 respiraciones en 2,4 s
            float env = Mathf.Pow(Mathf.Sin(ciclo * Mathf.PI), 2f) * (ciclo < 0.5f ? 0.7f : 1f);
            s[k] = filtrado * env;
        }
        return Normalizar(s, 0.45f);
    }

    // Quejido grave (le pegaron en el cuchillo)
    static float[] Quejido()
    {
        float largo = 0.75f;
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        float fase = 0f, filtrado = 0f;
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            float f = Mathf.Lerp(150f, 95f, t / largo) * (1f + 0.03f * Mathf.Sin(t * 40f));
            fase += f / frecuencia;
            float sierra = Mathf.Repeat(fase, 1f) * 2f - 1f;
            filtrado += 0.12f * (sierra - filtrado);
            float env = Mathf.Min(1f, t / 0.04f) * Mathf.Min(1f, (largo - t) / 0.2f);
            s[k] = (filtrado + (Random.value * 2f - 1f) * 0.05f) * env;
        }
        return Normalizar(s, 0.8f);
    }

    // Rugido: grave, aspero, que crece y se corta
    static float[] Rugido()
    {
        float largo = 1.3f;
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        float fase = 0f, filtrado = 0f, ruido = 0f;
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            fase += Mathf.Lerp(75f, 95f, t / largo) / frecuencia;
            float sierra = Mathf.Repeat(fase, 1f) * 2f - 1f;
            filtrado += 0.2f * (sierra - filtrado);
            ruido += 0.15f * ((Random.value * 2f - 1f) - ruido);
            float aspereza = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 32f * t);
            float env = Mathf.Min(1f, t / 0.25f) * Mathf.Min(1f, (largo - t) / 0.25f);
            s[k] = (filtrado * 0.8f + ruido * 1.6f) * aspereza * env;
        }
        return Normalizar(s, 0.9f);
    }

    // Gruñido: ruido grave con temblor
    static float[] Grunido()
    {
        float largo = 0.9f;
        int n = (int)(largo * frecuencia);
        float[] s = new float[n];
        float filtrado = 0f;
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)frecuencia;
            filtrado += 0.06f * ((Random.value * 2f - 1f) - filtrado);
            float temblor = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 28f * t);
            float tono = Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.4f;
            float env = Mathf.Min(1f, t / 0.08f) * Mathf.Min(1f, (largo - t) / 0.15f);
            s[k] = (filtrado * 3f + tono) * temblor * env;
        }
        return Normalizar(s, 0.8f);
    }

    static float[] Normalizar(float[] s, float pico)
    {
        float max = 0.0001f;
        foreach (float v in s) max = Mathf.Max(max, Mathf.Abs(v));
        for (int k = 0; k < s.Length; k++) s[k] = s[k] / max * pico;
        return s;
    }

    // WAV PCM 16 bits mono
    static void Guardar(string nombre, float[] muestras)
    {
        using (FileStream f = new FileStream(carpeta + nombre + ".wav", FileMode.Create))
        using (BinaryWriter w = new BinaryWriter(f))
        {
            int datos = muestras.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + datos);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(frecuencia); w.Write(frecuencia * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(datos);
            foreach (float v in muestras) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32000f));
        }
    }
}
