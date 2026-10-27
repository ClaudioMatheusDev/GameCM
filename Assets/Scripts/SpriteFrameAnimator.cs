using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float framesPerSecond;
    private bool loop;
    private float timer;

    public bool IsFinished { get; private set; } = true;

    public void Play(Sprite[] newFrames, float fps, bool looping)
    {
        frames = newFrames;
        framesPerSecond = Mathf.Max(0.01f, fps);
        loop = looping;
        timer = 0f;
        IsFinished = frames == null || frames.Length == 0;
        Apply();
    }

    public void Stop()
    {
        frames = null;
        IsFinished = true;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        timer += Time.deltaTime;
        Apply();
    }

    private void Apply()
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        int index = Mathf.FloorToInt(timer * framesPerSecond);
        if (loop)
        {
            index %= frames.Length;
        }
        else if (index >= frames.Length)
        {
            index = frames.Length - 1;
            IsFinished = true;
        }

        spriteRenderer.sprite = frames[index];
    }
}
