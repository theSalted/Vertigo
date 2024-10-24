using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactRange = 3f; // 交互范围
    public float minHoldDistance = 2f; // 持有物品的最小距离
    public float maxHoldDistance = 5f; // 持有物品的最大距离
    public UIManager uiManager; // 引用UIManager
    public float smoothSpeed = 0.1f; // 平滑速度

    private bool isHoldingObject = false;
    private GameObject heldObject = null;
    private Quaternion initialRotation;
    private Collider heldObjectCollider;
    private GameObject lastHighlightedObject = null;

    void Update()
    {
        Interact();
        DragObject();
    }

    void Interact()
    {
        if (playerCamera == null || uiManager == null)
        {
            Debug.LogError("PlayerCamera or UIManager is not assigned.");
            return;
        }

        if (isHoldingObject)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                // 放下物品
                heldObject.GetComponent<InteractableObject>().DisableOutline();
                Rigidbody heldObjectRigidbody = heldObject.GetComponent<Rigidbody>();
                heldObjectRigidbody.isKinematic = false;
                heldObjectRigidbody.linearVelocity = Vector3.zero; // 确保物品不会继续移动
                if (heldObjectCollider != null)
                {
                    heldObjectCollider.enabled = true;
                }
                heldObject = null;
                isHoldingObject = false;
                uiManager.HideInteractionPrompt();
                return; // 确保放下物品后立即返回，不再执行后续代码
            }
        }

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            GameObject hitObject = hit.collider.gameObject;
            InteractableObject interactableObject = hitObject.GetComponent<InteractableObject>();

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
                uiManager.ShowInteractionPrompt("Press E to interact");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    // 拾取物品
                    heldObject = hitObject;
                    interactableObject.DisableOutline();
                    heldObject.GetComponent<Rigidbody>().isKinematic = true;
                    heldObjectCollider = heldObject.GetComponent<Collider>();
                    if (heldObjectCollider != null)
                    {
                        heldObjectCollider.enabled = false;
                    }
                    isHoldingObject = true;
                    // 设置物品位置到摄像头前方一定距离
                    heldObject.transform.position = playerCamera.transform.position + playerCamera.transform.forward * minHoldDistance;
                    // 重置物品的旋转为正着的状态
                    heldObject.transform.rotation = Quaternion.identity;
                    // 保存物品的初始旋转
                    initialRotation = heldObject.transform.rotation;
                    // 隐藏UI提示
                    uiManager.HideInteractionPrompt();
                    Debug.Log("Picked up: " + heldObject.name);
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

            if (Physics.Raycast(ray, out hit, maxHoldDistance))
            {
                // 如果射线碰撞到物体，调整持有物品的距离
                targetDistance = Mathf.Clamp(hit.distance, minHoldDistance, maxHoldDistance);
                hitObject = hit.collider.gameObject;
            }

            

            // 保持物品在摄像头前方一定距离
            Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * targetDistance;

            // 检测物体是否会穿过地板或其他物体
            bool isPortal = hitObject == null ? false : hit.collider.gameObject.tag == "Portal"; // 检测物体是否是传送门
            Debug.Log("isPortal: " + isPortal);
            if (!isPortal && Physics.Raycast(targetPosition, Vector3.down, out hit, 1f) )
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
}
