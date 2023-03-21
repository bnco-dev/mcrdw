using UnityEngine;
using System.Collections;

public class RDWAdjustableTranslationGainMethod : RDWMethod {

    public float translationMultiplier = 2.0f;

    public override void _OnAttach () {
        _firstFrame = true;
    }

    public override void Discontinuity() {
        _firstFrame = true;
    }

    public override IEnumerator Step (Vector2 trackPos, float trackDir, float deltaTime) {
        if (_firstFrame) {
            _prevTrackPos = trackPos;
            _firstFrame = false;
        }
        var virtPos = transform.TransformPoint(new Vector3(trackPos.x, 0, trackPos.y));
        var prevVirtPos = transform.TransformPoint(new Vector3(_prevTrackPos.x, 0, _prevTrackPos.y));
        var gain = virtPos - prevVirtPos;

        transform.position += gain * (translationMultiplier - 1);
        yield break;
    }

    bool _firstFrame;
    Vector2 _prevTrackPos;
}
