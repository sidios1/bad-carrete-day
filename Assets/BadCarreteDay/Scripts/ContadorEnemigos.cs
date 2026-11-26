using UnityEngine;
using UnityEngine.Events;

// Cuenta enemigos derrotados de un grupo (ej: los cumas del pasaje oscuro)
// y avisa cuando no queda ninguno (para abrir la reja y completar el objetivo).
public class ContadorEnemigos : MonoBehaviour
{
    public int totalEnemigos = 3;

    // PROPIEDAD: al llegar al total, avisa una sola vez
    [SerializeField]
    private int _derrotados = 0;
    public int derrotados
    {
        get { return _derrotados; }
        set
        {
            if(value != _derrotados)
            {
                _derrotados = value;
                if(_derrotados == totalEnemigos)
                    alTodosDerrotados.Invoke();
            }
        }
    }

    [Header("Configuracion Eventos")]
    public UnityEvent alTodosDerrotados;

    // Conectar a ControladorEnemigo.alDerrotado de cada enemigo del grupo
    public void EnemigoDerrotado()
    {
        derrotados++;
    }
}
