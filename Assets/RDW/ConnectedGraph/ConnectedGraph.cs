using System.Collections.Generic;
using UnityEngine;

// Connected graph layout, data only
public class ConnectedGraph : ScriptableObject {

    [System.Serializable]
    public class Node {
        public Vector3 position;
    }

    [System.Serializable]
    public class Edge {
        public int a;
        public int b;
    }

    public List<Node> nodes;
    public List<Edge> edges;

    public void GetNeighboursNoAlloc (Node node, List<Node> neighbours) {
        for (int i = 0; i < edges.Count; i++) {
            var a = nodes[edges[i].a];
            var b = nodes[edges[i].b];

            if (a == node) {
                neighbours.Add(b);
            } else if (b == node) {
                neighbours.Add(a);
            }

            // if (nodes[edges[i].a] == node) {
            //     toFill.Add(nodes[edges[i].b]);
            // } else if (nodes[edges[i].b] == node) {
            //     toFill.Add(nodes[edges[i].a]);
            // }
        }
    }

    public bool IsConnected (Node a, Node b) {
        for (int ei = 0; ei < edges.Count; ei++) {
            var edge = edges[ei];
            if ((nodes[edge.a] == a && nodes[edge.b] == b) ||
                (nodes[edge.a] == b && nodes[edge.b] == a)) {
                return true;
            }
        }
        return false;
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ConnectedGraph))]
public class ConnectedGraphEditor : UnityEditor.Editor {

    // public override void OnInspectorGUI () {
    //     var cg = (ConnectedGraph)target;
    //     // GUI.enabled = false;
    //     UnityEditor.EditorGUILayout.IntField("Node Count",cg.nodes.Count);
    //     UnityEditor.EditorGUILayout.IntField("Edge Count",cg.edges.Count);
    //     // GUI.enabled = true;
    // }

    [UnityEditor.MenuItem("Assets/Create/Connected Graph")]
    static void Create () {
        CreateAsset<ConnectedGraph>();
    }

    static void CreateAsset<T> () where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T> ();

        string path = UnityEditor.AssetDatabase.GetAssetPath (UnityEditor.Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (System.IO.Path.GetExtension (path) != "")
        {
            path = path.Replace (System.IO.Path.GetFileName (UnityEditor.AssetDatabase.GetAssetPath (UnityEditor.Selection.activeObject)), "");
        }

        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath (path + "/New " + typeof(T).ToString() + ".asset");

        UnityEditor.AssetDatabase.CreateAsset (asset, assetPathAndName);

        UnityEditor.AssetDatabase.SaveAssets ();
        UnityEditor.AssetDatabase.Refresh();
        UnityEditor.EditorUtility.FocusProjectWindow ();
        UnityEditor.Selection.activeObject = asset;
    }
}
#endif