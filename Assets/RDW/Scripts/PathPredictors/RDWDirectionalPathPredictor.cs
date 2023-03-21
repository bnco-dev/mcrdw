using System;
using UnityEngine;

public class RDWDirectionalPathPredictor : ARDWPathPredictor {

    public float confidence = 0.95f;

    RDWDirectionalPathPredictorAlgo _algo = new RDWDirectionalPathPredictorAlgo();

    public override void Clear () {
        _algo.Clear();
    }

    public override void Clear (float minSampleTime) {
        _algo.Clear(minSampleTime);
    }

    public override float GetCurrentTime() {
        return _algo.GetCurrentTime();
    }

    public override void SubmitSample (Vector2 position, float direction,
        float deltaTime) {
        _algo.confidence = confidence;
        _algo.SubmitSample(position,direction,deltaTime);
    }

    public override bool Predict (out float direction, out float confidence) {
        _algo.confidence = this.confidence;
        return _algo.Predict(out direction, out confidence);
    }
}