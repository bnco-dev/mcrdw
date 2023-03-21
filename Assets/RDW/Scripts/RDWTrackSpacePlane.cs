using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class RDWTrackSpacePlane : MonoBehaviour {

    public float sizeMult = 1.0f;

    [SerializeField, HideInInspector]
    private RDWTrackSpace __trackSpace;
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
        var trans = GetComponent<Transform>();
        var bounds = trackSpace.bounds;

        if (bounds.width == 0 || bounds.height == 0) {
            return;
        }

        trans.localPosition = new Vector3(bounds.center.x, 0, bounds.center.y);
        trans.localScale = new Vector3(bounds.width*sizeMult, 0.01f, bounds.height*sizeMult);
    }
}
