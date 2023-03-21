using UnityEngine;
using System.Collections;

public class RDWSteerToCenterExMethod : RDWMethod {

    public enum TranslationGainMode {
        None,
        Static,
        Dynamic
    }

    public bool useTempTargets = false;
    public float tempTargetThresholdDegrees = 160.0f;

    public TranslationGainMode translationGainMode = TranslationGainMode.None;
    public RDWRedirector.RedirectInstruction.ScaleDirection staticTranslationGainDirection;

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

        _time += deltaTime;

        var targetTrackPos = trackSpace.bounds.center;

        var targetTrackDir = RDWMethodUtil.VecToAngle(targetTrackPos - trackPos);
        var deltaAngle = Mathf.DeltaAngle(trackDir,targetTrackDir);

        if (useTempTargets) {
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

                // Update target track dir for target
                targetTrackDir = RDWMethodUtil.VecToAngle(targetTrackPos - trackPos);
                deltaAngle = Mathf.DeltaAngle(trackDir,targetTrackDir);

            } else if (_usingTempTarget) {
                _usingTempTarget = false;
            }
        }

        var instr = new RDWRedirector.RedirectInstruction();
        instr.rotationDirection = deltaAngle < 0
            ? RDWRedirector.RedirectInstruction.RotationDirection.Right
            : RDWRedirector.RedirectInstruction.RotationDirection.Left;
        instr.scaleDirection =  RDWRedirector.RedirectInstruction.ScaleDirection.None;
        if (translationGainMode == TranslationGainMode.Static) {
            instr.scaleDirection = staticTranslationGainDirection;
        } else if (translationGainMode == TranslationGainMode.Dynamic) {
            if (Mathf.Abs(deltaAngle) < 90) {
                // If we're walking towards the center, reduce
                instr.scaleDirection =
                    RDWRedirector.RedirectInstruction.ScaleDirection.Reduce;
            } else {
                // If we're walking away, magnify
                instr.scaleDirection =
                    RDWRedirector.RedirectInstruction.ScaleDirection.Magnify;
            }
        }

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
