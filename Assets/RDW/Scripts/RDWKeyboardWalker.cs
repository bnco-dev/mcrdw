using UnityEngine;
using System.Collections;

public class RDWKeyboardWalker : MonoBehaviour {

    public float forwardSpeed = 1.38582f;
    public float turnSpeed = 90.0f;

    public KeyCode forward = KeyCode.W;
    public KeyCode backward = KeyCode.S;
    public KeyCode turnLeft = KeyCode.A;
    public KeyCode turnRight = KeyCode.D;

    void OnEnable () {
        _rot = transform.rotation;
    }

    void Update () {
        var fwd = (Input.GetKey(forward) ? 1 : 0) -
            (Input.GetKey(backward) ? 1 : 0);
        var right = (Input.GetKey(turnRight) ? 1 : 0) -
            (Input.GetKey(turnLeft) ? 1 : 0);

        var deltaRot = Quaternion.AngleAxis(right * turnSpeed * Time.deltaTime, Vector3.up);
        _rot *= deltaRot;

        transform.rotation = _rot;
        transform.position += fwd * forwardSpeed * Time.deltaTime * transform.forward;
    }

    Quaternion _rot;
}
