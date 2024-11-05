using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class BedManager : MonoBehaviour
{
    [Header("Scene Transition")]
    public string nextSceneName; // 下一个场景的名称

    public bool IsShrunk = false;

    // Can sleep computed property
    public bool CanSleep => SizeManager.Instance.IsShrunk == IsShrunk;

    public Image fadeImage; // 用于渐变效果的UI Image
    public AudioSource audioSource; // 用于播放音效的AudioSource
    public AudioClip transitionSound; // 关卡切换前的音效
    public AudioClip nextSceneSound; // 进入下一个关卡时的音效

    private void Start()
    {
        // 确保fadeImage是全屏的
        fadeImage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        fadeImage.color = new Color(0, 0, 0, 0); // 初始为透明
    }

    public void Sleep()
    {
        if (CanSleep)
        {
            StartCoroutine(FadeAndLoadScene());
        }
    }

    private IEnumerator FadeAndLoadScene()
    {
        // 播放音效
        audioSource.PlayOneShot(transitionSound);

        // 渐渐暗下去
        yield return StartCoroutine(Fade(1, 4));

        // 保留音频源对象
        DontDestroyOnLoad(audioSource);

        // 加载下一个场景
        SceneManager.LoadScene(nextSceneName);

        // 渐渐亮起
        yield return StartCoroutine(Fade(0, 2));

        // 播放进入下一个关卡的音效
        audioSource.PlayOneShot(nextSceneSound);

        // 恢复音频源对象的销毁
        Destroy(audioSource.gameObject, nextSceneSound.length);
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = fadeImage.color.a;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);
    }
}
