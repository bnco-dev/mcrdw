using System.Collections.Generic;
using UnityEngine;

namespace RDWMCExperiment {

public class DebugLayoutGenerator : MonoBehaviour {

public Vector2Int dimensions = new Vector2Int(5, 5);
public float edgeFactor = 0.5f;
public float nodeSpacingMeters = 1.0f;
public float pathLengthMeters = 10.0f;

public TrialVisualizer trialVisualizer;

FastLayoutGeneratorAlgo _tg = new FastLayoutGeneratorAlgo();
Layout _layout = new Layout();

public void Generate () {
    _tg.Set(dimensions.x,dimensions.y,edgeFactor,nodeSpacingMeters,pathLengthMeters);
    _tg.Generate(_layout);
    trialVisualizer.mode = TrialVisualizer.Mode.LayoutOnly;
    trialVisualizer.layout = _layout;
}

}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(DebugLayoutGenerator))]
public class DebugLayoutGeneratorEditor : UnityEditor.Editor {

override public void OnInspectorGUI() {
    var dtg = (DebugLayoutGenerator)target;
    if (GUILayout.Button("Generate")) {
        dtg.Generate();
        UnityEditor.EditorWindow.GetWindow<UnityEditor.SceneView>().Repaint();
    }
    DrawDefaultInspector();
}

}
#endif
}