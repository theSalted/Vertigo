using UnityEngine;

public class Slingshot : MonoBehaviour
{
    public float maxLaunchForce = 20f; // 最大发射力
    public Vector3 launchDirection = Vector3.up; // 发射方向，默认为向上
    public float maxChargeTime = 2f; // 最大蓄力时间
    private bool isPlayerInRange = false;
    private CharacterController playerController;
    private float currentChargeTime = 0f; // 当前蓄力时间
    private Renderer slingshotRenderer; // 用于改变颜色的Renderer组件
    private Coroutine launchCoroutine; // 用于跟踪当前运行的协程

    void Start()
    {
        slingshotRenderer = GetComponent<Renderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerController = other.GetComponent<CharacterController>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerController = null;
            ResetCharge(); // 重置蓄力时间和颜色
            StopLaunchCoroutine(); // 停止发射协程
        }
    }

    void Update()
    {
        if (isPlayerInRange)
        {
            if (Input.GetKey(KeyCode.E))
            {
                // 持续蓄力
                currentChargeTime += Time.deltaTime;
                currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, maxChargeTime);
                UpdateColor(); // 更新颜色
            }

            if (Input.GetKeyUp(KeyCode.E))
            {
                // 松开E键，发射玩家
                float launchForce = (currentChargeTime / maxChargeTime) * maxLaunchForce;
                launchCoroutine = StartCoroutine(LaunchPlayer(launchForce));
                ResetCharge(); // 重置蓄力时间和颜色
            }
        }
    }

    System.Collections.IEnumerator LaunchPlayer(float launchForce)
    {
        float elapsedTime = 0f;
        while (elapsedTime < maxChargeTime)
        {
            if (playerController != null)
            {
                playerController.Move(launchDirection.normalized * launchForce * Time.deltaTime);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    void UpdateColor()
    {
        if (slingshotRenderer != null)
        {
            float t = currentChargeTime / maxChargeTime;
            Color newColor = Color.Lerp(Color.white, Color.red, t);
            slingshotRenderer.material.color = newColor;
        }
    }

    void ResetCharge()
    {
        currentChargeTime = 0f; // 重置蓄力时间
        ResetColor(); // 重置颜色
    }

    void ResetColor()
    {
        if (slingshotRenderer != null)
        {
            slingshotRenderer.material.color = Color.white;
        }
    }

    void StopLaunchCoroutine()
    {
        if (launchCoroutine != null)
        {
            StopCoroutine(launchCoroutine);
            launchCoroutine = null;
        }
    }
}
