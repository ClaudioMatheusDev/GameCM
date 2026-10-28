using UnityEngine;

/// <summary>
/// Camera do jogo: segue o jogador com suavizacao (limitada ao cenario) e aplica o tremor de impacto.
/// Usa tempo real, entao continua funcionando durante o hit-stop.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    private const float SmoothTime = 0.15f;

    private Camera cam;
    private Transform target;
    private float minX;
    private float maxX;
    private bool following;
    private float velocityX;
    private Vector3 basePosition;

    private float shakeMagnitude;
    private float shakeDuration;
    private float shakeElapsed;
    private bool shaking;

    public static CameraRig Instance { get; private set; }

    public float CenterX => basePosition.x;
    public float HalfWidth => cam != null ? cam.orthographicSize * cam.aspect : 0f;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        basePosition = transform.position;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Passa a seguir o alvo; a camera nunca mostra nada alem de [worldMinX, worldMaxX].</summary>
    public void Follow(Transform followTarget, float worldMinX, float worldMaxX)
    {
        target = followTarget;
        minX = worldMinX;
        maxX = worldMaxX;
        following = true;
        Snap();
    }

    /// <summary>Posiciona a camera no alvo imediatamente (sem suavizacao), por exemplo apos teleporte.</summary>
    public void Snap()
    {
        if (!following || target == null)
        {
            return;
        }

        basePosition.x = DesiredX();
        velocityX = 0f;
        transform.position = basePosition;
    }

    public void Shake(float magnitude, float seconds)
    {
        if (shaking && magnitude < CurrentMagnitude())
        {
            return;
        }

        shakeMagnitude = magnitude;
        shakeDuration = Mathf.Max(0.01f, seconds);
        shakeElapsed = 0f;
        shaking = true;
    }

    private void LateUpdate()
    {
        if (following && target != null)
        {
            basePosition.x = Mathf.SmoothDamp(basePosition.x, DesiredX(), ref velocityX, SmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        Vector3 offset = Vector3.zero;
        if (shaking)
        {
            shakeElapsed += Time.unscaledDeltaTime;
            if (shakeElapsed >= shakeDuration)
            {
                shaking = false;
            }
            else
            {
                Vector2 random = Random.insideUnitCircle * CurrentMagnitude();
                offset = new Vector3(random.x, random.y, 0f);
            }
        }

        transform.position = basePosition + offset;
    }

    private float DesiredX()
    {
        float half = HalfWidth;
        float low = minX + half;
        float high = maxX - half;
        if (low > high)
        {
            return (minX + maxX) * 0.5f;
        }

        return Mathf.Clamp(target.position.x, low, high);
    }

    private float CurrentMagnitude()
    {
        return shakeMagnitude * (1f - Mathf.Clamp01(shakeElapsed / shakeDuration));
    }
}
