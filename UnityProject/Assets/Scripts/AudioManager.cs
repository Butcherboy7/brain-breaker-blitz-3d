using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton AudioManager — procedurally synthesizes all game sounds.
/// No audio clips needed: every sound is built with AnimationCurves on AudioSources.
/// Attach to a "AudioManager" GameObject (AutoSetupGame does this automatically).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume  = 0.85f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.40f;

    // ── Pooled sources ───────────────────────────────────────
    private const int POOL_SIZE = 12;
    private AudioSource[] sfxPool;
    private int poolIndex;

    // ── Music ────────────────────────────────────────────────
    private AudioSource musicSource;
    private Coroutine   musicCoroutine;

    // ── Combo pitch tracking ─────────────────────────────────
    private int lastCombo;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // Build SFX pool
        sfxPool = new AudioSource[POOL_SIZE];
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var go = new GameObject("SFX_" + i);
            go.transform.SetParent(transform);
            sfxPool[i] = go.AddComponent<AudioSource>();
            sfxPool[i].playOnAwake = false;
            sfxPool[i].spatialBlend = 0f; // 2D
        }

        // Music source
        var mgo = new GameObject("Music");
        mgo.transform.SetParent(transform);
        musicSource = mgo.AddComponent<AudioSource>();
        musicSource.loop       = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;
    }

    // ─────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────

    /// <summary>Play a brick-break tone, pitch rises with combo level.</summary>
    public void PlayBrickBreak(int combo = 1)
    {
        float pitch = 1f + Mathf.Clamp((combo - 1) * 0.07f, 0f, 0.8f);
        float freq  = Mathf.Lerp(440f, 880f, Mathf.Clamp01((combo - 1) / 10f));
        StartCoroutine(PlayToneCoroutine(GetSource(), freq, 0.12f, pitch, sfxVolume * 0.6f, ToneShape.BrickBreak));
    }

    /// <summary>Play paddle-bounce sound.</summary>
    public void PlayPaddleBounce()
    {
        StartCoroutine(PlayToneCoroutine(GetSource(), 220f, 0.07f, 1.0f, sfxVolume * 0.5f, ToneShape.Bounce));
    }

    /// <summary>Play wall-bounce (quieter, softer).</summary>
    public void PlayWallBounce()
    {
        StartCoroutine(PlayToneCoroutine(GetSource(), 180f, 0.05f, 0.9f, sfxVolume * 0.25f, ToneShape.Bounce));
    }

    /// <summary>Play power-up collect jingle.</summary>
    public void PlayPowerUpCollect()
    {
        StartCoroutine(PowerUpJingle());
    }

    /// <summary>Play life-lost dramatic sound.</summary>
    public void PlayLifeLost()
    {
        StartCoroutine(LifeLostSequence());
    }

    /// <summary>Play level-complete fanfare.</summary>
    public void PlayLevelComplete()
    {
        StartCoroutine(LevelCompleteJingle());
    }

    /// <summary>Play game-over sound.</summary>
    public void PlayGameOver()
    {
        StartCoroutine(GameOverSequence());
    }

    /// <summary>Play UI click tick.</summary>
    public void PlayUIClick()
    {
        StartCoroutine(PlayToneCoroutine(GetSource(), 600f, 0.04f, 1.1f, sfxVolume * 0.3f, ToneShape.Click));
    }

    /// <summary>Play Countdown beep.</summary>
    public void PlayCountdownBeep(int step)
    {
        if (step < 3) 
            StartCoroutine(PlayToneCoroutine(GetSource(), 440f, 0.2f, 1f, sfxVolume * 0.6f, ToneShape.Click));
        else 
            StartCoroutine(PlayToneCoroutine(GetSource(), 880f, 0.4f, 1f, sfxVolume * 0.6f, ToneShape.Click));
    }

    /// <summary>Play combo milestone celebration.</summary>
    public void PlayComboMilestone(int combo)
    {
        StartCoroutine(ComboFanfare(combo));
    }

    /// <summary>Start looping background music.</summary>
    public void StartMusic()
    {
        if (musicCoroutine != null) StopCoroutine(musicCoroutine);
        musicCoroutine = StartCoroutine(GenerateMusicLoop());
    }

    /// <summary>Stop background music.</summary>
    public void StopMusic()
    {
        if (musicCoroutine != null) { StopCoroutine(musicCoroutine); musicCoroutine = null; }
        musicSource.Stop();
    }

    public void SetSFXVolume(float v)  { sfxVolume   = Mathf.Clamp01(v); }
    public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); if (musicSource) musicSource.volume = v; }

    // ─────────────────────────────────────────────────────────
    //  INTERNAL: TONE GENERATOR
    // ─────────────────────────────────────────────────────────
    private enum ToneShape { BrickBreak, Bounce, Click }

    private IEnumerator PlayToneCoroutine(AudioSource src, float freq, float duration,
                                          float pitch, float vol, ToneShape shape)
    {
        if (src == null) yield break;

        int sampleRate = AudioSettings.outputSampleRate;
        int samples    = Mathf.CeilToInt(sampleRate * duration);
        var clip       = AudioClip.Create("tone", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / samples;
            float env = shape switch
            {
                ToneShape.BrickBreak => Mathf.Pow(1f - t, 1.5f),
                ToneShape.Bounce     => (t < 0.1f ? t / 0.1f : Mathf.Pow(1f - (t - 0.1f) / 0.9f, 2f)),
                ToneShape.Click      => Mathf.Pow(1f - t, 3f),
                _ => 1f - t
            };

            float snd = shape switch
            {
                ToneShape.BrickBreak => Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate)
                                      + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * i / sampleRate)
                                      + 0.15f * Random.Range(-1f, 1f), // noise crunch
                ToneShape.Bounce     => Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate),
                ToneShape.Click      => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate)),
                _ => Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate)
            };

            data[i] = Mathf.Clamp(snd * env * 0.5f, -1f, 1f);
        }

        clip.SetData(data, 0);
        src.clip   = clip;
        src.pitch  = pitch;
        src.volume = vol;
        src.Play();

        yield return new WaitForSeconds(duration + 0.05f);
        Destroy(clip);
    }

    // ─────────────────────────────────────────────────────────
    //  JINGLES
    // ─────────────────────────────────────────────────────────
    private IEnumerator PowerUpJingle()
    {
        float[] notes = { 523f, 659f, 784f, 1047f };
        foreach (var n in notes)
        {
            StartCoroutine(PlayToneCoroutine(GetSource(), n, 0.08f, 1f, sfxVolume * 0.5f, ToneShape.Bounce));
            yield return new WaitForSeconds(0.07f);
        }
    }

    private IEnumerator LifeLostSequence()
    {
        float[] notes = { 440f, 370f, 311f, 220f };
        foreach (var n in notes)
        {
            StartCoroutine(PlayToneCoroutine(GetSource(), n, 0.18f, 1f, sfxVolume * 0.7f, ToneShape.Bounce));
            yield return new WaitForSeconds(0.14f);
        }
    }

    private IEnumerator LevelCompleteJingle()
    {
        float[] notes = { 523f, 659f, 784f, 659f, 784f, 1047f };
        float[] durs  = { 0.1f, 0.1f, 0.1f, 0.08f, 0.08f, 0.25f };
        for (int i = 0; i < notes.Length; i++)
        {
            StartCoroutine(PlayToneCoroutine(GetSource(), notes[i], durs[i], 1f, sfxVolume * 0.6f, ToneShape.Bounce));
            yield return new WaitForSeconds(durs[i] + 0.01f);
        }
    }

    private IEnumerator GameOverSequence()
    {
        float[] notes = { 392f, 311f, 262f, 196f };
        foreach (var n in notes)
        {
            StartCoroutine(PlayToneCoroutine(GetSource(), n, 0.22f, 0.9f, sfxVolume * 0.7f, ToneShape.Bounce));
            yield return new WaitForSeconds(0.18f);
        }
    }

    private IEnumerator ComboFanfare(int combo)
    {
        float basePitch = 1f + combo * 0.05f;
        for (int i = 0; i < 3; i++)
        {
            float freq = 440f * Mathf.Pow(2f, i / 12f);
            StartCoroutine(PlayToneCoroutine(GetSource(), freq, 0.07f, basePitch, sfxVolume * 0.45f, ToneShape.Click));
            yield return new WaitForSeconds(0.06f);
        }
    }

    private IEnumerator GenerateMusicLoop()
    {
        // Simple arpeggiated bass-line loop
        float[] scale = { 130.8f, 155.6f, 174.6f, 196f, 220f, 261.6f };
        while (true)
        {
            foreach (var n in scale)
            {
                StartCoroutine(PlayToneCoroutine(GetSource(), n, 0.20f, 1f, musicVolume * 0.3f, ToneShape.Bounce));
                yield return new WaitForSeconds(0.22f);
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    //  POOL
    // ─────────────────────────────────────────────────────────
    private AudioSource GetSource()
    {
        // Round-robin pool: find a free source
        for (int i = 0; i < POOL_SIZE; i++)
        {
            int idx = (poolIndex + i) % POOL_SIZE;
            if (!sfxPool[idx].isPlaying)
            {
                poolIndex = idx;
                return sfxPool[idx];
            }
        }
        // All busy — steal oldest
        poolIndex = (poolIndex + 1) % POOL_SIZE;
        sfxPool[poolIndex].Stop();
        return sfxPool[poolIndex];
    }
}
