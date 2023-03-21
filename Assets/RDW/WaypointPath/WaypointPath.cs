using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaypointPath : ScriptableObject {
    public List<Vector3> positions = new List<Vector3>();

    public void Copy (WaypointPath other) {
        positions.Clear();
        for (int i = 0; i < other.positions.Count; i++) {
            positions.Add(other.positions[i]);
        }
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

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(WaypointPath))]
public class WaypointPathEditor : UnityEditor.Editor {

    [UnityEditor.MenuItem("Assets/Create/Waypoint Path")]
    static void Create () {
        CreateAsset<WaypointPath>();
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