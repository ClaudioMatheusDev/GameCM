using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum MenuScreen
{
    None,
    Main,
    StageSelect,
    HowToPlay,
    Options,
    Credits,
    Ranking,
    Pause,
    Result
}

/// <summary>Todas as telas do jogo (menu, selecao de fase, opcoes, pausa, resultado, introducao de fase).</summary>
public class GameMenus : MonoBehaviour
{
    private const string GameTitle = "GAMECM";
    private const string Credits =
        "Projeto GameCM\n\n"
        + "Programação e design: Claudio Matheus\n"
        + "Arte: pixel art (frames do Pixilart)\n"
        + "Trilha sonora: criada no BeepBox\n"
        + "Motor: Unity 6";

    private const string HowToPlayText =
        "A / D ou setas: mover\n"
        + "Shift: correr\n"
        + "Espaço: pular (ajuda a desviar dos zumbis)\n"
        + "J: soco. Aperte J de novo logo após o impacto para fazer um combo de 3 socos\n"
        + "K: poder (recarga de 2 s)\n"
        + "Esc ou Start: pausar\n\n"
        + "Zumbis ficam amarelos um instante antes de atacar: pule ou golpeie primeiro.\n"
        + "Elimine todos os zumbis e vá até a borda direita para avançar.\n"
        + "Gamepad: analógico, A pula, X soco, Y poder, gatilho direito corre.";

    private const float IntroSeconds = 2f;
    private const float VolumeStep = 0.1f;

    private readonly Dictionary<MenuScreen, GameObject> panels = new Dictionary<MenuScreen, GameObject>();
    private readonly Dictionary<MenuScreen, Button> firstButtons = new Dictionary<MenuScreen, Button>();
    private readonly List<Button> stageButtons = new List<Button>();

    private PlayerController player;
    private StageManager stage;
    private ScoreTracker score;
    private GameHud hud;
    private MenuScreen current = MenuScreen.None;
    private MenuScreen returnTo = MenuScreen.Main;

    private Text volumeLabel;
    private Button fullscreenButton;
    private Button effectsButton;
    private Button difficultyButton;
    private Text rankingBody;
    private Text resultTitle;
    private Text resultBody;
    private GameObject introRoot;
    private Text introTitle;
    private Text introSubtitle;
    private Coroutine introRoutine;

    public MenuScreen Current => current;

    public void Configure(PlayerController playerController, StageManager stageManager, ScoreTracker scoreTracker, GameHud gameHud)
    {
        player = playerController;
        stage = stageManager;
        score = scoreTracker;
        hud = gameHud;

        ApplySavedSettings();
        UiFactory.EnsureEventSystem();
        BuildAll();

        stage.StateChanged += OnStateChanged;
        stage.StageBegan += OnStageBegan;
        stage.BossAppeared += OnBossAppeared;

        if (GameSession.SkipMenu)
        {
            Open(MenuScreen.None);
            ShowIntro(stage.StageIndex);
        }
        else
        {
            Open(MenuScreen.Main);
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (stage != null)
        {
            stage.StateChanged -= OnStateChanged;
            stage.StageBegan -= OnStageBegan;
            stage.BossAppeared -= OnBossAppeared;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (current == MenuScreen.Result && player.RestartPressed)
        {
            GameSession.Restart(0, false);
            return;
        }

        if (!player.BackPressed)
        {
            return;
        }

        switch (current)
        {
            case MenuScreen.None:
                if (stage.State == GameState.Playing)
                {
                    Open(MenuScreen.Pause);
                }

                break;
            case MenuScreen.Pause:
                Open(MenuScreen.None);
                break;
            case MenuScreen.StageSelect:
            case MenuScreen.HowToPlay:
            case MenuScreen.Options:
            case MenuScreen.Credits:
            case MenuScreen.Ranking:
                Open(returnTo);
                break;
        }
    }

    private void Open(MenuScreen screen)
    {
        current = screen;
        if (HitStop.Instance != null)
        {
            HitStop.Instance.Cancel();
        }

        foreach (KeyValuePair<MenuScreen, GameObject> entry in panels)
        {
            entry.Value.SetActive(entry.Key == screen);
        }

        bool paused = screen != MenuScreen.None;
        Time.timeScale = paused ? 0f : 1f;
        player.InputLocked = paused || stage.State != GameState.Playing;
        hud.Visible = !paused;

        if (paused)
        {
            SfxPlayer.UiMutedUntilFrame = Time.frameCount + 1;
            HideIntro();
            RefreshDynamicTexts();
            SelectFirst(screen);
        }
    }

    private void OpenSub(MenuScreen screen)
    {
        returnTo = current;
        Open(screen);
    }

    private void SelectFirst(MenuScreen screen)
    {
        if (EventSystem.current == null || !firstButtons.TryGetValue(screen, out Button button))
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void StartGame()
    {
        if (stage.StageIndex == 0)
        {
            Open(MenuScreen.None);
            ShowIntro(0);
        }
        else
        {
            GameSession.Restart(0, false);
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ApplySavedSettings()
    {
        AudioListener.volume = GameProgress.Volume;
        if (GameProgress.HasFullscreenPreference)
        {
            Screen.fullScreen = GameProgress.Fullscreen;
        }
    }

    private void ChangeVolume(float delta)
    {
        float volume = Mathf.Round(Mathf.Clamp01(GameProgress.Volume + delta) * 10f) / 10f;
        GameProgress.Volume = volume;
        GameProgress.Save();
        AudioListener.volume = volume;
        RefreshDynamicTexts();
    }

    private void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        GameProgress.Fullscreen = Screen.fullScreen;
        GameProgress.Save();
        RefreshDynamicTexts();
    }

    private void ToggleEffects()
    {
        GameProgress.ScreenEffects = !GameProgress.ScreenEffects;
        ImpactFx.ScreenEffectsEnabled = GameProgress.ScreenEffects;
        GameProgress.Save();
        RefreshDynamicTexts();
    }

    private void CycleDifficulty()
    {
        GameProgress.Difficulty = (Difficulty)(((int)GameProgress.Difficulty + 1) % 3);
        GameProgress.Save();
        RefreshDynamicTexts();
    }

    private void OnStateChanged(GameState state)
    {
        if (state != GameState.Playing)
        {
            StartCoroutine(ShowResultAfterDelay(state));
        }
    }

    private IEnumerator ShowResultAfterDelay(GameState state)
    {
        yield return new WaitForSeconds(state == GameState.Victory ? 0.8f : 1.5f);
        resultTitle.text = state == GameState.Victory ? "VITÓRIA!" : "VOCÊ MORREU";
        resultBody.text = BuildResultText(state);
        Open(MenuScreen.Result);
    }

    private string BuildResultText(GameState state)
    {
        int minutes = Mathf.FloorToInt(score.Elapsed / 60f);
        int seconds = Mathf.FloorToInt(score.Elapsed % 60f);

        StringBuilder text = new StringBuilder();
        text.AppendLine("Pontuação: " + score.Score);
        if (score.NewRecord)
        {
            text.AppendLine("NOVO RECORDE!");
        }
        else if (score.Rank > 0)
        {
            text.AppendLine("Você entrou no ranking: " + score.Rank + "º lugar");
        }
        else
        {
            text.AppendLine("Recorde: " + score.HighScore);
        }

        text.AppendLine("Zumbis derrotados: " + score.Kills);
        text.AppendLine("Maior combo: " + score.BestCombo);
        text.AppendLine("Estrelas: " + score.TotalStars + "/" + (stage.StageCount * ScoreCalculator.MaxStars));
        text.AppendLine("Tempo: " + minutes.ToString("00") + ":" + seconds.ToString("00"));
        text.Append("Dificuldade: " + DifficultyProfile.Current.Label);
        if (state == GameState.GameOver)
        {
            text.Append("\nFase alcançada: " + (stage.StageIndex + 1) + "/" + stage.StageCount);
        }

        return text.ToString();
    }

    private void OnStageBegan(int stageIndex)
    {
        if (current == MenuScreen.None)
        {
            ShowIntro(stageIndex);
        }
    }

    private void OnBossAppeared()
    {
        if (current == MenuScreen.None)
        {
            ShowBanner("CHEFE!", "Derrote-o para vencer.");
        }
    }

    private void ShowIntro(int stageIndex)
    {
        ShowBanner(
            "FASE " + (stageIndex + 1) + "/" + stage.StageCount,
            stageIndex == stage.StageCount - 1
                ? "Última fase! Derrote todos os zumbis e o chefe."
                : "Elimine todos os zumbis e siga para a direita.");
    }

    private void ShowBanner(string title, string subtitle)
    {
        HideIntro();
        introTitle.text = title;
        introSubtitle.text = subtitle;
        introRoot.SetActive(true);
        introRoutine = StartCoroutine(HideIntroAfterDelay());
    }

    private IEnumerator HideIntroAfterDelay()
    {
        yield return new WaitForSeconds(IntroSeconds);
        introRoot.SetActive(false);
        introRoutine = null;
    }

    private void HideIntro()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        introRoot.SetActive(false);
    }

    private void RefreshDynamicTexts()
    {
        volumeLabel.text = "Volume: " + Mathf.RoundToInt(GameProgress.Volume * 100f) + "%";
        UiFactory.SetButtonText(fullscreenButton, "Tela cheia: " + (Screen.fullScreen ? "Sim" : "Não"));
        UiFactory.SetButtonText(effectsButton, "Tremor e pausa nos golpes: " + (GameProgress.ScreenEffects ? "Sim" : "Não"));
        UiFactory.SetButtonText(difficultyButton, "Dificuldade: " + DifficultyProfile.Get(GameProgress.Difficulty).Label);

        int unlocked = GameProgress.UnlockedStage;
        for (int i = 0; i < stageButtons.Count; i++)
        {
            bool isUnlocked = i <= unlocked;
            stageButtons[i].interactable = isUnlocked;

            string label = "Fase " + (i + 1);
            if (!isUnlocked)
            {
                label += " (bloqueada)";
            }
            else if (GameProgress.GetStageStars(i) > 0)
            {
                label += "  -  " + GameProgress.GetStageStars(i) + "/" + ScoreCalculator.MaxStars + " estrelas";
            }

            UiFactory.SetButtonText(stageButtons[i], label);
        }

        rankingBody.text = BuildRankingText();
    }

    private static string BuildRankingText()
    {
        int[] scores = GameProgress.TopScores;
        if (scores.Length == 0)
        {
            return "Ainda não há pontuações.\nJogue uma partida!";
        }

        StringBuilder text = new StringBuilder();
        for (int i = 0; i < scores.Length; i++)
        {
            text.Append(i + 1).Append("º   ").Append(scores[i]);
            if (i < scores.Length - 1)
            {
                text.Append('\n');
            }
        }

        return text.ToString();
    }

    private void BuildAll()
    {
        Canvas canvas = UiFactory.CreateCanvas("Game Menus", 100);

        BuildMain(canvas.transform);
        BuildStageSelect(canvas.transform);
        BuildInfoScreen(canvas.transform, MenuScreen.HowToPlay, "COMO JOGAR", HowToPlayText, 340f);
        BuildOptions(canvas.transform);
        BuildInfoScreen(canvas.transform, MenuScreen.Credits, "CRÉDITOS", Credits, 220f);
        BuildRanking(canvas.transform);
        BuildPause(canvas.transform);
        BuildResult(canvas.transform);
        BuildIntro(canvas.transform);
    }

    private RectTransform CreateScreen(Transform parent, MenuScreen screen, string title)
    {
        RectTransform panel = UiFactory.CreatePanel(parent, screen + " Screen", UiFactory.OverlayColor, 14f);
        panels[screen] = panel.gameObject;
        UiFactory.CreateLabel(panel, title, 56, 80f, UiFactory.TitleColor, FontStyle.Bold);
        return panel;
    }

    private Button AddButton(RectTransform panel, MenuScreen screen, string label, UnityEngine.Events.UnityAction action)
    {
        Button button = UiFactory.CreateButton(panel, label, action);
        if (!firstButtons.ContainsKey(screen))
        {
            firstButtons[screen] = button;
        }

        return button;
    }

    private void BuildMain(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.Main, GameTitle);
        UiFactory.CreateLabel(panel, "Beat'em up 2D", 24, 36f, UiFactory.MutedColor);
        AddButton(panel, MenuScreen.Main, "Jogar", StartGame);
        AddButton(panel, MenuScreen.Main, "Selecionar fase", () => OpenSub(MenuScreen.StageSelect));
        AddButton(panel, MenuScreen.Main, "Ranking", () => OpenSub(MenuScreen.Ranking));
        AddButton(panel, MenuScreen.Main, "Como jogar", () => OpenSub(MenuScreen.HowToPlay));
        AddButton(panel, MenuScreen.Main, "Opções", () => OpenSub(MenuScreen.Options));
        AddButton(panel, MenuScreen.Main, "Créditos", () => OpenSub(MenuScreen.Credits));
        AddButton(panel, MenuScreen.Main, "Sair", QuitGame);
    }

    private void BuildStageSelect(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.StageSelect, "SELECIONAR FASE");
        for (int i = 0; i < stage.StageCount; i++)
        {
            int stageIndex = i;
            Button button = AddButton(panel, MenuScreen.StageSelect, "Fase " + (i + 1), () => GameSession.Restart(stageIndex, false));
            button.GetComponent<LayoutElement>().preferredWidth = 460f;
            stageButtons.Add(button);
        }

        AddButton(panel, MenuScreen.StageSelect, "Voltar", () => Open(returnTo));
    }

    private void BuildInfoScreen(Transform parent, MenuScreen screen, string title, string body, float bodyHeight)
    {
        RectTransform panel = CreateScreen(parent, screen, title);
        UiFactory.CreateLabel(panel, body, 24, bodyHeight, Color.white);
        AddButton(panel, screen, "Voltar", () => Open(returnTo));
    }

    private void BuildRanking(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.Ranking, "RANKING");
        rankingBody = UiFactory.CreateLabel(panel, string.Empty, 30, 230f, Color.white, FontStyle.Bold);
        AddButton(panel, MenuScreen.Ranking, "Voltar", () => Open(returnTo));
    }

    private void BuildOptions(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.Options, "OPÇÕES");

        RectTransform row = UiFactory.CreateRow(panel, 50f);
        Button minus = UiFactory.CreateButton(row, "-", () => ChangeVolume(-VolumeStep), 70f);
        firstButtons[MenuScreen.Options] = minus;
        volumeLabel = UiFactory.CreateLabel(row, "Volume", 26, 50f, Color.white, FontStyle.Bold);
        volumeLabel.GetComponent<LayoutElement>().preferredWidth = 240f;
        UiFactory.CreateButton(row, "+", () => ChangeVolume(VolumeStep), 70f);

        fullscreenButton = UiFactory.CreateButton(panel, "Tela cheia", ToggleFullscreen, 520f);
        effectsButton = UiFactory.CreateButton(panel, "Tremor e pausa nos golpes", ToggleEffects, 520f);
        difficultyButton = UiFactory.CreateButton(panel, "Dificuldade", CycleDifficulty, 520f);
        UiFactory.CreateLabel(panel, "A dificuldade vale a partir da próxima partida.", 20, 28f, UiFactory.MutedColor);
        AddButton(panel, MenuScreen.Options, "Voltar", () => Open(returnTo));
    }

    private void BuildPause(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.Pause, "PAUSADO");
        AddButton(panel, MenuScreen.Pause, "Continuar", () => Open(MenuScreen.None));
        AddButton(panel, MenuScreen.Pause, "Reiniciar fase", () => GameSession.Restart(stage.StageIndex, false));
        AddButton(panel, MenuScreen.Pause, "Como jogar", () => OpenSub(MenuScreen.HowToPlay));
        AddButton(panel, MenuScreen.Pause, "Opções", () => OpenSub(MenuScreen.Options));
        AddButton(panel, MenuScreen.Pause, "Menu principal", () => GameSession.Restart(0, true));
    }

    private void BuildResult(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.Result, "RESULTADO");
        resultTitle = panel.GetChild(0).GetComponent<Text>();
        resultBody = UiFactory.CreateLabel(panel, string.Empty, 28, 300f, Color.white);
        AddButton(panel, MenuScreen.Result, "Jogar novamente (R)", () => GameSession.Restart(0, false));
        AddButton(panel, MenuScreen.Result, "Menu principal", () => GameSession.Restart(0, true));
    }

    private void BuildIntro(Transform parent)
    {
        RectTransform panel = UiFactory.CreatePanel(parent, "Stage Intro", Color.clear, 8f);
        introRoot = panel.gameObject;
        introTitle = UiFactory.CreateLabel(panel, string.Empty, 72, 100f, UiFactory.TitleColor, FontStyle.Bold);
        introSubtitle = UiFactory.CreateLabel(panel, string.Empty, 28, 44f, Color.white);
        introRoot.SetActive(false);
    }
}
