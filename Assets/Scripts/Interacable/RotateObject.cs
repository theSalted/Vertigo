using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public float rotationSpeed = 30f; // 旋转速度

    void Update()
    {
        // 使物体绕Y轴旋转
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}