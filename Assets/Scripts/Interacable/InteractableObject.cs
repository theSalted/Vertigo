using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    // Outline materials
    public Material outlineMaterial; // 轮廓材质
    private Renderer objectRenderer;
    private Material[] originalMaterials; // 原始材质数组

    // Rest transform variables
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 restScale;

    // Rigidbody reference
    private Rigidbody rb;

    void Awake()
    {
        // Store the initial transform values
        restPosition = transform.position;
        restRotation = transform.rotation;
        restScale = transform.localScale;
    }

    void Start()
    {
        // Initialize Renderer and original materials
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterials = objectRenderer.materials;
        }

        // Ensure the object has a Rigidbody for trigger detection
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Prevent physics from affecting the object
        }
    }

    /// <summary>
    /// Enables the outline by adding the outline material.
    /// </summary>
    public void EnableOutline()
    {
        if (objectRenderer != null && outlineMaterial != null)
        {
            // Create a new material array with an extra slot for the outline
            Material[] materialsWithOutline = new Material[originalMaterials.Length + 1];
            originalMaterials.CopyTo(materialsWithOutline, 0);
            materialsWithOutline[materialsWithOutline.Length - 1] = outlineMaterial;
            objectRenderer.materials = materialsWithOutline;
        }
    }

    /// <summary>
    /// Disables the outline by reverting to the original materials.
    /// </summary>
    public void DisableOutline()
    {
        if (objectRenderer != null && originalMaterials != null)
        {
            objectRenderer.materials = originalMaterials;
        }
    }

    /// <summary>
    /// Called when the collider enters a trigger collider.
    /// </summary>
    /// <param name="other">The other collider involved in the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Portal"))
        {
            ResetTransform();
        }
    }

    /// <summary>
    /// Resets the object's transform to its initial state.
    /// </summary>
    private void ResetTransform()
    {
        transform.position = restPosition;
        transform.rotation = restRotation;
        transform.localScale = restScale;

        // Optionally, you can also reset velocity if using physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"{gameObject.name} has been reset to its original transform due to portal interaction.");
    }
}