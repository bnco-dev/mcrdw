using System.Collections.Generic;
using UnityEngine;

public class RDWPositionalPathPredictorAlgo : IRDWPathPredictorAlgo {
    struct Sample {
        public readonly Vector2 position;
        public readonly float direction;
        public readonly float time;
        public Sample (Vector2 position, float direction, float time) {
            this.position = position;
            this.direction = direction;
            this.time = time;
        }
    }

    public int windowCount {get; set;}
    public float windowSizeSeconds {get; set;}
    public float time {get; private set;}
    public float confidenceFloor { get; set; }

    List<Sample> _samples = new List<Sample>();
    List<Vector2> _windowAveragePositions = new List<Vector2>();

    float _lastDirection;
    float _lastConfidence;
    bool _hasNewSamples;

    public RDWPositionalPathPredictorAlgo () { }

    public RDWPositionalPathPredictorAlgo (int windowCount,
        float windowSizeSeconds, float confidenceFloor) {
        this.windowCount = windowCount;
        this.windowSizeSeconds = windowSizeSeconds;
        this.confidenceFloor = confidenceFloor;
    }

    public void Clear () {
        _samples.Clear();
    }

    public void Clear (float minSampleTime) {
        for (int si = _samples.Count-1; si >= 0; si++) {
            if (_samples[si].time < minSampleTime) {
                _samples.RemoveRange(0,si+1);
                return;
            }
        }
    }

    public float GetCurrentTime () {
        if (_samples.Count <= 0) {
            return 0.0f;
        }

        return _samples[_samples.Count-1].time;
    }

    void UpdatePrediction () {
        if (!_hasNewSamples) {
            return;
        }

        // Run through samples, build windows, only store full, complete windows
        _windowAveragePositions.Clear();
        var windowTotalPosition = Vector2.zero;
        var windowEndIndex = _samples.Count-1;
        var windowStartTime = _samples[windowEndIndex].time - windowSizeSeconds;
        for (int wi = 0, si = _samples.Count-1; si >= 0; si--) {
            var sampleTime = _samples[si].time;
            if (sampleTime >= windowStartTime) {
                windowTotalPosition += _samples[si].position;
                continue;
            }

            // Finish/store complete window
            var windowSampleCount = windowEndIndex - si;
            _windowAveragePositions.Add(windowTotalPosition / windowSampleCount);

            wi++;

            // Prepare next window
            windowTotalPosition = _samples[si].position;
            windowEndIndex = si;
            windowStartTime = sampleTime - windowSizeSeconds;
            if (wi >= windowCount) {
                break;
            }
        }

        // Run through windows
        var totalDirVectors = Vector2.zero;
        var prevDirVec = Vector2.zero;
        var dotProdSum = 0.0f;
        for (int wi = 1; wi < _windowAveragePositions.Count; wi++) {
            var dirVec = (_windowAveragePositions[wi-1] -
                _windowAveragePositions[wi]).normalized;

            if (wi > 1) {
                dotProdSum += Vector2.Dot(dirVec,prevDirVec);
            }

            totalDirVectors += dirVec;

            prevDirVec = dirVec;
        }
        var direction = RDWMethodUtil.VecToAngle(totalDirVectors);
        // confidence = totalDirVectors.magnitude / _windowAveragePositions.Count;
        var confidence = 0.0f;
        if (_windowAveragePositions.Count > 2) {
            confidence = dotProdSum / (_windowAveragePositions.Count-2);
        }
        confidence = (confidence - confidenceFloor) / (1.0f - confidenceFloor);
        confidence = Mathf.Clamp01(confidence);

        _lastDirection = direction;
        _lastConfidence = confidence;
        _hasNewSamples = false;
    }

    public bool Predict (out float direction, out float confidence) {
        if (windowCount <= 0 || windowSizeSeconds <= 0 || _samples.Count <= 0) {
            direction = 0.0f;
            confidence = 0.0f;
            return false;
        }

        UpdatePrediction();

        direction = _lastDirection;
        confidence = _lastConfidence;
        return true;
    }

    public void SubmitSample(Vector2 position,
        float direction, float deltaTime) {

        if (deltaTime <= 0) {
            Debug.LogWarning(
                "Path predictor samples must be sequential, deltaTime > 0. " +
                "Ignoring sample...");
            return;
        }

        time = time + deltaTime;

        _samples.Add (
            new Sample (position: position, direction: direction, time: time));
        _hasNewSamples = true;
    }

    // void MoveWindows () {
    //     // This function moves windows up along the list of samples so that the
    //     // first window covers the current time. After this function completes,
    //     // all recent samples will be assigned to windows, and there will be
    //     // (0+) old samples which are now unassigned (at other end of list)
    //     // Precepts:
    //     // window sample indexes are always accurate, always point to real locations
    //     // _samples can be very large or completely empty, need to handle both
    //     // _samples
    //     var endIndex = _samples.Count-1;
    //     for (int wi = 0; wi < _windows.Count; wi++) {
    //         var window = _windows[wi];
    //         // Add samples to end
    //         for (int si = window.endSampleIndex; si <= endIndex; si++) {
    //             window.totalDirection += _samples[si].direction;
    //             window.totalPosition += _samples[si].position;
    //         }
    //         window.endTime = _samples[endIndex].time;
    //         window.startTime = window.endTime - windowSizeSeconds;
    //         window.endSampleIndex = endIndex;

    //         // Remove samples from beginning
    //         var startIndex = window.startSampleIndex;
    //         int nextStartIndex = startIndex;
    //         for (;; nextStartIndex++) {
    //             if (_samples[nextStartIndex].time >= window.startTime) {
    //                 break;
    //             }
    //             window.totalDirection -= _samples[nextStartIndex].direction;
    //             window.totalPosition -= _samples[nextStartIndex].position;
    //         }
    //         window.startSampleIndex = nextStartIndex;
    //         endIndex = startIndex - 1;
    //     }
    // }

    // void MoveWindow (Window window, float deltaTime) {
    //     var nextStartTime = window.startTime + deltaTime;
    //     var nextEndTime = window.endTime + deltaTime;
    //     // Remove records from before new window starts
    //     for (var i = GetSampleIndex(window.startTime);;i++) {
    //         if (_samples[i].time >= nextStartTime) {
    //             break;
    //         }
    //         window.totalDirection -= _samples[i].direction;
    //         window.totalPosition -= _samples[i].position;
    //         window.sampleCount--;
    //     }
    //     window.startTime = nextStartTime;

    //     // Add records new records for after old window ended
    //     for (var i = GetSampleIndex(window.endTime);;i++) {
    //         if (_samples[i].time >= nextEndTime) {
    //             break;
    //         }
    //         window.totalDirection += _samples[i].direction;
    //         window.totalPosition += _samples[i].position;
    //         window.sampleCount++;
    //     }
    //     window.endTime = nextEndTime;
    // }

    // int GetSampleIndex(float time) {
    //     return 0;
    // }

}