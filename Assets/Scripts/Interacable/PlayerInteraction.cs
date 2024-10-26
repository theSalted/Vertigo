using UnityEditor.PackageManager;
using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactRange = 3f; // 交互范围
    public float minHoldDistance = 2f; // 持有物品的最小距离
    public float maxHoldDistance = 5f; // 持有物品的最大距离
    public UIManager uiManager; // 引用UIManager
    public float smoothSpeed = 0.1f; // 平滑速度
    public float bulletRadius = 0.1f; // 子弹半径

    private bool isHoldingObject = false;
    private GameObject heldObject = null;
    private Quaternion initialRotation;
    private Collider heldObjectCollider;
    private GameObject lastHighlightedObject = null;
    private List<GameObject> inventory = new List<GameObject>(); // 仓库
    private int originalLayer; // 存储物品的原始Layer

    void Update()
    {
        Interact();
        DragObject();
        RotateHeldObject();
    }

    void Interact()
    {
        if (playerCamera == null || uiManager == null)
        {
            Debug.LogError("PlayerCamera or UIManager is not assigned.");
            return;
        }

        if (isHoldingObject && Input.GetMouseButtonDown(1)) // 右键放下物品
        {
            DropHeldObject();
            return; // 确保放下物品后立即返回，不再执行后续代码
        }

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.SphereCast(ray, bulletRadius, out hit, interactRange))
        {
            GameObject hitObject = hit.collider.gameObject;
            InteractableObject interactableObject = hitObject.GetComponent<InteractableObject>();

            // 检查是否是传送门
            if (hitObject.CompareTag("Portal"))
            {
                uiManager.HideInteractionPrompt();
                return;
            }

            if (interactableObject != null)
            {
                // 启用轮廓材质
                if (lastHighlightedObject != hitObject)
                {
                    if (lastHighlightedObject != null)
                    {
                        lastHighlightedObject.GetComponent<InteractableObject>().DisableOutline();
                    }
                    interactableObject.EnableOutline();
                    lastHighlightedObject = hitObject;
                }

                // 显示UI提示
                uiManager.ShowInteractionPrompt("Press Left Mouse Button to interact");

                if (Input.GetMouseButtonDown(0)) // 左键拾取物品
                {
                    if (isHoldingObject)
                    {
                        // 放下当前持有的物品
                        DropHeldObject();
                    }

                    // 拾取新物品并存入仓库
                    heldObject = hitObject;
                    interactableObject.DisableOutline();
                    heldObject.GetComponent<Rigidbody>().isKinematic = true;
                    heldObjectCollider = heldObject.GetComponent<Collider>();
                    if (heldObjectCollider != null)
                    {
                        heldObjectCollider.enabled = false;
                    }
                    isHoldingObject = true;
                    inventory.Add(heldObject); // 将物品添加到仓库
                    originalLayer = heldObject.layer; // 存储原始Layer
                    heldObject.SetActive(false); // 隐藏物品
                    heldObject = null; // 清空当前持有物品
                    // 隐藏UI提示
                    uiManager.HideInteractionPrompt();
                    uiManager.ShowItem(hitObject); // 显示物品在UI中
                    Debug.Log("Picked up: " + hitObject.name);
                }
            }
        }
        else
        {
            // 隐藏UI提示
            uiManager.HideInteractionPrompt();
            if (lastHighlightedObject != null)
            {
                lastHighlightedObject.GetComponent<InteractableObject>().DisableOutline();
                lastHighlightedObject = null;
            }
        }
    }

    void DropHeldObject()
    {
        if (inventory.Count > 0)
        {
            // 从仓库中取出最后一个物品
            heldObject = inventory[inventory.Count - 1];
            inventory.RemoveAt(inventory.Count - 1);

            // 确保物品被激活
            heldObject.SetActive(true);
            Debug.Log("DropHeldObject: " + heldObject.name + " is now active.");

            // 恢复原始Layer
            heldObject.layer = originalLayer;
            Debug.Log("DropHeldObject: " + heldObject.name + " layer set to " + originalLayer);

            // 获取物品的Rigidbody组件
            Rigidbody heldObjectRigidbody = heldObject.GetComponent<Rigidbody>();
            if (heldObjectRigidbody != null)
            {
                heldObjectRigidbody.isKinematic = false;
                heldObjectRigidbody.linearVelocity = Vector3.zero; // 确保物品不会继续移动
                Debug.Log("DropHeldObject: " + heldObject.name + " Rigidbody set to non-kinematic.");
            }
            else
            {
                Debug.LogWarning("DropHeldObject: " + heldObject.name + " does not have a Rigidbody component.");
            }

            // 恢复物品的Collider
            heldObjectCollider = heldObject.GetComponent<Collider>();
            if (heldObjectCollider != null)
            {
                heldObjectCollider.enabled = true;
                Debug.Log("DropHeldObject: " + heldObject.name + " Collider enabled.");
            }
            else
            {
                Debug.LogWarning("DropHeldObject: " + heldObject.name + " does not have a Collider component.");
            }

            // 将物品放置在屏幕中心位置
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            Vector3 dropPosition;

            if (Physics.Raycast(ray, out hit, maxHoldDistance))
            {
                // 检查是否是传送门
                if (hit.collider.gameObject.CompareTag("Portal"))
                {
                    Debug.Log("Cannot drop object on a portal.");
                    return;
                }

                dropPosition = hit.point;
            }
            else
            {
                dropPosition = ray.GetPoint(maxHoldDistance);
            }

            // 向下发射射线，确保物品不会穿透地板
            if (Physics.Raycast(dropPosition, Vector3.down, out hit, 1f))
            {
                dropPosition = hit.point + Vector3.up * 0.5f; // 调整高度，确保物品在地板上方
            }

            heldObject.transform.position = dropPosition;
            isHoldingObject = false;
            uiManager.HideInteractionPrompt();
            uiManager.HideItem(); // 隐藏物品在UI中
            Debug.Log("Dropped: " + heldObject.name);
        }
    }

    void DragObject()
    {
        // Short-circuit if heldObject condition has not met
        if (isHoldingObject && heldObject != null)
        {
            // 从摄像头位置向前发射射线，检测与其他物体的碰撞
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;
            GameObject hitObject = null;
            float targetDistance = maxHoldDistance;

            if (Physics.SphereCast(ray, bulletRadius, out hit, maxHoldDistance))
            {
                // 如果射线碰撞到物体，调整持有物品的距离
                targetDistance = Mathf.Clamp(hit.distance, minHoldDistance, maxHoldDistance);
                hitObject = hit.collider.gameObject;
            }

            // 保持物品在摄像头前方一定距离
            Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * targetDistance;

            // 检测物体是否会穿过地板或其他物体
            bool isPortal = hitObject == null ? false : hit.collider.gameObject.tag == "Portal";
            if (!isPortal && Physics.Raycast(targetPosition, Vector3.down, out hit, 1f))
            {
                // 如果物体会穿过地板或其他物体，调整物体的位置
                targetPosition = hit.point + Vector3.up * 0.5f; // 调整高度，确保物体在地板上方
            }

            // 使用插值平滑地更新物体的位置
            heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, smoothSpeed);

            // 调整物品的旋转以贴合碰撞表面
            if (Physics.Raycast(targetPosition, -playerCamera.transform.forward, out hit, 0.1f))
            {
                heldObject.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                // 锁定物品的初始旋转
                heldObject.transform.rotation = initialRotation;
            }
        }
    }

    void RotateHeldObject()
    {
        if (isHoldingObject && heldObject != null)
        {
            heldObject.transform.Rotate(Vector3.up, 20 * Time.deltaTime, Space.World);
        }
    }
}

