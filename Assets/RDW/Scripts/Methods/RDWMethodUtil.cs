using UnityEngine;
using System.Collections;

public static class RDWMethodUtil {

    public static float VecToAngle (Vector2 v) {
        // Matching unity's weird co-ordinate system
        return Vector2.Angle(v,Vector2.up) * (v.x < 0 ? -1 : 1);
    }

    public static Vector2 AngleToVec (float angle) {
        return new Vector2(
            // Matching unity's weird co-ordinate system
            x: Mathf.Sin(angle*Mathf.Deg2Rad),
            y: Mathf.Cos(angle*Mathf.Deg2Rad)
        );
    }

    // Weird one this - match Unity's co-ordinate system
    // Conventional system: defined by unit circle (cos(theta),sin(theta))
    // i.e., (1,0) is at 0 degrees, (0,1) is at 90
    // Unity system: (sin(theta),cos(theta))
    public static float ConventionalAngleToAngle (float theta) {
        return VecToAngle(ConventionalAngleToVec(theta));
    }

    public static Vector2 ConventionalAngleToVec (float theta) {
        return new Vector2(
            x: Mathf.Cos(theta),
            y: Mathf.Sin(theta)
        );
    }

    // Calculate signed track space turn that would generate the (redirected)
    // virtual turn. 'Estimated' track distance is 1:1 with virtual distance
    // unless also using translation scaling
    public static float InverseSteerTurn (float estimatedTrackDistance, float deltaTime,
        float virtTurn, int targetTurnDirection, float baselineRotPerSecDegrees,
        float linearTargetRadiusMeters, float angularCoRotMultiplier,
        float angularAntiRotMultiplier) {

        var baseline =
            Baseline(targetTurnDirection,baselineRotPerSecDegrees,deltaTime);
        var linear =
            Linear(targetTurnDirection,estimatedTrackDistance,
            linearTargetRadiusMeters);

        // We need to know track turn direction to calculate angular scale factor.
        // Possibilities:
        // 1) Virt turn is in same direction as track turn but less/more
        // 2) Virt turn is in opposite direction from track turn

        // 2 unusual, happens when track turn is small but user moves quickly

        var angularScaleFactorWith =
            Angular(targetTurnDirection,Mathf.Sign(virtTurn),angularCoRotMultiplier,
            angularAntiRotMultiplier);
        var angularScaleFactorAgainst =
            Angular(targetTurnDirection,-Mathf.Sign(virtTurn),angularCoRotMultiplier,
            angularAntiRotMultiplier);

        var withTrackTurn =  ((virtTurn - (baseline + linear)) /
            (1+angularScaleFactorWith));
        var againstTrackTurn = ((virtTurn - (baseline + linear)) /
            (1+angularScaleFactorAgainst));

        var withVirtTurn = SteerTurn(estimatedTrackDistance,deltaTime,withTrackTurn,
            targetTurnDirection,baselineRotPerSecDegrees,linearTargetRadiusMeters,
            angularCoRotMultiplier,angularAntiRotMultiplier);

        return Mathf.Approximately(withVirtTurn,virtTurn) ? withTrackTurn : againstTrackTurn;

        // var againstVirtTurn = SteerTurn(estimatedTrackDistance,deltaTime,againstTrackTurn,
        //     targetTurnDirection,baselineRotPerSecDegrees,linearTargetRadiusMeters,
        //     angularCoRotMultiplier,angularAntiRotMultiplier);
    }

    static float Baseline (int targetTurnDirection, float rotPerSecDegrees,
            float deltaTime) {
        return targetTurnDirection * rotPerSecDegrees * deltaTime;
    }

    static float Linear (int targetTurnDirection, float trackDistance,
            float targetRadiusMeters) {
        return targetTurnDirection *
            (360 * trackDistance) / (2 * Mathf.PI * targetRadiusMeters);
    }

    static float Angular (int targetTurnDirection, float trackTurn,
            float coRoutMultiplier, float antiRotMultiplier) {
        var scale = Mathf.Sign(targetTurnDirection) == Mathf.Sign(trackTurn)
            ? coRoutMultiplier
            : antiRotMultiplier;
        return trackTurn * (scale - 1);
    }

    // Get redirected virtual turn angle from track movements
    // Returned angle includes direction (sign) and original track turn
    // i.e., This is not a delta, it is the complete angle that should be applied
    public static float SteerTurn (float trackDistance, float deltaTime,
        float trackTurn, int targetTurnDirection, float baselineRotPerSecDegrees,
        float linearTargetRadiusMeters, float angularCoRotMultiplier,
        float angularAntiRotMultiplier) {

        return trackTurn + SteerTurnDelta(trackDistance,deltaTime,trackTurn,
            targetTurnDirection,baselineRotPerSecDegrees,
            linearTargetRadiusMeters,angularCoRotMultiplier,
            angularAntiRotMultiplier);
    }

    // Get redirection in degrees from track movements
    // Signed delta only - not the complete virtual turn
    public static float SteerTurnDelta (float trackDistance, float deltaTime,
        float trackTurn, int targetTurnDirection, float baselineRotPerSecDegrees,
        float linearTargetRadiusMeters, float angularCoRotMultiplier,
        float angularAntiRotMultiplier) {

        var baseline =
            Baseline(targetTurnDirection,baselineRotPerSecDegrees,deltaTime);
        var linear =
            Linear(targetTurnDirection,trackDistance,linearTargetRadiusMeters);
        var angular =
            Angular(targetTurnDirection,trackTurn,angularCoRotMultiplier,
            angularAntiRotMultiplier);

        return baseline + linear + angular;
    }

    // What happens if we stop using the caps?
    // Returns the signed maximum possible rotation in degrees for this pos, dir and time window
    public static float SteeringDelta (float trackDistance, float deltaTime,
        float trackTurn, int targetTurnDirection, float baselineRotPerSecDegrees,
        float linearTargetRadiusMeters, float angularCoRotMultiplier,
        float angularAntiRotMultiplier) {
        // BASELINE
        var baseline = targetTurnDirection * baselineRotPerSecDegrees * deltaTime;

        // LINEAR (CURVATURE GAIN)
        // var velocity = trackDistance / deltaTime;
        // var linear = (360 * velocity) / (2 * Mathf.PI * linearTargetRadiusMeters);
        var linear = targetTurnDirection * (360 * trackDistance) / (2 * Mathf.PI * linearTargetRadiusMeters);

        // ANGULAR
        // Also includes direction
        // var angularVelocity = trackTurn / deltaTime;
        // var angularScale = Mathf.Sign(targetTurnDirection) == Mathf.Sign(angularVelocity)
        //     ? angularCoRotMultiplier
        //     : angularAntiRotMultiplier;
        // var angular = Mathf.Abs(angularVelocity * (1 - angularScale));
        var angularScale = Mathf.Sign(targetTurnDirection) == Mathf.Sign(trackTurn)
            ? angularCoRotMultiplier
            : angularAntiRotMultiplier;
        var angular = trackTurn * (angularScale - 1);

        return baseline + linear + angular;
    }

    public static float SteeringDelta (Vector2 deltaTrackPos, float deltaTime,
        float deltaTrackDir, float targetTurnDirection,
        float baselineRotPerSecDegrees, float linearTargetRadiusMeters,
        float angularCoRotMultiplier, float angularAntiRotMultiplier,
        float angularMaxRotPerSecDegrees, float linearMaxRotPerSecDegrees) {

        // BASELINE
        var baseline = baselineRotPerSecDegrees;

        // LINEAR (CURVATURE GAIN)
        var velocity = deltaTrackPos.magnitude / deltaTime;
        var linear = Mathf.Min(linearMaxRotPerSecDegrees, (360 * velocity) / (2 * Mathf.PI * linearTargetRadiusMeters));

        // ANGULAR
        var angularVelocity = deltaTrackDir / deltaTime;
        var angularScale = Mathf.Sign(targetTurnDirection) == Mathf.Sign(angularVelocity) ? angularCoRotMultiplier : angularAntiRotMultiplier;
        // var angular = Mathf.Min(angularMaxRotPerSecDegrees, Mathf.Abs(angularVelocity) * angularScale);
        var angular = Mathf.Min(angularMaxRotPerSecDegrees, Mathf.Abs(angularVelocity * (1 - angularScale)));

        // Reduce full rotation to undetectable levels as given by baseline, linear and angular
        return Mathf.Max(Mathf.Max(baseline, linear), angular) * deltaTime;
    }

    public static float SteeringDeltaToTarget (float prevDelta, Vector2 targetTrackPos,
        Vector2 trackPos, float trackDir, Vector2 prevTrackPos, float prevTrackDir,
        float baselineRotPerSecDegrees, float linearMaxRotPerSecDegrees,
        float linearTargetRadiusMeters, float angularCoRotMultiplier,
        float angularAntiRotMultiplier, float angularMaxRotPerSecDegrees,
        float dampeningAngleDegrees, float dampeningDistanceMeters,
        float smoothingConstant, float deltaTime) {

        // Walk direction estimation
        // Should be smarter than this
        // Use previous track pos, weighted by time and distance, with hard reset potential?
        var walkDir = trackDir;

        // Calculate full rotation to target
        var toTarget = targetTrackPos - trackPos;
        var toTargetDelta = Vector2.Angle(targetTrackPos - trackPos, Vector2.up);
        toTargetDelta *= toTarget.x < 0 ? -1 : 1;
        var fullDelta = Mathf.DeltaAngle(walkDir, toTargetDelta);

        // Reduce full rotation to undetectable levels as given by baseline, linear and angular
        var delta = SteeringDelta(trackPos - prevTrackPos, deltaTime, Mathf.DeltaAngle(trackDir, prevTrackDir), fullDelta,
                        baselineRotPerSecDegrees, linearTargetRadiusMeters, angularCoRotMultiplier,
                        angularAntiRotMultiplier, angularMaxRotPerSecDegrees, linearMaxRotPerSecDegrees);

        // Dampen by angle and distance to target
        if (Mathf.Abs(fullDelta) < dampeningAngleDegrees) {
            delta *= Mathf.Sin(Mathf.Deg2Rad * Mathf.Abs(fullDelta) * (90.0f / dampeningAngleDegrees));
        }
        if (Vector2.Distance(targetTrackPos, trackPos) < dampeningDistanceMeters) {
            delta *= Vector2.Distance(targetTrackPos, trackPos) / dampeningDistanceMeters;
        }
        delta *= -Mathf.Sign(fullDelta);

        // Smoothing, but frame (not time) dependent, so maybe useless
        delta = ((1 - smoothingConstant) * prevDelta) + (smoothingConstant * delta);

        // Cap rotation to ideal. Necessary...?
        if (Mathf.Abs(delta) > Mathf.Abs(fullDelta)) {
            delta = fullDelta;
        }

        return delta;
    }

    public static void ApplySteeringDelta (Transform transform, Vector2 trackPos, float delta) {
        transform.RotateAround(transform.TransformPoint(trackPos.x, 0, trackPos.y), Vector3.up, delta);
    }

    public static void GetTangentPoints (Vector2 relativePoint, float radius, out Vector2 cw, out Vector2 ccw) {
        // Point always on the outside of the circle, invert if on inside
        var distance = relativePoint.magnitude;
        if (distance < radius) {
            distance = 2 * radius - distance;
        }

        var deltaAngle = Mathf.Acos(radius / distance);
        var ptAngle = Mathf.Atan2(relativePoint.y, relativePoint.x);
        cw = radius * new Vector2(Mathf.Cos(ptAngle + deltaAngle), Mathf.Sin(ptAngle + deltaAngle));
        ccw = radius * new Vector2(Mathf.Cos(ptAngle - deltaAngle), Mathf.Sin(ptAngle - deltaAngle));
    }

    public static Vector2 RotateVector2 (Vector2 v, float degrees) {
        // Clockwise!
        var rad = Mathf.Deg2Rad * -degrees;
        return new Vector2(
            v.x * Mathf.Cos(rad) - v.y * Mathf.Sin(rad),
            v.x * Mathf.Sin(rad) + v.y * Mathf.Cos(rad)
        );
    }

    public static string ToStringEx (this Vector2 v) {
        return "(" + v.x + ", " + v.y + ")";
    }
    public static void Log (Vector2 v) {
        Debug.Log(v.ToStringEx());
    }

    public static string ToStringEx (this Vector3 v) {
        return "(" + v.x + ", " + v.y + ", " + v.z + ")";
    }
    public static void Log (Vector3 v) {
        Debug.Log(v.ToStringEx());
    }

    public static string ToStringEx (this Quaternion q) {
        return "(" + q.x + ", " + q.y + ", " + q.z + "," + q.w + ")";
    }
    public static void Log (Quaternion q) {
        Debug.Log(q.ToStringEx());
    }
}
