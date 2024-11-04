using UnityEngine;

/// <summary>
/// The PaintingPortal class represents a two-way portal system between paired paintings (portals),
/// allowing each painting to display a view of the space behind its paired painting. This effect
/// creates an illusion as though each painting is a window into the space on the opposite side of
/// its paired portal, accurately simulating perspective from both the front and back viewing angles.
///
/// The portal system is designed to work seamlessly whether the paintings are upright or lying flat,
/// using configurable parameters to control the viewing perspective and distance without requiring
/// manual setup of specific viewing points.
///
/// <para>
/// <b>Specification and Functionality:</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     Each painting (referred to as Painting A and Painting B) is paired, with each displaying the
///     view from a specific observation point on the opposite painting's side. These observation
///     points are calculated based on the player's position relative to each painting, along with
///     configurable distance and height values:
///     </description>
///   </item>
///   <item>
///     <description>
///     - If the player is on the front side of Painting A, they will see the view from an observation
///       point in front of Painting B, aligned with the height and distance settings configured in
///       this class.
///     </description>
///   </item>
///   <item>
///     <description>
///     - If the player is on the back side of Painting A, the player sees the view from the back of
///       Painting B.
///     </description>
///   </item>
/// </list>
///
/// <para>
/// <b>Parameters:</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <c>viewingDistance</c> (D): Defines the distance along the forward axis of the opposite
///     painting at which the observation point is placed. Default is 10 units.
///     </description>
///   </item>
///   <item>
///     <description>
///     <c>viewingHeight</c> (H): Defines the height above the ground plane of the observation point
///     from which the view is taken. Default is 2 units.
///     </description>
///   </item>
/// </list>
///
/// <para>
/// <b>Features and Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     - Automatic calculation of observation points: The positions of observation points on each
///       portal are calculated automatically based on <c>viewingDistance</c> and <c>viewingHeight</c>,
///       so no manual setup of specific observation points is required.
///     </description>
///   </item>
///   <item>
///     <description>
///     - Perspective simulation: The portal camera (portalCam) is placed at the calculated observation
///       point and rotated to align with the linked painting, maintaining the correct perspective
///       based on the player’s current side relative to each painting.
///     </description>
///   </item>
///   <item>
///     <description>
///     - Works in both standing and horizontal orientations: The portal can function accurately whether
///       the paintings are upright or lying down, with viewing angles adjusted based on local space
///       coordinates.
///     </description>
///   </item>
///   <item>
///     <description>
///     - Hidden linked portal in view: To prevent self-referential display issues, each painting
///       temporarily hides its paired portal’s screen from the view during rendering, preventing
///       "portal feedback" from occurring.
///     </description>
///   </item>
/// </list>
///
/// <para>
/// <b>Implementation Overview:</b>
/// </para>
/// The PaintingPortal class implements the following key methods:
/// <list type="bullet">
///   <item>
///     <term><c>PrePortalRender()</c></term>
///     <description>
///     Called before rendering the portal view, hiding the linked painting’s screen to prevent it
///     from being rendered by the portal camera.
///     </description>
///   </item>
///   <item>
///     <term><c>Render()</c></term>
///     <description>
///     Manages the rendering process for the portal view. It sets up the RenderTexture for the portal
///     camera, captures the view, and assigns it to the material on the screen of the paired painting.
///     </description>
///   </item>
///   <item>
///     <term><c>PostPortalRender()</c></term>
///     <description>
///     Called after rendering to restore the visibility of the linked painting’s screen.
///     </description>
///   </item>
///   <item>
///     <term><c>UpdateCamera()</c></term>
///     <description>
///     Calculates the correct position and orientation for the portal camera (portalCam) based on the
///     player’s location relative to each painting and aligns the camera with the linked painting’s
///     perspective at the observation point.
///     </description>
///   </item>
///   <item>
///     <term><c>IsCameraBehindLinkedPortal()</c></term>
///     <description>
///     Determines whether the portal camera is positioned on the back side of the linked painting,
///     applying a 180-degree rotation if necessary to ensure the image is displayed correctly.
///     </description>
///   </item>
/// </list>
///
/// <b>Usage:</b>
/// <para>
/// Attach the PaintingPortal script to each painting object in the scene, and assign the linked
/// painting as its paired portal. Configure the <c>viewingDistance</c> and <c>viewingHeight</c>
/// parameters as desired to control the observation point distances and height offsets. Ensure both
/// paintings have a MeshRenderer on their screens for displaying the portal view.
/// </para>
/// </summary>
public class PaintingPortal : MonoBehaviour
{
    [Header("Main Settings")]
    public PaintingPortal linkedPainting;
    public MeshRenderer screen;

    [Header("Viewing Settings")]
    public float viewingDistance = 10f;
    public float viewingHeight = 2f;

    // Private variables
    
    public Camera portalCam;
    private Camera playerCam;
    private RenderTexture viewTexture;
    private MeshFilter screenMeshFilter;

    void Awake()
    {
        if (screen == null)
        {
            Debug.LogError("Screen MeshRenderer is not assigned.");
            return;
        }

        if (linkedPainting == null)
        {
            Debug.LogError("Linked Painting is not assigned.");
            return;
        }

        // Automatically find the main camera
        playerCam = Camera.main;

        // Use the portal camera included in the prefab
        portalCam = GetComponentInChildren<Camera>();
        if (portalCam == null)
        {
            Debug.LogError("Portal Camera is not found in children.");
            return;
        }
        portalCam.enabled = false;

        // Get the MeshFilter to calculate aspect ratio
        screenMeshFilter = screen.GetComponent<MeshFilter>();
        if (screenMeshFilter == null)
        {
            Debug.LogError("Screen MeshFilter is not found.");
            return;
        }

        // Set the field of view and aspect ratio
        // portalCam.fieldOfView = playerCam.fieldOfView;
        portalCam.aspect = playerCam.aspect;

        // Initialize shader properties
        screen.material.SetInt("_displayMask", 1);
    }

    void LateUpdate()
    {
        UpdateCamera();
    }

    void UpdateCamera()
    {
        if (linkedPainting == null)
            return;

        // Determine whether the player is in front of or behind this painting
        Vector3 toPlayer = playerCam.transform.position - transform.position;
        float side = Mathf.Sign(Vector3.Dot(toPlayer, transform.forward));

        // Calculate the camera position on the same side as the player
        Vector3 camPosition = linkedPainting.transform.position + linkedPainting.transform.forward * viewingDistance * side;
        camPosition += linkedPainting.transform.up * viewingHeight;

        // Set camera position
        portalCam.transform.position = camPosition;

        // Look at the linked painting's position, adjusted for height
        Vector3 lookAtPoint = linkedPainting.transform.position + linkedPainting.transform.up * viewingHeight;

        // Adjust the camera's rotation to match the portal's orientation
        portalCam.transform.LookAt(lookAtPoint, linkedPainting.transform.up);

        // Rotate the camera by 180 degrees around its forward axis if necessary
        if (IsCameraBehindLinkedPortal())
        {
            portalCam.transform.Rotate(0f, 0f, 180f, Space.Self);
        }
    }

    bool IsCameraBehindLinkedPortal()
    {
        Vector3 toCamera = portalCam.transform.position - linkedPainting.transform.position;
        float dot = Vector3.Dot(toCamera.normalized, linkedPainting.transform.forward);
        return dot < 0f;
    }

    public void PrePortalRender()
    {
        if (linkedPainting == null)
            return;

        // Hide the linked painting's screen to prevent it from rendering in the camera's view
        linkedPainting.screen.enabled = false;
    }

    public void Render()
    {
        if (linkedPainting == null)
            return;

        CreateViewTexture();

        // Set the target texture
        portalCam.targetTexture = viewTexture;

        // Render the camera
        portalCam.Render();

        // Assign the RenderTexture to the screen material
        screen.material.SetTexture("_MainTex", viewTexture);
    }

    public void PostPortalRender()
    {
        if (linkedPainting == null)
            return;

        // Re-enable the linked painting's screen
        linkedPainting.screen.enabled = true;
    }

    void CreateViewTexture()
    {
        // Calculate the aspect ratio based on the screen's mesh bounds
        Bounds meshBounds = screenMeshFilter.sharedMesh.bounds;
        Vector3 meshSize = meshBounds.size;

        // Assuming the mesh is a plane, the width and height are in the x and y axes
        float meshWidth = meshSize.x * screen.transform.lossyScale.x;
        float meshHeight = meshSize.y * screen.transform.lossyScale.y;

        float aspectRatio = meshWidth / meshHeight;

        // Decide on a base width; height is calculated to maintain aspect ratio
        int baseWidth = 1024;
        int calculatedHeight = Mathf.RoundToInt(baseWidth / aspectRatio);

        // Check if the current RenderTexture matches the desired dimensions
        if (viewTexture == null || viewTexture.width != baseWidth || viewTexture.height != calculatedHeight)
        {
            if (viewTexture != null)
            {
                viewTexture.Release();
            }
            viewTexture = new RenderTexture(baseWidth, calculatedHeight, 24);
            viewTexture.Create();

            // Assign the aspect ratio to the portal camera
            portalCam.aspect = (float)baseWidth / calculatedHeight;
        }
    }

    void OnValidate()
    {
        if (linkedPainting != null && linkedPainting.linkedPainting != this)
        {
            linkedPainting.linkedPainting = this;
        }
    }
}