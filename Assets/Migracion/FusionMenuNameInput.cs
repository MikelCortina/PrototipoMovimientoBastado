using TMPro;
using UnityEngine;

public class FusionMenuNameInput : MonoBehaviour
{
    public TMP_InputField nameInput;

    private void Start()
    {
        if (nameInput != null && FusionPlayerNameStore.Instance != null)
        {
            nameInput.text = FusionPlayerNameStore.Instance.CurrentPlayerName;
        }
    }

    public void SavePlayerName()
    {
        if (nameInput == null || FusionPlayerNameStore.Instance == null)
            return;

        FusionPlayerNameStore.Instance.SetPlayerName(nameInput.text);
    }
}