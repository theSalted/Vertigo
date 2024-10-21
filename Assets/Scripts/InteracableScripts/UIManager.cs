using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI interactionPrompt; // UI提示

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
}