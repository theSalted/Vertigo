using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    public List<GameObject> interactableObjects = new List<GameObject>();

    public void RegisterInteractable(GameObject interactable)
    {
        if (!interactableObjects.Contains(interactable))
        {
            interactableObjects.Add(interactable);
        }
    }

    public void UnregisterInteractable(GameObject interactable)
    {
        if (interactableObjects.Contains(interactable))
        {
            interactableObjects.Remove(interactable);
        }
    }

    public GameObject GetInteractableObject(Ray ray, float range)
    {
        foreach (var interactable in interactableObjects)
        {
            if (interactable != null)
            {
                Collider collider = interactable.GetComponent<Collider>();
                if (collider != null && collider.Raycast(ray, out RaycastHit hit, range))
                {
                    return interactable;
                }
            }
        }
        return null;
    }
}