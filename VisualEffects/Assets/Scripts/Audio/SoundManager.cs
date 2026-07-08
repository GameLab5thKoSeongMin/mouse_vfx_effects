// Code responsibility: provide a persistent singleton for scene loop audio, ID-based SFX playback,
// pooled SFX AudioSources, and user volume settings stored in PlayerPrefs.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SoundManager : MonoBehaviour
{
    private sealed class PooledSfxSource
    {
        public AudioSource Source;
        public float LastStartedAt;
    }

    public const string MasterVolumeKey = "Sound.MasterVolume";
    public const string LoopVolumeKey = "Sound.LoopVolume";
    public const string SfxVolumeKey = "Sound.SfxVolume";

    private static SoundManager instance;

    [Header("Scene Loop Table")]
    [SerializeField] private List<SceneLoopEntry> sceneLoopEntries = new List<SceneLoopEntry>();
    [SerializeField] private bool stopLoopWhenSceneHasNoEntry = true;

    [Header("Loop Source")]
    [SerializeField] private AudioSource loopSource;
    [SerializeField] private bool useLoopFade = true;
    [SerializeField, Min(0f)] private float loopFadeDuration = 0.5f;

    [Header("SFX Library")]
    [SerializeField] private List<SoundEntry> sfxLibrary = new List<SoundEntry>();

    [Header("SFX Pool")]
    [SerializeField, Min(1)] private int initialSfxPoolSize = 8;
    [SerializeField, Min(1)] private int maxSfxPoolSize = 24;
    [SerializeField] private bool allowPoolExpansion = true;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float loopVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool loadSavedVolumeOnAwake = true;

    private readonly Dictionary<string, SoundEntry> sfxById = new Dictionary<string, SoundEntry>();
    private readonly Dictionary<string, SceneLoopEntry> loopBySceneName = new Dictionary<string, SceneLoopEntry>();
    private readonly List<PooledSfxSource> sfxSources = new List<PooledSfxSource>();
    private Transform sfxPoolRoot;
    private Coroutine loopFadeRoutine;
    private AudioClip currentLoopClip;
    private string currentLoopSceneName;
    private float currentLoopEntryVolume = 1f;

    public static SoundManager Instance
    {
        get { return instance; }
    }

    public AudioClip CurrentLoopClip
    {
        get { return currentLoopClip; }
    }

    public string CurrentLoopSceneName
    {
        get { return currentLoopSceneName; }
    }

    public AudioSource LoopSource
    {
        get { return loopSource; }
    }

    public int SfxPoolCount
    {
        get { return sfxSources.Count; }
    }

    public int InitialSfxPoolSize
    {
        get { return initialSfxPoolSize; }
    }

    public int MaxSfxPoolSize
    {
        get { return maxSfxPoolSize; }
    }

    public bool AllowPoolExpansion
    {
        get { return allowPoolExpansion; }
    }

    public IReadOnlyList<SoundEntry> SfxLibrary
    {
        get { return sfxLibrary; }
    }

    public IReadOnlyList<SceneLoopEntry> SceneLoopEntries
    {
        get { return sceneLoopEntries; }
    }

    public int SceneLoopEntryCount
    {
        get { return sceneLoopEntries.Count; }
    }

    public int SfxEntryCount
    {
        get { return sfxLibrary.Count; }
    }

    public int ActiveSfxSourceCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < sfxSources.Count; i++)
            {
                if (sfxSources[i].Source != null && sfxSources[i].Source.isPlaying)
                {
                    count++;
                }
            }

            return count;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Duplicate SoundManager found. Destroying the new instance: " + name, this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureLoopSource();
        BuildSceneLoopTable();
        BuildSfxLibrary();
        BuildSfxPool();

        if (loadSavedVolumeOnAwake)
        {
            LoadVolumeSettings();
        }
        else
        {
            ApplyLoopVolume();
        }
    }

    private void OnEnable()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
    }

    private void Start()
    {
        if (instance == this)
        {
            PlayLoopForScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void OnValidate()
    {
        initialSfxPoolSize = Mathf.Max(1, initialSfxPoolSize);
        maxSfxPoolSize = Mathf.Max(initialSfxPoolSize, maxSfxPoolSize);
        masterVolume = Mathf.Clamp01(masterVolume);
        loopVolume = Mathf.Clamp01(loopVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        loopFadeDuration = Mathf.Max(0f, loopFadeDuration);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayLoopForScene(scene.name);
    }

    /// <summary>Plays an SFX by ID using the default volume scale.</summary>
    public void PlaySfx(string sfxId)
    {
        PlaySfx(sfxId, 1f);
    }

    /// <summary>Plays an SFX by ID with an extra per-call volume multiplier.</summary>
    public void PlaySfx(string sfxId, float volumeScale)
    {
        if (string.IsNullOrWhiteSpace(sfxId))
        {
            Debug.LogWarning("SoundManager.PlaySfx was called with an empty SFX ID.", this);
            return;
        }

        if (sfxById.Count == 0 && sfxLibrary.Count > 0)
        {
            BuildSfxLibrary();
        }

        SoundEntry entry;
        if (!sfxById.TryGetValue(sfxId, out entry))
        {
            Debug.LogWarning("SFX ID not found: " + sfxId, this);
            return;
        }

        PlaySfxInternal(entry.Clip, Mathf.Clamp01(entry.Volume * volumeScale), entry.GetRandomPitch());
    }

    /// <summary>Plays a direct AudioClip through the SFX pool.</summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        PlaySfxInternal(clip, volumeScale, 1f);
    }

    /// <summary>Convenience API for the default button click SFX.</summary>
    public void PlayButtonClick()
    {
        PlaySfx("button_click");
    }

    /// <summary>Convenience API for the default object click SFX.</summary>
    public void PlayObjectClick()
    {
        PlaySfx("object_click");
    }

    /// <summary>Sets the global volume multiplier used by loop audio and SFX.</summary>
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyLoopVolume();
    }

    /// <summary>Sets the scene loop audio volume multiplier.</summary>
    public void SetLoopVolume(float value)
    {
        loopVolume = Mathf.Clamp01(value);
        ApplyLoopVolume();
    }

    /// <summary>Sets the SFX volume multiplier used for future SFX playback.</summary>
    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }

    /// <summary>Returns the current global volume multiplier.</summary>
    public float GetMasterVolume()
    {
        return masterVolume;
    }

    /// <summary>Returns the current scene loop audio volume multiplier.</summary>
    public float GetLoopVolume()
    {
        return loopVolume;
    }

    /// <summary>Returns the current SFX volume multiplier.</summary>
    public float GetSfxVolume()
    {
        return sfxVolume;
    }

    /// <summary>Saves user volume settings to PlayerPrefs.</summary>
    public void SaveVolumeSettings()
    {
        // PlayerPrefs is used here only for user settings, not for game progress data.
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(LoopVolumeKey, loopVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    /// <summary>Loads user volume settings from PlayerPrefs, defaulting missing keys to 1.0.</summary>
    public void LoadVolumeSettings()
    {
        // PlayerPrefs is appropriate for small user preference values such as audio volume.
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        loopVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(LoopVolumeKey, 1f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        ApplyLoopVolume();
    }

    /// <summary>Removes only the SoundManager volume keys from PlayerPrefs.</summary>
    public void ClearVolumeSettings()
    {
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(LoopVolumeKey);
        PlayerPrefs.DeleteKey(SfxVolumeKey);
        PlayerPrefs.Save();
    }

    public static int CountLiveInstances()
    {
        return FindObjectsByType<SoundManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    public string GetCurrentLoopClipName()
    {
        return currentLoopClip != null ? currentLoopClip.name : "None";
    }

    public AudioClip GetCurrentLoopClip()
    {
        return currentLoopClip;
    }

    public string GetCurrentLoopSceneName()
    {
        return string.IsNullOrEmpty(currentLoopSceneName) ? "None" : currentLoopSceneName;
    }

    public bool HasLoopEntryForScene(string sceneName)
    {
        if (loopBySceneName.Count == 0 && sceneLoopEntries.Count > 0)
        {
            BuildSceneLoopTable();
        }

        return !string.IsNullOrWhiteSpace(sceneName) && loopBySceneName.ContainsKey(sceneName);
    }

    public bool TryPlayLoopForScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SoundManager.TryPlayLoopForScene was called with an empty scene name.", this);
            return false;
        }

        if (loopBySceneName.Count == 0 && sceneLoopEntries.Count > 0)
        {
            BuildSceneLoopTable();
        }

        SceneLoopEntry entry;
        if (!loopBySceneName.TryGetValue(sceneName, out entry))
        {
            // Default policy: scenes without a registered loop entry stop the loop source.
            // This prevents previous-scene BGM from leaking into screens that intentionally have no loop.
            if (stopLoopWhenSceneHasNoEntry)
            {
                StopLoopWithOptionalFade();
            }

            return false;
        }

        PlayLoopEntry(sceneName, entry);
        return true;
    }

    private void PlayLoopForScene(string sceneName)
    {
        TryPlayLoopForScene(sceneName);
    }

    private void PlayLoopEntry(string sceneName, SceneLoopEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("SoundManager tried to play a null SceneLoopEntry.", this);
            return;
        }

        AudioClip clip = entry.LoopClip;
        if (clip == null)
        {
            Debug.LogWarning("Scene loop entry has no AudioClip assigned: " + entry.SceneName, this);
            return;
        }

        EnsureLoopSource();

        if (currentLoopClip == clip && loopSource.clip == clip && loopSource.isPlaying)
        {
            currentLoopSceneName = sceneName;
            currentLoopEntryVolume = entry.Volume;
            ApplyLoopVolume();
            return;
        }

        if (loopFadeRoutine != null)
        {
            StopCoroutine(loopFadeRoutine);
            loopFadeRoutine = null;
        }

        if (useLoopFade && loopFadeDuration > 0f && loopSource.isPlaying)
        {
            loopFadeRoutine = StartCoroutine(FadeToLoopClip(sceneName, clip, entry.Volume));
            return;
        }

        SetLoopClipImmediately(sceneName, clip, entry.Volume);
    }

    private void StopLoopWithOptionalFade()
    {
        if (loopSource == null || !loopSource.isPlaying)
        {
            if (loopSource != null)
            {
                loopSource.Stop();
                loopSource.clip = null;
            }

            currentLoopClip = null;
            currentLoopSceneName = null;
            return;
        }

        if (loopFadeRoutine != null)
        {
            StopCoroutine(loopFadeRoutine);
            loopFadeRoutine = null;
        }

        if (useLoopFade && loopFadeDuration > 0f)
        {
            loopFadeRoutine = StartCoroutine(FadeOutAndStopLoop());
            return;
        }

        loopSource.Stop();
        loopSource.clip = null;
        currentLoopClip = null;
        currentLoopSceneName = null;
    }

    private IEnumerator FadeToLoopClip(string sceneName, AudioClip nextClip, float entryVolume)
    {
        yield return FadeLoopVolume(loopSource.volume, 0f, loopFadeDuration * 0.5f);
        SetLoopClipImmediately(sceneName, nextClip, entryVolume);
        loopSource.volume = 0f;
        yield return FadeLoopVolume(0f, GetEffectiveLoopVolume(), loopFadeDuration * 0.5f);
        loopFadeRoutine = null;
    }

    private IEnumerator FadeOutAndStopLoop()
    {
        yield return FadeLoopVolume(loopSource.volume, 0f, loopFadeDuration);
        loopSource.Stop();
        loopSource.clip = null;
        currentLoopClip = null;
        currentLoopSceneName = null;
        loopFadeRoutine = null;
    }

    private IEnumerator FadeLoopVolume(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            loopSource.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            loopSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        loopSource.volume = to;
    }

    private void SetLoopClipImmediately(string sceneName, AudioClip clip, float entryVolume)
    {
        currentLoopClip = clip;
        currentLoopSceneName = sceneName;
        currentLoopEntryVolume = Mathf.Clamp01(entryVolume);
        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.volume = GetEffectiveLoopVolume();
        loopSource.pitch = 1f;
        loopSource.Play();
    }

    private void PlaySfxInternal(AudioClip clip, float volumeScale, float pitch)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundManager tried to play a null SFX clip.", this);
            return;
        }

        PooledSfxSource pooledSource = GetAvailableSfxSource();
        if (pooledSource == null)
        {
            // This implementation drops the SFX when the fixed pool is exhausted.
            // It avoids cutting off an already-playing sound and keeps BGM/loop playback stable.
            Debug.LogWarning("No available SFX AudioSource. Increase maxSfxPoolSize or enable allowPoolExpansion.", this);
            return;
        }

        AudioSource source = pooledSource.Source;
        source.Stop();
        source.clip = clip;
        source.loop = false;
        source.playOnAwake = false;
        source.volume = Mathf.Clamp01(masterVolume * sfxVolume * Mathf.Clamp01(volumeScale));
        source.pitch = Mathf.Max(0.01f, pitch);
        pooledSource.LastStartedAt = Time.unscaledTime;
        source.Play();
    }

    private PooledSfxSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            if (!sfxSources[i].Source.isPlaying)
            {
                return sfxSources[i];
            }
        }

        if (allowPoolExpansion && sfxSources.Count < maxSfxPoolSize)
        {
            return CreateSfxSource(sfxSources.Count);
        }

        return null;
    }

    private void EnsureLoopSource()
    {
        if (loopSource == null)
        {
            loopSource = GetComponent<AudioSource>();
        }

        if (loopSource == null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
        }

        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;
        loopSource.volume = GetEffectiveLoopVolume();
    }

    private void BuildSceneLoopTable()
    {
        loopBySceneName.Clear();

        for (int i = 0; i < sceneLoopEntries.Count; i++)
        {
            SceneLoopEntry entry = sceneLoopEntries[i];
            if (entry == null)
            {
                Debug.LogWarning("SoundManager has a null SceneLoopEntry at index " + i + ".", this);
                continue;
            }

            if (!entry.HasValidSceneName)
            {
                Debug.LogWarning("SceneLoopEntry has an empty sceneName at index " + i + ".", this);
                continue;
            }

            if (!entry.HasValidLoopClip)
            {
                Debug.LogWarning("SceneLoopEntry has no loopClip assigned: " + entry.SceneName, this);
            }

            if (loopBySceneName.ContainsKey(entry.SceneName))
            {
                Debug.LogWarning("Duplicate SceneLoopEntry sceneName ignored: " + entry.SceneName, this);
                continue;
            }

            loopBySceneName.Add(entry.SceneName, entry);
        }
    }

    private void BuildSfxLibrary()
    {
        sfxById.Clear();

        for (int i = 0; i < sfxLibrary.Count; i++)
        {
            SoundEntry entry = sfxLibrary[i];
            if (entry == null || !entry.HasValidId)
            {
                Debug.LogWarning("SoundManager has an SFX entry with an empty ID at index " + i + ".", this);
                continue;
            }

            if (sfxById.ContainsKey(entry.Id))
            {
                Debug.LogWarning("Duplicate SFX ID ignored: " + entry.Id, this);
                continue;
            }

            sfxById.Add(entry.Id, entry);
        }
    }

    private void BuildSfxPool()
    {
        if (sfxPoolRoot == null)
        {
            GameObject root = new GameObject("SFX_Source_Pool");
            root.transform.SetParent(transform, false);
            sfxPoolRoot = root.transform;
        }

        int targetCount = Mathf.Min(initialSfxPoolSize, maxSfxPoolSize);
        while (sfxSources.Count < targetCount)
        {
            CreateSfxSource(sfxSources.Count);
        }
    }

    private PooledSfxSource CreateSfxSource(int index)
    {
        GameObject sourceObject = new GameObject("SFX_Source_" + index.ToString("00"));
        sourceObject.transform.SetParent(sfxPoolRoot != null ? sfxPoolRoot : transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;

        PooledSfxSource pooledSource = new PooledSfxSource
        {
            Source = source,
            LastStartedAt = -1f
        };
        sfxSources.Add(pooledSource);
        return pooledSource;
    }

    private void ApplyLoopVolume()
    {
        if (loopSource != null && loopFadeRoutine == null)
        {
            loopSource.volume = GetEffectiveLoopVolume();
        }
    }

    private float GetEffectiveLoopVolume()
    {
        return Mathf.Clamp01(masterVolume * loopVolume * currentLoopEntryVolume);
    }
}
