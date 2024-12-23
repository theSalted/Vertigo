﻿using UnityEngine;
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
    public GameObject followCube; // 跟随摄像机的Cube
    public float dropMaxHeightOffset = 3f; // 最大高度偏移

    private bool isHoldingObject = false;
    private GameObject heldObject = null;
    private Vector3 originalScale;
    private Quaternion initialRotation;
    private UnityEngine.Collider[] heldObjectCollider;
    private GameObject lastHighlightedObject = null;
    private (GameObject Object, bool IsShrunk) inventory = (null, false); // 仓库

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
                
                BedManager bedManager = hitObject.GetComponent<BedManager>();
                // 显示UI提示
                if (bedManager != null)
                {
                    if (bedManager.CanSleep)
                    {
                        uiManager.ShowInteractionPrompt("Left click to sleep");
                        if (Input.GetMouseButtonDown(0))
                        {
                            bedManager.Sleep();
                        }
                    }
                    else
                    {
                        uiManager.ShowInteractionPrompt("You couldn't fit this bed");
                    }
                    return;
                }


                uiManager.ShowInteractionPrompt("Left click to interact");

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
                    heldObjectCollider = heldObject.GetComponents<Collider>();
                    if (heldObjectCollider.Length != 0)
                    {
                        for (int i = 0; i < heldObjectCollider.Length; i++)
                        {
                            heldObjectCollider[i].enabled = false;
                        }
                    }
                    isHoldingObject = true;
                    inventory = (heldObject, SizeManager.Instance.IsShrunk); // 将物品添加到仓库

                    // Replace the followCube with the heldObject
                    ReplaceFollowCube(heldObject);

                    // 隐藏UI提示
                    uiManager.HideInteractionPrompt();
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
        if (inventory.Object != null)
        {

            // 将物品放置在屏幕中心位置
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            Vector3 dropPosition;

            if (Physics.Raycast(ray, out hit, maxHoldDistance))
            {
                dropPosition = hit.point;
                // 检查是否是传送门
                if (hit.collider.gameObject.CompareTag("Player"))
                {
                    dropPosition = ray.GetPoint(5.0f);
                    Debug.Log("Cannot drop object on Player.");
                }

                if (hit.collider.gameObject.CompareTag("Portal"))
                {
                    Debug.Log("Cannot drop object on Portal.");
                    return;
                }

                // Place object beyond hit point
            }
            else
            {
                dropPosition = ray.GetPoint(maxHoldDistance);
            }

            // 从仓库中取出最后一个物品
            heldObject = inventory.Object;
            bool hasShrunk = inventory.IsShrunk;
            inventory = (null, false); // 从仓库中移除物品
            heldObject.SetActive(true); // 显示物品
            Rigidbody heldObjectRigidbody = heldObject.GetComponent<Rigidbody>();
            heldObjectRigidbody.isKinematic = false;
            heldObjectRigidbody.linearVelocity = Vector3.zero; // 确保物品不会继续移动
            if (heldObjectCollider != null)
            {
                for (int i = 0; i < heldObjectCollider.Length; i++)
                {
                    heldObjectCollider[i].enabled = true;
                }
            }


            // 恢复物体的原始缩放
            heldObject.transform.localScale = originalScale;

            if (hasShrunk != SizeManager.Instance.IsShrunk)
            {
                // 如果物品的大小与当前物品的大小不匹配，则调整物品的大小
                if (SizeManager.Instance.IsShrunk)
                {
                    Debug.Log("Shrinking object by " + SizeManager.Instance.shrunkFactor);
                    heldObject.transform.localScale = heldObject.transform.localScale * SizeManager.Instance.shrunkFactor;
                }
                else
                {
                    Debug.Log("Growing object by " + SizeManager.Instance.growthFactor);
                    heldObject.transform.localScale = heldObject.transform.localScale * SizeManager.Instance.growthFactor;
                }
            }

            // 向下发射射线，确保物品不会穿透地板
            if (Physics.Raycast(dropPosition, Vector3.down, out hit, 1f))
            {
                dropPosition = hit.point + Vector3.up * 0.5f; // 调整高度，确保物品在地板上方
            }

            // 调整位置以避免碰撞
            dropPosition = FindValidDropPosition(dropPosition, heldObjectCollider[0], dropMaxHeightOffset);

            heldObject.transform.position = dropPosition;
            isHoldingObject = false;
            uiManager.HideInteractionPrompt();
            Debug.Log("Dropped: " + heldObject.name);

            // 删除UIElement下的所有子对象
            GameObject uiElement = GameObject.Find("UIElement");
            if (uiElement != null)
            {
                foreach (Transform child in uiElement.transform)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }
    }


    Vector3 FindValidDropPosition(Vector3 initialPosition, Collider objectCollider, float maxHeightOffset)
    {
        Vector3 validPosition = initialPosition;
        Collider[] colliders = Physics.OverlapBox(validPosition, objectCollider.bounds.extents);

        float totalOffset = 0f;

        while (colliders.Length > 0 && totalOffset < maxHeightOffset)
        {
            validPosition += Vector3.up * 0.1f; // Move upwards by 0.1 units
            totalOffset += 0.1f;
            colliders = Physics.OverlapBox(validPosition, objectCollider.bounds.extents);
        }

        if (colliders.Length > 0)
        {
            // Could not find a valid position within the height limit
            return initialPosition;
        }

        return validPosition;
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
            bool isPortal = hitObject == null ? false : hit.collider.gameObject.CompareTag("Portal"); // 检测物体是否是传送门
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

    void ReplaceFollowCube(GameObject newObject)
    {
        if (followCube != null)
        {
            // 存储原始缩放
            originalScale = newObject.transform.localScale;

            // 隐藏原物品
            newObject.SetActive(false);

            // 更新 followCube 为新物品
            followCube = newObject;

            // 将新物品移动到UIElementLayer图层
            int uiElementLayer = LayerMask.NameToLayer("Overlay");

            // 替换UIElement下的模型
            GameObject uiElement = GameObject.Find("UIElement");
            if (uiElement != null)
            {
                // 删除UIElement下的所有子对象
                foreach (Transform child in uiElement.transform)
                {
                    GameObject.Destroy(child.gameObject);
                }
                Vector3 localScale = newObject.transform.localScale;
                // 实例化新物品的克隆体并设置为UIElement的子对象
                GameObject newUIObject = Instantiate(newObject, uiElement.transform);
                newUIObject.transform.localScale = new Vector3(localScale.x * 0.22f, localScale.y * 0.22f, localScale.z * 0.22f);
                newUIObject.transform.localPosition = new Vector3(1, 1, 1);
                newUIObject.layer = uiElementLayer; // 确保新UI对象也在UIElementLayer图层


                newUIObject.SetActive(true);


                // 添加旋转脚本
                newUIObject.AddComponent<RotateObject>();
            }
        }
    }
}