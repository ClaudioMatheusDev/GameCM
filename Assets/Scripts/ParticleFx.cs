using UnityEngine;

/// <summary>
/// Particulas pixeladas emitidas por codigo em um unico ParticleSystem compartilhado.
/// Todas as chamadas sao seguras mesmo sem a instancia na cena (nao fazem nada).
/// </summary>
public class ParticleFx : MonoBehaviour
{
    private const int SortingOrder = 30;
    private const int MaxParticles = 600;

    private static readonly Color Spark = new Color(1f, 0.95f, 0.55f, 1f);
    private static readonly Color Orange = new Color(1f, 0.55f, 0.1f, 1f);
    private static readonly Color Blood = new Color(0.45f, 0.75f, 0.25f, 1f);
    private static readonly Color DarkBlood = new Color(0.25f, 0.5f, 0.15f, 1f);
    private static readonly Color Red = new Color(0.9f, 0.12f, 0.15f, 1f);
    private static readonly Color Purple = new Color(0.7f, 0.15f, 0.7f, 1f);
    private static readonly Color Green = new Color(0.4f, 1f, 0.5f, 1f);
    private static readonly Color DustColor = new Color(0.72f, 0.68f, 0.6f, 1f);
    private static readonly Color DarkDustColor = new Color(0.5f, 0.46f, 0.4f, 1f);

    private ParticleSystem system;

    public static ParticleFx Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        BuildSystem();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Faiscas ao acertar um golpe; direction e o sentido do empurrao (-1 ou 1).</summary>
    public static void Hit(Vector3 position, float direction)
    {
        Emit(position, 6, Spark, Orange, 3f, 6f, 0.07f, 0.14f, 0.15f, 0.3f, DirectionAngle(direction), 55f);
        Emit(position, 5, Blood, DarkBlood, 2f, 5f, 0.08f, 0.16f, 0.25f, 0.5f, DirectionAngle(direction), 70f);
    }

    public static void Kill(Vector3 position, float direction)
    {
        Emit(position, 16, Blood, DarkBlood, 2.5f, 7f, 0.1f, 0.2f, 0.35f, 0.7f, DirectionAngle(direction), 90f);
        Emit(position, 8, Spark, Orange, 3f, 7f, 0.07f, 0.14f, 0.2f, 0.4f, 90f, 180f);
    }

    public static void PowerBurst(Vector3 position)
    {
        Emit(position, 26, Orange, Spark, 4f, 9f, 0.1f, 0.2f, 0.25f, 0.5f, 0f, 180f);
    }

    public static void PlayerHurt(Vector3 position)
    {
        Emit(position, 14, Red, Orange, 3f, 7f, 0.09f, 0.18f, 0.25f, 0.55f, 90f, 180f);
    }

    public static void Dust(Vector3 feetPosition, int count)
    {
        Emit(feetPosition, count, DustColor, DarkDustColor, 0.6f, 2f, 0.1f, 0.2f, 0.25f, 0.5f, 90f, 80f);
    }

    public static void Heal(Vector3 position)
    {
        Emit(position, 18, Green, Spark, 1.5f, 4f, 0.08f, 0.16f, 0.5f, 0.9f, 90f, 70f);
    }

    public static void Sparkle(Vector3 position)
    {
        Emit(position, 6, Spark, Color.white, 0.8f, 2f, 0.06f, 0.12f, 0.3f, 0.5f, 90f, 180f);
    }

    public static void BossDeath(Vector3 position)
    {
        Emit(position, 50, Purple, Red, 3f, 11f, 0.14f, 0.3f, 0.5f, 1.1f, 90f, 180f);
        Emit(position, 24, Spark, Orange, 4f, 10f, 0.1f, 0.2f, 0.3f, 0.7f, 90f, 180f);
    }

    public static void PowerUpBurst(Vector3 position, Color color)
    {
        Emit(position, 16, color, Color.white, 1.5f, 4.5f, 0.08f, 0.16f, 0.4f, 0.8f, 90f, 180f);
    }

    private static float DirectionAngle(float direction)
    {
        return direction >= 0f ? 0f : 180f;
    }

    private static void Emit(
        Vector3 position,
        int count,
        Color colorA,
        Color colorB,
        float minSpeed,
        float maxSpeed,
        float minSize,
        float maxSize,
        float minLife,
        float maxLife,
        float centerAngle,
        float spread)
    {
        if (Instance == null || Instance.system == null)
        {
            return;
        }

        ParticleSystem.EmitParams parameters = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            float angle = (centerAngle + Random.Range(-spread, spread)) * Mathf.Deg2Rad;
            float speed = Random.Range(minSpeed, maxSpeed);
            parameters.position = position;
            parameters.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;
            parameters.startColor = Color.Lerp(colorA, colorB, Random.value);
            parameters.startSize = Random.Range(minSize, maxSize);
            parameters.startLifetime = Random.Range(minLife, maxLife);
            Instance.system.Emit(parameters, 1);
        }
    }

    private void BuildSystem()
    {
        system = gameObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = MaxParticles;
        main.gravityModifier = 0.8f;
        main.startLifetime = 0.5f;
        main.startSpeed = 0f;
        main.startSize = 0.1f;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        ParticleSystemRenderer particleRenderer = GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = SortingOrder;
        particleRenderer.material = new Material(Shader.Find("Sprites/Default"));

        system.Play();
    }
}
