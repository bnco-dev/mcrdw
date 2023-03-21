using System;
using UnityEngine;

public abstract class ARDWVisibilityTester : MonoBehaviour {
    public abstract bool Visible (Vector3 a, Vector3 b);
}