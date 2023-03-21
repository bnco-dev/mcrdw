using UnityEngine;
using System.Collections;

public class RDWSteerToCenterMethod : RDWMethod {

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

        var targetTrackPos = trackSpace.bounds.center;

        var targetTrackDir = RDWMethodUtil.VecToAngle(targetTrackPos - trackPos);
        var deltaAngle = Mathf.DeltaAngle(trackDir,targetTrackDir);

        var instr = new RDWRedirector.RedirectInstruction();
        instr.rotationDirection = deltaAngle < 0
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
