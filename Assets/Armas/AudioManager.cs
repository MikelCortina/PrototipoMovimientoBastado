using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource localFeedbackSource; // 2D (Spatial Blend = 0)
    public AudioSource worldImpactSource;  // 3D (Spatial Blend = 1)

    [Header("Hit Sounds (Local 2D)")]
    public AudioClip bodyHitLocal;
    public AudioClip headshotHitLocal;

    [Header("World Impacts (World 3D)")]
    public AudioClip fleshImpactWorld;

    private int lastFramePlayed = -1;
    private bool headshotPlayedThisFrame = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ESTA ES LA FUNCIÓN QUE BUSCA TU DAMAGEHANDLER
    public void PlayHitFeedback(Vector3 position, bool isHeadshot)
    {
        // Evitar saturación de sonido en el mismo frame (Priority System)
        if (Time.frameCount != lastFramePlayed)
        {
            lastFramePlayed = Time.frameCount;
            headshotPlayedThisFrame = false;
        }

        if (headshotPlayedThisFrame) return;

        // 1. Sonido LOCAL (El "Satisfyer" de Apex)
        AudioClip clip2D = isHeadshot ? headshotHitLocal : bodyHitLocal;
        if (localFeedbackSource != null && clip2D != null)
        {
            localFeedbackSource.PlayOneShot(clip2D);
        }

        // 2. Sonido MUNDO (Impacto físico)
        if (worldImpactSource != null && fleshImpactWorld != null)
        {
            worldImpactSource.transform.position = position;
            worldImpactSource.PlayOneShot(fleshImpactWorld);
        }

        if (isHeadshot) headshotPlayedThisFrame = true;
    }
}