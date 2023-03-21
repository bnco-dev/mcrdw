using UnityEngine;
using System.Collections;

public class RDWOverheadCameraKeyboardController : MonoBehaviour {

    public float speed = 5.0f;

    public KeyCode forward = KeyCode.UpArrow;
    public KeyCode backward = KeyCode.DownArrow;
    public KeyCode moveLeft = KeyCode.LeftArrow;
    public KeyCode moveRight = KeyCode.RightArrow;

    void Update () {
        var fwd = (Input.GetKey(forward) ? 1 : 0) -
            (Input.GetKey(backward) ? 1 : 0);
        var right = (Input.GetKey(moveRight) ? 1 : 0) -
            (Input.GetKey(moveLeft) ? 1 : 0);

        var dir = fwd * transform.up + right * transform.right;
        transform.position += dir.normalized * speed * Time.deltaTime;
    }
}
