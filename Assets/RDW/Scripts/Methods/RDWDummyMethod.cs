using UnityEngine;
using System.Collections;

public class RDWDummyMethod : RDWMethod {

    override protected void Awake () {
        base.Awake();
    }

    public override void _OnAttach () { }

    public override void Discontinuity () { }

    public override IEnumerator Step (Vector2 trackPos, float trackDir, float deltaTime) {

        yield break;
    }
}
