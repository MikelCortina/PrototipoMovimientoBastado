using System.Collections;
using TMPro;
using UnityEngine;

public class FusionKillFeedUI : MonoBehaviour
{
    public static FusionKillFeedUI Instance { get; private set; }

    [SerializeField] private TMP_Text killFeedText;
    [SerializeField] private float messageDuration = 3f;

    private Coroutine currentCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (killFeedText == null)
        {
            GameObject textObject = GameObject.Find("KillFeedText");
            if (textObject != null)
                killFeedText = textObject.GetComponent<TMP_Text>();
        }

        if (killFeedText == null)
        {
            killFeedText = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        }

        if (killFeedText != null)
            killFeedText.gameObject.SetActive(false);
        else
            Debug.LogWarning("FusionKillFeedUI no encontró ningún TMP_Text para el kill feed.");
    }

    public void ShowKill(string killerName, string victimName, FusionKillCause cause)
    {
        if (killFeedText == null)
            return;

        string causeText = CauseToText(cause);
        killFeedText.text = $"{killerName} eliminó a {victimName} [{causeText}]";

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        killFeedText.gameObject.SetActive(true);
        yield return new WaitForSeconds(messageDuration);
        killFeedText.gameObject.SetActive(false);
        currentCoroutine = null;
    }

    private string CauseToText(FusionKillCause cause)
    {
        switch (cause)
        {
            case FusionKillCause.Bullet: return "Arma";
            case FusionKillCause.Grenade: return "Granada";
            case FusionKillCause.AirStrike: return "Ataque aéreo";
            case FusionKillCause.Turret: return "Torreta";
            default: return "Desconocida";
        }
    }
}