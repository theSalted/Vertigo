using UnityEngine;

public class SizeManager : MonoBehaviour
{
    // Singleton instance
    private static SizeManager _instance;

    public static SizeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<SizeManager>();

                if (_instance == null)
                {
                    // Create a new GameObject with SizeManager
                    GameObject singletonObject = new GameObject();
                    _instance = singletonObject.AddComponent<SizeManager>();
                    singletonObject.name = typeof(SizeManager).ToString() + " (Singleton)";
                }
            }
            return _instance;
        }
    }

    // Reference to the player GameObject
    public GameObject player;

    // References to the two portals
    public Portal portalA;
    public Portal portalB;

    // Scale factors for shrinking and growing
    public float shrunkFactor { get; private set; }
    public float growthFactor { get; private set; }

    private void Awake()
    {
        // Enforce singleton pattern
        if (_instance == null)
        {
            _instance = this;
            // Optionally, set this to persist between scenes
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize scale factors
        UpdateScaleFactors();
    }

    private void Start()
    {
        // Ensure the scale factors are updated at the start
        UpdateScaleFactors();
    }

    /// <summary>
    /// Updates the shrunk and growth factors based on the scales of PortalA and PortalB.
    /// </summary>
    public void UpdateScaleFactors()
    {
        if (portalA != null && portalB != null)
        {
            // Assuming uniform scaling
            float scaleA = portalA.transform.localScale.x;
            float scaleB = portalB.transform.localScale.x;

            // Calculate scale factors
            shrunkFactor = scaleB / scaleA;  // Factor to apply when going from PortalA to PortalB (shrinking)
            growthFactor = scaleA / scaleB;  // Factor to apply when going from PortalB to PortalA (growing)
        }
        else
        {
            Debug.LogWarning("SizeManager: PortalA and PortalB references are not set.");
            shrunkFactor = 1.0f;
            growthFactor = 1.0f;
        }
    }

    /// <summary>
    /// Determines if the player is shrunk based on their current scale.
    /// </summary>
    public bool IsShrunk
    {
        get
        {
            if (player != null)
            {
                // Assume original scale is 1.0f
                float playerScale = player.transform.localScale.x;
                return playerScale < 1.0f;
            }
            else
            {
                Debug.LogWarning("SizeManager: Player GameObject not set.");
                return false;
            }
        }
    }
}