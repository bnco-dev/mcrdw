#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WaypointPathsFromConnectedGraphEditorWindow : EditorWindow {
    ConnectedGraph graphAsset;
    WaypointPath pathAsset;

    float walkSpeed = 1.0f;
    float turnSpeed = 90.0f;
    float backtrackProbabilityMultiplier = .1f;
    float backtrackRecoverRate = 4;
    float walkDuration = 30.0f;

    bool walkSimSettingsEnabled = false;

    // Add menu item named "My Window" to the Window menu
    [MenuItem("Window/WaypointPathGenerator")]
    public static void ShowWindow() {
        //Show existing window instance. If one doesn't exist, make one.
        GetWindow(typeof(WaypointPathsFromConnectedGraphEditorWindow));
    }

    void OnGUI() {
        GUILayout.Label ("Generate a set of waypoint paths based on " +
            "a connected graph. Existing paths will be destroyed.");
        graphAsset = (ConnectedGraph)EditorGUILayout.ObjectField(
            "Graph Asset",graphAsset,typeof(ConnectedGraph),
            allowSceneObjects: false);
        pathAsset = (WaypointPath)EditorGUILayout.ObjectField(
            "Path Asset",pathAsset,typeof(WaypointPath),
            allowSceneObjects: false);

        walkDuration = EditorGUILayout.FloatField("Walk Duration (seconds)", walkDuration);

        walkSimSettingsEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(walkSimSettingsEnabled,"Simulation Settings");
        if (walkSimSettingsEnabled) {
            walkSpeed = EditorGUILayout.FloatField("Walk Speed (meters/second)", walkSpeed);
            turnSpeed = EditorGUILayout.FloatField("Turn Speed (degrees/second)", turnSpeed);
            backtrackProbabilityMultiplier = EditorGUILayout.FloatField("Backtrack Probability Multiplier", backtrackProbabilityMultiplier);
            backtrackRecoverRate = EditorGUILayout.FloatField("Backtrack Recover Rate",backtrackRecoverRate);
        }
        EditorGUILayout.EndFoldoutHeaderGroup ();


        if (GUILayout.Button("Generate")) {
            GenerateWalk();
        }
    }

    void GenerateWalk () {
        if (!graphAsset || graphAsset == null) {
            Debug.LogWarning("Missing graph asset - no path generated");
            return;
        }

        if (!pathAsset || pathAsset == null) {
            Debug.LogWarning("Missing path asset - no path generated");
            return;
        }

        if (graphAsset.nodes == null || graphAsset.nodes.Count < 2) {
            Debug.LogWarning("Less than 2 nodes in graph - no path generated");
        }

        // pathAsset.positions.Clear();
        // var pathGen = new ConnectedGraphPathGenerator(graphAsset,
        //     backtrackProbabilityMultiplier,backtrackRecoverRate);
        // var lookDir = Vector2.one;
        // var time = 0.0f;
        // var firstWalk = true;
        // pathAsset.positions.Add(pathGen.currentNode.position);

        // while (time < walkDuration) {
        //     var node = pathGen.currentNode;
        //     if (!pathGen.Advance()) {
        //         Debug.LogWarning("Graph has dead ends - no path generated");
        //         pathAsset.positions.Clear();
        //         return;
        //     }

        //     var nextNode = pathGen.currentNode;

        //     // Walk to next node + update time
        //     var to = nextNode.position - node.position;
        //     var nextLookDir = to.normalized;
        //     var angle = Mathf.Acos(Vector3.Dot(lookDir,nextLookDir)) * Mathf.Rad2Deg;
        //     var turnTime = angle / turnSpeed;
        //     var walkTime = to.magnitude / walkSpeed;

        //     if (firstWalk) {
        //         turnTime = 0.0f;
        //         firstWalk = false;
        //     }

        //     time += turnTime + walkTime;
        //     node = nextNode;
        //     lookDir = nextLookDir;

        //     pathAsset.positions.Add(node.position);
        // }
    }
}
#endif