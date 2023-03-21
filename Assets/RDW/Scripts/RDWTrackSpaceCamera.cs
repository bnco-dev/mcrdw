using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RDWTrackSpaceCamera : MonoBehaviour {
    
    [SerializeField, HideInInspector] private RDWTrackSpace __trackSpace;
    public RDWTrackSpace trackSpace {
        get { return __trackSpace; } 
        set { if (!Application.isPlaying) { __trackSpace = value; } }
    }

#if UNITY_EDITOR
    [SerializeField] private RDWTrackSpace _trackSpace;
    void OnValidate () {
        trackSpace = _trackSpace;
    }
#endif

    void OnEnable () {
        if (!trackSpace) {
            Debug.LogWarning("Track space required. Disabling...");
            enabled = false;
            return;            
        }

        BoundsChanged();

        trackSpace.boundsChanged += BoundsChanged;
    }

    void OnDisable () {
        if (!trackSpace) {
            return;            
        }

        trackSpace.boundsChanged -= BoundsChanged;
    }

    void BoundsChanged () {
        var cam = GetComponent<Camera>();
        var trans = GetComponent<Transform>();
        var bounds = trackSpace.bounds;

        if (bounds.width == 0 || bounds.height == 0) {
            return;
        }
        
        trans.localPosition = new Vector3(bounds.center.x, 100, bounds.center.y);
        var aspect = cam.pixelWidth / (float)cam.pixelHeight;
        if (bounds.width / bounds.height > aspect) {
            cam.orthographicSize = bounds.width / (2.0f * aspect);
        } else {
            cam.orthographicSize = bounds.height / 2.0f;
        }                
    }
}
