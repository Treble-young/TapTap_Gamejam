using UnityEngine;

public class WorldAudioManager : MonoBehaviour
{
    public static WorldAudioManager Instance { get; private set; }

    public AudioSource backgroundMusicSource;
    public AudioSource sfxSource;

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
}
