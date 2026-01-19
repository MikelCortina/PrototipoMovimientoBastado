using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform cameraTransform;
    public float launchForce = 50f;
    public float fireRate = 0.15f;

    private float nextTimeToFire = 0f;

    // Usamos LateUpdate para que la cámara ya se haya movido antes de disparar
    void LateUpdate()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (cameraTransform == null) return;

        Vector3 forwardDirection = cameraTransform.forward;

        // Aumentamos ligeramente el offset para evitar colisiones con el cuerpo
        Vector3 spawnPosition = cameraTransform.position + forwardDirection * 1.2f;

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, cameraTransform.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Limpiamos cualquier velocidad previa y aplicamos la nueva
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(forwardDirection * launchForce, ForceMode.Impulse);
        }
    }
}