using UnityEngine;

public class CharacterDemoController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource themeSource;
    [SerializeField] private string[] stateNames = { "Idle", "Walk", "Run", "Jump", "Punch", "Power", "Hurt", "Dead" };
    [SerializeField] private float secondsPerState = 1.35f;
    [SerializeField] private float moveDistance = 1.4f;
    [SerializeField] private float jumpHeight = 1.15f;

    private Vector3 startPosition;
    private float timer;
    private int stateIndex;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        startPosition = transform.position;
    }

    private void Start()
    {
        if (themeSource != null && themeSource.clip != null)
        {
            themeSource.loop = true;
            themeSource.playOnAwake = false;
            themeSource.Play();
        }

        ApplyState(0);
    }

    private void Update()
    {
        if (stateNames == null || stateNames.Length == 0)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= secondsPerState)
        {
            timer = 0f;
            stateIndex = (stateIndex + 1) % stateNames.Length;
            ApplyState(stateIndex);
        }

        PreviewMotion();
    }

    private void ApplyState(int index)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetInteger("State", index);
    }

    private void PreviewMotion()
    {
        float phase = Mathf.Clamp01(timer / secondsPerState);
        Vector3 position = startPosition;

        if (stateNames[stateIndex] == "Walk")
        {
            position.x += Mathf.Lerp(-moveDistance, moveDistance, phase);
        }
        else if (stateNames[stateIndex] == "Run")
        {
            position.x += Mathf.Lerp(moveDistance, -moveDistance, phase);
        }
        else if (stateNames[stateIndex] == "Jump")
        {
            position.y += Mathf.Sin(phase * Mathf.PI) * jumpHeight;
        }

        transform.position = position;
    }
}
