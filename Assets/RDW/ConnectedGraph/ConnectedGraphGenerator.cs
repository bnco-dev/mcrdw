#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ConnectedGraphGenerator : MonoBehaviour {
    public enum DrawMode {
        Hide,
        Simple,
        Advanced
    }

    [System.Serializable]
    public class EditableEdge {
        public Transform a;
        public Transform b;
    }

    public ConnectedGraph asset;
    public List<Transform> nodes = new List<Transform>();
    public List<EditableEdge> edges = new List<EditableEdge>();

    public DrawMode drawMode {
        get { return _drawMode; }
        set {
            if (value == _drawMode) {
                return;
            }

            for (int i = 0; i < nodes.Count; i++) {
                nodes[i].GetComponent<Renderer>().enabled = value != DrawMode.Hide;
            }
            _drawMode = value;
        }
    }
    [SerializeField]
    DrawMode _drawMode = DrawMode.Simple;
    public Color nodeColor {
        get { return _nodeColor; }
        set {
            var modified =
                !Mathf.Approximately(nodeColor.a,value.a) ||
                !Mathf.Approximately(nodeColor.r,value.r) ||
                !Mathf.Approximately(nodeColor.g,value.g) ||
                !Mathf.Approximately(nodeColor.b,value.b);

            _nodeColor = value;
            if (modified && nodes.Count > 0) {
                nodes[0].GetComponent<Renderer>().
                sharedMaterial.SetColor("_EmissionColor",nodeColor);
            }
        }
    }
    Color _nodeColor = new Color(1.0f,0.7263f,0.0f);
    public Color edgeColor = new Color(1.0f,1.0f,1.0f);

    GameObject _nodePrefab = null;

    GameObject RequirePrefab () {
        if (!_nodePrefab || _nodePrefab == null) {
            _nodePrefab = (GameObject)Resources.Load("node.connectedgraph");
        }

        return _nodePrefab;
    }

    void OnDrawGizmos () {
        for (int i = 0; i < edges.Count; i++) {
            // Works but no depth test
            // var p1 = edges[i].a.position;
            // var p2 = edges[i].b.position;
            // var thickness = 3;
            // Handles.DrawBezier(p1,p2,p1,p2, Color.red,null,thickness);

            // Works but 1-pixel width
            if (drawMode == DrawMode.Simple) {
                var prevColor = Gizmos.color;
                Gizmos.color = edgeColor;
                Gizmos.DrawLine(edges[i].a.position,edges[i].b.position);
                Gizmos.color = prevColor;
            } else if (drawMode == DrawMode.Advanced) {
                var prevColor = Gizmos.color;
                Gizmos.color = edgeColor;
                var aBounds = edges[i].a.GetComponent<Renderer>().bounds;
                var bBounds = edges[i].b.GetComponent<Renderer>().bounds;
                var p0 = aBounds.ClosestPoint(edges[i].b.position);
                var p1 = bBounds.ClosestPoint(edges[i].a.position);
                // Gizmos.DrawCube(p0,Vector3.one*GetGizmoSize(p0)*.1f);
                Gizmos.DrawCube(p0,Vector3.one*.1f);
                Gizmos.DrawLine(p0,p1);
                // Gizmos.DrawCube(p1,Vector3.one*GetGizmoSize(p1)*.1f);
                Gizmos.DrawCube(p1,Vector3.one*.1f);
                Gizmos.color = prevColor;
            }
        }
    }

    // // https://forum.unity.com/threads/constant-screen-size-gizmos.64027/
    // public static float GetGizmoSize(Vector3 position) {
    //     Camera current = Camera.current;
    //     position = Gizmos.matrix.MultiplyPoint(position);

    //     if (current)
    //     {
    //         Transform transform = current.transform;
    //         Vector3 position2 = transform.position;
    //         float z = Vector3.Dot(position - position2, transform.TransformDirection(new Vector3(0f, 0f, 1f)));
    //         Vector3 a = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(0f, 0f, z)));
    //         Vector3 b = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(1f, 0f, z)));
    //         float magnitude = (a - b).magnitude;
    //         return 80f / Mathf.Max(magnitude, 0.0001f);
    //     }

    //     return 20f;
    // }

    public bool IsNode (Transform transform) {
        return nodes.IndexOf(transform) >= 0;
    }

    public bool AddEdge (Transform a, Transform b) {
        if (nodes.Contains(a) && nodes.Contains(b)) {
            var edge = new EditableEdge();
            edge.a = a;
            edge.b = b;
            edges.Add(edge);
            return true;
        }
        return false;
    }

    public void AddNode (Vector3 position) {
        var prefab = RequirePrefab();

        var node = Instantiate(prefab).GetComponent<Transform>();
        node.tag = "EditorOnly";
        node.name = "Graph Node " + nodes.Count;
        node.parent = GetComponent<Transform>();
        node.position = position;
        node.GetComponent<Renderer>().
            sharedMaterial.SetColor("_EmissionColor",nodeColor);

        nodes.Add(node);
    }

    public bool DeleteNode (Transform node) {
        for (int i = 0; i < edges.Count; i++) {
            if (edges[i].a == node || edges[i].b == node) {
                edges.RemoveAt(i);
                i--;
            }
        }
        return nodes.Remove(node);
    }

    public bool DeleteNodes (Transform[] nodes) {
        for (int i = 0; i < nodes.Length; i++) {
            if (!IsNode(nodes[i])) {
                return false;
            }
        }

        for (int i = 0; i < nodes.Length; i++) {
            DeleteNode(nodes[i]);
        }
        return true;
    }

    public bool DeleteEdge (Transform a, Transform b) {
        for (int i = 0; i < edges.Count; i++) {
            if ((edges[i].a == a && edges[i].b == b)
                || (edges[i].a == b && edges[i].b == a)) {

                edges.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool Save () {
        if (!asset || asset == null) {
            return false;
        }

        if (asset.nodes == null) {
            asset.nodes = new List<ConnectedGraph.Node>();
        }

        asset.nodes.Clear();
        for (int i = 0; i < nodes.Count; i++) {
            var dataNode = new ConnectedGraph.Node();
            dataNode.position = nodes[i].position;
            asset.nodes.Add(dataNode);
        }

        if (asset.edges == null) {
            asset.edges = new List<ConnectedGraph.Edge>();
        }

        asset.edges.Clear();
        for (int i = 0; i < edges.Count; i++) {
            var edge = new ConnectedGraph.Edge();
            edge.a = nodes.IndexOf(edges[i].a);
            edge.b = nodes.IndexOf(edges[i].b);
            asset.edges.Add(edge);
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return true;
    }

    public bool Load () {
        if (!asset || asset == null) {
            return false;
        }

        var transform = GetComponent<Transform>();
        var initialChildCount = transform.childCount;
        for (int n = 0; n < initialChildCount; n++) {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        nodes.Clear();
        for (int i = 0; i < asset.nodes.Count; i++) {
            AddNode(asset.nodes[i].position);
        }

        edges.Clear();
        for (int i = 0; i < asset.edges.Count; i++) {
            var edge = new EditableEdge();
            edge.a = nodes[asset.edges[i].a];
            edge.b = nodes[asset.edges[i].b];
            edges.Add(edge);
        }
        return true;
    }
}

[CustomEditor(typeof(ConnectedGraphGenerator))]
public class ConnectedGraphGeneratorEditor : Editor {

    public override void OnInspectorGUI () {
        var cgg = (ConnectedGraphGenerator)target;

        cgg.drawMode = (ConnectedGraphGenerator.DrawMode)EditorGUILayout.EnumPopup("Draw Mode", cgg.drawMode);
        cgg.nodeColor = EditorGUILayout.ColorField("Node Color", cgg.nodeColor);
        cgg.edgeColor = EditorGUILayout.ColorField("Edge Color", cgg.edgeColor);

        cgg.asset = (ConnectedGraph)EditorGUILayout.ObjectField(
            "Graph Asset",cgg.asset,typeof(ConnectedGraph),
            allowSceneObjects: false);

        if (GUILayout.Button("Save")) {
            var success = cgg.Save();
            if (!success) {
                Debug.LogWarning("Save: failed. Is there an asset to save to?");
            }
        }

        if (GUILayout.Button("Load")) {
            var trans = cgg.GetComponent<Transform>();
            var proceed = true;

            if (trans.childCount > 0) {
                proceed = EditorUtility.DisplayDialog("Confirm Load",
                    "Graph generator already has child objects. " +
                    "These will be destroyed on load. Continue?","OK","Cancel");
            }

            if (proceed) {
                var success = cgg.Load();

                if (!success) {
                    Debug.LogWarning("Load: failed. Is there an asset to load from?");
                }
            }
        }

        GUILayout.Space(4);

        if (GUILayout.Button("Add Node")) {
            cgg.AddNode(Vector3.zero);
        }

        if (GUILayout.Button("Add Edge")) {
            var success =
                Selection.gameObjects.Length == 2 &&
                cgg.AddEdge(Selection.transforms[0],Selection.transforms[1]);

            if (!success) {
                Debug.LogWarning("Add Edge: failed. " +
                    "Two nodes must be selected. " +
                    "Both nodes must already be part of the graph");
            }
        }

        GUILayout.Space(4);

        if (GUILayout.Button("Delete Node(s)")) {
            var success =
                Selection.gameObjects.Length > 0 &&
                cgg.DeleteNodes(Selection.transforms);

            if (!success) {
                Debug.LogWarning("Delete Node(s): failed. " +
                    "All selected objects must be nodes and part of the graph");
            }
        }

        if (GUILayout.Button("Delete Edge")) {
            if (Selection.gameObjects.Length != 2 ||
                !cgg.IsNode(Selection.transforms[0]) ||
                !cgg.IsNode(Selection.transforms[1])) {

                Debug.LogWarning("Add Edge: failed. " +
                    "Two nodes must be selected.");
            }

            var success =
                cgg.DeleteEdge(Selection.transforms[0],Selection.transforms[1]);

            if (!success) {
                Debug.Log("Delete Edge: No edge found");
            }
        }
    }
}

#endif