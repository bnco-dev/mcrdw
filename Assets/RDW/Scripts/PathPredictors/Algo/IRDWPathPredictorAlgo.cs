using System.Collections.Generic;
using UnityEngine;

public interface IRDWPathPredictorAlgo {
    void Clear ();
    void Clear (float minSampletime);
    bool Predict (out float direction, out float confidence);
    void SubmitSample (Vector2 position, float direction, float deltaTime);
    float GetCurrentTime ();
}