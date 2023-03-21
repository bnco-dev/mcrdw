using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RDWTrackSpace))]
public class RDWChildTransformTracker : MonoBehaviour {

    public Transform childTransform;
    public bool useFixedTimestep = false;
    public float fixedTimestep = 0.016666f;

    void Awake () {
        _trackSpace = GetComponent<RDWTrackSpace>();
    }

    void OnEnable () {
        if (!childTransform || childTransform.parent != transform) {
            Debug.LogError("Child of the RDWTrackSpace object expected");
            enabled = false;
            return;
        }
    }

    void LateUpdate () {
        // Debug.Log(Time.frameCount + " : WP : " + childTransform.position.ToStringEx());
        if (useFixedTimestep) {
            _trackSpace.Step(childTransform.localPosition, childTransform.localRotation,fixedTimestep);
        } else {
            _trackSpace.Step(childTransform.localPosition, childTransform.localRotation);
        }
    }

    RDWTrackSpace _trackSpace;
}
