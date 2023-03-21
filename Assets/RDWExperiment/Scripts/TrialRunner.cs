using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RDWMCExperiment {
public class TrialRunner : MonoBehaviour {

public enum RunMode {
    Realtime,
    RenderAfterEachFrame,
    NonInteractive
}

public class Results {
    public float totalScaleModMeters;
    public float totalTurnModMeters;
    public int collisions;
}

class FormattedLayout {
    public List<Vector3> nodePositions;
    public List<Vector2Int> edgeNodeIndexes;
    public List<Vector3> wallPositions;
    public List<Vector3> pathPositions;
}

public event Action<TrialRunner,Layout,Condition> starting = delegate {};
public event Action<TrialRunner,Layout,Condition> stopped = delegate {};
public event Action<TrialRunner,Layout,Condition,TrialRunner.Results> completed = delegate {};

public delegate void Complete (int collisions);
//public event Complete TrialComplete = delegate {};

public RunMode runMode = RunMode.RenderAfterEachFrame;
public float realtimeSpeedMultiplier = 1.0f;

public bool isRunning { get; private set; }
public Layout currentLayout { get; private set; }
public Condition currentCondition { get; private set; }
public RDWTrackSpace currentTrackSpace { get; private set; }

public delegate void PositionUpdatedHandler (Vector3 localPosition, Quaternion localRotation);
public event PositionUpdatedHandler participantUpdated = delegate {};

Results _results = new Results();

RDWRedirector _redirector;
float _totalScaleModMeters;
float _totalTurnModMeters;

public void Stop () {
    StopAllCoroutines();
    var layout = currentLayout;
    var condition = currentCondition;
    Cleanup();
    stopped(this,layout,condition);
}

void Cleanup () {
    // Remove listeners
    if (_redirector) {
        _redirector.translationGained -= RDWRedirector_OnTranslationGained;
        _redirector.rotationGained -= RDWRedirector_OnRotationGained;
    }
    // Destroy gameobjects
    if (currentTrackSpace != null) {
        Destroy(currentTrackSpace.gameObject);
    }
    // Unset private vars
    _redirector = null;
    _totalScaleModMeters = 0;
    _totalTurnModMeters = 0;

    // Unset public vars
    isRunning = false;
    currentLayout = null;
    currentCondition = null;
    currentTrackSpace = null;
}

public IEnumerator Run (Layout layout, Condition condition) {
    var ft = ConvertToFormattedLayout(layout);

    // Init trackspace
    var trackSpace = Instantiate(condition.prefab);
    trackSpace.bounds = new Rect(
        -condition.trackSpaceDimensions.x*0.5f,
        -condition.trackSpaceDimensions.y*0.5f,
        condition.trackSpaceDimensions.x,
        condition.trackSpaceDimensions.y
    );
    trackSpace.trackSpaceTransform.position = ft.pathPositions[0];

    // Add listeners
    _redirector = trackSpace.GetComponentInChildren<RDWRedirector>();
    if (_redirector) {
        _redirector.translationGained += RDWRedirector_OnTranslationGained;
        _redirector.rotationGained += RDWRedirector_OnRotationGained;
    }

    // Set public vars
    isRunning = true;
    currentLayout = layout;
    currentCondition = condition;
    currentTrackSpace = trackSpace;

    // Init participant
    var participant = new GameObject("Participant Head").transform;
    participant.parent = trackSpace.transform;
    participant.localPosition = Vector3.zero;
    if (ft.pathPositions.Count > 1) {
        participant.rotation = Quaternion.LookRotation(ft.pathPositions[1] - ft.pathPositions[0],Vector3.up);
    } else {
        participant.rotation = Quaternion.LookRotation(Vector3.forward,Vector3.up);
    }

    // Init waypoints - remove first entry (start pos)
    var waypoints = new List<Vector3>(ft.pathPositions);
    waypoints.RemoveAt(0);

    var collisions = 0;

    starting(this,currentLayout,currentCondition);

    // while (true) {
    //     participantUpdated(participant.localPosition,participant.localRotation);
    //     yield return null;
    // }

    // Step through, place
    // Don't walk for the first frame, allow vars to be set etc
    trackSpace.Step(participant.localPosition,participant.localRotation,condition.timeStep);
    var resumeTime = Time.realtimeSinceStartup + condition.timeStep;
    while (true) {
        Walk(participant,waypoints,condition.walkSpeedMetersPerSecond,
            condition.turnSpeedDegreesPerSecond,condition.timeStep);

        if (waypoints.Count == 0) {
            break;
        }

        // Check if walk has taken participant out of track space
        if (!trackSpace.bounds.Contains(ToPlanar(participant.localPosition))) {
            // Mark up collision
            collisions++;
            // Simulate participant walking to center of trackspace
            trackSpace.trackSpaceTransform.position = participant.position;
            participant.localPosition = Vector3.zero;
            trackSpace.Discontinuity();
        }

        trackSpace.Step(participant.localPosition,participant.localRotation,condition.timeStep);

        // Send event
        participantUpdated(participant.localPosition,participant.localRotation);

        if (runMode == RunMode.Realtime) {
            while (Time.realtimeSinceStartup < resumeTime) {
                yield return null;
            }
            resumeTime = Time.realtimeSinceStartup +
                condition.timeStep / realtimeSpeedMultiplier;
        }

        if (runMode == RunMode.RenderAfterEachFrame) {
            yield return null;
        }
    }

    _results.totalScaleModMeters = _totalScaleModMeters;
    _results.totalTurnModMeters = _totalTurnModMeters;
    _results.collisions = collisions;
    completed(this,currentLayout,currentCondition,_results);

    Cleanup();
}

void RDWRedirector_OnTranslationGained (
    RDWRedirector.RedirectInstruction.ScaleDirection scaleDirection,
    Vector2 modMeters) {

    _totalScaleModMeters += modMeters.magnitude;
}

void RDWRedirector_OnRotationGained (
    RDWRedirector.RedirectInstruction.RotationDirection rotationDirection,
    float modDegrees) {

    _totalTurnModMeters += Mathf.Abs(modDegrees);
}

static Vector2 ToPlanar (Vector3 v) {
    return new Vector2(v.x,v.z);
}

static void Walk (Transform walker, List<Vector3> waypoints, float walkSpeedMetersPerSecond,
    float turnSpeedMetersPerSecond, float time) {

    // transform.position = _prevPos;
    // transform.rotation = _prevRot;

    while (waypoints.Count > 0) {
        var posDelta = waypoints[0] - walker.position;

        // Deal with case where we are moved past target by RDW
        if (posDelta.magnitude < time * walkSpeedMetersPerSecond) {
            walker.position = waypoints[0];
            waypoints.RemoveAt(0);
            time -= posDelta.magnitude / walkSpeedMetersPerSecond;
            continue;
        }

        var toDir = Angle(new Vector2(posDelta.x, posDelta.z));// + 180.0f;
        var fwdDir = Angle(new Vector2(walker.forward.x, walker.forward.z));// + 180.0f;
        var angle = Mathf.DeltaAngle(fwdDir,toDir);

        if (Mathf.Abs(angle) < time * turnSpeedMetersPerSecond) {
            walker.eulerAngles = new Vector3(0,toDir,0);
            time -= Mathf.Abs(angle) / turnSpeedMetersPerSecond;
        } else {
            var reducedAngle = time * turnSpeedMetersPerSecond * Mathf.Sign(angle);
            walker.eulerAngles = new Vector3(0,walker.eulerAngles.y + reducedAngle,0);
            return;
        }

        if (posDelta.magnitude < time * walkSpeedMetersPerSecond) {
            walker.position = waypoints[0];
            waypoints.RemoveAt(0);
            time -= posDelta.magnitude / walkSpeedMetersPerSecond;
        } else {
            walker.position += posDelta.normalized * time * walkSpeedMetersPerSecond;
            return;
        }
    }

    // _prevPos = transform.position;
    // _prevRot = transform.rotation;
}

static float Angle (Vector2 v) {
    return Vector2.Angle(v, Vector2.up) * (v.x < 0 ? -1 : 1);
}

FormattedLayout ConvertToFormattedLayout (Layout layout) {
    var ft = new FormattedLayout();
    ft.nodePositions = new List<Vector3>();
    for (int i = 0; i < layout.nodePositions.Length; i+=3) {
        ft.nodePositions.Add (new Vector3(
            layout.nodePositions[i+0],
            layout.nodePositions[i+1],
            layout.nodePositions[i+2]
        ));
    }
    ft.edgeNodeIndexes = new List<Vector2Int>();
    for (int i = 0; i < layout.edgeNodeIndexes.Length; i+=2) {
        ft.edgeNodeIndexes.Add (new Vector2Int(
            (int)layout.edgeNodeIndexes[i+0],
            (int)layout.edgeNodeIndexes[i+1]
        ));
    }
    ft.wallPositions = new List<Vector3>();
    for (int i = 0; i < layout.wallPositions.Length; i+=3) {
        ft.wallPositions.Add (new Vector3(
            layout.wallPositions[i+0],
            layout.wallPositions[i+1],
            layout.wallPositions[i+2]
        ));
    }
    ft.pathPositions = new List<Vector3>();
    for (int i = 0; i < layout.pathPositions.Length; i+=3) {
        ft.pathPositions.Add (new Vector3(
            layout.pathPositions[i+0],
            layout.pathPositions[i+1],
            layout.pathPositions[i+2]
        ));
    }
    return ft;
}
}
}