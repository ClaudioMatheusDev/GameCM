using UnityEngine;

/// <summary>HUD simples desenhado com IMGUI: vida, fase, zumbis restantes e telas de fim de jogo.</summary>
public class GameHud : MonoBehaviour
{
    private const float ReferenceHeight = 720f;

    private PlayerController player;
    private StageManager stage;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    private GUIStyle hintStyle;

    public void Configure(PlayerController playerController, StageManager stageManager)
    {
        player = playerController;
        stage = stageManager;
    }

    private void OnGUI()
    {
        if (player == null || stage == null)
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

        string hint = "A/D mover  Shift correr  Espaco pular  J soco  K poder";
        if (stage.State == GameState.Playing && stage.StageCleared)
        {
            hint = "Fase limpa! Va ate a borda direita da tela.";
        }

        GUI.Label(new Rect(0f, height - 40f, width, 30f), hint, hintStyle);

        if (stage.State == GameState.GameOver)
        {
            DrawCenterMessage(width, height, "VOCE MORREU", "Pressione R para reiniciar");
        }
        else if (stage.State == GameState.Victory)
        {
            DrawCenterMessage(width, height, "VITORIA!", "Pressione R para jogar novamente");
        }
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

    private string PowerText()
    {
        float remaining = player.PowerCooldownRemaining;
        return remaining > 0f ? "Poder: " + remaining.ToString("0.0") + "s" : "Poder: pronto";
    }

    private void DrawCenterMessage(float width, float height, string title, string subtitle)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(new Rect(0f, height * 0.35f, width, 80f), title, titleStyle);
        GUI.Label(new Rect(0f, height * 0.35f + 90f, width, 40f), subtitle, hintStyle);
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 64,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        hintStyle.normal.textColor = Color.white;
    }
}
