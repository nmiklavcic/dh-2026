using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance 
    { 
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SoundManager>();
                if (instance == null)
                {
                    GameObject soundManagerObj = new GameObject("SoundManager");
                    instance = soundManagerObj.AddComponent<SoundManager>();
                }
            }
            return instance;
        }
    }
    
    private static SoundManager instance;
    private AudioSource audioSource;
    private AudioSource loopAudioSource;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Create separate audio source for looping sounds
        loopAudioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlaySound(string soundName)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource not initialized");
            return;
        }
        
        AudioClip clip = LoadAudioClip(soundName, "Effects");
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"Sound not found: {soundName}");
        }
    }

    public void PlaySoundLoop(string soundName, float volume = 1f)
    {
        if (loopAudioSource == null)
        {
            Debug.LogError("Loop AudioSource not initialized");
            return;
        }

        AudioClip clip = LoadAudioClip(soundName, "Ambient");
        if (clip != null)
        {
            loopAudioSource.clip = clip;
            loopAudioSource.loop = true;
            loopAudioSource.volume = volume;
            loopAudioSource.Play();
            Debug.Log($"Playing loop: {soundName}");
        }
        else
        {
            Debug.LogWarning($"Sound not found: {soundName}");
        }
    }

    public void StopLoop()
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
            loopAudioSource.clip = null;
        }
    }

    private AudioClip LoadAudioClip(string soundName, string folder)
    {
#if UNITY_EDITOR
        // In editor, use AssetDatabase to load from actual path
        string assetPath = $"Assets/Audio/{folder}/{soundName}.wav";
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
#else
        // At runtime, need to use Resources - would require moving Audio to Resources folder
        return Resources.Load<AudioClip>($"Audio/{folder}/{soundName}");
#endif
    }

    public void PlayAudioClip(AudioClip clip, float volume = 100f)
{
    if (audioSource == null)
    {
        Debug.LogError("AudioSource not initialized");
        return;
    }
    
    audioSource.volume = volume;
    audioSource.PlayOneShot(clip);
}
}