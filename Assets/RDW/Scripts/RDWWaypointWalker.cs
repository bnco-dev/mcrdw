using UnityEngine;
using System.Collections.Generic;

public class RDWWaypointWalker : MonoBehaviour {

    public float forwardSpeed = 1.38582f;
    public float turnSpeed = 90.0f;

    public bool useFixedTimestep = false;
    public float fixedTimestep = 0.016666f;

    public List<Vector3> waypoints;

    void Awake () {
        _prevPos = transform.position;
        _prevRot = transform.rotation;
    }

    void Update () {
        if (useFixedTimestep) {
            Step(fixedTimestep);
        } else {
            Step(Time.deltaTime);
        }
    }

    public void Step (float deltaTime) {
        transform.position = _prevPos;
        transform.rotation = _prevRot;

        var time = deltaTime;
        while (time > 0 && waypoints.Count > 0) {
            var to = waypoints[0] - transform.position;
            var rot = Quaternion.FromToRotation(transform.forward, to);

            var toDir = Angle(new Vector2(to.x, to.z)) + 180.0f;
            var fwdDir = Angle(new Vector2(transform.forward.x, transform.forward.z)) + 180.0f;
            var angle = Mathf.DeltaAngle(fwdDir,toDir);

            time -= Mathf.Abs(angle) / turnSpeed;

            if (time < 0) {
                time = 0;
                angle = deltaTime * turnSpeed * Mathf.Sign(angle);
            }

            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y + angle, 0);

            if (time * forwardSpeed > to.magnitude) {
                transform.position = waypoints[0];
                waypoints.RemoveAt(0);
                time -= to.magnitude / forwardSpeed;
            } else {
                transform.position += to.normalized * time * forwardSpeed;
                time = 0;
            }
        }

        _prevPos = transform.position;
        _prevRot = transform.rotation;
    }

    static float Angle (Vector2 v) {
        return Vector2.Angle(v, Vector2.up) * (v.x < 0 ? -1 : 1);
    }

    Vector3 _prevPos;
    Quaternion _prevRot;
}
