using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public KeyCode resetKey = KeyCode.F;

    private float deltaTime;
    private float currentFPS;
    private float minFPS = float.MaxValue;

    void Update()
    {
        // Suavizado del deltaTime
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        currentFPS = 1f / deltaTime;

        if (currentFPS < minFPS)
            minFPS = currentFPS;

        // Resetear FPS mínimos
        if (Input.GetKeyDown(resetKey))
            minFPS = currentFPS;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 300, 30), $"FPS: {currentFPS:0}", style);
        GUI.Label(new Rect(10, 40, 300, 30), $"FPS MIN: {minFPS:0}", style);
        GUI.Label(new Rect(10, 70, 300, 30), $"Pulsa F para resetear", style);
    }
}
