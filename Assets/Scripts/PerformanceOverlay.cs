using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Medidor de desempenho (F3): FPS, tempo medio e pior quadro, coletas de lixo e escala de tempo.</summary>
public class PerformanceOverlay : MonoBehaviour
{
    private const float WindowSeconds = 0.5f;

    private bool visible;
    private float elapsed;
    private int frames;
    private float worstFrame;
    private int lastGcCount;
    private string text = string.Empty;
    private GUIStyle style;

    private void Start()
    {
        lastGcCount = GC.CollectionCount(0);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
        {
            visible = !visible;
        }

        float frameTime = Time.unscaledDeltaTime;
        frames++;
        elapsed += frameTime;
        worstFrame = Mathf.Max(worstFrame, frameTime);

        if (elapsed < WindowSeconds)
        {
            return;
        }

        int gcCount = GC.CollectionCount(0);
        float average = elapsed / frames;
        text = "FPS " + Mathf.RoundToInt(frames / elapsed)
            + "  |  medio " + (average * 1000f).ToString("0.0") + " ms"
            + "  |  pior " + (worstFrame * 1000f).ToString("0.0") + " ms"
            + "  |  GC +" + (gcCount - lastGcCount)
            + "  |  VSync " + QualitySettings.vSyncCount
            + "  |  tempo x" + Time.timeScale.ToString("0.00");

        lastGcCount = gcCount;
        elapsed = 0f;
        frames = 0;
        worstFrame = 0f;
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.yellow;
        }

        GUI.matrix = Matrix4x4.identity;
        GUI.Label(new Rect(10f, Screen.height - 32f, Screen.width - 20f, 28f), text, style);
    }
}
