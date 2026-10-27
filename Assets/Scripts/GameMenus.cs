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
    Pause,
    Result
}

/// <summary>Todas as telas do jogo (menu, selecao de fase, opcoes, pausa, resultado, introducao de fase).</summary>
public class GameMenus : MonoBehaviour
{
    private const string GameTitle = "GAMECM";
    private const string Credits =
        "Projeto GameCM\n\n"
        + "Programacao e design: Claudio Matheus\n"
        + "Arte: pixel art (frames do Pixilart)\n"
        + "Trilha sonora: criada no BeepBox\n"
        + "Motor: Unity 6";

    private const string HowToPlayText =
        "A / D ou setas: mover\n"
        + "Shift: correr\n"
        + "Espaco: pular (ajuda a desviar dos zumbis)\n"
        + "J: soco     K: poder (recarga de 2 s)\n"
        + "Esc ou Start: pausar\n\n"
        + "Elimine todos os zumbis e va ate a borda direita para avancar.\n"
        + "Gamepad: analogico, A pula, X soco, Y poder, gatilho direito corre.";

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
                Open(returnTo);
                break;
        }
    }

    private void Open(MenuScreen screen)
    {
        current = screen;
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
        resultTitle.text = state == GameState.Victory ? "VITORIA!" : "VOCE MORREU";
        resultBody.text = BuildResultText(state);
        Open(MenuScreen.Result);
    }

    private string BuildResultText(GameState state)
    {
        int minutes = Mathf.FloorToInt(score.Elapsed / 60f);
        int seconds = Mathf.FloorToInt(score.Elapsed % 60f);

        StringBuilder text = new StringBuilder();
        text.AppendLine("Pontuacao: " + score.Score);
        text.AppendLine(score.NewRecord ? "NOVO RECORDE!" : "Recorde: " + score.HighScore);
        text.AppendLine("Zumbis derrotados: " + score.Kills);
        text.AppendLine("Maior combo: " + score.BestCombo);
        text.AppendLine("Estrelas: " + score.TotalStars + "/" + (stage.StageCount * 3));
        text.Append("Tempo: " + minutes.ToString("00") + ":" + seconds.ToString("00"));
        if (state == GameState.GameOver)
        {
            text.Append("\nFase alcancada: " + (stage.StageIndex + 1) + "/" + stage.StageCount);
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

    private void ShowIntro(int stageIndex)
    {
        HideIntro();
        introTitle.text = "FASE " + (stageIndex + 1) + "/" + stage.StageCount;
        introSubtitle.text = stageIndex == stage.StageCount - 1
            ? "Ultima fase! Elimine todos os zumbis."
            : "Elimine todos os zumbis e siga para a direita.";
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
        UiFactory.SetButtonText(fullscreenButton, "Tela cheia: " + (Screen.fullScreen ? "Sim" : "Nao"));

        int unlocked = GameProgress.UnlockedStage;
        for (int i = 0; i < stageButtons.Count; i++)
        {
            bool isUnlocked = i <= unlocked;
            stageButtons[i].interactable = isUnlocked;
            UiFactory.SetButtonText(stageButtons[i], isUnlocked ? "Fase " + (i + 1) : "Fase " + (i + 1) + " (bloqueada)");
        }
    }

    private void BuildAll()
    {
        Canvas canvas = UiFactory.CreateCanvas("Game Menus", 100);

        BuildMain(canvas.transform);
        BuildStageSelect(canvas.transform);
        BuildInfoScreen(canvas.transform, MenuScreen.HowToPlay, "COMO JOGAR", HowToPlayText, 300f);
        BuildOptions(canvas.transform);
        BuildInfoScreen(canvas.transform, MenuScreen.Credits, "CREDITOS", Credits, 220f);
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
        AddButton(panel, MenuScreen.Main, "Como jogar", () => OpenSub(MenuScreen.HowToPlay));
        AddButton(panel, MenuScreen.Main, "Opcoes", () => OpenSub(MenuScreen.Options));
        AddButton(panel, MenuScreen.Main, "Creditos", () => OpenSub(MenuScreen.Credits));
        AddButton(panel, MenuScreen.Main, "Sair", QuitGame);
    }

    private void BuildStageSelect(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.StageSelect, "SELECIONAR FASE");
        for (int i = 0; i < stage.StageCount; i++)
        {
            int stageIndex = i;
            stageButtons.Add(AddButton(panel, MenuScreen.StageSelect, "Fase " + (i + 1), () => GameSession.Restart(stageIndex, false)));
        }

        AddButton(panel, MenuScreen.StageSelect, "Voltar", () => Open(returnTo));
    }

    private void BuildInfoScreen(Transform parent, MenuScreen screen, string title, string body, float bodyHeight)
    {
        RectTransform panel = CreateScreen(parent, screen, title);
        UiFactory.CreateLabel(panel, body, 24, bodyHeight, Color.white);
        AddButton(panel, screen, "Voltar", () => Open(returnTo));
    }

    private void BuildOptions(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.Options, "OPCOES");

        RectTransform row = UiFactory.CreateRow(panel, 50f);
        Button minus = UiFactory.CreateButton(row, "-", () => ChangeVolume(-VolumeStep), 70f);
        firstButtons[MenuScreen.Options] = minus;
        volumeLabel = UiFactory.CreateLabel(row, "Volume", 26, 50f, Color.white, FontStyle.Bold);
        volumeLabel.GetComponent<LayoutElement>().preferredWidth = 240f;
        UiFactory.CreateButton(row, "+", () => ChangeVolume(VolumeStep), 70f);

        fullscreenButton = UiFactory.CreateButton(panel, "Tela cheia", ToggleFullscreen);
        AddButton(panel, MenuScreen.Options, "Voltar", () => Open(returnTo));
    }

    private void BuildPause(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.Pause, "PAUSADO");
        AddButton(panel, MenuScreen.Pause, "Continuar", () => Open(MenuScreen.None));
        AddButton(panel, MenuScreen.Pause, "Reiniciar fase", () => GameSession.Restart(stage.StageIndex, false));
        AddButton(panel, MenuScreen.Pause, "Como jogar", () => OpenSub(MenuScreen.HowToPlay));
        AddButton(panel, MenuScreen.Pause, "Opcoes", () => OpenSub(MenuScreen.Options));
        AddButton(panel, MenuScreen.Pause, "Menu principal", () => GameSession.Restart(0, true));
    }

    private void BuildResult(Transform parent)
    {
        RectTransform panel = CreateScreen(parent, MenuScreen.Result, "RESULTADO");
        resultTitle = panel.GetChild(0).GetComponent<Text>();
        resultBody = UiFactory.CreateLabel(panel, string.Empty, 28, 260f, Color.white);
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
