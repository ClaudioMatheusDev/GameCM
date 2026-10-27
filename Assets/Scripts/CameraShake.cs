using UnityEngine;

/// <summary>Tremor de camera por deslocamento aleatorio decrescente (usa tempo real, funciona durante o hit-stop).</summary>
public class CameraShake : MonoBehaviour
{
    private Vector3 basePosition;
    private float magnitude;
    private float duration;
    private float elapsed;
    private bool shaking;

    public static CameraShake Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        basePosition = transform.localPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Shake(float shakeMagnitude, float shakeDuration)
    {
        if (!shaking)
        {
            basePosition = transform.localPosition;
        }
        else if (shakeMagnitude < CurrentMagnitude())
        {
            return;
        }

        magnitude = shakeMagnitude;
        duration = Mathf.Max(0.01f, shakeDuration);
        elapsed = 0f;
        shaking = true;
    }

    private void LateUpdate()
    {
        if (!shaking)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= duration)
        {
            shaking = false;
            transform.localPosition = basePosition;
            return;
        }

        Vector2 offset = Random.insideUnitCircle * CurrentMagnitude();
        transform.localPosition = basePosition + new Vector3(offset.x, offset.y, 0f);
    }

    private float CurrentMagnitude()
    {
        return magnitude * (1f - Mathf.Clamp01(elapsed / duration));
    }
}
