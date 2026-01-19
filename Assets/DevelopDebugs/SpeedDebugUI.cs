using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ForwardSpeedUI : MonoBehaviour
{
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnGUI()
    {
        // Velocidad del rigidbody
        Vector3 velocity = rb.linearVelocity;

        // Dirección a la que mira el jugador (horizontal)
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        // Velocidad proyectada en la dirección forward
        float forwardSpeed = Vector3.Dot(velocity, forward);

        // Posición arriba a la derecha
        float width = 250f;
        float height = 25f;

        GUI.Label(
            new Rect(Screen.width - width - 10f, 10f, width, height),
            $"Forward Speed: {forwardSpeed:F2}"
        );
    }
}
