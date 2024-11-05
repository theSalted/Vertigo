using System;
using System.Collections;
using InputAssets;
using UnityEngine;

public class LinkedPortalController : MonoBehaviour
{
    public LinkedPortalController linkedPortal;

    public bool debugMode = false;

    public enum PortalMode { Portal, Painting }
    public PortalMode mode = PortalMode.Painting;

    // Enum for determining portal state
    public enum PortalState { Leader, Follower }
    public PortalState state = PortalState.Leader;

    public Portal portal;
    public PaintingPortal painting;

    [Tooltip("Painting Mode Settings")]
    public float fov = 17.2f;
    public float rotationYOffset = 0.0f;

    [Tooltip("Detection Volume Settings")]
    public float portalWidth = 2.0f;      // Width of the portal (X-axis)
    public float portalHeight = 4.0f;     // Height of the portal (Y-axis)
    public float viewingDistance = 10.0f; // Depth of the detection volume (Z-axis)

    [HideInInspector]
    public bool isPlayerInRange = false;

    private Camera playerCam;
    private FirstPersonController playerController;

    public float transitionDuration = 1.0f; // Duration of the position and rotation interpolation
    public float pushDistance = 0.0f;       // Distance to push the player forward after switching modes
    public float pushDuration = 0.5f;       // Duration of the push after the portal mode switches

    private bool isTransitioning = false;   // Flag to check if this portal is transitioning

    private bool readyToEndTransition = false;

    private void OnValidate()
    {
        // Establish link and leader/follower relationship in the editor
        if (linkedPortal != null && linkedPortal.linkedPortal != this)
        {
            linkedPortal.linkedPortal = this;
            InitializeComponentsAndLinks();
            DetermineLeaderFollower();
        }
        else if (linkedPortal == null)
        {
            ResetState();
        }
    }

    private void OnDestroy()
    {
        // Unlink the other portal when this object is destroyed
        if (linkedPortal != null && linkedPortal.linkedPortal == this)
        {
            linkedPortal.linkedPortal = null;
            linkedPortal.ResetState();
        }
    }

    /// <summary>
    /// Caches the portal components and sets up links between this portal
    /// and the linked portal's components.
    /// </summary>
    private void InitializeComponentsAndLinks()
    {
        portal = GetComponentInChildren<Portal>();
        painting = GetComponentInChildren<PaintingPortal>();

        if (linkedPortal != null)
        {
            linkedPortal.portal = linkedPortal.GetComponentInChildren<Portal>();
            linkedPortal.painting = linkedPortal.GetComponentInChildren<PaintingPortal>();

            portal.linkedPortal = linkedPortal.portal;
            painting.linkedPainting = linkedPortal.painting;
        }
    }

    /// <summary>
    /// Resets this portal's state to be the Leader by default.
    /// </summary>
    private void ResetState()
    {
        state = PortalState.Leader;
    }

    /// <summary>
    /// Determines which portal is the Leader based on instance ID. 
    /// The portal with the lower instance ID becomes the Leader.
    /// </summary>
    private void DetermineLeaderFollower()
    {
        if (linkedPortal == null)
        {
            Debug.LogError("Portal is not linked to another portal.");
            return;
        }

        if (GetInstanceID() < linkedPortal.GetInstanceID())
        {
            state = PortalState.Leader;
            linkedPortal.state = PortalState.Follower;
        }
        else
        {
            state = PortalState.Follower;
            linkedPortal.state = PortalState.Leader;
        }
    }

    /// <summary>
    /// Ensures all necessary portal components are linked and initialized.
    /// Logs errors if any links are missing.
    /// </summary>
    private void InitializationCheck()
    {
        if (portal == null || painting == null)
        {
            Debug.LogError("Portal components are not assigned.");
        }

        if (portal.linkedPortal == null || painting.linkedPainting == null)
        {
            Debug.LogError("Linked portal components are not assigned.");
        }
    }

    private void Awake()
    {
        // Initialize components and set up links
        InitializeComponentsAndLinks();
        DetermineLeaderFollower();
    }

    private void Start()
    {
        InitializationCheck();
        playerCam = Camera.main;
        painting.portalCam.fieldOfView = fov;
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<FirstPersonController>();
    }

    private void Update()
    {
        // Both portals check if the player is in range
        CheckPlayerInRange();

        if (state == PortalState.Leader)
        {
            // Leader portal manages the transition
            if ((isPlayerInRange || linkedPortal.isPlayerInRange) && !isTransitioning)
            {
                Transform targetPortalTransform = isPlayerInRange ? transform : linkedPortal.transform;

                
                
                float targetViewHeight = isPlayerInRange ? painting.viewingHeight : linkedPortal.painting.viewingHeight;

                Vector3 toPlayer = playerCam.transform.position - targetPortalTransform.position;
                float side = Mathf.Sign(Vector3.Dot(toPlayer, targetPortalTransform.forward));
                Vector3 rendezvousPos = targetPortalTransform.position + targetPortalTransform.forward * viewingDistance * side;
                rendezvousPos += targetPortalTransform.up * targetViewHeight;

                float targetPortalRotateY = side == 1 ? targetPortalTransform.eulerAngles.y + 180 : targetPortalTransform.eulerAngles.y;
                // Define the coordinates for the transition
                Vector3 startPosition = playerController.transform.position;
                Quaternion startRotation = playerController.transform.rotation;

                // Coordinates to move to in the first stage (linked portal's painting position)
                Vector3 stage1Position = rendezvousPos;
                stage1Position.y = playerController.transform.position.y; // Keep player's current Y position
                Quaternion stage1Rotation = Quaternion.Euler(0, targetPortalRotateY, 0);

                // Coordinates to move to in the second stage (this portal's position, slightly forward)
                Vector3 stage2Position = targetPortalTransform.position + targetPortalTransform.forward * pushDistance;

                // Start the transition
                StartCoroutine(TransitionPlayer(startPosition, startRotation, stage1Position, stage1Rotation, stage2Position));
            }
        }

        // Update portal mode based on the current mode
        if (mode == PortalMode.Portal)
        {
            portal.screen.enabled = true;
            portal.portalCam.enabled = true;

            painting.screen.gameObject.SetActive(false);
            painting.portalCam.enabled = false;
        }
        else
        {
            portal.screen.enabled = false;
            portal.portalCam.enabled = false;

            painting.screen.gameObject.SetActive(true);
            painting.portalCam.enabled = true;
        }

        if (readyToEndTransition && !(isPlayerInRange || linkedPortal.isPlayerInRange) && !(isPlayerInRadius() || linkedPortal.isPlayerInRadius()))
        {
            Debug.Log("Transition complete.");
            isTransitioning = false;
            linkedPortal.isTransitioning = false;
            readyToEndTransition = false;
        }

        if (debugMode)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = new Vector3(
                painting.portalCam.transform.position.x,
                player.transform.position.y,
                painting.portalCam.transform.position.z
            );
        }
    }

    // Detect if player is within a certain radius of the portal
    private bool isPlayerInRadius()
    {
        if (Vector3.Distance(playerCam.transform.position, transform.position) < portalWidth)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private IEnumerator TransitionPlayer(Vector3 startPos, Quaternion startRot, Vector3 stage1Pos, Quaternion stage1Rot, Vector3 stage2Pos)
    {
        isTransitioning = true;
        linkedPortal.isTransitioning = true;

        // Disable player movement and rotation
        playerController.allowCameraRotation = false;
        playerController.allowMovement = false;

        float elapsedTime = 0f;

        // Stage 1: Move to stage1Pos and stage1Rot
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;

            playerController.transform.position = Vector3.Lerp(startPos, stage1Pos, t);
            playerController.transform.rotation = Quaternion.Slerp(startRot, stage1Rot, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final position and rotation are set
        playerController.transform.position = stage1Pos;
        playerController.transform.rotation = stage1Rot;

        // Switch portal mode from painting to portal
        mode = PortalMode.Portal;
        linkedPortal.mode = mode;
        // wait for a short time before pushing the player
        // yield return new WaitForSeconds(0.5f);
        // yield return new WaitForSecondsRealtime(120f);
        // Stage 2: Move to stage2Pos
        elapsedTime = 0f;

        while (elapsedTime < pushDuration)
        {
            float t = elapsedTime / pushDuration;

            playerController.transform.position = Vector3.Lerp(stage1Pos, stage2Pos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final position is set
        playerController.transform.position = stage2Pos;
        // rotation should be zero
        // Re-enable player movement and rotation
        playerController.ResetYawAndPitch();
        playerController.allowCameraRotation = true;
        playerController.allowMovement = true;

        // Keep isTransitioning = true to prevent re-triggering during testing
        // Uncomment the lines below when ready to re-enable detection
        // yield return new WaitForSeconds(0.5f);
        // readyToEndTransition = true;
    }

    /// <summary>
    /// Checks if the player is within the 3D diamond-shaped detection volume (octahedron).
    /// Sets the isPlayerInRange variable accordingly.
    /// </summary>
    private void CheckPlayerInRange()
    {
        if (playerCam == null)
        {
            playerCam = Camera.main;
            if (playerCam == null)
            {
                Debug.LogError("Player camera not found.");
                return;
            }
        }

        Vector3 localCamPos = transform.InverseTransformPoint(playerCam.transform.position);

        // Adjust Y position to align with portal center
        float yOffset = portalHeight / 2f;
        float adjustedY = localCamPos.y - yOffset;

        // Normalize positions
        float normalizedX = localCamPos.x / (portalWidth / 2f);
        float normalizedY = adjustedY / (portalHeight / 2f);
        float normalizedZ = localCamPos.z / viewingDistance;

        // Check if the player is within the octahedron
        if (Mathf.Abs(normalizedX) + Mathf.Abs(normalizedY) + Mathf.Abs(normalizedZ) <= 1f)
        {
            isPlayerInRange = true;
        }
        else
        {
            isPlayerInRange = false;
        }
    }

    private bool IsEitherTransitioning()
    {
        return isTransitioning || (linkedPortal != null && linkedPortal.isTransitioning);
    }

    private void OnDrawGizmos()
    {
        // Draw the detection volume (octahedron) in the editor
        Gizmos.color = Color.green;

        // Adjust Y positions to align with portal center
        float yOffset = portalHeight / 2f;

        // Define the six vertices of the octahedron in local space
        Vector3 top = new Vector3(0, yOffset + (portalHeight / 2f), 0);    // Top vertex
        Vector3 bottom = new Vector3(0, yOffset - (portalHeight / 2f), 0); // Bottom vertex
        Vector3 front = new Vector3(0, yOffset, viewingDistance);          // Front vertex
        Vector3 back = new Vector3(0, yOffset, -viewingDistance);          // Back vertex
        Vector3 left = new Vector3(-portalWidth / 2f, yOffset, 0);         // Left vertex
        Vector3 right = new Vector3(portalWidth / 2f, yOffset, 0);         // Right vertex

        // Transform to world space
        top = transform.TransformPoint(top);
        bottom = transform.TransformPoint(bottom);
        front = transform.TransformPoint(front);
        back = transform.TransformPoint(back);
        left = transform.TransformPoint(left);
        right = transform.TransformPoint(right);

        // Draw edges
        // Top edges
        Gizmos.DrawLine(top, front);
        Gizmos.DrawLine(top, back);
        Gizmos.DrawLine(top, left);
        Gizmos.DrawLine(top, right);

        // Bottom edges
        Gizmos.DrawLine(bottom, front);
        Gizmos.DrawLine(bottom, back);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(bottom, right);

        // Middle edges
        Gizmos.DrawLine(front, left);
        Gizmos.DrawLine(front, right);
        Gizmos.DrawLine(back, left);
        Gizmos.DrawLine(back, right);
    }
}