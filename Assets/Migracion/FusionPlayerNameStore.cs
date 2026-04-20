using UnityEngine;

public class FusionPlayerNameStore : MonoBehaviour
{
    public static FusionPlayerNameStore Instance { get; private set; }

    [SerializeField] private string defaultPlayerName = "Player";

    public string CurrentPlayerName { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentPlayerName = PlayerPrefs.GetString("FusionPlayerName", defaultPlayerName);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            newName = defaultPlayerName;

        CurrentPlayerName = newName.Trim();

        PlayerPrefs.SetString("FusionPlayerName", CurrentPlayerName);
        PlayerPrefs.Save();

        Debug.Log($"Nombre local guardado: {CurrentPlayerName}");
    }
}