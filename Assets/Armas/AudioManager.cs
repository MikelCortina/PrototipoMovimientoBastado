using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Impact Sounds")]
    public AudioClip bodyHitSound;
    public AudioClip headshotHitSound;
    public AudioClip shieldBreakSound; // Extra para el futuro

    void Awake() { Instance = this; }

    public void PlayHitSound(Vector3 position, bool isHeadshot)
    {
        AudioClip clipToPlay = isHeadshot ? headshotHitSound : bodyHitSound;

        // Reproducir el sonido
        AudioSource.PlayClipAtPoint(clipToPlay, position, 1.0f);
    }
}