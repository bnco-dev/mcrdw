using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RDWWaypointWalker))]
public class RDWWaypointOnClick : MonoBehaviour {

    public Camera overheadCamera;

    void Awake () {
        _walker = GetComponent<RDWWaypointWalker>();
    }

    void Update () {
        if (Input.GetMouseButtonDown(0)) {
            var pt = overheadCamera.ScreenToWorldPoint(Input.mousePosition);
            pt.y = transform.position.y;
            _walker.waypoints.Add(pt);
        }
    }

    RDWWaypointWalker _walker;
}
