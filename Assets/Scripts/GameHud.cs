using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD de jogo em uGUI (criado em codigo): vida, fase, zumbis restantes, pontuacao, combo, tempo e barra do chefe.</summary>
public class GameHud : MonoBehaviour
{
    private const float Margin = 20f;
    private const float LineHeight = 28f;
    private const float BarBorder = 2f;
    private const int FontSize = 20;

    private static readonly Color BarBackColor = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color HealthColor = new Color(0.85f, 0.15f, 0.15f, 1f);
    private static readonly Color BossColor = new Color(0.7f, 0.1f, 0.6f, 1f);

    private PlayerController player;
    private StageManager stage;
    private ScoreTracker score;
    private Canvas canvas;

    private RectTransform healthFill;
    private Text healthText;
    private Text stageText;
    private Text remainingText;
    private Text powerText;
    private Text scoreText;
    private Text timeText;
    private Text comboText;
    private Text hintText;
    private GameObject bossRoot;
    private RectTransform bossFill;

    // Textos so sao remontados quando o valor muda (evita lixo a cada frame).
    private int stageKey = -1;
    private int remainingKey = -1;
    private int scoreKey = -1;
    private int timeKey = -1;
    private int comboKey = -1;
    private int powerKey = -2;
    private int healthKey = -1;
    private string hintKey = string.Empty;

    public bool Visible
    {
        get => canvas != null && canvas.enabled;
        set
        {
            if (canvas != null)
            {
                canvas.enabled = value;
            }
        }
    }

    public void Configure(PlayerController playerController, StageManager stageManager, ScoreTracker scoreTracker)
    {
        player = playerController;
        stage = stageManager;
        score = scoreTracker;
        Build();
    }

    private void Update()
    {
        if (canvas == null || !canvas.enabled || player == null || stage == null || score == null)
        {
            return;
        }

        UpdateHealth();

        int stageValue = stage.StageIndex * 100 + stage.StageCount;
        if (Changed(ref stageKey, stageValue))
        {
            stageText.text = "Fase " + (stage.StageIndex + 1) + "/" + stage.StageCount;
        }

        if (Changed(ref remainingKey, stage.ZombiesRemaining))
        {
            remainingText.text = "Zumbis restantes: " + stage.ZombiesRemaining;
        }

        if (Changed(ref scoreKey, score.Score))
        {
            scoreText.text = "Pontos: " + score.Score;
        }

        int seconds = Mathf.FloorToInt(score.Elapsed);
        if (Changed(ref timeKey, seconds))
        {
            timeText.text = "Tempo: " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        comboText.enabled = score.Combo > 1;
        if (Changed(ref comboKey, score.Combo))
        {
            comboText.text = "Combo x" + score.Combo;
        }

        float remaining = player.PowerCooldownRemaining;
        int powerTenths = remaining > 0f ? Mathf.CeilToInt(remaining * 10f) : 0;
        if (Changed(ref powerKey, powerTenths))
        {
            powerText.text = powerTenths > 0 ? "Poder: " + (powerTenths / 10f).ToString("0.0") + "s" : "Poder: pronto";
        }

        string hint = stage.State == GameState.Playing && stage.StageCleared
            ? "Fase limpa! Va ate a borda direita da tela."
            : ControlsText.HudHint;
        if (hint != hintKey)
        {
            hintKey = hint;
            hintText.text = hint;
        }

        UpdateBoss();
    }

    private void UpdateHealth()
    {
        Health health = player.Health;
        float fraction = health.Max > 0 ? (float)health.Current / health.Max : 0f;
        healthFill.anchorMax = new Vector2(fraction, 1f);
        if (Changed(ref healthKey, health.Current * 1000 + health.Max))
        {
            healthText.text = health.Current + "/" + health.Max;
        }
    }

    private void UpdateBoss()
    {
        ZombieController boss = stage.ActiveBoss;
        bool show = boss != null && boss.gameObject.activeInHierarchy;
        if (bossRoot.activeSelf != show)
        {
            bossRoot.SetActive(show);
        }

        if (show)
        {
            float fraction = boss.Health.Max > 0 ? (float)boss.Health.Current / boss.Health.Max : 0f;
            bossFill.anchorMax = new Vector2(fraction, 1f);
        }
    }

    private static bool Changed(ref int key, int newKey)
    {
        if (newKey == key)
        {
            return false;
        }

        key = newKey;
        return true;
    }

    private void Build()
    {
        canvas = UiFactory.CreateCanvas("Game Hud", 50);
        canvas.transform.SetParent(transform, false);

        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 topRight = new Vector2(1f, 1f);
        Vector2 line = new Vector2(400f, LineHeight);

        healthFill = CreateBar(canvas.transform, "Health Bar", topLeft, new Vector2(Margin, -Margin), new Vector2(260f, 22f), HealthColor, out RectTransform healthBack);
        healthText = CreateText(healthBack, "Health Text", TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 22f), 16);
        StretchToParent(healthText.rectTransform);

        stageText = CreateText(canvas.transform, "Stage", TextAnchor.MiddleLeft, topLeft, new Vector2(Margin, -48f), line, FontSize);
        remainingText = CreateText(canvas.transform, "Remaining", TextAnchor.MiddleLeft, topLeft, new Vector2(Margin, -76f), line, FontSize);
        powerText = CreateText(canvas.transform, "Power", TextAnchor.MiddleLeft, topLeft, new Vector2(Margin, -104f), line, FontSize);

        scoreText = CreateText(canvas.transform, "Score", TextAnchor.MiddleRight, topRight, new Vector2(-Margin, -Margin), line, FontSize);
        timeText = CreateText(canvas.transform, "Time", TextAnchor.MiddleRight, topRight, new Vector2(-Margin, -48f), line, FontSize);
        comboText = CreateText(canvas.transform, "Combo", TextAnchor.MiddleRight, topRight, new Vector2(-Margin, -76f), line, FontSize);

        hintText = CreateText(canvas.transform, "Hint", TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(1000f, LineHeight), 18);

        Vector2 bossSize = new Vector2(420f, 20f);
        bossFill = CreateBar(canvas.transform, "Boss Bar", new Vector2(0.5f, 1f), new Vector2(0f, -56f), bossSize, BossColor, out RectTransform bossBack);
        bossRoot = bossBack.gameObject;
        Text bossLabel = CreateText(bossBack, "Boss Label", TextAnchor.LowerCenter, new Vector2(0.5f, 1f), new Vector2(0f, 2f), new Vector2(bossSize.x, 28f), 18);
        bossLabel.text = "CHEFE";
        bossLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
        bossRoot.SetActive(false);
    }

    /// <summary>Barra com fundo escuro e preenchimento controlado por anchorMax.x.</summary>
    private static RectTransform CreateBar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color, out RectTransform back)
    {
        GameObject backObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        backObject.transform.SetParent(parent, false);
        back = backObject.GetComponent<RectTransform>();
        back.anchorMin = anchor;
        back.anchorMax = anchor;
        back.pivot = new Vector2(anchor.x, 1f);
        back.anchoredPosition = position;
        back.sizeDelta = size;
        Image backImage = backObject.GetComponent<Image>();
        backImage.color = BarBackColor;
        backImage.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(back, false);
        RectTransform fill = fillObject.GetComponent<RectTransform>();
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(BarBorder, BarBorder);
        fill.offsetMax = new Vector2(-BarBorder, -BarBorder);
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = color;
        fillImage.raycastTarget = false;
        return fill;
    }

    private static Text CreateText(Transform parent, string name, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = UiFactory.DefaultFont;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return text;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
