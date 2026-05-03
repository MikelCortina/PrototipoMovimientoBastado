using System.Collections;
using TMPro;
using UnityEngine;

public class FusionStreakMessageUI : MonoBehaviour
{
    public static FusionStreakMessageUI Instance { get; private set; }

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 3f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (messageText == null)
        {
            GameObject found = GameObject.Find("StreakMessageText");
            if (found != null)
                messageText = found.GetComponent<TMP_Text>();
        }

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        if (messageText == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageDuration);

        messageText.gameObject.SetActive(false);
        currentRoutine = null;
    }
}