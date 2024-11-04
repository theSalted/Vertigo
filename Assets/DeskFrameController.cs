using UnityEngine;

public class DeskFrameController : MonoBehaviour
{
    MeshRenderer meshRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Called when the Collider other enters the trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            meshRenderer.enabled = false;
            Debug.Log("Player entered the desk frame trigger");
        }
        
    }

    // Called when the Collider other exits the trigger
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            meshRenderer.enabled = true;
            Debug.Log("Player exited the desk frame trigger");
        }
    }
}
