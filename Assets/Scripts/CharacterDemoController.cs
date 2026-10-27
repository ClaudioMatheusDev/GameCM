using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Raiz da cena: guarda as referencias montadas pelo editor (fundos, sprites do zumbi, audio)
/// e liga os componentes de jogo: PlayerController, StageManager e GameHud.
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
            playerSettings.stageEndX);

        GetOrAdd<GameHud>().Configure(player, stage);

        if (themeSource != null && themeSource.clip != null)
        {
            themeSource.loop = true;
            themeSource.playOnAwake = false;
            themeSource.Play();
        }
    }

    private void Update()
    {
        if (stage != null && stage.State != GameState.Playing && player.RestartPressed)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
