using UnityEngine;
using UnityEngine.SceneManagement;

public class BedManager : MonoBehaviour
{
    [Header("Scene Transition")]
    public string nextSceneName; // 下一个场景的名称

    public bool IsShrunk = false;

    // Can sleep computed property
    public bool CanSleep => SizeManager.Instance.IsShrunk == IsShrunk;

    public void Sleep() {
        if (CanSleep) {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
