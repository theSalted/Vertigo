using UnityEngine;

public class MainCamera : MonoBehaviour {

    Portal[] portals;
    PaintingPortal[] paintings;

    void Awake () {
        portals = FindObjectsOfType<Portal> ();
        paintings = FindObjectsOfType<PaintingPortal> ();
    }

    void OnPreCull () {

        for (int i = 0; i < portals.Length; i++) {
            portals[i].PrePortalRender ();
        }
        
        for (int i = 0; i < portals.Length; i++) {
            portals[i].Render();
        }

        for (int i = 0; i < portals.Length; i++) {
            portals[i].PostPortalRender ();
        }

        for (int i = 0; i < paintings.Length; i++) {
            paintings[i].PrePortalRender ();
        }

        for (int i = 0; i < paintings.Length; i++) {
            paintings[i].Render();
        }

        for (int i = 0; i < paintings.Length; i++) {
            paintings[i].PostPortalRender ();
        }

    }   

}