using System;
using UnityEngine;

// Thin monobehaviour wrapper over Path Predictor Algo classes
public abstract class ARDWPathPredictor : MonoBehaviour {
    public abstract void Clear ();
    public abstract void Clear (float minSampletime);
    public abstract void SubmitSample (Vector2 position, float direction,
        float deltaTime);
    public abstract bool Predict (out float direction, out float confidence);
    public abstract float GetCurrentTime ();
}