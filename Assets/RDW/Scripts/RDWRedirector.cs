using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RDWTrackSpace))]
public class RDWRedirector : MonoBehaviour {

    [System.Serializable]
    public struct RedirectInstruction {
        public enum RotationDirection {
            None = 0,
            Left,
            Right
        }
        public enum ScaleDirection {
            None = 0,
            Magnify,
            Reduce
        }

        public RotationDirection rotationDirection;
        public ScaleDirection scaleDirection;
    }

    public RDWTrackSpace trackSpace { get; private set; }

    // These default values are the 75% detection thresholds from Steinicke 2010
    public float angularCoRotMultiplier = 1.3f;
    public float angularAntiRotMultiplier = 0.85f;
    public float transGainMagnifyMultiplier = 1.3f;
    public float transGainReduceMultiplier = 0.7f;
    // Curvature on a circular arc with radius of 22.03m
    public float curvatureGainDegreesPerMeter = (1.0f/(2*Mathf.PI*22.03f))*360.0f;

    public float smoothingWindowSeconds = 0.5f;

    public event TranslationGainedHandler translationGained = delegate {};
    public event RotationGainedHandler rotationGained = delegate {};

    public delegate void TranslationGainedHandler (
        RedirectInstruction.ScaleDirection scaleDirection,
        Vector2 modMeters);
    public delegate void RotationGainedHandler (
        RedirectInstruction.RotationDirection rotationDirection,
        float modDegrees);

    struct SmoothingFrame {
        public float unsmoothedScaleModMul;
        public float unsmoothedTurnModMul;
        public float time;
    }

    List<SmoothingFrame> _smoothingFrames = new List<SmoothingFrame>();

    void Awake () {
        trackSpace = GetComponent<RDWTrackSpace>();
    }

    public void Clear () {
        _smoothingFrames.Clear();
    }

    public void Apply (RedirectInstruction instr,
        Vector2 trackPos, Vector2 prevTrackPos,
        float trackDir, float prevTrackDir,
        Vector2 worldPos, Vector2 prevWorldPos,
        float currentTime) {

        var trackPosDeltaMeters = (trackPos - prevTrackPos).magnitude;

        // Smoothing - calc scale mod mul
        var unsmoothedScaleModMul = 0.0f;
        if (instr.scaleDirection == RedirectInstruction.ScaleDirection.Magnify) {
            unsmoothedScaleModMul = transGainMagnifyMultiplier - 1.0f;
        } else if (instr.scaleDirection == RedirectInstruction.ScaleDirection.Reduce) {
            unsmoothedScaleModMul = transGainReduceMultiplier - 1.0f;
        }

        // Smoothing - calc turn mod mul
        var unsmoothedTurnModMul = 0.0f;
        var trackDirDelta = Mathf.DeltaAngle(prevTrackDir,trackDir);
        var dirDeltaDirection = trackDirDelta < 0
            ? RedirectInstruction.RotationDirection.Left
            : RedirectInstruction.RotationDirection.Right;
        if (instr.rotationDirection != RedirectInstruction.RotationDirection.None) {
            unsmoothedTurnModMul = dirDeltaDirection == instr.rotationDirection
                ? angularCoRotMultiplier - 1.0f
                : angularAntiRotMultiplier - 1.0f;
        }

        // Smoothing - calc smoothed values (linearly weighted)
        _smoothingFrames.Add(new SmoothingFrame {
            unsmoothedScaleModMul = unsmoothedScaleModMul,
            unsmoothedTurnModMul = unsmoothedTurnModMul,
            time = currentTime
        });
        var totalWeight = 0.0f;
        var scaleModMul = 0.0f;
        var turnModMul = 0.0f;
        for (int i = 0; i < _smoothingFrames.Count; i++) {
            var age = currentTime - _smoothingFrames[i].time;
            if (age >= smoothingWindowSeconds) {
                _smoothingFrames.RemoveAt(i);
                i--;
                continue;
            }

            var weight = 1.0f - (age / smoothingWindowSeconds);
            if (weight > 0) {
                scaleModMul += _smoothingFrames[i].unsmoothedScaleModMul * weight;
                turnModMul += _smoothingFrames[i].unsmoothedTurnModMul * weight;
                totalWeight += weight;
            }
        }

        scaleModMul /= totalWeight;
        turnModMul /= totalWeight;

        // Scale mod
        if (instr.scaleDirection != RedirectInstruction.ScaleDirection.None) {
            // var scaleModMul =
            //     instr.scaleDirection == RedirectInstruction.ScaleDirection.Magnify
            //         ? transGainMagnifyMultiplier - 1.0f
            //         : transGainReduceMultiplier - 1.0f;

            var trackPosDeltaDistance = (trackPos - prevTrackPos).magnitude;
            var scaleModDistance = scaleModMul * trackPosDeltaDistance;
            var scaleModDirVec = (worldPos - prevWorldPos).normalized;
            var scaleMod = scaleModDirVec*scaleModDistance;
            trackSpace.trackSpaceTransform.position +=
                RedirectionPlaneToUnityPos(scaleMod);

            translationGained(instr.scaleDirection, scaleMod);
        }

        // Rotation mod
        if (instr.rotationDirection != RedirectInstruction.RotationDirection.None) {
            // Turn
            // var trackDirDelta = Mathf.DeltaAngle(prevTrackDir,trackDir);
            // var dirDeltaDirection = trackDirDelta < 0
            //     ? RedirectInstruction.RotationDirection.Left
            //     : RedirectInstruction.RotationDirection.Right;
            // var turnModMul = dirDeltaDirection == instr.rotationDirection
            //         ? angularCoRotMultiplier - 1.0f
            //         : angularAntiRotMultiplier - 1.0f;

            var turnModDegrees = trackDirDelta * turnModMul;

            var curveMul = instr.rotationDirection == RedirectInstruction.RotationDirection.Left ? 1 : -1;
            var curveModDegrees = curveMul * trackPosDeltaMeters * curvatureGainDegreesPerMeter;

            var rotateAroundTpos3 = RedirectionPlaneToUnityPos(trackPos);
            var rotateAroundPos = trackSpace.trackSpaceTransform.TransformPoint(rotateAroundTpos3);

            var totalModDegrees = turnModDegrees + curveModDegrees;
            // Rotation gain
            trackSpace.trackSpaceTransform.RotateAround(
                rotateAroundPos,
                Vector3.up,
                totalModDegrees
            );

            rotationGained(instr.rotationDirection, totalModDegrees);
        }
    }

    Vector3 RedirectionPlaneToUnityPos (Vector2 redPlaneWorldPos) {
        return new Vector3(redPlaneWorldPos.x,0.0f,redPlaneWorldPos.y);
    }
}