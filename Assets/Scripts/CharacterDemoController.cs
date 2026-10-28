using UnityEngine;

/// <summary>
/// Raiz da cena: guarda as referencias montadas pelo editor (fundos, sprites do zumbi, audio e efeitos)
/// e liga os componentes de jogo: PlayerController, StageManager, ScoreTracker, GameHud e GameMenus.
/// </summary>
public class CharacterDemoController : MonoBehaviour
{
    [SerializeField] private Animator animator = null;
    [SerializeField] private AudioSource themeSource = null;
    [SerializeField] private SpriteRenderer stageBackground = null;
    [SerializeField] private Sprite[] stageBackgrounds = null;
    [SerializeField] private Sprite[] zombieWalkFrames = null;
    [SerializeField] private Sprite[] zombieHurtFrames = null;
    [SerializeField] private Sprite[] zombieDeathFrames = null;
    [SerializeField] private string[] stateNames = { "Idle", "Walk", "Run", "Jump", "Punch", "Power", "Hurt", "Dead" };
    [SerializeField] private SoundBank sounds = new SoundBank();
    [SerializeField] private PlayerTuning playerSettings = new PlayerTuning();
    [SerializeField] private ZombieSettings zombieSettings = new ZombieSettings();
    [SerializeField] private StageSettings stageSettings = new StageSettings();

    private PlayerController player;
    private StageManager stage;

    private void Start()
    {
        if (!ValidateSetup(out Camera stageCamera))
        {
            enabled = false;
            return;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        GetOrAdd<SfxPlayer>().Configure(sounds);
        GetOrAdd<HitStop>();
        if (ParticleFx.Instance == null)
        {
            new GameObject("Particle Effects").AddComponent<ParticleFx>();
        }
        if (stageCamera.GetComponent<CameraShake>() == null)
        {
            stageCamera.gameObject.AddComponent<CameraShake>();
        }

        player = GetOrAdd<PlayerController>();
        player.Configure(playerSettings, stateNames);

        ZombieFrames frames = new ZombieFrames
        {
            Walk = zombieWalkFrames,
            Hurt = zombieHurtFrames,
            Death = zombieDeathFrames
        };

        stage = GetOrAdd<StageManager>();
        stage.Configure(
            stageBackground,
            stageBackgrounds,
            stageCamera,
            player,
            frames,
            zombieSettings,
            stageSettings,
            playerSettings.stageStartX,
            playerSettings.stageEndX,
            GameSession.StartStage);

        ScoreTracker score = GetOrAdd<ScoreTracker>();
        score.Configure(stage, player);

        GameHud hud = GetOrAdd<GameHud>();
        hud.Configure(player, stage, score);

        GetOrAdd<GameMenus>().Configure(player, stage, score, hud);

        if (themeSource != null && themeSource.clip != null)
        {
            themeSource.loop = true;
            themeSource.playOnAwake = false;
            themeSource.Play();
        }
    }

    private bool ValidateSetup(out Camera stageCamera)
    {
        stageCamera = Camera.main;

        if (stageBackground == null || stageBackgrounds == null || stageBackgrounds.Length == 0)
        {
            Debug.LogError("Character Demo precisa de um SpriteRenderer de fundo e de ao menos um sprite de fase.", this);
            return false;
        }

        for (int i = 0; i < stageBackgrounds.Length; i++)
        {
            if (stageBackgrounds[i] == null)
            {
                Debug.LogError("O sprite de fundo da fase " + (i + 1) + " nao foi atribuido.", this);
                return false;
            }
        }

        if (stageCamera == null || !stageCamera.orthographic)
        {
            Debug.LogError("Character Demo precisa de uma camera principal ortografica para ajustar os fundos.", this);
            return false;
        }

        if (zombieWalkFrames == null || zombieWalkFrames.Length == 0)
        {
            Debug.LogError("Os quadros da animacao de caminhada do zumbi nao foram atribuidos.", this);
            return false;
        }

        return true;
    }

    private T GetOrAdd<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
