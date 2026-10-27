using System;
using UnityEngine;

public enum Sfx
{
    Punch,
    Power,
    Jump,
    PlayerHurt,
    PlayerDeath,
    ZombieHurt,
    ZombieDeath,
    UiClick,
    UiSelect,
    BossRoar
}

[Serializable]
public class SoundBank
{
    public AudioClip punch;
    public AudioClip power;
    public AudioClip jump;
    public AudioClip hurt;
    public AudioClip death;
    public AudioClip uiClick;
    public AudioClip uiSelect;
}

/// <summary>
/// Toca efeitos sonoros curtos com um conjunto fixo de AudioSources.
/// Os arquivos de efeito sao longos, entao cada som e cortado (com fade) apos uma duracao maxima.
/// </summary>
public class SfxPlayer : MonoBehaviour
{
    private const int VoiceCount = 8;
    private const float FadeSeconds = 0.1f;
    private const float PitchVariation = 0.05f;

    private struct Voice
    {
        public AudioSource Source;
        public float BaseVolume;
        public float StopAt;
    }

    private readonly struct Playback
    {
        public readonly AudioClip Clip;
        public readonly float Volume;
        public readonly float Pitch;
        public readonly float MaxSeconds;

        public Playback(AudioClip clip, float volume, float pitch, float maxSeconds)
        {
            Clip = clip;
            Volume = volume;
            Pitch = pitch;
            MaxSeconds = maxSeconds;
        }
    }

    private SoundBank bank;
    private Voice[] voices;
    private int nextVoice;

    public static SfxPlayer Instance { get; private set; }

    /// <summary>Sons de selecao de UI ficam mudos ate este frame (evita som ao abrir uma tela).</summary>
    public static int UiMutedUntilFrame { get; set; }

    public static void Play(Sfx sfx)
    {
        if (Instance != null)
        {
            Instance.PlayInternal(sfx);
        }
    }

    public void Configure(SoundBank soundBank)
    {
        bank = soundBank;
        voices = new Voice[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            voices[i].Source = source;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (voices == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        for (int i = 0; i < voices.Length; i++)
        {
            AudioSource source = voices[i].Source;
            if (!source.isPlaying)
            {
                continue;
            }

            float remaining = voices[i].StopAt - now;
            if (remaining <= 0f)
            {
                source.Stop();
            }
            else if (remaining < FadeSeconds)
            {
                source.volume = voices[i].BaseVolume * remaining / FadeSeconds;
            }
        }
    }

    private void PlayInternal(Sfx sfx)
    {
        if (bank == null || voices == null)
        {
            return;
        }

        if (sfx == Sfx.UiSelect && Time.frameCount <= UiMutedUntilFrame)
        {
            return;
        }

        Playback playback = Resolve(sfx);
        if (playback.Clip == null)
        {
            return;
        }

        int index = FindVoice();
        float pitch = playback.Pitch * (1f + UnityEngine.Random.Range(-PitchVariation, PitchVariation));
        AudioSource source = voices[index].Source;
        source.Stop();
        source.clip = playback.Clip;
        source.volume = playback.Volume;
        source.pitch = pitch;
        source.Play();

        voices[index].BaseVolume = playback.Volume;
        voices[index].StopAt = Time.unscaledTime + playback.MaxSeconds;
    }

    private int FindVoice()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            if (!voices[i].Source.isPlaying)
            {
                return i;
            }
        }

        nextVoice = (nextVoice + 1) % voices.Length;
        return nextVoice;
    }

    private Playback Resolve(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Punch:
                return new Playback(bank.punch, 0.9f, 1f, 0.7f);
            case Sfx.Power:
                return new Playback(bank.power, 1f, 1f, 1.1f);
            case Sfx.Jump:
                return new Playback(bank.jump, 0.7f, 1f, 0.6f);
            case Sfx.PlayerHurt:
                return new Playback(bank.hurt, 1f, 1f, 0.8f);
            case Sfx.PlayerDeath:
                return new Playback(bank.death, 1f, 1f, 1.8f);
            case Sfx.ZombieHurt:
                return new Playback(bank.hurt, 0.6f, 0.75f, 0.5f);
            case Sfx.ZombieDeath:
                return new Playback(bank.death, 0.6f, 1.25f, 1f);
            case Sfx.UiClick:
                return new Playback(bank.uiClick, 0.8f, 1f, 0.5f);
            case Sfx.UiSelect:
                return new Playback(bank.uiSelect, 0.6f, 1f, 0.3f);
            case Sfx.BossRoar:
                return new Playback(bank.power, 1f, 0.6f, 1.5f);
            default:
                return default;
        }
    }
}
