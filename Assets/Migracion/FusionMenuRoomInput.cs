using TMPro;
using UnityEngine;

public class FusionMenuRoomInput : MonoBehaviour
{
    public TMP_InputField roomNameInput;
    public string defaultRoomName = "Sala_1";

    public string GetRoomName()
    {
        if (roomNameInput == null)
            return defaultRoomName;

        string roomName = roomNameInput.text;

        if (string.IsNullOrWhiteSpace(roomName))
            roomName = defaultRoomName;

        return roomName.Trim();
    }
}