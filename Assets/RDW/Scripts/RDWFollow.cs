using UnityEngine;

public class RDWFollow : MonoBehaviour {

    public Transform toFollow;

    void Awake () {
        _transform = transform;
    }

    void LateUpdate () {
        // Follow XZ only        
        _transform.position = new Vector3(
            toFollow.position.x,
            _transform.position.y,
            toFollow.position.z
            );
    }

    Transform _transform;
}
