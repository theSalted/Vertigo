using InputAssets;
using UnityEngine;

public class TiaoChuang : MonoBehaviour
{
    public float bounceForce = 10f; // 弹飞的力度

    // 当其他碰撞体进入触发器时调用此方法
    private void OnTriggerEnter(Collider other)
    {
        // 检查碰撞体是否有FirstPersonController组件
        FirstPersonController controller = other.GetComponent<FirstPersonController>();
        if (controller != null)
        {
            // 调用FirstPersonController的Bounce方法
            controller.Bounce(bounceForce);
        }
    }
}