using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class RDWMonteCarloMethodSimulationTask {

    public Task task { get; private set; }

    int runTimeMilliseconds;
    List<RDWMonteCarloMethod.Redirect> redirects;
    RDWRedirector redirector;
    float predTrackDir;
    float predConfidence;
    ConnectedGraph connectedGraph;
    List<ConnectedGraph.Node> startNodes;
    List<float> startNodeProbabilities;
    ConnectedGraphTimeBacktrackNodeWeighter initialPathGenWeighter;
    Rect walkableTrackSpaceBounds;
    Vector2 trackPos;
    float trackDir;
    Vector2 worldPos;
    float worldDir;
    float maxLookaheadTimeSeconds;
    float walkSpeedMetersPerSecond;
    float turnSpeedDegreesPerSecond;

    public float totalPathSeconds;
    float maxTotalPathSeconds;

    ConnectedGraphWeightedPathGenerator _pathGen = new ConnectedGraphWeightedPathGenerator();
    ConnectedGraphTimeBacktrackNodeWeighter _pathGenWeighter = new ConnectedGraphTimeBacktrackNodeWeighter();
    Stopwatch _stopWatch = new Stopwatch();
    System.Random _random = new System.Random();

    public RDWMonteCarloMethodSimulationTask () { }

    public void Start () {
        totalPathSeconds = 0;
        task = Task.Factory.StartNew(Run);
    }

    public void Wait () {
        task.Wait();
    }

    // public void Reset () {
    //     this.runTimeMilliseconds = 0;
    //     this.redirects = null;
    //     this.redirector = null;
    //     this.predTrackDir = 0.0f;
    //     this.predConfidence = 0.0f;
    //     this.connectedGraph = null;
    //     this.startNodes = null;
    //     this.startNodeProbabilities = null;
    //     this.initialPathGenWeighter = null;
    //     this.walkableTrackSpaceBounds = new Rect();
    //     this.trackPos = Vector2.zero;
    //     this.trackDir = 0.0f;
    //     this.worldPos = Vector2.zero;
    //     this.worldDir = 0.0f;
    //     this.maxLookaheadTimeSeconds = 0.0f;
    //     this.walkSpeedMetersPerSecond = 0.0f;
    //     this.turnSpeedDegreesPerSecond = 0.0f;
    // }

    public void Set (int runTimeMilliseconds,
        List<RDWMonteCarloMethod.Redirect> redirects,
        RDWRedirector redirector,
        float predTrackDir, float predConfidence,
        ConnectedGraph connectedGraph,
        List<ConnectedGraph.Node> startNodes,
        List<float> startNodeProbabilities,
        ConnectedGraphTimeBacktrackNodeWeighter initialPathGenWeighter,
        Rect walkableTrackSpaceBounds, Vector2 trackPos, float trackDir,
        Vector2 worldPos, float worldDir, float maxLookaheadTimeSeconds,
        float walkSpeedMetersPerSecond, float turnSpeedDegreesPerSecond) {

        this.runTimeMilliseconds = runTimeMilliseconds;
        this.redirects = redirects;
        this.redirector = redirector;
        this.predTrackDir = predTrackDir;
        this.predConfidence = predConfidence;
        this.connectedGraph = connectedGraph;
        this.startNodes = startNodes;
        this.startNodeProbabilities = startNodeProbabilities;
        this.initialPathGenWeighter = initialPathGenWeighter;
        this.walkableTrackSpaceBounds = walkableTrackSpaceBounds;
        this.trackPos = trackPos;
        this.trackDir = trackDir;
        this.worldPos = worldPos;
        this.worldDir = worldDir;
        this.maxLookaheadTimeSeconds = maxLookaheadTimeSeconds;
        this.walkSpeedMetersPerSecond = walkSpeedMetersPerSecond;
        this.turnSpeedDegreesPerSecond = turnSpeedDegreesPerSecond;

        this.maxTotalPathSeconds = -1;
    }

    // For simulation purposes - semi-deterministic operation
    public void Set (float maxComputedPathSeconds,
        int seed,
        List<RDWMonteCarloMethod.Redirect> redirects,
        RDWRedirector redirector,
        float predTrackDir, float predConfidence,
        ConnectedGraph connectedGraph,
        List<ConnectedGraph.Node> startNodes,
        List<float> startNodeProbabilities,
        ConnectedGraphTimeBacktrackNodeWeighter initialPathGenWeighter,
        Rect walkableTrackSpaceBounds, Vector2 trackPos, float trackDir,
        Vector2 worldPos, float worldDir, float maxLookaheadTimeSeconds,
        float walkSpeedMetersPerSecond, float turnSpeedDegreesPerSecond) {

        this.runTimeMilliseconds = System.Int32.MaxValue;
        this.redirects = redirects;
        this.redirector = redirector;
        this.predTrackDir = predTrackDir;
        this.predConfidence = predConfidence;
        this.connectedGraph = connectedGraph;
        this.startNodes = startNodes;
        this.startNodeProbabilities = startNodeProbabilities;
        this.initialPathGenWeighter = initialPathGenWeighter;
        this.walkableTrackSpaceBounds = walkableTrackSpaceBounds;
        this.trackPos = trackPos;
        this.trackDir = trackDir;
        this.worldPos = worldPos;
        this.worldDir = worldDir;
        this.maxLookaheadTimeSeconds = maxLookaheadTimeSeconds;
        this.walkSpeedMetersPerSecond = walkSpeedMetersPerSecond;
        this.turnSpeedDegreesPerSecond = turnSpeedDegreesPerSecond;

        this._random = new System.Random(seed);
        this.maxTotalPathSeconds = maxComputedPathSeconds;
    }

    void Run () {
        _stopWatch.Restart();
        while (_stopWatch.ElapsedMilliseconds < runTimeMilliseconds) {
            RunSimulation ();

            if (maxTotalPathSeconds > 0 && totalPathSeconds > maxTotalPathSeconds) {
                break;
            }
        }
    }

    void RunSimulation () {
        _pathGenWeighter.Reset();
        _pathGenWeighter.probabilityDecayPerSecond = initialPathGenWeighter.probabilityDecayPerSecond;
        _pathGenWeighter.probabilityRecoverPerSecond = initialPathGenWeighter.probabilityRecoverPerSecond;
        _pathGenWeighter.probabilityMinimum = initialPathGenWeighter.probabilityMinimum;
        _pathGenWeighter.CopyWeights(initialPathGenWeighter);

        // Currently just pick a redirection randomly (uniformly)
        var redirect = redirects[_random.Next(redirects.Count)];

        var trackPos = this.trackPos;
        var trackDir = this.trackDir;
        var worldPos = this.worldPos;
        var worldDir = this.worldDir;
        var redirectInstr = redirect.option.instruction;
        var simTimeSeconds = 0.0f;
        var collision = false;
        var targetGraphNode = GetRandomNodeWeighted(_random,
            startNodeProbabilities,startNodes);
        var pathGenIsInit = false;
        var turnTime = 0.0f;
        var walkTime = 0.0f;

        // Are we already out of bounds?
        // if (!trackSpace.bounds.Contains(trackPos)) {
        //     collision = true;
        // }

        // Simulate initial turn-to-face
        {
            var targetWorldPos = UnityToRedirectionPlanePos(
                targetGraphNode.position);
            var targetWorldDir = RDWMethodUtil.VecToAngle(
                targetWorldPos - worldPos);
            WorldWalkDirInfo(worldDir, targetWorldDir,
                out float worldDirDelta,
                out RDWRedirector.RedirectInstruction.RotationDirection worldDirDeltaDirection);
            var trackDirDelta = CalculateTrackDirDelta(_random,redirector,
                redirectInstr.rotationDirection,worldDirDeltaDirection,
                worldDirDelta);

            trackDir += trackDirDelta;
            worldDir = targetWorldDir;
            simTimeSeconds += CalculateTurnDuration(worldDirDelta,
                turnSpeedDegreesPerSecond);
        }

        while (simTimeSeconds < maxLookaheadTimeSeconds && !collision) {

            // Walk towards target graph node, applying scale ONLY (currently)
            {
                var targetWorldPos = UnityToRedirectionPlanePos(
                    targetGraphNode.position);
                WorldWalkPosInfo(worldPos, targetWorldPos,
                    out _, out float worldPosDeltaDistance);
                var trackPosDeltaDistance = CalculateTrackPosDeltaDistance(
                    _random,redirector,redirectInstr.scaleDirection,
                    worldPosDeltaDistance);
                var trackPosInitDirVec = RDWMethodUtil.AngleToVec(trackDir);

            //     // Break arc into line segments with max 45 degree curvature
            //     // Work out max length of a line segment
            //     var clockwise = redirectInstr.rotationDirection == RDWRedirector.RedirectInstruction.RotationDirection.Right;
            //     var circleRadius = (1.0f/(_redirector.curvatureGainDegreesPerMeter/360.0f))/(2*Mathf.PI);
            //     var circleCircumference = 2 * Mathf.PI * circleRadius;
            //     var arcProportion = trackPosDeltaDistance / circleCircumference;
            //     // var maxLineSegmentLength = circleCircumference / 8; // 45 degree segments
            //     var trackPosToCircleCenterAngle = trackDir + (clockwise ? 90.0f : 270.0f);
            //     var circleCenter = trackPos + circleRadius * RDWMethodUtil.AngleToVec(trackPosToCircleCenterAngle);
            //     var arcDegrees = arcProportion * 360.0f;
            //     var maxDegreesPerSegment = 45.0f;
            //     // var circleZeroAngle = RDWMethodUtil.VecToAngle(trackPos - circleCenter) + 90.0f;
            //     var circleZeroAngle = trackPosToCircleCenterAngle - 180.0f;

            //     var workingTrackPos = trackPos;
            //     var workingTrackDir = trackDir;
            //     for (var degreesTurned = 0.0f; degreesTurned < arcDegrees; degreesTurned += maxDegreesPerSegment) {

            //         var degreesThisSegment = Mathf.Min(maxDegreesPerSegment,arcDegrees - degreesTurned);
            //         var totalDegrees = degreesTurned + degreesThisSegment;
            //         var nextTrackDir = trackDir + totalDegrees * (clockwise ? 1 : -1);
            //         var nextTrackPos = circleCenter + circleRadius * RDWMethodUtil.AngleToVec(circleZeroAngle + totalDegrees * (clockwise ? 1 : -1));

            //         // var angleSign = redirectInstr.rotationDirection == RDWRedirector.RedirectInstruction.RotationDirection.Left ? -1 : 1;
            //         // var nextTrackDir = workingTrackDir + distanceThisStep * _redirector.curvatureGainDegreesPerMeter * angleSign;
            //         // var nextTrackPos = workingTrackPos + distanceThisStep * RDWMethodUtil.AngleToVec(nextTrackDir);

            //         // Check we're still in the track space and < time limit
            //         if (!trackSpace.bounds.Contains(nextTrackPos)) {
            //             // Rough estimate of when we would hit the bounds
            //             // Use GetHit because we know current is in, next is out
            //             collision = CollideLineSegmentWithRect(workingTrackPos,
            //                 nextTrackPos,trackSpace.bounds,out Vector2 collisionPoint);
            //             if (collision) {
            //                 // Correct simulationTime
            //                 var collidedWalk = collisionPoint - workingTrackPos;
            //                 simulationTime += CalculateWalkDuration(
            //                     CalculateWorldPosDeltaDistance(
            //                         redirectInstr.scaleDirection,
            //                         collidedWalk.magnitude
            //                     ));
            //                 break;
            //             }
            //         }

            //         workingTrackDir = nextTrackDir;
            //         workingTrackPos = nextTrackPos;
            //     }

            //     if (collision) {
            //         break;
            //     }

            //     trackPos = workingTrackPos;
            //     trackDir = workingTrackDir;
            //     worldPos = targetWorldPos;
            //     walkTime = CalculateWalkDuration(worldPosDeltaDistance);
            //     simulationTime += walkTime;
            // }

            // var nextRedirect = frame.redirects[Random.Range(0,frame.redirects.Count)];
            // redirectInstr = nextRedirect.option.instruction;

                var targetTrackPos = trackPos +
                    (trackPosInitDirVec * trackPosDeltaDistance);

                // Check we're still in the track space and < time limit
                if (!walkableTrackSpaceBounds.Contains(targetTrackPos)) {
                    // Rough estimate of when we would hit the bounds
                    // Use GetHit because we know current is in, next is out
                    collision = CollideLineSegmentWithRect(trackPos,
                        targetTrackPos,walkableTrackSpaceBounds,
                        out Vector2 collisionPoint);
                    if (collision) {
                        // Correct simulationTime
                        var collidedWalk = collisionPoint - trackPos;
                        simTimeSeconds += CalculateWalkDuration(
                            CalculateWorldPosDeltaDistance(_random,
                                redirector,redirectInstr.scaleDirection,
                                collidedWalk.magnitude
                            ),
                            walkSpeedMetersPerSecond);
                        break;
                    }
                }

                trackPos = targetTrackPos;
                worldPos = targetWorldPos;
                walkTime = CalculateWalkDuration(worldPosDeltaDistance,
                    walkSpeedMetersPerSecond);
                simTimeSeconds += walkTime;
            }

            // var nextRedirect = frame.redirects[Random.Range(0,frame.redirects.Count)];
            // redirectInstr = nextRedirect.option.instruction;

            var nextRedirect = redirects[_random.Next(redirects.Count)];
            redirectInstr = nextRedirect.option.instruction;

            // Pick a new graph node from those connected to the current node
            // If path gen isn't ready, set it up
            if (!pathGenIsInit) {
                // Update weights for first walk
                _pathGenWeighter.Visit(targetGraphNode,simTimeSeconds);
                // Setup path gen
                _pathGen.Setup(_random,connectedGraph,_pathGenWeighter,targetGraphNode);
                pathGenIsInit = true;
            } else {
                _pathGen.Advance(turnTime + walkTime*0.5f,walkTime*0.5f);
            }

            _pathGen.Predict();
            targetGraphNode = _pathGen.nextNode;

            // Turn towards target graph node, applying rotation
            {
                var targetWorldPos = UnityToRedirectionPlanePos(
                    targetGraphNode.position);
                var targetWorldDir = RDWMethodUtil.VecToAngle(
                    targetWorldPos - worldPos);
                WorldWalkDirInfo(worldDir, targetWorldDir,
                    out float worldDirDelta,
                    out RDWRedirector.RedirectInstruction.RotationDirection worldDirDeltaDirection);
                var trackDirDelta = CalculateTrackDirDelta(_random,
                    redirector,redirectInstr.rotationDirection,
                    worldDirDeltaDirection,
                    worldDirDelta);

                trackDir += trackDirDelta;
                worldDir = targetWorldDir;
                turnTime = CalculateTurnDuration(worldDirDelta,
                    turnSpeedDegreesPerSecond);
                simTimeSeconds += turnTime;
            }



            // Select new redirect instruction
            // redirectInstr =
            //     redirectOptions[Random.Range(0,redirectOptions.Count)].instruction;
        }

        // Score the simulation - higher is better
        // Let's say the score is the proportion of available time used without
        // a collision. So if collided immediately, score is 0. If no collision,
        // score is 1.
        redirect.simulationCount++;

        var thisValue = Mathf.Clamp01(simTimeSeconds / maxLookaheadTimeSeconds);
        thisValue = EaseOutExpo(thisValue);// Mathf.Log(thisValue+1,2);
        // var thisValue = simulationTime > maxSimulationTime ? 1 : 0;
        redirect.value += thisValue;

        totalPathSeconds += simTimeSeconds;
    }

    static ConnectedGraph.Node GetRandomNodeWeighted (
        System.Random random,
        List<float> weights,
        List<ConnectedGraph.Node> nodes) {

        // rval is between 0 (inclusive) 1 (exclusive)
        var rval = random.NextDouble();
        for (int i = 0; i < nodes.Count; i++) {
            if (weights[i] > rval) {
                return nodes[i];
            } else {
                rval -= weights[i];
            }
        }

        // Handle case where rval is exactly 1 or empty nodes
        if (nodes.Count > 0) {
            return nodes[nodes.Count-1];
        } else {
            return null;
        }
    }

    static bool CollideLineSegmentWithLineSegment (Vector2 line00, Vector2 line01,
        Vector2 line10, Vector2 line11, out Vector2 hit) {

        // https://stackoverflow.com/a/1968345
        Vector2 s0 = line01 - line00;
        Vector2 s1 = line11 - line10;

        float denom = -s1.x * s0.y + s0.x * s1.y;
        float s = (-s0.y * (line00.x - line10.x) + s0.x * (line00.y - line10.y)) / denom;
        float t = ( s1.x * (line00.y - line10.y) - s1.y * (line00.x - line10.x)) / denom;

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {

            hit = line00 + (t * s0);
            return true;
        }

        hit = Vector2.zero;
        return false;
    }

    // Check if line segment collides with rect and return collision point
    static bool CollideLineSegmentWithRect (Vector2 line0, Vector2 line1,
        Rect rect, out Vector2 hit) {

        var r0 = rect.min;
        var r1 = new Vector2(rect.xMax,rect.yMin);
        var r2 = rect.max;
        var r3 = new Vector2(rect.xMin,rect.yMax);

        return
            CollideLineSegmentWithLineSegment(line0,line1,r0,r3,out hit) ||
            CollideLineSegmentWithLineSegment(line0,line1,r3,r2,out hit) ||
            CollideLineSegmentWithLineSegment(line0,line1,r2,r1,out hit) ||
            CollideLineSegmentWithLineSegment(line0,line1,r1,r0,out hit);
    }

    static float EaseOutExpo(float x) {
        return x == 1 ? 1 : 1 - Mathf.Pow (2, -10 * x);
    }

    static float CalculateWalkDuration (float worldWalkDistance,
        float walkSpeedMetersPerSecond) {
        return worldWalkDistance / walkSpeedMetersPerSecond;
    }

    static float CalculateTurnDuration (float worldDirDelta,
        float turnSpeedDegreesPerSecond) {
        return Mathf.Abs(worldDirDelta) / turnSpeedDegreesPerSecond;
    }

    static void WorldWalkPosInfo (Vector2 startWorldPos, Vector2 endWorldPos,
        out Vector2 worldPosDelta, out float worldPosDeltaDistance) {

        worldPosDelta = endWorldPos - startWorldPos;
        worldPosDeltaDistance = worldPosDelta.magnitude;
    }

    static void WorldWalkDirInfo (float startWorldDir, float endWorldDir,
        out float worldDirDelta,
        out RDWRedirector.RedirectInstruction.RotationDirection worldDirDeltaDirection) {

        worldDirDelta = Mathf.DeltaAngle(startWorldDir,endWorldDir);
        worldDirDeltaDirection = worldDirDelta < 0
            ? RDWRedirector.RedirectInstruction.RotationDirection.Left
            : RDWRedirector.RedirectInstruction.RotationDirection.Right;
    }

    static float CalculateWorldPosDeltaDistance (
        System.Random random,
        RDWRedirector redirector,
        RDWRedirector.RedirectInstruction.ScaleDirection redirectScaleDirection,
        float trackWalkDistance) {

        return trackWalkDistance *
            GetWalkDistanceScaleFactor(random,redirector,redirectScaleDirection);
    }

    // For walking model - concept is that walk/look direction is the same
    static float CalculateTrackPosDeltaDistance (
        System.Random random,
        RDWRedirector redirector,
        RDWRedirector.RedirectInstruction.ScaleDirection redirectScaleDirection,
        float worldWalkDistance) {
        // Redirection while walking (translation gain)
        // scale_red_dir := magnify | reduce
        // virt_walk = track_walk * scale_factor
        // scale_factor = magnify_scale_mul :: scale_red_dir == magnify
        // scale_factor = reduce_scale_mul  :: otherwise
        return worldWalkDistance /
            GetWalkDistanceScaleFactor(random,redirector, redirectScaleDirection);
    }

    static float CalculateTrackDirDelta (
        System.Random random,
        RDWRedirector redirector,
        RDWRedirector.RedirectInstruction.RotationDirection redirectRotationDirection,
        RDWRedirector.RedirectInstruction.RotationDirection worldTurnDirection,
        float worldDirDelta) {
        // Redirection while turning (rotation gain)
        // turn_red_dir := left | right
        // virt_turn = track_turn * turn_factor
        // turn_factor = co_turn_mul   :: dir(track_turn) == turn_red_dir
        // turn_factor = anti_turn_mul :: otherwise
        return worldDirDelta /
            GetTurnScaleFactor(random,redirector,redirectRotationDirection,
            worldTurnDirection,worldDirDelta);
    }

    static float GetWalkDistanceScaleFactor (
        System.Random random,
        RDWRedirector redirector,
        RDWRedirector.RedirectInstruction.ScaleDirection redirectScaleDirection) {

        // switch(redirectScaleDirection) {
        //     case RDWRedirector.RedirectInstruction.ScaleDirection.Magnify :
        //         return _redirector.transGainMagnifyMultiplier;
        //     case RDWRedirector.RedirectInstruction.ScaleDirection.Reduce :
        //         return _redirector.transGainReduceMultiplier;
        // }
        // return 1.0f;
        switch(redirectScaleDirection) {
            case RDWRedirector.RedirectInstruction.ScaleDirection.Magnify :
                return RandomRange(random,1.0f,redirector.transGainMagnifyMultiplier);
            case RDWRedirector.RedirectInstruction.ScaleDirection.Reduce :
                return RandomRange(random,redirector.transGainReduceMultiplier, 1.0f);
        }
        return 1.0f;
    }

    static float GetTurnScaleFactor (
        System.Random random,
        RDWRedirector redirector,
        RDWRedirector.RedirectInstruction.RotationDirection redirectRotationDirection,
        RDWRedirector.RedirectInstruction.RotationDirection worldTurnDirection,
        float worldDirDelta) {

        // if (redirectRotationDirection == RDWRedirector.RedirectInstruction.RotationDirection.None) {
        //     return 1.0f;
        // } else {
        //     return redirectRotationDirection == worldTurnDirection
        //         ? _redirector.angularCoRotMultiplier
        //         : _redirector.angularAntiRotMultiplier;
        // }
        if (redirectRotationDirection == RDWRedirector.RedirectInstruction.RotationDirection.None) {
            return 1.0f;
        } else {
            return redirectRotationDirection == worldTurnDirection
                ? RandomRange(random,1.0f,redirector.angularCoRotMultiplier)
                : RandomRange(random,redirector.angularAntiRotMultiplier,1.0f);
        }
    }

    static float RandomRange (System.Random random, float min, float max) {
        return min + ((float)random.NextDouble() * (max - min));
    }

    Vector2 UnityToRedirectionPlanePos (Vector3 worldPos) {
        return new Vector2(worldPos.x,worldPos.z);
    }
}