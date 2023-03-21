using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RDWTrackSpace))]
public class RDWManualChildTransformTracker : MonoBehaviour {

    public KeyCode stepKey;
    public float stepTime;
    public Transform childTransform;

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

    void Update () {
        if (Input.GetKeyDown(stepKey)) {
            _updateFrame = Time.frameCount;
        }
    }

    void LateUpdate () {
        if (_updateFrame == Time.frameCount) {
            var childPos = childTransform.localPosition;
            var childRot = childTransform.localRotation;
            _trackSpace.Step(childPos,childRot,stepTime);
        }
    }

    int _updateFrame = -1;
    RDWTrackSpace _trackSpace;
}
