using UnityEngine;

public class PlaySoundOnStart : MonoBehaviour
{
    // 在Inspector中分配音效
    public AudioClip startSound;
    private AudioSource audioSource;

    // 设置音量，范围是0.0到1.0
    public float volume = 0.1f;

    void Start()
    {
        // 获取AudioSource组件
        audioSource = GetComponent<AudioSource>();

        // 播放音效
        if (startSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startSound, volume);
        }
        else
        {
            Debug.LogWarning("AudioSource或startSound未设置");
        }
    }
}

