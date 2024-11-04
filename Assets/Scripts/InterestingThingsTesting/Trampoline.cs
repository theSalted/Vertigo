using InputAssets;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [SerializeField]
    private float bounceFactor = 2.0f; // 弹跳系数
    [SerializeField]
    private float maxBounceHeight = 10.0f; // 最大弹跳高度
    [SerializeField]
    private float maxMass = 50.0f; // 最大质量，超过此质量的物体不会被弹起

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collision collision)
    {
        Rigidbody rb = collision.collider.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 检查碰撞点是否在弹床的上方
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.point.y > transform.position.y)
                {
                    float mass = rb.mass;
                    if (mass <= maxMass)
                    {
                        float bounceHeight = Mathf.Min(mass * bounceFactor, maxBounceHeight);
                        rb.AddForce(Vector3.up * bounceHeight, ForceMode.Impulse);
                    }
                    break;
                }
            }
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.point.y > transform.position.y)
        {
            FirstPersonController playerController = hit.controller.GetComponent<FirstPersonController>();
            if (playerController != null)
            {
                float bounceHeight = Mathf.Min(bounceFactor, maxBounceHeight);
                playerController.Bounce(bounceHeight);
            }
        }
    }
}


