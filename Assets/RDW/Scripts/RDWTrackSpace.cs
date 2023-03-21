using UnityEngine;
using System;
using System.Collections;

public class RDWTrackSpace : MonoBehaviour {

    [SerializeField, HideInInspector]
    private Transform __trackSpaceTransform;
    public Transform trackSpaceTransform {
        get {
            if (__trackSpaceTransform == null) {
                trackSpaceTransform = GetComponent<Transform>();
            }

            return __trackSpaceTransform;
        }
        set {
            if (__trackSpaceTransform != value) {
                __trackSpaceTransform = value;
#if UNITY_EDITOR
                _trackSpaceTransform = __trackSpaceTransform;
#endif
                trackSpaceRootChanged();
            }
        }
    }

    [SerializeField, HideInInspector]
    private Rect __bounds;
    public Rect bounds {
        get { return __bounds; }
        set {
            if (__bounds != value) {
                __bounds = value;
#if UNITY_EDITOR
                _bounds = __bounds;
#endif
                boundsChanged();
            }
        }
    }

#if UNITY_EDITOR
    [SerializeField]
    private Rect _bounds;
    [SerializeField]
    private Transform _trackSpaceTransform;

    void OnValidate () {
        bounds = _bounds;
        trackSpaceTransform = _trackSpaceTransform;
    }
#endif

    public event Action boundsChanged = delegate { };
    public event Action trackSpaceRootChanged = delegate { };

    void Awake () {
        if (!trackSpaceTransform) {
            trackSpaceTransform = GetComponent<Transform>();
        }
    }

    public Vector3 TrackToWorldPosition (Vector2 trackPos) {
        return trackSpaceTransform.TransformPoint(trackPos.x, 0, trackPos.y);
    }

    public Vector3 TrackToWorldDirection (float trackDir) {
        var dirVec = Quaternion.Euler(0,trackDir,0) * Vector3.forward;
        return trackSpaceTransform.TransformDirection(dirVec);
    }

    public Vector2 WorldToTrackPosition (Vector3 worldPos) {
        var tp3 = trackSpaceTransform.InverseTransformPoint(worldPos);
        return new Vector2(tp3.x,tp3.z);
    }

    public float WorldToTrackDirection (Vector3 worldDir) {
        return trackSpaceTransform.InverseTransformDirection(worldDir).y;
    }

    public RDWMethod method { get; private set; }

    public void Attach (RDWMethod newMethod) {
        if (method == newMethod) {
            return;
        }

        Detach();

        method = newMethod;
        method.enabled = true;
        method._OnAttach();
    }

    public void Detach () {
        if (method != null || method) {
            method._OnDetach();
            method.enabled = false;
            method = null;
        }
    }

    public void Discontinuity () { 
        if (method != null || method) {
            method.Discontinuity();
        }
    }

    public IEnumerator Step (Vector3 trackPos, Quaternion trackDir) {
        // Detach();
        _Step(trackPos, trackDir, Time.deltaTime);
        return null;
        // return Step(trackPos, trackDir, Time.deltaTime);
    }

    public IEnumerator Step (Vector3 trackPos, Quaternion trackDir, float deltaTime) {

        _Step(trackPos, trackDir, deltaTime);
        return null;
    }

    void _Step (Vector3 trackPos, Quaternion trackDir, float deltaTime) {
        // 2d only for now
        var tPos = new Vector2(trackPos.x, trackPos.z);
        var tDir = trackDir.eulerAngles.y;

        // UnityEngine.Debug.Log(method);
        if (method) {
            // UnityEngine.Debug.Log(method);
            StartCoroutine(method.Step(tPos, tDir, deltaTime));
        }
    }
}

#if UNITY_EDITOR
public class RDWTrackSpaceEditor : UnityEditor.Editor {
    public override void OnInspectorGUI () {

        DrawDefaultInspector();
    }
}
#endif