using System.Collections.Generic;
using UnityEngine;

// Built for flexibility and correctness rather than speed
// Consider all operations except for Add to be O(n)
public class Window <T> {
    protected struct Sample {
        public T data;
        public float time;
    }

    protected List<Sample> samples = new List<Sample>();

    public float duration { get; private set; }
    public float currentTime { get; private set;}

    public Window (float duration = 0, float initialTime = 0) {
        this.duration = duration;
        this.currentTime = initialTime;
    }

    public void Advance (T sample, float time) {

        if (time <= currentTime) {
            Debug.LogWarning(
                "Window samples must be sequential, deltaTime > 0. " +
                "Ignoring sample...");
            return;
        }

        samples.Add(new Sample { data = sample, time = time } );
        currentTime = time;

        for (int i = 0; i < samples.Count; i++) {
            if (samples[i].time > currentTime - duration) {
                if (i > 0) {
                    samples.RemoveRange(0,i);
                }
                break;
            }
        }
    }

    public void AdvanceDelta (T sample, float timeSinceLastSample) {
        Advance(sample,currentTime + timeSinceLastSample);
    }

    public void Clear () {
        samples.Clear();
    }

    public void Clear (float duration, float initialTime) {
        this.duration = duration;
        this.currentTime = currentTime;
        Clear();
    }

    public void GetSamples (List<T> outputSamples, List<float> outputTimes) {
        for (int i = 0; i < samples.Count; i++) {
            if (outputSamples != null) {
                outputSamples.Add(samples[i].data);
            }

            if (outputTimes != null) {
                outputTimes.Add(samples[i].time);
            }
        }
    }
}