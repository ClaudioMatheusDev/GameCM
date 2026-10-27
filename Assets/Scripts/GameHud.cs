using UnityEngine;

/// <summary>HUD de jogo desenhado com IMGUI: vida, fase, zumbis restantes, pontuacao, combo e tempo.</summary>
public class GameHud : MonoBehaviour
{
    private const float ReferenceHeight = 720f;

    private PlayerController player;
    private StageManager stage;
    private ScoreTracker score;
    private GUIStyle labelStyle;
    private GUIStyle rightStyle;
    private GUIStyle hintStyle;

    // Textos em cache: so sao remontados quando o valor muda (evita lixo a cada frame do OnGUI).
    private string stageText = "";
    private string remainingText = "";
    private string scoreText = "";
    private string timeText = "";
    private string comboText = "";
    private string powerText = "";
    private string healthText = "";
    private int stageKey = -1;
    private int remainingKey = -1;
    private int scoreKey = -1;
    private int timeKey = -1;
    private int comboKey = -1;
    private int powerKey = -2;
    private int healthKey = -1;

    public bool Visible { get; set; } = true;

    public void Configure(PlayerController playerController, StageManager stageManager, ScoreTracker scoreTracker)
    {
        player = playerController;
        stage = stageManager;
        score = scoreTracker;
    }

    private void OnGUI()
    {
        if (!Visible || player == null || stage == null || score == null)
        {
            return;
        }

        EnsureStyles();
        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = ReferenceHeight;

        DrawHealthBar(new Rect(20f, 20f, 260f, 22f));
        GUI.Label(new Rect(20f, 48f, 400f, 30f), StageText(), labelStyle);
        GUI.Label(new Rect(20f, 76f, 400f, 30f), RemainingText(), labelStyle);
        GUI.Label(new Rect(20f, 104f, 400f, 30f), PowerText(), labelStyle);

        GUI.Label(new Rect(width - 420f, 20f, 400f, 30f), ScoreText(), rightStyle);
        GUI.Label(new Rect(width - 420f, 48f, 400f, 30f), ElapsedText(), rightStyle);
        if (score.Combo > 1)
        {
            GUI.Label(new Rect(width - 420f, 76f, 400f, 30f), ComboText(), rightStyle);
        }

        string hint = ControlsText.HudHint;
        if (stage.State == GameState.Playing && stage.StageCleared)
        {
            hint = "Fase limpa! Va ate a borda direita da tela.";
        }

        GUI.Label(new Rect(0f, height - 40f, width, 30f), hint, hintStyle);
        DrawBossBar(width);
    }

    private void DrawHealthBar(Rect rect)
    {
        Health health = player.Health;
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        float fraction = health.Max > 0 ? (float)health.Current / health.Max : 0f;
        GUI.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * fraction, rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(rect, HealthText(health), hintStyle);
    }

    private void DrawBossBar(float width)
    {
        ZombieController boss = stage.ActiveBoss;
        if (boss == null || !boss.gameObject.activeInHierarchy)
        {
            return;
        }

        Rect rect = new Rect((width - 420f) * 0.5f, 56f, 420f, 20f);
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        float fraction = boss.Health.Max > 0 ? (float)boss.Health.Current / boss.Health.Max : 0f;
        GUI.color = new Color(0.7f, 0.1f, 0.6f, 1f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * fraction, rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(new Rect(rect.x, rect.y - 30f, rect.width, 28f), "CHEFE", hintStyle);
    }

    private string StageText()
    {
        int key = stage.StageIndex * 100 + stage.StageCount;
        if (key != stageKey)
        {
            stageKey = key;
            stageText = "Fase " + (stage.StageIndex + 1) + "/" + stage.StageCount;
        }

        return stageText;
    }

    private string RemainingText()
    {
        if (stage.ZombiesRemaining != remainingKey)
        {
            remainingKey = stage.ZombiesRemaining;
            remainingText = "Zumbis restantes: " + remainingKey;
        }

        return remainingText;
    }

    private string ScoreText()
    {
        if (score.Score != scoreKey)
        {
            scoreKey = score.Score;
            scoreText = "Pontos: " + scoreKey;
        }

        return scoreText;
    }

    private string ElapsedText()
    {
        int seconds = Mathf.FloorToInt(score.Elapsed);
        if (seconds != timeKey)
        {
            timeKey = seconds;
            timeText = "Tempo: " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        return timeText;
    }

    private string ComboText()
    {
        if (score.Combo != comboKey)
        {
            comboKey = score.Combo;
            comboText = "Combo x" + comboKey;
        }

        return comboText;
    }

    private string PowerText()
    {
        float remaining = player.PowerCooldownRemaining;
        int key = remaining > 0f ? Mathf.CeilToInt(remaining * 10f) : 0;
        if (key != powerKey)
        {
            powerKey = key;
            powerText = key > 0 ? "Poder: " + (key / 10f).ToString("0.0") + "s" : "Poder: pronto";
        }

        return powerText;
    }

    private string HealthText(Health health)
    {
        int key = health.Current * 1000 + health.Max;
        if (key != healthKey)
        {
            healthKey = key;
            healthText = health.Current + "/" + health.Max;
        }

        return healthText;
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = Color.white;

        rightStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleRight };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        hintStyle.normal.textColor = Color.white;
    }
}
