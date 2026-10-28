using UnityEngine;

/// <summary>HUD de jogo desenhado com IMGUI: vida, fase, zumbis restantes, pontuação, combo e tempo.</summary>
public class GameHud : MonoBehaviour
{
    private const float ReferenceHeight = 720f;

    /// <summary>Guarda um texto e os valores que o geraram, para refazê-lo só quando mudam (sem lixo por frame).</summary>
    private sealed class CachedText
    {
        private int keyA = int.MinValue;
        private int keyB = int.MinValue;

        public string Text { get; set; } = string.Empty;

        public bool Changed(int a, int b = 0)
        {
            if (a == keyA && b == keyB)
            {
                return false;
            }

            keyA = a;
            keyB = b;
            return true;
        }
    }

    private readonly CachedText stageText = new CachedText();
    private readonly CachedText zombiesText = new CachedText();
    private readonly CachedText scoreText = new CachedText();
    private readonly CachedText comboText = new CachedText();
    private readonly CachedText timeText = new CachedText();
    private readonly CachedText healthText = new CachedText();
    private readonly CachedText powerText = new CachedText();

    private PlayerController player;
    private StageManager stage;
    private ScoreTracker score;
    private GUIStyle labelStyle;
    private GUIStyle rightStyle;
    private GUIStyle hintStyle;

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

        if (stageText.Changed(stage.StageIndex, stage.StageCount))
        {
            stageText.Text = "Fase " + (stage.StageIndex + 1) + "/" + stage.StageCount;
        }

        if (zombiesText.Changed(stage.ZombiesRemaining))
        {
            zombiesText.Text = "Zumbis restantes: " + stage.ZombiesRemaining;
        }

        GUI.Label(new Rect(20f, 48f, 400f, 30f), stageText.Text, labelStyle);
        GUI.Label(new Rect(20f, 76f, 400f, 30f), zombiesText.Text, labelStyle);
        GUI.Label(new Rect(20f, 104f, 400f, 30f), PowerLabel(), labelStyle);

        if (scoreText.Changed(score.Score))
        {
            scoreText.Text = "Pontos: " + score.Score;
        }

        int totalSeconds = Mathf.FloorToInt(score.Elapsed);
        if (timeText.Changed(totalSeconds))
        {
            timeText.Text = "Tempo: " + (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
        }

        GUI.Label(new Rect(width - 420f, 20f, 400f, 30f), scoreText.Text, rightStyle);
        GUI.Label(new Rect(width - 420f, 48f, 400f, 30f), timeText.Text, rightStyle);
        if (score.Combo > 1)
        {
            if (comboText.Changed(score.Combo))
            {
                comboText.Text = "Combo x" + score.Combo;
            }

            GUI.Label(new Rect(width - 420f, 76f, 400f, 30f), comboText.Text, rightStyle);
        }

        string hint = "A/D mover  Shift correr  Espaço pular  J soco (aperte de novo para encadear)  K poder  Esc pausa";
        if (stage.State == GameState.Playing && stage.StageCleared)
        {
            hint = "Fase limpa! Vá até a borda direita da tela.";
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

        if (healthText.Changed(health.Current, health.Max))
        {
            healthText.Text = health.Current + "/" + health.Max;
        }

        GUI.Label(rect, healthText.Text, hintStyle);
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

    private string PowerLabel()
    {
        float remaining = player.PowerCooldownRemaining;
        if (remaining <= 0f)
        {
            return "Poder: pronto";
        }

        // O texto só é refeito quando o décimo de segundo muda.
        int tenths = Mathf.CeilToInt(remaining * 10f);
        if (powerText.Changed(tenths))
        {
            powerText.Text = "Poder: " + (tenths / 10f).ToString("0.0") + "s";
        }

        return powerText.Text;
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
