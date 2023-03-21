using System.Collections.Generic;
using UnityEngine;

public class RDWDirectionalPathPredictorAlgo : IRDWPathPredictorAlgo {

    public float time {get; private set;}
    public float confidence { get; set; }

    float _predDirection;
    float _predConfidence;
    bool _samplesSubmitted;

    public RDWDirectionalPathPredictorAlgo () { }

    public RDWDirectionalPathPredictorAlgo (float confidence) {
        this.confidence = confidence;
    }

    public void Clear () {
        _samplesSubmitted = false;
    }

    public void Clear (float minSampleTime) {
        _samplesSubmitted = false;
    }

    public float GetCurrentTime () {
        return time;
    }

    public bool Predict (out float direction, out float confidence) {
        if (!_samplesSubmitted) {
            direction = 0.0f;
            confidence = 0.0f;
            return false;
        }

        direction = _predDirection;
        confidence = _predConfidence;
        return true;
    }

    public void SubmitSample(Vector2 position,
        float direction, float deltaTime) {

        _samplesSubmitted = true;

        time += deltaTime;

        _predDirection = direction;
        _predConfidence = confidence;
    }
}