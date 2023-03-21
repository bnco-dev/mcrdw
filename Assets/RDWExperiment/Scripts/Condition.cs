using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RDWMCExperiment {
[System.Serializable]
public class Condition {

public string name;
public float timeStep = 0.016666f;
public float walkSpeedMetersPerSecond = 1.0f;
public float turnSpeedDegreesPerSecond = 90.0f;
public Vector2 trackSpaceDimensions = Vector2.one*6.0f;
public RDWTrackSpace prefab;

}
}