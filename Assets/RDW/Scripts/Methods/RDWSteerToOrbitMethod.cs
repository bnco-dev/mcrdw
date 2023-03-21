using UnityEngine;
using System.Collections;

public class RDWSteerToOrbitMethod : RDWMethod {

    public float orbitRadiusMeters = 5.0f;

    RDWRedirector _redirector;
    bool _firstFrame;
    Vector2 _prevTrackPos;
    float _prevTrackDir;
    Vector2 _prevWorldPos;
    float _time;

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

        _time += deltaTime;

        var cw = Vector2.zero;
        var ccw = Vector2.zero;
        RDWMethodUtil.GetTangentPoints(trackPos, orbitRadiusMeters, out cw, out ccw);

        // Calculate angles to each orbit target
        var cwDir = RDWMethodUtil.VecToAngle(cw - trackPos);
        var cwDelta = Mathf.DeltaAngle(trackDir,cwDir);
        var ccwDir = RDWMethodUtil.VecToAngle(ccw - trackPos);
        var ccwDelta = Mathf.DeltaAngle(trackDir,ccwDir);

        var targetTrackPos = Mathf.Abs(cwDelta) < Mathf.Abs(ccwDir) ? cw : ccw;
        var targetTrackDir = RDWMethodUtil.VecToAngle(targetTrackPos - trackPos);

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
