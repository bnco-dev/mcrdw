// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;

// public class RDWBruteForceMethod : RDWMethod {

//     struct Config2 {
//         public Vector2 pos;
//         public Vector2 fwd;
//     }

//     public float lookaheadSeconds = 5;
//     public int lookaheadSteps = 5;
//     public float refreshRateSeconds = 0.2f;

//     public float baselineRotPerSecDegrees = 0.5f;

//     public float linearMaxRotPerSecDegrees = 15.0f;
//     public float linearTargetRadiusMeters = 7.5f;

//     public float angularCoRotMultiplier = 1.3f;
//     public float angularAntiRotMultiplier = 0.85f;
//     public float angularMaxRotPerSecDegrees = 30.0f;

//     public float transGainMagnifyMultiplier = 1.3f;
//     public float transGainReduceMultiplier = 0.7f;

//     // This is low, average is more like 1.4m/s
//     public float walkSpeedMetersPerSecond = 1.0f;
//     // This is also pretty slow
//     public float turnSpeedDegreesPerSecond = 90.0f;

//     public float earlyProbabilityCutoff = 0.001f;

//     public override void _OnAttach () {
//         _time = 0;
//         _lastRefreshTime = Mathf.NegativeInfinity;

//         _bestqs = new byte[lookaheadSteps];
//         _qs = new byte[lookaheadSteps];
//         // _vps = new byte[lookaheadSteps];
//         _vs = new Vector2[lookaheadSteps];
//         _ws = new Vector2[lookaheadSteps];

//         _firstFrame = true;
//     }

//     bool _firstFrame;
//     public Vector2 _prevTrackPos;
//     public float _prevTrackDir;

//     bool _toVisualize;
//     public void SetVisualizationFlag() {
//         _toVisualize = true;
//     }

//     void TestInverseFns () {

//         var deltaTime = 0.25f;
//         _w0 = new Vector2(10,10);
//         _w0dir = 90;
//         var w0fwd = Vector2.right;
//         var w1 = _w0 + new Vector2(2f,2f);
//         var trackTurn = 90.0f;
//         var w1fwd = RDWMethodUtil.RotateVector2(Vector2.right,trackTurn);

//         var v0 = new Vector2(50,20);
//         var v0fwd = Vector2.up;
//         var direction = -1;
//         // var w1dir = 100;
//         var deltaTrackDir = Mathf.DeltaAngle(Angle(w0fwd),Angle(w1fwd));

//         // Steer turn calculates the full virtual turn in degrees
//         var virtTurn = RDWMethodUtil.SteerTurn((w1 - _w0).magnitude, deltaTime, deltaTrackDir,
//             direction,
//             baselineRotPerSecDegrees, linearTargetRadiusMeters, angularCoRotMultiplier,angularAntiRotMultiplier);

//         // Track space dir is s.t. w0fwd -> v0fwd
//         var trackToVirtDir = Mathf.DeltaAngle(Angle(w0fwd),Angle(v0fwd));
//         // w1fwd -> v1fwd
//         var trackToRedVirtDir = trackToVirtDir + (virtTurn - deltaTrackDir);
//         // w0fwd -> v1fwd
//         var trackToRedVirtDirWithTurn = trackToVirtDir + virtTurn;
//         // var postTrackSpaceDir = trackToVirtDir + delta;

//         // V1 is V0 + the redirected trackspace movement
//         var v1 = v0 + RDWMethodUtil.RotateVector2(w1-_w0,trackToRedVirtDir);
//         var nored_v1 = v0 + RDWMethodUtil.RotateVector2(w1-_w0,trackToVirtDir);
//         // V1FWD includes the redirection and the turn between w0fwd and w1fwd
//         var v1fwd = RDWMethodUtil.RotateVector2(w1fwd,trackToRedVirtDir);
//         var nored_v1fwd = RDWMethodUtil.RotateVector2(w1fwd,trackToVirtDir);
//         // var v1fwd = RDWMethodUtil.RotateVector2(v0fwd,delta);'

//         // Debug.Log((w1-_w0).magnitude + " " + (v1-v0).magnitude);

//         // Find w1, w1fwd, operating only on: w0, v0, v1, v0fwd, v1fwd
//         // var virtTurn = Mathf.DeltaAngle(Angle(v0fwd),Angle(v1fwd));
//         var calcTrackTurn = RDWMethodUtil.InverseSteerTurn((v1-v0).magnitude,
//             deltaTime,virtTurn,direction,
//             baselineRotPerSecDegrees,linearTargetRadiusMeters,angularCoRotMultiplier,angularAntiRotMultiplier);

//         // calcTrackTurn is the track space turn that would generate a virtual redirection
//         // but we're giving the full virtual turn, not just the redirection
//         // we don't know what proportion of the virtual turn is redirection
//         // can we actually separate the two?
//         // but only a small amount of the virtual turn is

//         // var calc_w1 = ApplySteeringInverseNew(_w0,w0fwd,v0,v1,v0fwd,v1fwd,trackToWorldSpaceDir,direction,deltaTime);
//         // var calc_dw = ApplySteeringInverse(0,direction,1,dv);

//         // Debug.Log("w0: " + _w0.ToStringEx() + " w1: " + w1.ToStringEx() + " dw: " +
//         //     (w1-_w0).ToStringEx() + " dv: " + dv.ToStringEx() + " calc_dw: " + calc_dw.ToStringEx());

//         Debug.Log("w0: " + _w0.ToStringEx() + " w1: " + w1.ToStringEx() +
//         " w0fwd: " + w0fwd.ToStringEx() + " w1fwd: " + w1fwd.ToStringEx() +
//         " v0: " + v0.ToStringEx() + " v1: " + v1.ToStringEx() +
//         " v0fwd: " + v0fwd.ToStringEx() + " v1fwd: " + v1fwd.ToStringEx() +
//         " virtTurn: " + virtTurn + " trackTurn: " + trackTurn +
//         " calc_trackTurn: " + calcTrackTurn + " nored_v1: " + nored_v1 +
//         " nored_v1fwd: " + nored_v1fwd);
//     }

//     void TestPathPrediction (Vector2 trackPos, float trackDir, float deltaTime) {
//         _w0 = trackPos;
//         _w0dir = trackDir;
//         var virtPos = transform.TransformPoint(trackPos.x, 0, trackPos.y);
//         var wFwd = RDWMethodUtil.RotateVector2(Vector2.up, -trackDir);
//         var virtFwd = transform.TransformDirection(new Vector3(wFwd.x,0,wFwd.y));

//         _v0 = new Vector2(virtPos.x, virtPos.z);
//         _v0fwd = new Vector2(virtFwd.x, virtFwd.z);

//         InitPathPredictions();
//         var visualizer = GetComponent<DebugPathPredictionVisualizer>();
//         if (visualizer) {
//             visualizer.Visualize(_v0,_v0fwd);
//         }
//     }

//     public override IEnumerator Step (Vector2 trackPos, float trackDir, float deltaTime) {
//         //UnityEditor.EditorApplication.isPlaying = false;

//         TestInverseFns();
//         yield break;

//         if (_firstFrame) {
//             _prevTrackPos = trackPos;
//             _prevTrackDir = trackDir;
//             _firstFrame = false;

//             // TestPathPrediction(trackPos,trackDir,deltaTime);
//         }

//         if (_toVisualize) {
//             _toVisualize = false;

//             _w0 = trackPos;
//             _w0dir = trackDir;
//             var virtPos = transform.TransformPoint(trackPos.x, 0, trackPos.y);
//             var wFwd = RDWMethodUtil.RotateVector2(Vector2.up, -trackDir);
//             var virtFwd = transform.TransformDirection(new Vector3(wFwd.x,0,wFwd.y));

//             _v0 = new Vector2(virtPos.x, virtPos.z);
//             _v0fwd = new Vector2(virtFwd.x, virtFwd.z);

//             InitPathPredictions();
//             var visualizer = GetComponent<DebugPathPredictionVisualizer>();
//             if (visualizer) {
//                 visualizer.Visualize(_v0,_v0fwd);
//             }
//         }

//         _time += deltaTime;
//         if (_lastRefreshTime + refreshRateSeconds < _time) {
//             _lastRefreshTime = _time;
//             yield return StartCoroutine(Refresh(trackPos, trackDir));
//         }

//         ApplyRedirection((Redirection)_bestqs[0], deltaTime, trackPos, _prevTrackPos, trackDir, _prevTrackDir);

//         Debug.Log((Redirection)_bestqs[0]);

//         _prevTrackPos = trackPos;
//         _prevTrackDir = trackDir;

//         //var w = trackPos;
//         //var virtPos = transform.TransformPoint(trackPos.x, 0, trackPos.y);
//         //var v = new Vector2(virtPos.x, virtPos.z);
//         //for (int i = 0; i < 4; i++) {
//         //    var wDir = Vector3.zero;
//         //    var mag = 0.0f;
//         //    switch (i) {
//         //        case 0: wDir = Vector3.forward; mag = trackSpace.bounds.yMax - w.y; break;
//         //        case 1: wDir = Vector3.right; mag = trackSpace.bounds.xMax - w.x; break;
//         //        case 2: wDir = Vector3.back; mag = w.y - trackSpace.bounds.yMin; break;
//         //        case 3: wDir = Vector3.left; mag = w.x - trackSpace.bounds.xMin; break;
//         //    }
//         //    Debug.Log("wdir: " + wDir + " post: " + transform.TransformDirection(wDir).ToStringEx());

//         //    var vDir3 = transform.TransformDirection(wDir) * mag;
//         //    var vDir = new Vector2(vDir3.x, vDir3.z);

//         //    switch (i) {
//         //        case 0: i0.position = new Vector3(v.x + vDir.x, 0, v.y + vDir.y); break;
//         //        case 1: i1.position = new Vector3(v.x + vDir.x, 0, v.y + vDir.y); break;
//         //        case 2: i2.position = new Vector3(v.x + vDir.x, 0, v.y + vDir.y); break;
//         //        case 3: i3.position = new Vector3(v.x + vDir.x, 0, v.y + vDir.y); break;
//         //    }
//         //    //NavMesh.Raycast(v, v + vDir, out  );
//         //}
//     }

//     // void ApplyRedirection (Redirection redirection, float deltaTime, Vector2 trackPos,
//     //     Vector2 prevTrackPos, float trackDir, float prevTrackDir) {

//     //     switch (redirection) {

//     //         case Redirection.NoneNone: break;
//     //         case Redirection.LeftHalfNone: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); break;
//     //         case Redirection.LeftFullNone: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); break;
//     //         case Redirection.RightHalfNone: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); break;
//     //         case Redirection.RightFullNone: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); break;

//     //         case Redirection.NoneMagnifyHalf: ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;
//     //         case Redirection.LeftHalfMagnifyHalf: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;
//     //         case Redirection.LeftFullMagnifyHalf: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;
//     //         case Redirection.RightHalfMagnifyHalf: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;
//     //         case Redirection.RightFullMagnifyHalf: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;

//     //         case Redirection.NoneMagnifyFull: ApplyTransGain(1, 1, trackPos, prevTrackPos); break;
//     //         case Redirection.LeftHalfMagnifyFull: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 1, trackPos, prevTrackPos); break;
//     //         case Redirection.LeftFullMagnifyFull: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 1, trackPos, prevTrackPos); break;
//     //         case Redirection.RightHalfMagnifyFull: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 1, trackPos, prevTrackPos); break;
//     //         case Redirection.RightFullMagnifyFull: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 1, trackPos, prevTrackPos); break;

//     //         case Redirection.NoneReduceHalf: ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;
//     //         case Redirection.LeftHalfReduceHalf: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;
//     //         case Redirection.LeftFullReduceHalf: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;
//     //         case Redirection.RightHalfReduceHalf: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;
//     //         case Redirection.RightFullReduceHalf: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;

//     //         case Redirection.NoneReduceFull: ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//     //         case Redirection.LeftHalfReduceFull: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//     //         case Redirection.LeftFullReduceFull: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//     //         case Redirection.RightHalfReduceFull: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//     //         case Redirection.RightFullReduceFull: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//     //     }
//     // }

//     void ApplyRedirection (Redirection redirection, float deltaTime, Vector2 trackPos,
//         Vector2 prevTrackPos, float trackDir, float prevTrackDir) {

//         switch (redirection) {

//             case Redirection.NoneNone: break;
//             // case Redirection.LeftHalfNone: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); break;
//             case Redirection.LeftFullNone: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); break;
//             // case Redirection.RightHalfNone: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); break;
//             case Redirection.RightFullNone: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); break;

//             // case Redirection.NoneMagnifyHalf: ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;
//             // case Redirection.LeftHalfMagnifyHalf: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;
//             // case Redirection.LeftFullMagnifyHalf: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;
//             // case Redirection.RightHalfMagnifyHalf: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;
//             // case Redirection.RightFullMagnifyHalf: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 0.5f, trackPos, prevTrackPos); break;

//             case Redirection.NoneMagnifyFull: ApplyTransGain(1, 1, trackPos, prevTrackPos); break;
//             // case Redirection.LeftHalfMagnifyFull: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 1, trackPos, prevTrackPos); break;
//             case Redirection.LeftFullMagnifyFull: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 1, trackPos, prevTrackPos); break;
//             // case Redirection.RightHalfMagnifyFull: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 1, trackPos, prevTrackPos); break;
//             case Redirection.RightFullMagnifyFull: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(1, 1, trackPos, prevTrackPos); break;

//             // case Redirection.NoneReduceHalf: ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;
//             // case Redirection.LeftHalfReduceHalf: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;
//             // case Redirection.LeftFullReduceHalf: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;
//             // case Redirection.RightHalfReduceHalf: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;
//             // case Redirection.RightFullReduceHalf: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 0.5f, trackPos, prevTrackPos); break;

//             case Redirection.NoneReduceFull: ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//             // case Redirection.LeftHalfReduceFull: ApplySteering(-1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//             case Redirection.LeftFullReduceFull: ApplySteering(-1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//             // case Redirection.RightHalfReduceFull: ApplySteering(1, 0.5f, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//             case Redirection.RightFullReduceFull: ApplySteering(1, 1, deltaTime, trackPos, prevTrackPos, trackDir, prevTrackDir); ApplyTransGain(-1, 1, trackPos, prevTrackPos); break;
//         }
//     }

//     void ApplySteering (float direction, float magnitude, float deltaTime,
//         Vector2 trackPos, Vector2 prevTrackPos, float trackDir, float prevTrackDir) {

//         var delta = RDWMethodUtil.SteeringDelta(trackPos - prevTrackPos, deltaTime, Mathf.DeltaAngle(trackDir, prevTrackDir),
//             direction, baselineRotPerSecDegrees, linearMaxRotPerSecDegrees, angularCoRotMultiplier,
//             angularAntiRotMultiplier, angularMaxRotPerSecDegrees, linearMaxRotPerSecDegrees);

//         RDWMethodUtil.ApplySteeringDelta(transform, trackPos, delta * Mathf.Sign(direction));
//     }

//     void ApplyTransGain (float direction, float magnitude, Vector2 trackPos, Vector2 prevTrackPos) {
//         var virtPos = transform.TransformPoint(new Vector3(trackPos.x, 0, trackPos.y));
//         var prevVirtPos = transform.TransformPoint(new Vector3(_prevTrackPos.x, 0, _prevTrackPos.y));
//         var gain = virtPos - prevVirtPos;
//         var mult = direction > 0 ? transGainMagnifyMultiplier : transGainReduceMultiplier;
//         mult = (mult - 1) * magnitude;

//         Debug.Log("gain: " + gain.ToStringEx());
//         gain.y = 0; // ?????

//         transform.position += gain * mult;
//     }

//     public class PathPrediction {
//         public List<Vector2> vPoss;
//         public List<Vector2> vFwds;
//         public float probability;
//     }

//     enum PathPredictionNode {
//         Forward = 0,
//         Left,
//         Right,
//         None,

//         Count
//     }

//     float[] NODE_PROBABILITY = {
//         .8f,
//         .05f,
//         .05f,
//         .1f,
//     };

//     public List<PathPrediction> _pathPredictions = new List<PathPrediction>();

//     byte[] _bestqs;
//     byte[] _qs;
//     // byte[] _vps;
//     Vector2[] _vs;
//     Vector2[] _ws;
//     Vector2 _v0;
//     Vector2 _v0fwd;
//     Vector2 _w0;
//     float _w0dir;
//     // float _p;
//     IEnumerator Refresh (Vector2 trackPos, float trackDir) {
//         _w0 = trackPos;
//         _w0dir = trackDir;
//         var virtPos = transform.TransformPoint(trackPos.x, 0, trackPos.y);
//         var wFwd = RDWMethodUtil.RotateVector2(Vector2.up, -trackDir);
//         var virtFwd = transform.TransformDirection(new Vector3(wFwd.x,0,wFwd.y));

//         _v0 = new Vector2(virtPos.x, virtPos.z);
//         _v0fwd = new Vector2(virtFwd.x, virtFwd.z);

//         // missing: e (virtual env), f (track env)

//         var min = Mathf.Infinity;
//         // Init _pathPredictions;
//         InitPathPredictions();
//         // Init _qs
//         InitRedirections();
//         var count = 0;
//         while (true) {
//             var sum = EvaluateRedirections();

//             if (sum < min) {
//                 min = sum;
//                 Array.Copy(_qs, _bestqs, _qs.Length);
//             }

//             // Next _qs, quit if all checked
//             if (!IterateRedirections()) {
//                 break;
//             }

//             Debug.Log(++count);

//             yield return null;
//         }
//     }

//     // Interprets directions as global
//     // _v0, _vps -> _vs
//     //void ApplyPredictionsGlobal () {
//     //    var timeStep = lookaheadSeconds / lookaheadSteps;
//     //    for (int i = 0; i < lookaheadSteps; i++) {
//     //        var prev = i > 0 ? _vs[i - 1] : _v0;
//     //        switch ((Direction)_vps[i]) {
//     //            case Direction.None: _vs[i] = prev; break;
//     //            case Direction.Up: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(0, 1); break;
//     //            case Direction.UpRight: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(.7071068f, .7071068f); break;
//     //            case Direction.Right: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(1, 0); break;
//     //            case Direction.DownRight: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(.7071068f, -.7071068f); break;
//     //            case Direction.Down: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(0, -1); break;
//     //            case Direction.DownLeft: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(-.7071068f, -.7071068f); break;
//     //            case Direction.Left: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(-1, 0); break;
//     //            case Direction.UpLeft: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(-.7071068f, .7071068f); break;
//     //        }
//     //    }
//     //}

//     // Interprets directions locally (relative to current facing)
//     // _v0, _vps -> _vs
//     // void ApplyPredictionsOld () {
//     //     var timeStep = lookaheadSeconds / lookaheadSteps;
//     //     for (int i = 0; i < lookaheadSteps; i++) {
//     //         var prev = i > 0 ? _vs[i - 1] : _v0;
//     //         var fwd = (i == 0 ? _v0fwd : i == 1 ? prev - _v0 : prev - _vs[i - 2]).normalized;
//     //         switch ((Direction)_vps[i]) {
//     //             case Direction.None: _vs[i] = prev; break;
//     //             case Direction.Up: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * fwd; break;
//     //             //case Direction.UpRight: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(.7071068f, .7071068f); break;
//     //             case Direction.Right: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(fwd.y,-fwd.x); break;
//     //             //case Direction.DownRight: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(.7071068f, -.7071068f); break;
//     //             case Direction.Down: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(-fwd.x,-fwd.y); break;
//     //             //case Direction.DownLeft: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(-.7071068f, -.7071068f); break;
//     //             case Direction.Left: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(-fwd.y, fwd.x); break;
//     //             //case Direction.UpLeft: _vs[i] = prev + walkSpeedMetersPerSecond * timeStep * new Vector2(-.7071068f, .7071068f); break;
//     //         }
//     //     }
//     // }

//     // _v0, _w0, _vs, _qs -> _ws
//     // void ApplyRedirectionsInverse () {
//     //     for (int i = 0; i < lookaheadSteps; i++) {
//     //         var dv = _vs[i] - (i > 0 ? _vs[i - 1] : _v0);
//     //         var prev = i > 0 ? _ws[i - 1] : _w0;
//     //         switch ((Redirection)_qs[i]) {

//     //             case Redirection.NoneNone: _ws[i] = prev + dv; break;
//     //             case Redirection.LeftHalfNone: _ws[i] = prev + ApplySteeringInverse(i, -1, 0.5f, dv); break;
//     //             case Redirection.LeftFullNone: _ws[i] = prev + ApplySteeringInverse(i, -1, 1, dv); break;
//     //             case Redirection.RightHalfNone: _ws[i] = prev + ApplySteeringInverse(i, 1, 0.5f, dv); break;
//     //             case Redirection.RightFullNone: _ws[i] = prev + ApplySteeringInverse(i, 1, 1, dv); break;

//     //             case Redirection.NoneMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, dv); break;
//     //             case Redirection.LeftHalfMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, ApplySteeringInverse(i, -1, 0.5f, dv)); break;
//     //             case Redirection.LeftFullMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, ApplySteeringInverse(i, -1, 1, dv)); break;
//     //             case Redirection.RightHalfMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, ApplySteeringInverse(i, 1, 0.5f, dv)); break;
//     //             case Redirection.RightFullMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, ApplySteeringInverse(i, 1, 1, dv)); break;

//     //             case Redirection.NoneMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, dv); break;
//     //             case Redirection.LeftHalfMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, ApplySteeringInverse(i, -1, 0.5f, dv)); break;
//     //             case Redirection.LeftFullMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, ApplySteeringInverse(i, -1, 1, dv)); break;
//     //             case Redirection.RightHalfMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, ApplySteeringInverse(i, 1, 0.5f, dv)); break;
//     //             case Redirection.RightFullMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, ApplySteeringInverse(i, 1, 1, dv)); break;

//     //             case Redirection.NoneReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, dv); break;
//     //             case Redirection.LeftHalfReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, ApplySteeringInverse(i, -1, 0.5f, dv)); break;
//     //             case Redirection.LeftFullReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, ApplySteeringInverse(i, -1, 1, dv)); break;
//     //             case Redirection.RightHalfReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, ApplySteeringInverse(i, 1, 0.5f, dv)); break;
//     //             case Redirection.RightFullReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, ApplySteeringInverse(i, 1, 1, dv)); break;

//     //             case Redirection.NoneReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, dv); break;
//     //             case Redirection.LeftHalfReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, ApplySteeringInverse(i, -1, 0.5f, dv)); break;
//     //             case Redirection.LeftFullReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, ApplySteeringInverse(i, -1, 1, dv)); break;
//     //             case Redirection.RightHalfReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, ApplySteeringInverse(i, 1, 0.5f, dv)); break;
//     //             case Redirection.RightFullReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, ApplySteeringInverse(i, 1, 1, dv)); break;
//     //         }
//     //     }
//     // }

//     // Simple mode
//     void ApplyRedirectionsInverse () {
//         for (int i = 0; i < lookaheadSteps; i++) {
//             var dv = _vs[i] - (i > 0 ? _vs[i - 1] : _v0);
//             var prev = i > 0 ? _ws[i - 1] : _w0;
//             switch ((Redirection)_qs[i]) {

//                 case Redirection.NoneNone: _ws[i] = prev + dv; break;
//                 // case Redirection.LeftHalfNone: _ws[i] = prev + ApplySteeringInverse(i, -1, 0.5f, dv); break;
//                 case Redirection.LeftFullNone: _ws[i] = prev + ApplySteeringInverse(i, -1, 1, dv); break;
//                 // case Redirection.RightHalfNone: _ws[i] = prev + ApplySteeringInverse(i, 1, 0.5f, dv); break;
//                 case Redirection.RightFullNone: _ws[i] = prev + ApplySteeringInverse(i, 1, 1, dv); break;

//                 // case Redirection.NoneMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, dv); break;
//                 // case Redirection.LeftHalfMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, ApplySteeringInverse(i, -1, 0.5f, dv)); break;
//                 // case Redirection.LeftFullMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, ApplySteeringInverse(i, -1, 1, dv)); break;
//                 // case Redirection.RightHalfMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, ApplySteeringInverse(i, 1, 0.5f, dv)); break;
//                 // case Redirection.RightFullMagnifyHalf: _ws[i] = prev + ApplyTransGainInverse(1, 0.5f, ApplySteeringInverse(i, 1, 1, dv)); break;

//                 case Redirection.NoneMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, dv); break;
//                 // case Redirection.LeftHalfMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, ApplySteeringInverse(i, -1, 0.5f, dv)); break;
//                 case Redirection.LeftFullMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, ApplySteeringInverse(i, -1, 1, dv)); break;
//                 // case Redirection.RightHalfMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, ApplySteeringInverse(i, 1, 0.5f, dv)); break;
//                 case Redirection.RightFullMagnifyFull: _ws[i] = prev + ApplyTransGainInverse(1, 1, ApplySteeringInverse(i, 1, 1, dv)); break;

//                 // case Redirection.NoneReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, dv); break;
//                 // case Redirection.LeftHalfReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, ApplySteeringInverse(i, -1, 0.5f, dv)); break;
//                 // case Redirection.LeftFullReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, ApplySteeringInverse(i, -1, 1, dv)); break;
//                 // case Redirection.RightHalfReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, ApplySteeringInverse(i, 1, 0.5f, dv)); break;
//                 // case Redirection.RightFullReduceHalf: _ws[i] = prev + ApplyTransGainInverse(-1, 0.5f, ApplySteeringInverse(i, 1, 1, dv)); break;

//                 case Redirection.NoneReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, dv); break;
//                 // case Redirection.LeftHalfReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, ApplySteeringInverse(i, -1, 0.5f, dv)); break;
//                 case Redirection.LeftFullReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, ApplySteeringInverse(i, -1, 1, dv)); break;
//                 // case Redirection.RightHalfReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, ApplySteeringInverse(i, 1, 0.5f, dv)); break;
//                 case Redirection.RightFullReduceFull: _ws[i] = prev + ApplyTransGainInverse(-1, 1, ApplySteeringInverse(i, 1, 1, dv)); break;
//             }
//         }
//     }

//     //void SteeringInverseTest (float direction, Vector2 w) {
//     //    var timeStep = lookaheadSeconds / lookaheadSteps;
//     //    var v = RDWMethodUtil.RotateVector2(w, Mathf.Sign(direction) *
//     //        RDWMethodUtil.SteeringDelta(w, timeStep, Angle(w), direction,
//     //        baselineRotPerSecDegrees, linearMaxRotPerSecDegrees, angularCoRotMultiplier,
//     //        angularAntiRotMultiplier, angularMaxRotPerSecDegrees, linearMaxRotPerSecDegrees));

//     //    var learnedW = v;

//     //    // Get world space movement from virtual space movement with (dumb) iterative solver
//     //    for (int i = 0; i < 1; i++) {
//     //        var deltaTrackDir = Angle(learnedW);
//     //        var steeringDelta = RDWMethodUtil.SteeringDelta(learnedW, timeStep, Angle(learnedW), direction,
//     //            baselineRotPerSecDegrees, linearMaxRotPerSecDegrees, angularCoRotMultiplier,
//     //            angularAntiRotMultiplier, angularMaxRotPerSecDegrees, linearMaxRotPerSecDegrees);

//     //        // Update w
//     //        if (i < 9) {
//     //            learnedW = RDWMethodUtil.RotateVector2(v, -Mathf.Sign(direction) * steeringDelta);
//     //        } else {
//     //            learnedW = RDWMethodUtil.RotateVector2(v, -Mathf.Sign(direction) * steeringDelta);
//     //        }
//     //    }

//     //    //-Mathf.Sign(direction)

//     //    Debug.Log("dir: " + direction  + " w: " + w.ToStringEx() + " v: " + v.ToStringEx() + " learnedW: " + learnedW.ToStringEx());
//     //}

//     Vector2 ApplySteeringInverse (int step, float direction, float magnitude, Vector2 v) {
//         var prevW = step > 0 ? _ws[step - 1] : _w0;
//         var timeStep = lookaheadSeconds / lookaheadSteps;
//         var prevTrackDir = 0.0f;
//         if (step == 0) {
//             prevTrackDir = _w0dir;
//         } else {
//             prevTrackDir = Angle(prevW - (step == 1 ? _w0 : _ws[step - 2]));
//         }

//         var w = v;

//         // Get world space movement from virtual space movement with (dumb) iterative solver
//         for (int i = 0; i < 3; i++) {
//             var deltaTrackDir = Mathf.DeltaAngle(Angle(w), prevTrackDir);
//             var steeringDelta = RDWMethodUtil.SteeringDelta(w, timeStep, deltaTrackDir, direction,
//                 baselineRotPerSecDegrees, linearMaxRotPerSecDegrees, angularCoRotMultiplier,
//                 angularAntiRotMultiplier, angularMaxRotPerSecDegrees, linearMaxRotPerSecDegrees);
//             // var steeringDelta = RDWMethodUtil.SteeringDelta(w + prevW, timeStep, deltaTrackDir, direction,
//             //     baselineRotPerSecDegrees, linearMaxRotPerSecDegrees, angularCoRotMultiplier,
//             //     angularAntiRotMultiplier, angularMaxRotPerSecDegrees, linearMaxRotPerSecDegrees);

//             // Update w
//             if (i < 2) {
//                 w = RDWMethodUtil.RotateVector2(v, Mathf.Sign(direction) * -steeringDelta);
//             } else {
//                 w = RDWMethodUtil.RotateVector2(v, Mathf.Sign(direction) * magnitude * -steeringDelta);
//             }
//         }

//         return w;
//     }

//     Config2 ApplySteeringInverseNew (Vector2 w0, Vector2 w0fwd,
//         Vector2 v0, Vector2 v1, Vector2 v0fwd, Vector2 v1fwd, float wtov,
//         float direction, float deltaTime) {

//         var deltaVirtDir = Mathf.DeltaAngle(Angle(v0fwd),Angle(v1fwd));
//         var steeringDelta = RDWMethodUtil.SteeringDelta((v1-v0).magnitude,deltaTime,deltaVirtDir,direction > 0 ? 1 : -1,
//         baselineRotPerSecDegrees,linearTargetRadiusMeters,angularCoRotMultiplier,
//         angularAntiRotMultiplier);

//         Debug.Log("angv0: " + Angle(v0fwd) + " angv1: " + Angle(v1fwd) + "dvdir: " + deltaVirtDir +" v1-v0: " + (v1-v0).ToStringEx() + " delta: " + steeringDelta);

//         // Approx steering delta will be similar to the one to get v1 from w0,w1
//         // How would this have worked "forwards?"
//         // v1 = v0 + rotate(w1-w0,track_to_world_deg + delta)

//         // Backwards:
//         // v1-v0 = rotate...
//         // rotate(v1-v0,-1*(track_to_world_deg + delta)) = w1 - w0
//         // w1 = w0 + rotate(v1-v0,-1*(track_to_world_deg + delta))

//         var w = new Config2();
//         w.pos = w0 + RDWMethodUtil.RotateVector2(v1-v0,-(wtov + steeringDelta));
//         w.fwd = RDWMethodUtil.RotateVector2(w0fwd,-(wtov + steeringDelta));
//         return w;
//     }

//     // Assume given track movement corresponds to virt movement
//     // Get track movement required to reach virt movement if given trans gain was applied
//     Vector2 ApplyTransGainInverse (float direction, float magnitude, Vector2 w) {
//         var mult = direction > 0 ? transGainMagnifyMultiplier : transGainReduceMultiplier;
//         mult = ((mult - 1) * magnitude) + 1;
//         return w * (1.0f / mult);
//     }

//     static float Angle (Vector2 v) {
//         return Vector2.Angle(v, Vector2.up) * (v.x < 0 ? -1 : 1);
//     }

//     float Metric () {
//         var score = 0.0f;

//         // Boundary collisions
//         // for (int i = 0; i < _ws.Length; i++) {
//         //     //score += _ws[i].magnitude;
//         //     if (!trackSpace.bounds.Contains(_ws[i])) {
//         //         score += 500;
//         //     }
//         // }

//         // Distance from Center (like Mean Distance from Center, MDC)
//         for (int i = 0; i < _ws.Length; i++) {
//             score += Vector2.SqrMagnitude(_ws[i] - trackSpace.bounds.center);
//         }

//         // False edges (last step only)
//         var w = _ws[lookaheadSteps - 1];
//         var v = _vs[lookaheadSteps - 1];
//         for (int i = 0; i < 4; i++) {
//             var wDir = Vector3.zero;
//             var mag = 0.0f;
//             switch (i) {
//                 case 0: wDir = Vector3.forward; mag = trackSpace.bounds.yMax - w.y; break;
//                 case 1: wDir = Vector3.right; mag = trackSpace.bounds.xMax - w.x; break;
//                 case 2: wDir = Vector3.back; mag = w.y - trackSpace.bounds.yMin; break;
//                 case 3: wDir = Vector3.left; mag = w.x - trackSpace.bounds.xMin; break;
//             }

//             var vDir3 = transform.TransformDirection(wDir) * mag;
//             var vDir = new Vector2(vDir3.x, vDir3.z);

//             var hit = new UnityEngine.AI.NavMeshHit();
//             if (!UnityEngine.AI.NavMesh.Raycast(v, v + vDir, out hit, UnityEngine.AI.NavMesh.AllAreas)) {
//                 // if no intervening wall, false edge
//                 // add distance / max distance to score
//                 score += mag / trackSpace.bounds.height;
//             }
//         }

//         // score += _ws[lookaheadSteps - 1].magnitude;

//         return score;
//     }

//     // // Directions in world space
//     // enum Direction
//     // {
//     //     None = 0,

//     //     Up,
//     //     //UpRight,
//     //     Right,
//     //     //DownRight,
//     //     Down,
//     //     //DownLeft,
//     //     Left,
//     //     //UpLeft,

//     //     Count
//     // }
//     // float[] DIRECTION_PROBABILITY = {
//     //     .1f, //.2f

//     //     .66f, //.1
//     //     //.1125f,
//     //     .1f,
//     //     //.1125f,
//     //     .1f,
//     //     //.1125f,
//     //     .04f,
//     //     //.1125f
//     // };

//     //float EvaluateRedirections () {
//     //    var sum = 0.0f;

//     //    // Init _vps, _p
//     //    InitPathPredictions();
//     //    while (true) {
//     //        // Fill _vs from _vps and _v0
//     //        ApplyPredictions();
//     //        // Fill _ws from _v0, _w0, _vs and _qs
//     //        ApplyRedirectionsInverse();
//     //        // Calculate badness metric, weight by probability of path
//     //        sum += _p * Metric();

//     //        // Next _vps, _p, quit if all checked
//     //        if (!IteratePathPredictions()) {
//     //            break;
//     //        }
//     //    }

//     //    return sum;
//     //}

//     // Recursive method
//     float EvaluateRedirections () {
//         var sum = 0.0f;
//         for (int i = 0 ; i < _pathPredictions.Count; i++) {
//             _pathPredictions[i].vPoss.CopyTo(_vs);
//             // Fill _ws from _v0, _w0, _vs and _qs
//             ApplyRedirectionsInverse();
//             sum += _pathPredictions[i].probability * Metric();
//         }

//         return sum;
//     }

//     // float EvaluateRedirectionsOld () {
//     //     var sum = 0.0f;
//     //     for (int i = 0; i < (byte)Direction.Count; i++) {
//     //         sum += EvaluateRedirectionsBody(0, (byte)i, DIRECTION_PROBABILITY[i]);
//     //     }

//     //     return sum;
//     // }

//     // float EvaluateRedirectionsBody (int step, byte path, float p) {
//     //     _vps[step] = path;

//     //     if (step + 1 >= lookaheadSteps) {
//     //         // Fill _vs from _vps and _v0
//     //         ApplyPredictions();
//     //         // Fill _ws from _v0, _w0, _vs and _qs
//     //         ApplyRedirectionsInverse();
//     //         // Calculate badness metric, weight by probability of path
//     //         return p * Metric();
//     //     }

//     //     if (p < earlyProbabilityCutoff) {
//     //         return 0.0f;
//     //     }

//     //     var sum = 0.0f;
//     //     for (int i = 0; i < (byte)Direction.Count; i++) {
//     //         sum += EvaluateRedirectionsBody(step + 1, (byte)i, p * DIRECTION_PROBABILITY[i]);
//     //     }

//     //     return sum;
//     // }

//     // As with UniformPathPredictor, static at runtime
//     // But would occupy a huge amount of space, so use iterator like syntax
//     // void InitPathPredictions () {
//     //     for (int i = 0; i < _vps.Length; i++) {
//     //         _vps[i] = 0;
//     //     }

//     //     _p = Mathf.Pow(DIRECTION_PROBABILITY[0], lookaheadSteps);
//     // }

//     void InitPathPredictions () {
//         // Generating huge amount of garbage...
//         _pathPredictions.Clear();

//         if (lookaheadSteps < 1) {
//             return;
//         }

//         // For each possible movement:
//         //    If walk, check if virtual path would collide and skip if so

//         var timestep = lookaheadSeconds / lookaheadSteps;
//         var nodes = new List<int> ();
//         var ps = new List<float>();
//         var poss = new List<Vector2>();
//         var fwds = new List<Vector2>();
//         for (int i = 0; i < lookaheadSteps; i++) {
//             nodes.Add(0);
//             ps.Add(0.0f);
//             poss.Add(Vector2.zero);
//             fwds.Add(Vector2.zero);
//         }

//         var depth = 0;
//         while (true) {
//             // Have we tried all predictions for this combination of nodes?
//             if (nodes[depth] >= (int) PathPredictionNode.Count) {
//                 // Tried all combinations for all depths, we're done!
//                 if (depth == 0) {
//                     break;
//                 }

//                 // Othwerwise we need to back up and try the next node
//                 depth--;
//                 nodes[depth]++;
//                 continue;
//             }

//             // Apply current node
//             var v = depth > 0 ? poss[depth-1] : _v0;
//             var fwd = depth > 0 ? fwds[depth-1] : _v0fwd;
//             var p = depth > 0 ? ps[depth-1] : 1.0f;
//             switch((PathPredictionNode)nodes[depth]) {
//                 case PathPredictionNode.Forward :
//                     poss[depth] = v + timestep * walkSpeedMetersPerSecond * fwd;
//                     fwds[depth] = fwd;
//                     ps[depth] = p * NODE_PROBABILITY[nodes[depth]];
//                     break;
//                 case PathPredictionNode.Left :
//                     poss[depth] = v;
//                     fwds[depth] = RDWMethodUtil.RotateVector2(fwd,-timestep * turnSpeedDegreesPerSecond);
//                     ps[depth] = p * NODE_PROBABILITY[nodes[depth]];
//                     break;
//                 case PathPredictionNode.Right :
//                     poss[depth] = v;
//                     fwds[depth] = RDWMethodUtil.RotateVector2(fwd,timestep * turnSpeedDegreesPerSecond);
//                     ps[depth] = p * NODE_PROBABILITY[nodes[depth]];
//                     break;
//                 case PathPredictionNode.None :
//                     ps[depth] = p * NODE_PROBABILITY[nodes[depth]];
//                     break;
//             }

//             if ((PathPredictionNode)nodes[depth] == PathPredictionNode.Forward) {
//                 // Test if current path segment is possible

//                 var v3d = new Vector3(v.x,0.0f,v.y);
//                 var nextv3d = new Vector3(poss[depth].x,0.0f,poss[depth].y);
//                 var hit = new UnityEngine.AI.NavMeshHit();
//                 if (UnityEngine.AI.NavMesh.Raycast(v3d, nextv3d, out hit, UnityEngine.AI.NavMesh.AllAreas)) {
//                     // We're blocked - abandon this path and all subsequent
//                     nodes[depth]++;
//                     continue;
//                 }
//             }

//             // Is this the last step in the prediction window?
//             if (depth + 1 >= lookaheadSteps) {
//                 // This path is complete - store it
//                 var prediction = new PathPrediction();
//                 prediction.vPoss = new List<Vector2>(poss);
//                 prediction.vFwds = new List<Vector2>(fwds);
//                 prediction.probability = ps[depth];
//                 _pathPredictions.Add(prediction);

//                 // Back up and try next node
//                 depth--;
//                 nodes[depth]++;
//                 continue;
//             }

//             // We're not blocked but we haven't reached the end of the path
//             // Go deeper!
//             depth++;
//             nodes[depth] = 0;
//         }

//         // Now correct probabilities so they sum to 1
//         var pSum = 0.0f;
//         for (int i = 0; i < _pathPredictions.Count; i++) {
//             pSum += _pathPredictions[i].probability;
//         }

//         for (int i = 0; i < _pathPredictions.Count; i++) {
//             _pathPredictions[i].probability = _pathPredictions[i].probability / pSum;
//         }
//     }

//     // bool IteratePathPredictionsOld () {
//     //     var step = lookaheadSteps - 1;
//     //     while (true) {
//     //         _vps[step]++;
//     //         if (_vps[step] >= (byte)Direction.Count) {
//     //             _vps[step] = 0;
//     //             step--;
//     //             if (step < 0) {
//     //                 return false;
//     //             }
//     //         } else {
//     //             break;
//     //         }
//     //     }

//     //     _p = 1;
//     //     for (int i = 0; i < _vps.Length; i++) {
//     //         _p *= DIRECTION_PROBABILITY[_vps[i]];
//     //     }
//     //     return true;
//     // }

//     // enum Redirection
//     // {
//     //     NoneNone = 0,
//     //     LeftHalfNone,
//     //     LeftFullNone,
//     //     RightHalfNone,
//     //     RightFullNone,

//     //     NoneMagnifyHalf,
//     //     LeftHalfMagnifyHalf,
//     //     LeftFullMagnifyHalf,
//     //     RightHalfMagnifyHalf,
//     //     RightFullMagnifyHalf,

//     //     NoneMagnifyFull,
//     //     LeftHalfMagnifyFull,
//     //     LeftFullMagnifyFull,
//     //     RightHalfMagnifyFull,
//     //     RightFullMagnifyFull,

//     //     NoneReduceHalf,
//     //     LeftHalfReduceHalf,
//     //     LeftFullReduceHalf,
//     //     RightHalfReduceHalf,
//     //     RightFullReduceHalf,

//     //     NoneReduceFull,
//     //     LeftHalfReduceFull,
//     //     LeftFullReduceFull,
//     //     RightHalfReduceFull,
//     //     RightFullReduceFull,

//     //     Count
//     // }

//     // Simple mode
//     enum Redirection
//     {
//         NoneNone = 0,
//         // LeftHalfNone,
//         LeftFullNone,
//         // RightHalfNone,
//         RightFullNone,

//         // NoneMagnifyHalf,
//         // // LeftHalfMagnifyHalf,
//         // LeftFullMagnifyHalf,
//         // // RightHalfMagnifyHalf,
//         // RightFullMagnifyHalf,

//         NoneMagnifyFull,
//         // LeftHalfMagnifyFull,
//         LeftFullMagnifyFull,
//         // RightHalfMagnifyFull,
//         RightFullMagnifyFull,

//         // NoneReduceHalf,
//         // // LeftHalfReduceHalf,
//         // LeftFullReduceHalf,
//         // // RightHalfReduceHalf,
//         // RightFullReduceHalf,

//         NoneReduceFull,
//         // LeftHalfReduceFull,
//         LeftFullReduceFull,
//         // RightHalfReduceFull,
//         RightFullReduceFull,

//         Count
//     }

//     void InitRedirections () {
//         for (int i = 0; i < _qs.Length; i++) {
//             _qs[i] = 0;
//         }
//     }

//     bool IterateRedirections () {
//         var step = lookaheadSteps - 1;
//         while (true) {
//             _qs[step]++;
//             if (_qs[step] >= (byte)Redirection.Count) {
//                 _qs[step] = 0;
//                 step--;
//                 if (step < 0) {
//                     return false;
//                 }
//             } else {
//                 break;
//             }
//         }

//         return true;
//     }

//     float _lastRefreshTime;
//     float _time;
// }
