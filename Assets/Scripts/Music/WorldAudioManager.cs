using UnityEngine;
using UnityEngine.Audio;

public class WorldAudioManager : MonoBehaviour
{
    public static WorldAudioManager Instance { get; private set; }

    private const string MasterVolumeParameter = "MasterVolume";
    private const string BgmVolumeParameter = "BGMVolume";
    private const string SfxVolumeParameter = "SFXVolume";

    public AudioSource backgroundMusicSource;
    public AudioSource sfxSource;
    public AudioMixer audioMixer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayBackgroundMusic(backgroundMusicSource.clip);
    }

    public void PlayBackgroundMusic(AudioClip clip)
    {
        if (backgroundMusicSource.clip == clip)
            return;

        backgroundMusicSource.clip = clip;
        backgroundMusicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    public void SetMasterVolume(float value)
    {
        SetVolume(MasterVolumeParameter, value);
    }

    public void SetBGMVolume(float value)
    {
        SetVolume(BgmVolumeParameter, value);
    }

    public void SetSFXVolume(float value)
    {
        SetVolume(SfxVolumeParameter, value);
    }

    private void SetVolume(string parameterName, float value)
    {
        if (audioMixer == null)
            return;

        float decibels = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(parameterName, decibels);
    }
}
