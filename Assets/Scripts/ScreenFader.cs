using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Tela preta em tela cheia para transicoes (usa tempo real, funciona com o jogo pausado).</summary>
public class ScreenFader : MonoBehaviour
{
    private Image image;
    private Canvas canvas;

    private void Awake()
    {
        canvas = UiFactory.CreateCanvas("Screen Fader", 200);

        GameObject fade = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fade.transform.SetParent(canvas.transform, false);
        RectTransform rect = fade.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        image = fade.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;
        image.enabled = false;
    }

    private void OnDestroy()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    public float Alpha => image != null ? image.color.a : 0f;

    public IEnumerator FadeTo(float targetAlpha, float seconds)
    {
        float startAlpha = Alpha;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / seconds));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
        // Transparente nao desenha nada: evita um quadro de tela cheia a cada frame.
        image.enabled = alpha > 0.001f;
    }
}
