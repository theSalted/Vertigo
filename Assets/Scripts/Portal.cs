using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {
    [Header("Main Settings")]
    public Portal linkedPortal;
    public MeshRenderer screen;
    public int recursionLimit = 5;

    [Header("Advanced Settings")]
    public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;

    [Header("Projection Settings")]
    [Tooltip("Adjusts the amount of projection exaggeration. Default is 1.")]
    public float projectionScale = 1.0f;

    // Private variables
    RenderTexture viewTexture;
    Camera portalCam;
    Camera playerCam;
    List<PortalTraveller> trackedTravellers;
    MeshFilter screenMeshFilter;
    MeshFilter linkedScreenMeshFilter;

    void Awake() {
        playerCam = Camera.main;
        portalCam = GetComponentInChildren<Camera>();
        portalCam.enabled = false;
        trackedTravellers = new List<PortalTraveller>();
        screenMeshFilter = screen.GetComponent<MeshFilter>();
        linkedScreenMeshFilter = linkedPortal.screen.GetComponent<MeshFilter>();
        screen.material.SetInt("displayMask", 1);
    }

    void LateUpdate() {
        HandleTravellers();
    }

    void HandleTravellers() {
        for (int i = 0; i < trackedTravellers.Count; i++) {
            PortalTraveller traveller = trackedTravellers[i];
            Transform travellerT = traveller.transform;

            // Compute transformation matrix from traveller to linked portal
            Matrix4x4 m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix;

            Vector3 offsetFromPortal = travellerT.position - transform.position;
            int portalSide = System.Math.Sign(Vector3.Dot(offsetFromPortal, transform.forward));
            int portalSideOld = System.Math.Sign(Vector3.Dot(traveller.previousOffsetFromPortal, transform.forward));

            // Teleport the traveller if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld) {
                var positionOld = travellerT.position;
                var rotationOld = travellerT.rotation;
                var scaleOld = travellerT.localScale;

                // Decompose the matrix to get position and rotation
                Vector3 newPosition = m.GetColumn(3);
                Quaternion newRotation = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));

                // Determine scale factor based on the direction
                float scaleFactor = 0.5f;
                if (traveller.transform.localScale.x < 1.0f) {
                    // If already scaled down, scale back up
                    scaleFactor = 2.0f;
                }

                Vector3 newScale = traveller.transform.localScale * scaleFactor;

                traveller.Teleport(transform, linkedPortal.transform, newPosition, newRotation, newScale);

                traveller.graphicsClone.transform.SetPositionAndRotation(positionOld, rotationOld);
                traveller.graphicsClone.transform.localScale = scaleOld;

                linkedPortal.OnTravellerEnterPortal(traveller);
                trackedTravellers.RemoveAt(i);
                i--;

            } else {
                // Update graphics clone
                traveller.graphicsClone.transform.SetPositionAndRotation(traveller.transform.position, traveller.transform.rotation);
                traveller.graphicsClone.transform.localScale = traveller.transform.localScale;

                traveller.previousOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    // Convert Matrix4x4 to position, rotation, and scale
    void MatrixToTRS(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale) {
        position = matrix.GetColumn(3);

        // Extract rotation and scale from the matrix
        scale = new Vector3(
            matrix.GetColumn(0).magnitude,
            matrix.GetColumn(1).magnitude,
            matrix.GetColumn(2).magnitude
        );

        // Normalize the basis vectors to remove scale from rotation
        Vector3 forward = matrix.GetColumn(2) / scale.z;
        Vector3 up = matrix.GetColumn(1) / scale.y;
        rotation = Quaternion.LookRotation(forward, up);
    }

    // Called before any portal cameras are rendered for the current frame
    public void PrePortalRender() {
        foreach (var traveller in trackedTravellers) {
            UpdateSliceParams(traveller);
        }
    }

    // Manually render the camera attached to this portal
    // Called after PrePortalRender, and before PostPortalRender
    public void Render() {

        // Skip rendering the view from this portal if player is not looking at the linked portal
        if (!CameraUtility.VisibleFromCamera(linkedPortal.screen, playerCam)) {
            return;
        }

        CreateViewTexture();

        var localToWorldMatrix = playerCam.transform.localToWorldMatrix;
        var renderPositions = new Vector3[recursionLimit];
        var renderRotations = new Quaternion[recursionLimit];
        var renderScales = new Vector3[recursionLimit];

        int startIndex = 0;

        for (int i = 0; i < recursionLimit; i++) {
            if (i > 0) {
                // No need for recursive rendering if linked portal is not visible through this portal
                if (!CameraUtility.BoundsOverlap(screenMeshFilter, linkedPortal.screenMeshFilter, portalCam)) {
                    break;
                }
            }
            localToWorldMatrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * localToWorldMatrix;

            // Decompose the matrix to get position, rotation, and scale
            Vector3 position;
            Quaternion rotation;
            Vector3 scale;
            MatrixToTRS(localToWorldMatrix, out position, out rotation, out scale);

            int renderOrderIndex = recursionLimit - i - 1;
            renderPositions[renderOrderIndex] = position;
            renderRotations[renderOrderIndex] = rotation;
            renderScales[renderOrderIndex] = scale;

            startIndex = renderOrderIndex;
        }

        // Hide screen so that camera can see through portal
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        linkedPortal.screen.material.SetInt("displayMask", 0);

        for (int i = startIndex; i < recursionLimit; i++) {
            portalCam.transform.SetPositionAndRotation(renderPositions[i], renderRotations[i]);
            // Do not scale the camera's transform

            // Reset the portal camera's field of view
            portalCam.fieldOfView = playerCam.fieldOfView;

            // Adjust the camera's field of view to account for scaling
            float scaleFactor = renderScales[i].x; // Assuming uniform scaling
            AdjustCameraForScale(portalCam, scaleFactor);

            SetNearClipPlane();
            HandleClipping();
            portalCam.Render();

            if (i == startIndex) {
                linkedPortal.screen.material.SetInt("displayMask", 1);
            }
        }

        // Unhide objects hidden at start of render
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    // Adjust the camera's field of view to account for scaling
    void AdjustCameraForScale(Camera cam, float scaleFactor) {
        // Apply the projectionScale to the scaleFactor
        float adjustedScaleFactor = scaleFactor * projectionScale;

        // Calculate the new field of view based on the adjusted scale factor
        float originalFOV = playerCam.fieldOfView;
        float newFOV = 2f * Mathf.Atan(Mathf.Tan(originalFOV * Mathf.Deg2Rad * 0.5f) / adjustedScaleFactor) * Mathf.Rad2Deg;

        // Apply the new field of view to the portal camera
        cam.fieldOfView = newFOV;
        cam.aspect = playerCam.aspect;
    }

    // Use custom projection matrix to align portal camera's near clip plane with the surface of the portal
    void SetNearClipPlane() {
        Transform clipPlane = transform;
        int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, transform.position - portalCam.transform.position));

        Vector3 camSpacePos = portalCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = portalCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal) + nearClipOffset;

        if (Mathf.Abs(camSpaceDst) > nearClipLimit) {
            Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

            // Update projection based on new clip plane
            portalCam.projectionMatrix = portalCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
    }

    void HandleClipping() {
        // Handle slicing of objects to prevent them from appearing through the portal
        float screenThickness = linkedPortal.ProtectScreenFromClipping(portalCam.transform.position);

        foreach (var traveller in trackedTravellers) {
            if (SameSideOfPortal(traveller.transform.position, portalCamPos)) {
                traveller.SetSliceOffsetDst(0.1f, false);
            } else {
                traveller.SetSliceOffsetDst(-0.1f, false);
            }

            int cloneSideOfLinkedPortal = -SideOfPortal(traveller.transform.position);
            bool camSameSideAsClone = linkedPortal.SideOfPortal(portalCamPos) == cloneSideOfLinkedPortal;
            if (camSameSideAsClone) {
                traveller.SetSliceOffsetDst(screenThickness, true);
            } else {
                traveller.SetSliceOffsetDst(-screenThickness, true);
            }
        }

        var offsetFromPortalToCam = portalCamPos - transform.position;
        foreach (var linkedTraveller in linkedPortal.trackedTravellers) {
            var travellerPos = linkedTraveller.graphicsObject.transform.position;
            var clonePos = linkedTraveller.graphicsClone.transform.position;
            bool cloneOnSameSideAsCam = linkedPortal.SideOfPortal(travellerPos) != SideOfPortal(portalCamPos);
            if (cloneOnSameSideAsCam) {
                linkedTraveller.SetSliceOffsetDst(0.1f, true);
            } else {
                linkedTraveller.SetSliceOffsetDst(-0.1f, true);
            }

            bool camSameSideAsTraveller = linkedPortal.SameSideOfPortal(linkedTraveller.transform.position, portalCamPos);
            if (camSameSideAsTraveller) {
                linkedTraveller.SetSliceOffsetDst(screenThickness, false);
            } else {
                linkedTraveller.SetSliceOffsetDst(-screenThickness, false);
            }
        }
    }

    // Called once all portals have been rendered, but before the player camera renders
    public void PostPortalRender() {
        foreach (var traveller in trackedTravellers) {
            UpdateSliceParams(traveller);
        }
        ProtectScreenFromClipping(playerCam.transform.position);
    }

    void CreateViewTexture() {
        int width = Screen.width;
        int height = Screen.height;

        if (viewTexture == null || viewTexture.width != width || viewTexture.height != height) {
            if (viewTexture != null) {
                viewTexture.Release();
            }
            viewTexture = new RenderTexture(width, height, 24);
            // Render the view from the portal camera to the view texture
            portalCam.targetTexture = viewTexture;
            // Display the view texture on the screen of the linked portal
            linkedPortal.screen.material.SetTexture("_MainTex", viewTexture);
        }
    }

    // Updates the slice parameters for the traveller's materials
    void UpdateSliceParams(PortalTraveller traveller) {
        // Calculate slice normal
        int side = SideOfPortal(traveller.transform.position);
        Vector3 sliceNormal = transform.forward * -side;
        Vector3 cloneSliceNormal = linkedPortal.transform.forward * side;

        // Calculate slice centre
        Vector3 slicePos = transform.position;
        Vector3 cloneSlicePos = linkedPortal.transform.position;

        // Apply parameters
        for (int i = 0; i < traveller.originalMaterials.Length; i++) {
            traveller.originalMaterials[i].SetVector("sliceCentre", slicePos);
            traveller.originalMaterials[i].SetVector("sliceNormal", sliceNormal);

            traveller.cloneMaterials[i].SetVector("sliceCentre", cloneSlicePos);
            traveller.cloneMaterials[i].SetVector("sliceNormal", cloneSliceNormal);
        }
    }

    // Sets the thickness of the portal screen to prevent clipping
    float ProtectScreenFromClipping(Vector3 viewPoint) {
        float halfHeight = playerCam.nearClipPlane * Mathf.Tan(playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCam.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, playerCam.nearClipPlane).magnitude;
        float screenThickness = dstToNearClipPlaneCorner;

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - viewPoint) > 0;
        screenT.localScale = new Vector3(screenT.localScale.x, screenT.localScale.y, screenThickness);
        screenT.localPosition = Vector3.forward * screenThickness * (camFacingSameDirAsPortal ? 0.5f : -0.5f);
        return screenThickness;
    }

    void OnTravellerEnterPortal(PortalTraveller traveller) {
        if (!trackedTravellers.Contains(traveller)) {
            traveller.EnterPortalThreshold();
            traveller.previousOffsetFromPortal = traveller.transform.position - transform.position;
            trackedTravellers.Add(traveller);
        }
    }

    void OnTriggerEnter(Collider other) {
        var traveller = other.GetComponent<PortalTraveller>();
        if (traveller) {
            OnTravellerEnterPortal(traveller);
        }
    }

    void OnTriggerExit(Collider other) {
        var traveller = other.GetComponent<PortalTraveller>();
        if (traveller && trackedTravellers.Contains(traveller)) {
            traveller.ExitPortalThreshold();
            trackedTravellers.Remove(traveller);
        }
    }

    /*
     ** Helper methods:
     */

    int SideOfPortal(Vector3 pos) {
        return System.Math.Sign(Vector3.Dot(pos - transform.position, transform.forward));
    }

    bool SameSideOfPortal(Vector3 posA, Vector3 posB) {
        return SideOfPortal(posA) == SideOfPortal(posB);
    }

    Vector3 portalCamPos {
        get {
            return portalCam.transform.position;
        }
    }

    void OnValidate() {
        if (linkedPortal != null) {
            linkedPortal.linkedPortal = this;
        }
    }
}