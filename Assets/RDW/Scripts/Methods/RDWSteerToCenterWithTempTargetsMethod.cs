using UnityEngine;
using System.Collections;

public class RDWSteerToCenterWithTempTargetsMethod : RDWMethod {

    public float tempTargetThresholdDegrees = 160.0f;

    RDWRedirector _redirector;
    bool _firstFrame;
    Vector2 _prevTrackPos;
    float _prevTrackDir;
    Vector2 _prevWorldPos;
    float _time;

    bool _usingTempTarget;
    int _tempTargetCw;

    override protected void Awake () {
        base.Awake();
        _redirector = GetComponent<RDWRedirector>();
    }

    public override void _OnAttach () {
        _firstFrame = true;
    }

    public override void Discontinuity () {
        _firstFrame = true;
    }

    public override IEnumerator Step (Vector2 trackPos, float trackDir, float deltaTime) {
        var worldPos = UnityToRedirectionPlanePos(trackSpace.TrackToWorldPosition(trackPos));
        var worldDirVec = trackSpace.TrackToWorldDirection(trackDir);
        var worldDirVec2 = new Vector2(worldDirVec.x,worldDirVec.z);
        var worldDir = RDWMethodUtil.VecToAngle(worldDirVec2);

        if (_firstFrame) {
            _prevTrackPos = trackPos;
            _prevTrackDir = trackDir;
            _prevWorldPos = worldPos;
            _time = 0.0f;
            _firstFrame = false;
        }

        var targetTrackPos = trackSpace.bounds.center;

        var targetTrackDir = RDWMethodUtil.VecToAngle(targetTrackPos - trackPos);
        var deltaAngle = Mathf.DeltaAngle(trackDir,targetTrackDir);

        // If we're pointing for enough away from the center, create temp target
        // Temp target stays consistent so we don't switch redirection dir often
        if (Mathf.Abs(deltaAngle) > tempTargetThresholdDegrees) {
            if (!_usingTempTarget) {
                _usingTempTarget = true;
                _tempTargetCw = deltaAngle < 0 ? 1 : -1;
            }

            var fromTargetDir = RDWMethodUtil.VecToAngle(trackPos - targetTrackPos);
            var offset = Vector2.up * 4;

            var offsetAngle = fromTargetDir + 90.0f * _tempTargetCw;
            var tempTarget = trackPos + RDWMethodUtil.RotateVector2(offset, offsetAngle);

            targetTrackPos = tempTarget;
        } else if (_usingTempTarget) {
            _usingTempTarget = false;
        }

        // Update target track dir for (possible) new target
        targetTrackDir = RDWMethodUtil.VecToAngle(targetTrackPos - trackPos);

        var instr = new RDWRedirector.RedirectInstruction();
        instr.rotationDirection = Mathf.DeltaAngle(trackDir,targetTrackDir) < 0
            ? RDWRedirector.RedirectInstruction.RotationDirection.Right
            : RDWRedirector.RedirectInstruction.RotationDirection.Left;
        instr.scaleDirection = RDWRedirector.RedirectInstruction.ScaleDirection.None;

        _redirector.Apply(instr,trackPos,_prevTrackPos,trackDir,_prevTrackDir,
            worldPos,_prevWorldPos,_time);
        _prevTrackPos = trackPos;
        _prevTrackDir = trackDir;
        _prevWorldPos = worldPos;
        yield break;
    }

    Vector2 UnityToRedirectionPlanePos (Vector3 worldPos) {
        return new Vector2(worldPos.x,worldPos.z);
    }
}
