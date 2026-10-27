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
        GUI.Label(new Rect(20f, 48f, 400f, 30f), "Fase " + (stage.StageIndex + 1) + "/" + stage.StageCount, labelStyle);
        GUI.Label(new Rect(20f, 76f, 400f, 30f), "Zumbis restantes: " + stage.ZombiesRemaining, labelStyle);
        GUI.Label(new Rect(20f, 104f, 400f, 30f), PowerText(), labelStyle);

        GUI.Label(new Rect(width - 420f, 20f, 400f, 30f), "Pontos: " + score.Score, rightStyle);
        GUI.Label(new Rect(width - 420f, 48f, 400f, 30f), "Tempo: " + TimeText(score.Elapsed), rightStyle);
        if (score.Combo > 1)
        {
            GUI.Label(new Rect(width - 420f, 76f, 400f, 30f), "Combo x" + score.Combo, rightStyle);
        }

        string hint = "A/D mover  Shift correr  Espaco pular  J soco  K poder  Esc pausa";
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
        GUI.Label(rect, health.Current + "/" + health.Max, hintStyle);
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

    private string PowerText()
    {
        float remaining = player.PowerCooldownRemaining;
        return remaining > 0f ? "Poder: " + remaining.ToString("0.0") + "s" : "Poder: pronto";
    }

    private static string TimeText(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int rest = Mathf.FloorToInt(seconds % 60f);
        return minutes.ToString("00") + ":" + rest.ToString("00");
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
