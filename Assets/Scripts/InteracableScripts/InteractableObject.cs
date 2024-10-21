using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public Material outlineMaterial; // 轮廓材质
    private Renderer objectRenderer;
    private Material[] originalMaterials; // 原始材质数组

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterials = objectRenderer.materials;
        }
    }

    public void EnableOutline()
    {
        if (objectRenderer != null && outlineMaterial != null)
        {
            Material[] materialsWithOutline = new Material[originalMaterials.Length + 1];
            originalMaterials.CopyTo(materialsWithOutline, 0);
            materialsWithOutline[materialsWithOutline.Length - 1] = outlineMaterial;
            objectRenderer.materials = materialsWithOutline;
        }
    }

    public void DisableOutline()
    {
        if (objectRenderer != null && originalMaterials != null)
        {
            objectRenderer.materials = originalMaterials;
        }
    }
}