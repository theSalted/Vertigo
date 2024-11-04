using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Tooltip("The audio clip to be played as background music")]
    public AudioClip backgroundMusic;

    private AudioSource audioSource;

    private static MusicManager instance;

    private void Awake()
    {
        // Ensure that only one instance of MusicManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.volume = 0.5f; // Set the volume as needed
            audioSource.Play();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
