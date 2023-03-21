using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using NaughtyAttributes;

namespace RDWMCExperiment {
[System.Serializable]
public class TrialVisualizer : MonoBehaviour {
    public enum Mode {
        Live,
        LayoutOnly
    }

    public List<Camera> cameras;

    public Mode mode;

    bool ShowLayout => mode == Mode.LayoutOnly;
    bool ShowTrialRunner => mode == Mode.Live;

    [EnableIf("ShowLayout")]
    public Layout layout;
    [EnableIf("ShowTrialRunner")]
    public TrialRunner trialRunner;

    public Color nodeColor = new Color(1.0f,0.0f,0.0f);
    public Color edgeColor = new Color(1.0f,1.0f,1.0f);
    public Color wallColor = new Color (0.0f,0.0f,1.0f);
    public Color pathColorStart = new Color(1.0f,0.7263f,0.0f);
    public Color pathColorEnd = new Color(0.0f,0.7263f,1.0f);

    [EnableIf("ShowTrialRunner")]
    public Color userColor = new Color(0.5f,0.2f,0.487263f);
    [EnableIf("ShowTrialRunner")]
    public Color trackSpaceColor = new Color(0.8969655f,0.7670879f,0.9622642f);

    public float nodeRadius = 0.3f;
    public float userRadius = 0.5f;
    public float pathNodeSizeStep = 0.08f;

    Vector3 _position;
    Quaternion _rotation;
    Mesh _cubeMesh;

    void Awake () {
        if (trialRunner) {
            trialRunner.participantUpdated += TrialRunner_OnParticipantUpdated;
        }

        _cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }

    void TrialRunner_OnParticipantUpdated (Vector3 position, Quaternion rotation) {
        _position = position;
        _rotation = rotation;
    }

    // Trial _trial = new Trial();
    // TextAsset _currentlyLoadedAsset;
    bool IsTrialInitialized(Layout trial) {
        return trial != null && trial.nodePositions != null
            && trial.edgeNodeIndexes != null && trial.wallPositions != null
            && trial.pathPositions != null;
    }

    void OnDrawGizmos () {
        if (!IsTrialInitialized(layout)) {
            return;
        }

        if (cameras.Count > 0 && !cameras.Contains(Camera.current)) {
            return;
        }

        switch (mode) {
            case Mode.LayoutOnly : DrawLayoutOnly(); break;
            case Mode.Live : DrawLive(); break;
        }

        // This is a very ugly hack to fix render order with RDWMonteCarloMethod
        var mcm = FindObjectOfType<RDWMonteCarloMethod>();
        if (mcm) {
            mcm.DrawDebugNow();
        }
    }

    void DrawLayoutOnly () {
        DrawLayout(layout);
    }

    void DrawLayout (Layout layout) {
        var prevMatrix = Gizmos.matrix;
        var prevColor = Gizmos.color;

        Gizmos.matrix = Matrix4x4.identity;

        // Draw nodes
        for (int i = 0; i < layout.nodePositions.Length; i+=3) {
            Gizmos.color = nodeColor;
            Gizmos.DrawCube(P(GetVector3(layout.nodePositions,i)),Vector3.one*nodeRadius);
        }

        // Draw edges
        for (int i = 0; i < layout.edgeNodeIndexes.Length; i+=2) {
            var from = GetVector3(layout.nodePositions,layout.edgeNodeIndexes[i]*3);
            var to = GetVector3(layout.nodePositions,layout.edgeNodeIndexes[i+1]*3);
            var nodeOffset = (to-from).normalized * nodeRadius;
            Gizmos.color = edgeColor;
            Gizmos.DrawLine(P(from+nodeOffset),P(to-nodeOffset));
        }

        // Draw walls
        for (int i = 0; i < layout.wallPositions.Length; i+=6) {
            var from = GetVector3(layout.wallPositions,i);
            var to = GetVector3(layout.wallPositions,i+3);
            Gizmos.color = wallColor;
            Gizmos.DrawLine(P(from),P(to));
        }

        // Draw path
        for (int i = 0; i < layout.pathPositions.Length; i+=3) {
            var t = i/(float)layout.pathPositions.Length;
            var col = Color.Lerp(pathColorStart,pathColorEnd,t);
            var size = nodeRadius + pathNodeSizeStep * i;
            Gizmos.color = col;
            Gizmos.DrawWireCube(P(GetVector3(layout.pathPositions,i)),size*Vector3.one);
        }

        Gizmos.color = prevColor;
        Gizmos.matrix = prevMatrix;
    }

    void DrawLive () {
        if (!trialRunner || !trialRunner.isRunning) {
            return;
        }

        var prevMatrix = Gizmos.matrix;
        var prevColor = Gizmos.color;

        Gizmos.matrix = Matrix4x4.identity;

        // Draw trackspace first (no depth testing with gizmos)
        var bounds = trialRunner.currentTrackSpace.bounds;
        var trackSpaceTrans = trialRunner.currentTrackSpace.transform;
        var scale = new Vector3(
            bounds.width,
            0.01f,
            bounds.height
        );
        var offs = new Vector3(
            bounds.center.x,
            -1.0f, // draw underneath
            bounds.center.y
        );
        Gizmos.color = trackSpaceColor;
        Gizmos.DrawMesh(_cubeMesh,P(trackSpaceTrans.position+offs),R(trackSpaceTrans.rotation),scale);

        DrawLayout(trialRunner.currentLayout);

        // Draw user last
        prevColor = Gizmos.color;
        var userPos = P(trackSpaceTrans.TransformPoint(_position));
        var userFwd = (trackSpaceTrans.rotation * _rotation) * Vector3.forward;
        Gizmos.color = userColor;
        Gizmos.DrawSphere(userPos,userRadius);
        Gizmos.DrawSphere(userPos + userFwd*0.5f,userRadius * 0.5f);

        Gizmos.color = prevColor;
        Gizmos.matrix = prevMatrix;
    }

    // Transform a position vector from local to world space
    Vector3 P (Vector3 v) {
        return transform.TransformPoint(v);
    }

    // Transform a quaternion from local to world space
    Quaternion R (Quaternion q) {
        return transform.rotation * q;
    }

    Vector3 GetVector3 (float[] list, uint startingIndex) {
        return new Vector3(
            list[startingIndex],
            list[startingIndex+1],
            list[startingIndex+2]
        );
    }

    Vector3 GetVector3 (float[] list, int startingIndex) {
        return new Vector3(
            list[startingIndex],
            list[startingIndex+1],
            list[startingIndex+2]
        );
    }
}

// public class WaypointPathSet : ScriptableObject {
//     public List<WaypointPath> paths = new List<WaypointPath>();

//     public void Copy (WaypointPathSet other) {
//         paths.Clear();
//         for (int pathi = 0; pathi < other.paths.Count; pathi++) {
//             paths.Add(new WaypointPath());
//             for (int posi = 0; posi < other.paths[pathi].positions.Count; posi++) {
//                 paths[pathi].positions.Add(other.paths[pathi].positions[posi]);
//             }
//         }
//     }
// }

// #if UNITY_EDITOR
// [UnityEditor.CustomEditor(typeof(WaypointPath))]
// public class TrialEditor : UnityEditor.Editor {

//     [UnityEditor.MenuItem("Assets/Create/Waypoint Path")]
//     static void Create () {
//         CreateAsset<WaypointPath>();
//     }

//     static void CreateAsset<T> () where T : ScriptableObject
//     {
//         T asset = ScriptableObject.CreateInstance<T> ();

//         string path = UnityEditor.AssetDatabase.GetAssetPath (UnityEditor.Selection.activeObject);
//         if (path == "")
//         {
//             path = "Assets";
//         }
//         else if (System.IO.Path.GetExtension (path) != "")
//         {
//             path = path.Replace (System.IO.Path.GetFileName (UnityEditor.AssetDatabase.GetAssetPath (UnityEditor.Selection.activeObject)), "");
//         }

//         string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath (path + "/New " + typeof(T).ToString() + ".asset");

//         UnityEditor.AssetDatabase.CreateAsset (asset, assetPathAndName);

//         UnityEditor.AssetDatabase.SaveAssets ();
//         UnityEditor.AssetDatabase.Refresh();
//         UnityEditor.EditorUtility.FocusProjectWindow ();
//         UnityEditor.Selection.activeObject = asset;
//     }
// }
// #endif
}