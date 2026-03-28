using Fusion;
using UnityEngine;

public class FusionCameraHandler : NetworkBehaviour
{
    [Header("Ajustes de Ratón")]
    public float mouseSensitivity = 2f;
    public float maxPitch = 89f; // Límite para mirar arriba
    public float minPitch = -89f; // Límite para mirar abajo

    // Variables internas para acumular la rotación
    private float _yaw;   // Eje X (Izquierda / Derecha)
    private float _pitch; // Eje Y (Arriba / Abajo)

    public override void Spawned()
    {
        // Solo bloqueamos el ratón si este jugador es el nuestro
        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Inicializamos la rotación hacia donde esté mirando el spawn point
            _yaw = transform.eulerAngles.y;
            _pitch = 0f;
        }
    }

    void Update()
    {
        // Si somos un enemigo viéndonos en la pantalla de otro, no hacemos nada
        if (!HasInputAuthority) return;

        // Acumulamos el movimiento del ratón en cada frame visual (muy rápido)
        // Usamos GetAxisRaw porque queremos la señal cruda del ratón sin filtros raros de Unity
        _yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        _pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Evitamos que el jugador pueda dar volteretas con el cuello
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    // Esta es la función que llama tu LocalInputHandler para empaquetar los datos
    public Vector2 GetLookRotation()
    {
        // Devolvemos la X (Yaw) y la Y (Pitch)
        return new Vector2(_yaw, _pitch);
    }
}