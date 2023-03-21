using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RDWMCExperiment {
public class MonteCarloMethodTrialVarSetter : MonoBehaviour {

public RDWMonteCarloMethod method;
public RDWWallListVisibilityTester visibilityTester;
public bool setDebugDrawCameraMain;

TrialRunner[] _runners;

void Awake () {
    _runners = Resources.FindObjectsOfTypeAll<TrialRunner>();
    Subscribe();
}

void OnDestroy () {
    Unsubscribe();
}

void Subscribe () {
    for (int i = 0; i < _runners.Length; i++) {
        _runners[i].starting += TrialRunner_OnStarting;
    }
}

void Unsubscribe () {
    for (int i = 0; i < _runners.Length; i++) {
        if (!_runners[i] || _runners[i] == null) {
            continue;
        }
        _runners[i].starting -= TrialRunner_OnStarting;
    }
}

void TrialRunner_OnStarting (TrialRunner runner, Layout layout, Condition condition) {
    if (runner.currentTrackSpace == method.trackSpace) {
        method.debugDrawCamera = setDebugDrawCameraMain ? Camera.main : null;
        method.connectedGraph = ConnectedGraphFromLayout(layout);
        visibilityTester.walls = WallsFromLayout(layout);
    }
}

static List<RDWWallListVisibilityTester.Wall> WallsFromLayout (Layout layout) {
    var walls = new List<RDWWallListVisibilityTester.Wall>();

    for (int i = 0; i < layout.wallPositions.Length; i+=6) {
        walls.Add(new RDWWallListVisibilityTester.Wall {
            a = new Vector3(
                x: layout.wallPositions[i+0],
                y: layout.wallPositions[i+1],
                z: layout.wallPositions[i+2]
            ),
            b = new Vector3(
                x: layout.wallPositions[i+3],
                y: layout.wallPositions[i+4],
                z: layout.wallPositions[i+5]
            )
        });
    }

    return walls;
}

static ConnectedGraph ConnectedGraphFromLayout (Layout layout) {
    var cg = ScriptableObject.CreateInstance<ConnectedGraph>();

    cg.nodes = new List<ConnectedGraph.Node>();
    for (int i = 0; i < layout.nodePositions.Length; i+=3) {
        var nodePos = new Vector3 {
            x = layout.nodePositions[i+0],
            y = layout.nodePositions[i+1],
            z = layout.nodePositions[i+2]
        };
        cg.nodes.Add(new ConnectedGraph.Node {position = nodePos});
    }

    cg.edges = new List<ConnectedGraph.Edge>();
    for (int i = 0; i < layout.edgeNodeIndexes.Length; i+=2) {
        var edgeIdxA = (int)layout.edgeNodeIndexes[i];
        var edgeIdxB = (int)layout.edgeNodeIndexes[i+1];
        cg.edges.Add(new ConnectedGraph.Edge {a = edgeIdxA,b = edgeIdxB});
    }

    return cg;
}
}
}
