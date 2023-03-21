using System;
using UnityEngine;

public class RDWPositionalPathPredictor : ARDWPathPredictor {

    public int windowCount = 5;
    public float windowSizeSeconds = 0.2f;
    public float confidenceFloor = 0.94f;

    RDWPositionalPathPredictorAlgo _algo = new RDWPositionalPathPredictorAlgo();
    int _lastFrameSubmitted = -1;

    public override void Clear () {
        _lastFrameSubmitted = -1;
        _algo.Clear();
    }

    public override void Clear (float minSampleTime) {
        _lastFrameSubmitted = -1;
        _algo.Clear(minSampleTime);
    }

    public override float GetCurrentTime() {
        return _algo.GetCurrentTime();
    }

    public override void SubmitSample (Vector2 position, float direction,
        float deltaTime) {
        _algo.windowCount = windowCount;
        _algo.windowSizeSeconds = windowSizeSeconds;
        _algo.confidenceFloor = confidenceFloor;
        _algo.SubmitSample(position,direction,deltaTime);
    }

    public override bool Predict (out float direction, out float confidence) {
        _algo.windowCount = windowCount;
        _algo.windowSizeSeconds = windowSizeSeconds;
        _algo.confidenceFloor = confidenceFloor;
        return _algo.Predict(out direction, out confidence);
    }
}