using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterDemoController : MonoBehaviour
{
    [SerializeField] private Animator animator = null;
    [SerializeField] private AudioSource themeSource = null;
    [SerializeField] private SpriteRenderer stageBackground = null;
    [SerializeField] private Sprite[] stageBackgrounds = null;
    [SerializeField] private float backgroundZoom = 1f;
    [SerializeField] private string[] stateNames = { "Idle", "Walk", "Run", "Jump", "Punch", "Power", "Hurt", "Dead" };
    [SerializeField] private float horizontalSpeed = 5f;
    [SerializeField] private float stageStartX = -10.5f;
    [SerializeField] private float stageEndX = 10.5f;
    [SerializeField] private float jumpDuration = 0.55f;
    [SerializeField] private float jumpHeight = 1.15f;
    [SerializeField] private float punchDuration = 0.6f;
    [SerializeField] private float powerDuration = 0.7f;

    private float groundY;
    private float jumpTimer;
    private float attackTimer;
    private int stageIndex;
    private string activeAttackState;
    private Camera stageCamera;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        groundY = transform.position.y;
        jumpTimer = jumpDuration;
    }

    private void Start()
    {
        if (stageBackground == null || stageBackgrounds == null || stageBackgrounds.Length != 4)
        {
            Debug.LogError("Character Demo precisa de um SpriteRenderer de fundo e dos quatro sprites de fase.", this);
            enabled = false;
            return;
        }

        stageCamera = Camera.main;
        if (stageCamera == null || !stageCamera.orthographic)
        {
            Debug.LogError("Character Demo precisa de uma camera principal ortografica para ajustar os fundos.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < stageBackgrounds.Length; i++)
        {
            if (stageBackgrounds[i] == null)
            {
                Debug.LogError("O sprite de fundo da fase " + (i + 1) + " nao foi atribuido.", this);
                enabled = false;
                return;
            }
        }

        SetStageBackground(stageBackgrounds[stageIndex]);

        if (themeSource != null && themeSource.clip != null)
        {
            themeSource.loop = true;
            themeSource.playOnAwake = false;
            themeSource.Play();
        }

        ApplyState("Idle");
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        float direction = 0f;
        if (keyboard.aKey.isPressed)
        {
            direction -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            direction += 1f;
        }

        if (keyboard.spaceKey.wasPressedThisFrame && jumpTimer >= jumpDuration)
        {
            jumpTimer = 0f;
        }

        if (attackTimer <= 0f)
        {
            if (keyboard.kKey.wasPressedThisFrame)
            {
                StartAttack("Power", powerDuration);
            }
            else if (keyboard.jKey.wasPressedThisFrame)
            {
                StartAttack("Punch", punchDuration);
            }
        }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(
            position.x + direction * horizontalSpeed * Time.deltaTime,
            stageStartX,
            stageEndX);

        bool isJumping = jumpTimer < jumpDuration;
        if (isJumping)
        {
            jumpTimer = Mathf.Min(jumpTimer + Time.deltaTime, jumpDuration);
            float jumpPhase = jumpTimer / jumpDuration;
            position.y = groundY + Mathf.Sin(jumpPhase * Mathf.PI) * jumpHeight;
        }
        else
        {
            position.y = groundY;
        }

        transform.position = position;

        if (position.x >= stageEndX && stageIndex < stageBackgrounds.Length - 1)
        {
            AdvanceStage(position);
        }

        if (attackTimer > 0f)
        {
            ApplyState(activeAttackState);
            attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
            return;
        }

        ApplyState(isJumping ? "Jump" : direction == 0f ? "Idle" : "Walk");
    }

    private void StartAttack(string stateName, float duration)
    {
        activeAttackState = stateName;
        attackTimer = duration;
    }

    private void AdvanceStage(Vector3 position)
    {
        stageIndex++;
        SetStageBackground(stageBackgrounds[stageIndex]);
        position.x = stageStartX;
        transform.position = position;
    }

    private void SetStageBackground(Sprite background)
    {
        stageBackground.sprite = background;
        float targetHeight = stageCamera.orthographicSize * 2f * backgroundZoom;
        float scale = targetHeight / background.bounds.size.y;
        Vector3 localScale = stageBackground.transform.localScale;
        localScale.x = scale;
        localScale.y = scale;
        stageBackground.transform.localScale = localScale;
    }

    private void ApplyState(string stateName)
    {
        if (animator == null || stateNames == null)
        {
            return;
        }

        for (int i = 0; i < stateNames.Length; i++)
        {
            if (stateNames[i] == stateName)
            {
                animator.SetInteger("State", i);
                return;
            }
        }
    }
}
