using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cutscenes simples em uGUI: imagem em tela cheia, texto letra a letra e, no final, creditos subindo.
/// Usa tempo real (funciona com o jogo pausado). Enter/Espaco/clique avancam; Esc/Start pulam tudo.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    private const float CharactersPerSecond = 38f;
    private const float FadeSeconds = 0.35f;
    private const float AutoAdvanceSeconds = 5f;
    private const float CreditsSpeed = 55f;
    private const float ReferenceHeight = 720f;

    private Canvas canvas;
    private Image picture;
    private AspectRatioFitter pictureFitter;
    private Text body;
    private Text credits;
    private Image blackout;
    private bool skipRequested;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        Build();
        canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    public void Play(IList<CutsceneSlide> slides, string creditsText, Action onFinished)
    {
        StopAllCoroutines();
        StartCoroutine(Run(slides, creditsText, onFinished));
    }

    private IEnumerator Run(IList<CutsceneSlide> slides, string creditsText, Action onFinished)
    {
        IsPlaying = true;
        skipRequested = false;
        canvas.gameObject.SetActive(true);
        credits.gameObject.SetActive(false);
        picture.gameObject.SetActive(true);
        body.text = string.Empty;
        SetBlackout(1f);

        // Ignora a tecla que abriu a cutscene.
        yield return null;

        for (int i = 0; i < slides.Count && !skipRequested; i++)
        {
            yield return ShowSlide(slides[i]);
        }

        if (!skipRequested && !string.IsNullOrEmpty(creditsText))
        {
            yield return RollCredits(creditsText);
        }

        SetBlackout(1f);
        canvas.gameObject.SetActive(false);
        IsPlaying = false;
        onFinished?.Invoke();
    }

    private IEnumerator ShowSlide(CutsceneSlide slide)
    {
        picture.sprite = slide.Image;
        picture.enabled = slide.Image != null;
        if (slide.Image != null)
        {
            pictureFitter.aspectRatio = slide.Image.rect.width / slide.Image.rect.height;
        }

        body.text = string.Empty;
        yield return Fade(1f, 0f);

        float elapsed = 0f;
        int shown = -1;
        int length = slide.Text.Length;
        while (!skipRequested)
        {
            if (SkipPressed())
            {
                skipRequested = true;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            int visible = CutsceneTiming.VisibleCharacters(elapsed, CharactersPerSecond, length);
            if (AdvancePressed())
            {
                visible = length;
            }

            if (visible != shown)
            {
                shown = visible;
                body.text = slide.Text.Substring(0, visible);
            }

            if (visible >= length)
            {
                break;
            }

            yield return null;
        }

        yield return null;
        float waited = 0f;
        while (!skipRequested && waited < AutoAdvanceSeconds)
        {
            if (SkipPressed())
            {
                skipRequested = true;
                yield break;
            }

            if (AdvancePressed())
            {
                break;
            }

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return Fade(0f, 1f);
    }

    private IEnumerator RollCredits(string text)
    {
        picture.gameObject.SetActive(false);
        body.text = string.Empty;
        credits.text = text;
        credits.gameObject.SetActive(true);
        SetBlackout(0f);

        RectTransform rect = credits.rectTransform;
        float height = credits.preferredHeight;
        float y = -ReferenceHeight * 0.5f;
        float end = ReferenceHeight * 0.5f + height;
        while (y < end)
        {
            if (SkipPressed() || AdvancePressed())
            {
                break;
            }

            y += CreditsSpeed * Time.unscaledDeltaTime;
            rect.anchoredPosition = new Vector2(0f, y);
            yield return null;
        }

        credits.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < FadeSeconds)
        {
            if (SkipPressed())
            {
                skipRequested = true;
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            SetBlackout(Mathf.Lerp(from, to, elapsed / FadeSeconds));
            yield return null;
        }

        SetBlackout(to);
    }

    private void SetBlackout(float alpha)
    {
        Color color = blackout.color;
        color.a = alpha;
        blackout.color = color;
    }

    private static bool AdvancePressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        Mouse mouse = Mouse.current;
        return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame))
            || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    private static bool SkipPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.startButton.wasPressedThisFrame);
    }

    private void Build()
    {
        canvas = UiFactory.CreateCanvas("Cutscene", 150);
        Transform root = canvas.transform;

        GameObject backdrop = CreateStretched(root, "Backdrop");
        Image backdropImage = backdrop.AddComponent<Image>();
        backdropImage.color = Color.black;

        // A imagem cobre a tela inteira mantendo a proporcao (corta as sobras).
        GameObject pictureObject = CreateStretched(root, "Picture");
        picture = pictureObject.AddComponent<Image>();
        picture.raycastTarget = false;
        pictureFitter = pictureObject.AddComponent<AspectRatioFitter>();
        pictureFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        GameObject band = new GameObject("Text Band", typeof(RectTransform), typeof(Image));
        band.transform.SetParent(root, false);
        RectTransform bandRect = band.GetComponent<RectTransform>();
        bandRect.anchorMin = new Vector2(0f, 0f);
        bandRect.anchorMax = new Vector2(1f, 0f);
        bandRect.pivot = new Vector2(0.5f, 0f);
        bandRect.sizeDelta = new Vector2(0f, 190f);
        Image bandImage = band.GetComponent<Image>();
        bandImage.color = new Color(0f, 0f, 0f, 0.72f);
        bandImage.raycastTarget = false;

        body = CreateText(band.transform, 30, TextAnchor.MiddleCenter, Color.white);
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(120f, 40f);
        bodyRect.offsetMax = new Vector2(-120f, -20f);

        Text hint = CreateText(band.transform, 18, TextAnchor.LowerRight, UiFactory.MutedColor);
        hint.text = "Enter: avançar    Esc: pular";
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = Vector2.zero;
        hintRect.anchorMax = Vector2.one;
        hintRect.offsetMin = new Vector2(20f, 10f);
        hintRect.offsetMax = new Vector2(-24f, -10f);

        credits = CreateText(root, 30, TextAnchor.UpperCenter, Color.white);
        RectTransform creditsRect = credits.rectTransform;
        creditsRect.anchorMin = new Vector2(0.5f, 0.5f);
        creditsRect.anchorMax = new Vector2(0.5f, 0.5f);
        creditsRect.pivot = new Vector2(0.5f, 1f);
        creditsRect.sizeDelta = new Vector2(1000f, 1600f);
        credits.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject blackoutObject = CreateStretched(root, "Blackout");
        blackout = blackoutObject.AddComponent<Image>();
        blackout.color = Color.black;
        blackout.raycastTarget = false;
    }

    private static GameObject CreateStretched(Transform parent, string name)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return child;
    }

    private static Text CreateText(Transform parent, int size, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = UiFactory.DefaultFont;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
