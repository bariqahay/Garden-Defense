using UnityEngine;

/// <summary>
/// Persistent Audio Manager - survives scene transitions
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music Tracks")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip winMusic;
    public AudioClip loseMusic;
    public AudioClip bossMusic; // Optional: untuk boss wave

    [Header("Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.7f;
    public bool fadeTransitions = true;
    public float fadeDuration = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip currentTrack;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // PENTING: Ga destroy saat ganti scene
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void SetupAudioSources()
    {
        // Music Source (looping background music)
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        // SFX Source (one-shot sounds)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        Debug.Log("[AudioManager] Initialized");
    }

    /// <summary>
    /// Play music track
    /// </summary>
    public void PlayMusic(AudioClip clip, bool forceRestart = false)
    {
        if (clip == null) return;

        // Jangan restart kalau music yang sama masih playing
        if (!forceRestart && currentTrack == clip && musicSource.isPlaying)
        {
            Debug.Log($"[AudioManager] Already playing: {clip.name}");
            return;
        }

        if (fadeTransitions && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutAndPlayNew(clip));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
            currentTrack = clip;
            Debug.Log($"[AudioManager] Playing music: {clip.name}");
        }
    }

    /// <summary>
    /// Play menu music
    /// </summary>
    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    /// <summary>
    /// Play gameplay music
    /// </summary>
    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    /// <summary>
    /// Play win music (short fanfare, non-looping)
    /// </summary>
    public void PlayWinMusic()
    {
        if (winMusic == null) return;

        musicSource.loop = false; // Win music ga loop
        PlayMusic(winMusic, forceRestart: true);
    }

    /// <summary>
    /// Play lose music
    /// </summary>
    public void PlayLoseMusic()
    {
        if (loseMusic == null) return;

        musicSource.loop = false; // Lose music ga loop
        PlayMusic(loseMusic, forceRestart: true);
    }

    /// <summary>
    /// Play boss music (optional)
    /// </summary>
    public void PlayBossMusic()
    {
        if (bossMusic == null)
        {
            Debug.LogWarning("[AudioManager] Boss music not assigned!");
            return;
        }

        musicSource.loop = true; // Boss music loop
        PlayMusic(bossMusic);
    }

    /// <summary>
    /// Stop music
    /// </summary>
    public void StopMusic(bool fade = true)
    {
        if (fade && fadeTransitions)
        {
            StartCoroutine(FadeOut());
        }
        else
        {
            musicSource.Stop();
            currentTrack = null;
        }
    }

    /// <summary>
    /// Play sound effect (one-shot)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
    }

    /// <summary>
    /// Set music volume
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    /// <summary>
    /// Set SFX volume
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    /// <summary>
    /// Pause music (untuk pause menu)
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            Debug.Log("[AudioManager] Music paused");
        }
    }

    /// <summary>
    /// Resume music (dari pause menu)
    /// </summary>
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.UnPause();
            Debug.Log("[AudioManager] Music resumed");
        }
    }

    /// <summary>
    /// Check if music is currently playing
    /// </summary>
    public bool IsMusicPlaying()
    {
        return musicSource.isPlaying;
    }

    // ========== FADE TRANSITIONS ==========

    System.Collections.IEnumerator FadeOut()
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = musicVolume;
        currentTrack = null;
    }

    System.Collections.IEnumerator FadeOutAndPlayNew(AudioClip newClip)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        // Fade out
        while (elapsed < fadeDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration * 0.5f));
            yield return null;
        }

        // Switch track
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();
        currentTrack = newClip;

        // Fade in
        elapsed = 0f;
        while (elapsed < fadeDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / (fadeDuration * 0.5f));
            yield return null;
        }

        musicSource.volume = musicVolume;
    }
}