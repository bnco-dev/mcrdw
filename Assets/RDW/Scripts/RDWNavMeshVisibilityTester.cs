using UnityEngine;
using UnityEngine.AI;

public class RDWNavMeshVisibilityTester : ARDWVisibilityTester {

    public string walkableAreaName = "Walkable";

    int _walkableAreaMask;

    void Awake () {
        _walkableAreaMask = 1 << NavMesh.GetAreaFromName("Walkable");
    }

    public override bool Visible (Vector3 a, Vector3 b) {
        return NavMesh.Raycast(a,b,out _,_walkableAreaMask);
    }
}