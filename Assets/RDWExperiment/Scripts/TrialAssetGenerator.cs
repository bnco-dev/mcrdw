// using System.Collections.Generic;
// using UnityEngine;

// namespace RDWMCExperiment {

// public class TrialAssetGenerator : MonoBehaviour {
//     public enum Mode {
//         Single,
//         Batch
//     }
//     public Mode mode;

//     public TextAsset asset;
//     public Vector2Int dimensions;
//     public float edgeFactor;
//     public float nodeSpacingMeters;

//     public TrialVisualizer optionalTrialVisualizer;

//     TrialUtil.TrialGenerator _tg = new TrialUtil.TrialGenerator();
//     Trial _trial = new Trial();

//     public void Generate () {
//         if (asset == null) {
//             asset = new TextAsset();
//         }

//         _tg.Generate(_trial,dimensions.x,dimensions.y,edgeFactor,nodeSpacingMeters);


//         if (optionalTrialVisualizer) {
//             optionalTrialVisualizer.assets.Clear();
//             optionalTrialVisualizer.assets.Add(asset);
//         }
//     }

// }

// #if UNITY_EDITOR
// [UnityEditor.CustomEditor(typeof(TrialAssetGenerator))]
// public class TrialAssetGeneratorEditor : UnityEditor.Editor {

//     [UnityEditor.MenuItem("Assets/Create/Trial/Empty Trial")]
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

//     void OnInspectorGUI () {
//         DrawDefaultInspector();
//         var tg = (TrialAssetGenerator)target;
//         // tg.mode = (TrialAssetGenerator.Mode)UnityEditor.EditorGUILayout.EnumPopup("Mode",tg.mode);
//         // if (tg.mode == TrialAssetGenerator.Mode.Single) {
//         //     // Single mode
//         //     tg.asset = (TextAsset)UnityEditor.EditorGUILayout.ObjectField("Asset",tg.asset,typeof(TextAsset));
//         //     if (GUILayout.Button("New")) {
//         //         TextAsset
//         //         // CreateAsset<Trial>()
//         //     }
//         //     if (GUILayout.Button("Generate (Overwrite)")) {
//         //         trialGenerator.Generate();
//         //     }
//         //     if (GUILayout.Button("Save")) {

//         //     }
//         // }
//         // } else {
//         //     // Batch mode

//         // }
//         // tg.dimensions = UnityEditor.EditorGUILayout.Vector2IntField("Dimensions",tg.dimensions);
//         // tg.edgeFactor = UnityEditor.EditorGUILayout.FloatField("Edge Factor",tg.edgeFactor);
//         // tg.nodeSpacingMeters = UnityEditor.EditorGUILayout.FloatField("Node Spacing (meters)",tg.nodeSpacingMeters);

//         if (GUILayout.Button("Generate (Overwrite)")) {
//             tg.Generate();
//         }
//     }
// }
// #endif
// }
