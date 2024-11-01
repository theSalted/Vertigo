using UnityEngine;

public class CarItemFollower : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Pickable"))
        {
            // 将物品设置为车子的子对象
            other.transform.SetParent(transform);

            // 设置物品的刚体为 kinematic
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Pickable"))
        {
            // 取消物品的父子关系
            other.transform.SetParent(null);

            // 取消物品的刚体 kinematic 设置
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
    }
}