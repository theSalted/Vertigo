using UnityEngine;

public class PlaySoundOnStart : MonoBehaviour
{
    // ��Inspector�з�����Ч
    public AudioClip startSound;
    private AudioSource audioSource;

    // ������������Χ��0.0��1.0
    public float volume = 0.1f;

    void Start()
    {
        // ��ȡAudioSource���
        audioSource = GetComponent<AudioSource>();

        // ������Ч
        if (startSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startSound, volume);
        }
        else
        {
            Debug.LogWarning("AudioSource��startSoundδ����");
        }
    }
}

