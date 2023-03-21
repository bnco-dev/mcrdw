using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RDWTrackSpace))]
public abstract class RDWMethod : MonoBehaviour {

    public RDWTrackSpace trackSpace { get; private set; }

    protected virtual void Awake () {
        trackSpace = GetComponent<RDWTrackSpace>();
    }

    protected virtual void OnEnable () {
        trackSpace.Attach(this);
    }

    protected virtual void OnDisable () {
        if (trackSpace && trackSpace.method == this) {
            trackSpace.Detach();
        }
    }

    public virtual void _OnAttach () { }
    public virtual void _OnDetach () { }

    public abstract void Discontinuity ();
    public abstract IEnumerator Step (Vector2 trackPos, float trackDir, float deltaTime);
}
