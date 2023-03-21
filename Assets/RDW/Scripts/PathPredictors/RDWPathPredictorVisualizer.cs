using UnityEngine;

public class RDWPathPredictorVisualizer : MonoBehaviour {

    public Color confidenceMaxColor = Color.green;
    public Color confidenceMinColor = Color.red;
    public Color predictionFailColor = Color.blue;

    public RDWTrackSpace trackSpace;
    public ARDWPathPredictor predictor;
    public Transform trackedObject;

#if UNITY_EDITOR
    public void Update () {
        if (predictor == null) {
            return;
        }

        float direction, confidence;
        var success = predictor.Predict(out direction, out confidence);
        var worldDir = trackSpace.TrackToWorldDirection(direction);
        var dirVec = worldDir;
        Debug.DrawLine(
            start: trackedObject.position,
            end: trackedObject.position + dirVec,
            color: success
                ? Color.Lerp(confidenceMinColor,confidenceMaxColor,confidence)
                : predictionFailColor,
            duration: 0.0f
        );
    }
#endif

}