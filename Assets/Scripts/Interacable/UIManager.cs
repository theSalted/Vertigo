using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI interactionPrompt; // UI提示
    public RawImage itemDisplay; // 用于显示3D模型的RawImage
    public Camera itemCamera; // 用于捕捉3D模型的摄像头

    private const string OverlayLayerName = "Overlay";
    private GameObject currentItem; // 当前显示的物体

    public void ShowInteractionPrompt(string message)
    {
        if (interactionPrompt == null)
        {
            Debug.LogError("InteractionPrompt is not assigned.");
            return;
        }

        interactionPrompt.text = message;
        interactionPrompt.gameObject.SetActive(true);
    }

    public void HideInteractionPrompt()
    {
        if (interactionPrompt == null)
        {
            Debug.LogError("InteractionPrompt is not assigned.");
            return;
        }

        interactionPrompt.gameObject.SetActive(false);
    }

    public void ShowItem(GameObject item)
    {
        if (itemDisplay == null || itemCamera == null)
        {
            Debug.LogError("ItemDisplay or ItemCamera is not assigned.");
            return;
        }

        int overlayLayer = LayerMask.NameToLayer(OverlayLayerName);
        if (overlayLayer == -1)
        {
            Debug.LogError($"Layer '{OverlayLayerName}' does not exist.");
            return;
        }

        // 如果当前有显示的物体，先隐藏它
        if (currentItem != null)
        {
            currentItem.SetActive(false);
        }

        currentItem = item;
        currentItem.layer = overlayLayer;
        currentItem.SetActive(true);
        itemCamera.gameObject.SetActive(true);
        itemDisplay.gameObject.SetActive(true);
    }

    public void HideItem()
    {
        if (itemDisplay == null || itemCamera == null)
        {
            Debug.LogError("ItemDisplay or ItemCamera is not assigned.");
            return;
        }

        if (currentItem != null)
        {
            currentItem.SetActive(false);
            currentItem = null;
        }

        itemCamera.gameObject.SetActive(false);
        itemDisplay.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (currentItem != null)
        {
            // 更新物体的位置，使其跟随相机或其他逻辑
            // 例如：currentItem.transform.position = itemCamera.transform.position + itemCamera.transform.forward * distance;
        }
    }
}
