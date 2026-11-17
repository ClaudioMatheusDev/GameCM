using UnityEngine;

public class HitStop : MonoBehaviour
{
    private const float FrozenScale = 0.02f;

    private float endTime;
    private bool active;

    public static HitStop Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Cancel();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Freeze(float seconds)
    {
        if (!active && !Mathf.Approximately(Time.timeScale, 1f))
        {
            return;
        }

        endTime = Mathf.Max(endTime, Time.unscaledTime + seconds);
        active = true;
        Time.timeScale = FrozenScale;
    }

    public void Cancel()
    {
        if (active)
        {
            active = false;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }

        if (!Mathf.Approximately(Time.timeScale, FrozenScale))
        {
            active = false;
            return;
        }

        if (Time.unscaledTime >= endTime)
        {
            Cancel();
        }
    }
}
