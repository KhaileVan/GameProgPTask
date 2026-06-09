using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource bgmSource;
    private AudioSource bassSource;
    private AudioSource sfxSource;

    private AudioClip synthNoteClip;
    private AudioClip bassNoteClip;
    private AudioClip jumpClip;
    private AudioClip attackClip;
    private AudioClip hurtClip;
    private AudioClip pickupClip;

    // A minor pentatonic: A (0), C (3), D (5), E (7), G (10), A (12)
    private static readonly int[] melodyNotes = {
        0, 3, 5, 7, 10, 7, 5, 3,
        0, 3, 5, 7, 12, 10, 7, 5,
        7, 7, 10, 7, 12, 12, 15, 12,
        10, 7, 5, 3, 0, 3, 0, -2
    };

    private static readonly int[] bassNotes = {
        0, 3, 5, 7,
        0, 3, 5, 7,
        7, 10, 12, 10,
        5, 3, 0, 0
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("AudioManager");
            obj.AddComponent<AudioManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create and configure audio sources
        bgmSource = gameObject.AddComponent<AudioSource>();
        bassSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.volume = 0.12f;
        bassSource.volume = 0.18f;
        sfxSource.volume = 0.35f;

        // Generate procedural audio clips
        synthNoteClip = CreateSynthNoteClip();
        bassNoteClip = CreateBassNoteClip();
        jumpClip = CreateJumpSound();
        attackClip = CreateAttackSound();
        hurtClip = CreateHurtSound();
        pickupClip = CreatePickupSound();

        // Start BGM loop
        StartCoroutine(PlayBGMCo());
    }

    private IEnumerator PlayBGMCo()
    {
        int step = 0;
        while (true)
        {
            // Play melody note
            int melodyNote = melodyNotes[step % melodyNotes.Length];
            if (melodyNote != -99)
            {
                float pitch = Mathf.Pow(2f, melodyNote / 12f);
                bgmSource.pitch = pitch;
                bgmSource.PlayOneShot(synthNoteClip);
            }

            // Play bass note every 4 steps
            if (step % 4 == 0)
            {
                int bassNote = bassNotes[(step / 4) % bassNotes.Length];
                float pitch = Mathf.Pow(2f, bassNote / 12f);
                bassSource.pitch = pitch;
                bassSource.PlayOneShot(bassNoteClip);
            }

            step++;
            yield return new WaitForSeconds(0.25f); // 120 BPM sixteenth notes grid
        }
    }

    public void PlayJump()
    {
        if (sfxSource != null && jumpClip != null)
        {
            sfxSource.PlayOneShot(jumpClip);
        }
    }

    public void PlayAttack()
    {
        if (sfxSource != null && attackClip != null)
        {
            sfxSource.PlayOneShot(attackClip);
        }
    }

    public void PlayHurt()
    {
        if (sfxSource != null && hurtClip != null)
        {
            sfxSource.PlayOneShot(hurtClip);
        }
    }

    public void PlayPickup()
    {
        if (sfxSource != null && pickupClip != null)
        {
            sfxSource.PlayOneShot(pickupClip);
        }
    }

    private AudioClip CreateSynthNoteClip()
    {
        int sampleRate = 44100;
        float duration = 0.4f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = 440f; // A4 reference pitch
            float phase = 2f * Mathf.PI * freq * ((float)i / sampleRate);
            
            // Triangle wave for retro lead synth
            float tri = Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f;
            float envelope = Mathf.Exp(-6f * t); // Decays relatively quickly
            samples[i] = tri * 0.15f * envelope;
        }

        AudioClip clip = AudioClip.Create("SynthNote", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateBassNoteClip()
    {
        int sampleRate = 44100;
        float duration = 0.8f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = 110f; // A2 bass reference pitch
            float phase = 2f * Mathf.PI * freq * ((float)i / sampleRate);
            
            // Square wave for fat retro backing bass
            float sqr = Mathf.Sin(phase) > 0f ? 0.2f : -0.2f;
            float envelope = Mathf.Exp(-4f * t);
            samples[i] = sqr * 0.2f * envelope;
        }

        AudioClip clip = AudioClip.Create("BassNote", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateJumpSound()
    {
        int sampleRate = 44100;
        float duration = 0.15f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            // Sweep up pitch from 200Hz to 700Hz
            float freq = Mathf.Lerp(200f, 700f, t);
            float phase = 2f * Mathf.PI * freq * ((float)i / sampleRate);
            
            float sqr = Mathf.Sin(phase) > 0f ? 0.15f : -0.15f;
            samples[i] = sqr * (1f - t);
        }

        AudioClip clip = AudioClip.Create("JumpSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateAttackSound()
    {
        int sampleRate = 44100;
        float duration = 0.12f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            // Sweep down pitch from 900Hz to 150Hz
            float freq = Mathf.Lerp(900f, 150f, t);
            float phase = 2f * Mathf.PI * freq * ((float)i / sampleRate);
            
            float sqr = Mathf.Sin(phase) > 0f ? 0.1f : -0.1f;
            float noise = UnityEngine.Random.Range(-0.08f, 0.08f);
            samples[i] = (sqr + noise) * (1f - t);
        }

        AudioClip clip = AudioClip.Create("AttackSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateHurtSound()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            // Downward sweep with noise for impact crash
            float freq = Mathf.Lerp(300f, 70f, t);
            float phase = 2f * Mathf.PI * freq * ((float)i / sampleRate);
            
            float sqr = Mathf.Sin(phase) > 0f ? 0.15f : -0.15f;
            float noise = UnityEngine.Random.Range(-0.15f, 0.15f);
            samples[i] = (sqr + noise) * (1f - t);
        }

        AudioClip clip = AudioClip.Create("HurtSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreatePickupSound()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            // Arpeggio sound: E5 -> G#5 -> B5 -> E6
            float freq;
            if (t < 0.25f) freq = 659.25f;      // E5
            else if (t < 0.5f) freq = 830.61f; // G#5
            else if (t < 0.75f) freq = 987.77f; // B5
            else freq = 1318.51f;              // E6

            float phase = 2f * Mathf.PI * freq * ((float)i / sampleRate);
            float tri = Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f;
            samples[i] = tri * 0.12f * (1f - t);
        }

        AudioClip clip = AudioClip.Create("PickupSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
