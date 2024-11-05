using InputAssets;
using UnityEngine;

public class TiaoChuang : MonoBehaviour
{
    public float bounceForce = 20f; // 弹飞的力度
    public float weightThreshold = 50f; // 弹起的重量阈值

    // 当其他碰撞体进入触发器时调用此方法
    private void OnTriggerEnter(Collider other)
    {
        // 检查碰撞体是否有FirstPersonController组件
        FirstPersonController controller = other.GetComponent<FirstPersonController>();
        if (controller != null)
        {
            // 调用FirstPersonController的Bounce方法
            controller.Bounce(bounceForce);
            return;
        }

        // 检查碰撞体是否有Rigidbody组件
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 根据物体的重量决定是否弹起
            if (rb.mass <= weightThreshold)
            {
                // 施加向上的力
                rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
            }
        }
    }
}