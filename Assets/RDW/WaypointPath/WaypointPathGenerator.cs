#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WaypointPathGenerator : MonoBehaviour {

    public WaypointPath asset;
    [SerializeField] WaypointPath _workingAsset;

    public Color nodeColor = new Color(1.0f,0.7263f,0.0f);
    public Color edgeColor = new Color(1.0f,1.0f,1.0f);

    public void GenerateRandomPath (ConnectedGraph graph, float duration,
        float predictedWalkSpeed, float predictedTurnSpeed) {


    }

    WaypointPath RequireWorkingAsset () {
        if (!_workingAsset || _workingAsset == null) {
            _workingAsset = ScriptableObject.CreateInstance<WaypointPath>();
        }

        return _workingAsset;
    }


    public bool Save () {
        if (!asset || asset == null) {
            return false;
        }

        asset.Copy(RequireWorkingAsset());
        return true;
    }

    public bool Load () {
        if (!asset || asset == null) {
            return false;
        }

        RequireWorkingAsset().Copy(asset);
        return true;
    }

    public void PushNode () {
        var nextPos = Vector3.zero;
        var workingAsset = RequireWorkingAsset();
        if (workingAsset.positions.Count > 0) {
            nextPos = workingAsset.positions[workingAsset.positions.Count-1];
            nextPos += Vector3.forward * 0.5f;
        }
        workingAsset.positions.Add(nextPos);
    }

    public bool PopNode () {
        var workingAsset = RequireWorkingAsset();
        if (workingAsset.positions.Count == 0) {
            return false;
        }
        workingAsset.positions.RemoveAt(workingAsset.positions.Count-1);
        return true;
    }

    void OnDrawGizmos () {
        if (!_workingAsset) {
            return;
        }

        for (int i = 0; i < _workingAsset.positions.Count; i++) {
            // Works but no depth test
            // var p1 = edges[i].a.position;
            // var p2 = edges[i].b.position;
            // var thickness = 3;
            // Handles.DrawBezier(p1,p2,p1,p2, Color.red,null,thickness);
            const float NODE_RADIUS = 0.3f;

            // Draw sphere
            var prevColor = Gizmos.color;
            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(_workingAsset.positions[i],NODE_RADIUS);
            Gizmos.color = prevColor;

            // Draw edge
            if (i > 0) {
                var from = _workingAsset.positions[i-1];
                var to = _workingAsset.positions[i];
                var nodeOffset = (to-from).normalized * NODE_RADIUS;
                prevColor = Gizmos.color;
                Gizmos.color = edgeColor;
                Gizmos.DrawLine(from+nodeOffset,to-nodeOffset);
                Gizmos.color = prevColor;
            }

            // // Works but 1-pixel width
            // if (drawMode == DrawMode.Simple) {
            //     var prevColor = Gizmos.color;
            //     Gizmos.color = edgeColor;
            //     Gizmos.DrawLine(edges[i].a.position,edges[i].b.position);
            //     Gizmos.color = prevColor;
            // } else {
            //     var prevColor = Gizmos.color;
            //     Gizmos.color = edgeColor;
            //     var aBounds = edges[i].a.GetComponent<Renderer>().bounds;
            //     var bBounds = edges[i].b.GetComponent<Renderer>().bounds;
            //     var p0 = aBounds.ClosestPoint(edges[i].b.position);
            //     var p1 = bBounds.ClosestPoint(edges[i].a.position);
            //     // Gizmos.DrawCube(p0,Vector3.one*GetGizmoSize(p0)*.1f);
            //     Gizmos.DrawCube(p0,Vector3.one*.1f);
            //     Gizmos.DrawLine(p0,p1);
            //     // Gizmos.DrawCube(p1,Vector3.one*GetGizmoSize(p1)*.1f);
            //     Gizmos.DrawCube(p1,Vector3.one*.1f);
            //     Gizmos.color = prevColor;
            // }
        }
    }

}

[CustomEditor(typeof(WaypointPathGenerator))]
public class WaypointPathGeneratorEditor : Editor {

    public override void OnInspectorGUI () {
        var wpg = (WaypointPathGenerator)target;

        wpg.nodeColor = EditorGUILayout.ColorField("Node Color", wpg.nodeColor);
        wpg.edgeColor = EditorGUILayout.ColorField("Edge Color", wpg.edgeColor);

        wpg.asset = (WaypointPath)EditorGUILayout.ObjectField(
            "Path Asset",wpg.asset,typeof(WaypointPath),
            allowSceneObjects: false);

        if (GUILayout.Button("Save")) {
            var success = wpg.Save();
            if (!success) {
                Debug.LogWarning("Save: failed. Is there an asset to save to?");
            }
        }

        if (GUILayout.Button("Load")) {
            // var trans = wpg.GetComponent<Transform>();
            // var proceed = true;

            // if (trans.childCount > 0) {
            //     proceed = EditorUtility.DisplayDialog("Confirm Load",
            //         "Graph generator already has child objects. " +
            //         "These will be destroyed on load. Continue?","OK","Cancel");
            // }

            // if (proceed) {
            //     var success = wpg.Load();

            //     if (!success) {
            //         Debug.LogWarning("Load: failed. Is there an asset to load from?");
            //     }
            // }

            var success = wpg.Load();

            if (!success) {
                Debug.LogWarning("Load: failed. Is there an asset to load from?");
            }
        }

        GUILayout.Space(4);

        if (GUILayout.Button("Add Node")) {
            wpg.PushNode();
        }

        if (GUILayout.Button("Delete Last Node")) {
            var success =
                wpg.PopNode();

            if (!success) {
                Debug.LogWarning("Delete Node: no node to delete");
            }
        }
    }
}

#endif