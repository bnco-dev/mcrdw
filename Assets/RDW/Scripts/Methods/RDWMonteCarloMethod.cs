using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;

// #if UNITY_EDITOR
// using
// #endif

[RequireComponent(typeof(RDWRedirector))]
public class RDWMonteCarloMethod : RDWMethod {

    public enum SimulationLimitMode {
        PathLength, // Deterministic
        CalculationTime // Non-deterministic
    }

    [System.Serializable]
    public class RedirectInstructionOption {
        public RDWRedirector.RedirectInstruction instruction;
        public float value;
    }

    [System.Serializable]
    public class Redirect {
        public RedirectInstructionOption option;
        public int simulationCount;
        public float value;
    }

    [System.Serializable]
    public class SimulationFrame {
        public List<Redirect> redirects = new List<Redirect>();
        public float timeSeconds = -1.0f;
    }

    public ConnectedGraph connectedGraph;
    public List<RedirectInstructionOption> redirectOptions;

    public ARDWPathPredictor pathPredictor;
    public ARDWVisibilityTester visibilityTester;

    public float trackSpaceWalkableOffsetMeters = 0.5f;

    // This is low, average is more like 1.4m/s
    public float walkSpeedMetersPerSecond = 1.0f;
    // This is also pretty slow
    public float turnSpeedDegreesPerSecond = 90.0f;

    public float maxSimulationTime = 30.0f;
    public int simulationThreadCount = 6;

    public SimulationLimitMode mode = SimulationLimitMode.CalculationTime;
    public float calcTimePerStep = 0.00555555555555555556f;
    public int pathLengthModeSeed = 0;
    public float pathLengthModePathLengthPerStep = 2000.0f;

    public float backtrackProbabilityDecayPerSecond = 0.2f;
    public float backtrackProbabilityRecoverPerSecond = 0.3f;
    public float backtrackProbabilityMinimum = 0.1f;

    public float frameWindowSeconds = 2.0f;
    // public float velocityEstimationWindowSeconds = 0.5f;

    public float minAngleDifferenceForWalkableNodesDegrees = 10.0f;

    List<SimulationFrame> _simulationFrames = new List<SimulationFrame>();
    List<SimulationFrame> _simulationFramePool = new List<SimulationFrame>();
    // Window<Vector2> _velocityWindow = new Window<Vector2>();

    RDWRedirector _redirector;

    bool _firstFrame;
    float _time;

    Vector2 _trackPos;
    float _trackDir;
    Vector2 _prevTrackPos;
    float _prevTrackDir;

    Vector2 _worldPos;
    float _worldDir;
    Vector2 _prevWorldPos;
    float _prevWorldDir;

    int _walkableAreaMask;

    Stopwatch _stopwatch = new Stopwatch();
    ConnectedGraphTimeBacktrackNodeWeighter _weighter = new ConnectedGraphTimeBacktrackNodeWeighter();

    internal List<Redirect> _currentRedirects = new List<Redirect>();
    internal RDWRedirector.RedirectInstruction _currentBestRedirectInstruction;

    ConnectedGraph.Node _closestNodeDebug = null;

    List<RDWMonteCarloMethodSimulationTask> _simulationTasks;
    // Task[] _simulationTasksArr;
    List<List<Redirect>> _simulationRedirects;

    System.Random _random = new System.Random();

    public enum DebugDrawMode {
        None,
        Backtrack,
        Prediction
    }
    public DebugDrawMode debugDrawMode = DebugDrawMode.Backtrack;
    public Camera debugDrawCamera;

    public StreamWriter _outfile;

    // This is a very ugly hack to fix render order with TrialVisualiser
    public void DrawDebugNow () {
        OnDrawGizmos();
    }

    void OnDrawGizmos () {
        if (!Application.isPlaying || Camera.current != debugDrawCamera) {
            return;
        }

        switch(debugDrawMode) {
            case DebugDrawMode.Backtrack : DebugDrawBacktrack(); break;
            case DebugDrawMode.Prediction : DebugDrawPrediction(); break;
            case DebugDrawMode.None : break;
        }
    }

    void DebugDrawBacktrack () {
        // Draw most likely node with color from prediction confidence
        if (_closestNodeDebug != null) {
            var weight = _weighter.GetWeight(_closestNodeDebug);
            DebugDrawNode (_closestNodeDebug.position,
                Color.Lerp(Color.red,Color.green,weight),special:true);
        }

        for (int i = 0; i < connectedGraph.nodes.Count; i++) {
            var weight = _weighter.GetWeight(connectedGraph.nodes[i]);
            DebugDrawNode (connectedGraph.nodes[i].position,
                Color.Lerp(Color.red,Color.green,weight),special:false);
        }
    }

    List<ConnectedGraph.Node> _visibleNodesDebug = new List<ConnectedGraph.Node>();
    List<float> _probabilitiesDebug = new List<float>();
    void DebugDrawPrediction () {
        _visibleNodesDebug.Clear();
        GetImmediatelyWalkableNodes(_visibleNodesDebug,
            minAngleDifferenceForWalkableNodesDegrees,
            out ConnectedGraph.Node closestNode);
        // GetStraightLineWalkableNodes(_visibleNodesDebug);

        pathPredictor.Predict(out float predTrackDirection, out float predConfidence);
        _probabilitiesDebug.Clear();
        GetNodeProbabilitiesPredictionWeighted(_probabilitiesDebug,_visibleNodesDebug,
            predTrackDirection,predConfidence);

        var mostLikelyNode = null as ConnectedGraph.Node;
        var maxProbability = -1.0f;
        for (int i = 0; i < _visibleNodesDebug.Count; i++) {
            if (_probabilitiesDebug[i] > maxProbability) {
                mostLikelyNode = _visibleNodesDebug[i];
                maxProbability = _probabilitiesDebug[i];
            }
        }

        for (int i = 0; i < _visibleNodesDebug.Count; i++) {
            var col = Color.Lerp(Color.red,Color.green,_probabilitiesDebug[i]);
            DebugDrawNode(_visibleNodesDebug[i].position,col);
        }
    }

    void DebugDrawNode (Vector3 position, Color color, bool special = false) {
        var halfGizmoSize = GetGizmoSize(position)*0.5f;
        var left = new Vector3(position.x-halfGizmoSize,position.y,position.z);
        var right = new Vector3(position.x+halfGizmoSize,position.y,position.z);
        var down = new Vector3(position.x,position.y-halfGizmoSize,position.z);
        var up = new Vector3(position.x,position.y+halfGizmoSize,position.z);
        var back = new Vector3(position.x,position.y,position.z-halfGizmoSize);
        var fwd = new Vector3(position.x,position.y,position.z+halfGizmoSize);

        var prevColor = Gizmos.color;

        Gizmos.color = color;
        Gizmos.DrawLine(left,right);
        Gizmos.DrawLine(down,up);
        Gizmos.DrawLine(back,fwd);

        if (special) {
            Gizmos.DrawLine(left,back);
            Gizmos.DrawLine(back,right);
            Gizmos.DrawLine(right,fwd);
            Gizmos.DrawLine(fwd,left);
        }

        Gizmos.color = prevColor;
    }

    // https://forum.unity.com/threads/constant-screen-size-gizmos.64027/
    public static float GetGizmoSize(Vector3 position) {
        Camera current = Camera.current;
        position = Gizmos.matrix.MultiplyPoint(position);

        if (current)
        {
            Transform transform = current.transform;
            Vector3 position2 = transform.position;
            float z = Vector3.Dot(position - position2, transform.TransformDirection(new Vector3(0f, 0f, 1f)));
            Vector3 a = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(0f, 0f, z)));
            Vector3 b = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(1f, 0f, z)));
            float magnitude = (a - b).magnitude;
            return 80f / Mathf.Max(magnitude, 0.0001f);
        }

        return 20f;
    }

    override protected void Awake () {
        base.Awake();
        _redirector = GetComponent<RDWRedirector>();

        _simulationTasks = new List<RDWMonteCarloMethodSimulationTask>();
        // _simulationTasksArr = new Task[simulationThreadCount];
        _simulationRedirects = new List<List<Redirect>>();
        for (int simI = 0; simI < simulationThreadCount; simI++) {
            _simulationTasks.Add(new RDWMonteCarloMethodSimulationTask());
            // _simulationTasksArr[simI] = _simulationTasks[simI].task;

            var redirects = new List<Redirect>();
            for (int redOptI = 0; redOptI < redirectOptions.Count; redOptI++) {
                redirects.Add(new Redirect {
                    option = redirectOptions[redOptI],
                    simulationCount = 0,
                    value = 0.0f
                });
            }
            _simulationRedirects.Add(redirects);
        }
    }

    public override void _OnAttach () {
        _firstFrame = true;
    }

    public override void Discontinuity() {
        _firstFrame = true;
    }

    #region SimulationFramePool
    void ResetSimulationFrame (SimulationFrame simulationFrame) {
        for (int i = 0; i < simulationFrame.redirects.Count; i++) {
            simulationFrame.redirects[i].value = 0.0f;
            simulationFrame.redirects[i].simulationCount = 0;
        }
    }

    SimulationFrame ConstructSimulationFrame (float timeSeconds) {
        var simFrame = new SimulationFrame();
        simFrame.timeSeconds = timeSeconds;
        simFrame.redirects = new List<Redirect>();
        for (int i = 0; i < redirectOptions.Count; i++) {
            simFrame.redirects.Add(new Redirect {
                option = redirectOptions[i],
                simulationCount = 0,
                value = 0.0f
            });
        }
        return simFrame;
    }

    SimulationFrame AcquireSimulationFrame (float timeSeconds) {
        if (_simulationFramePool.Count == 0) {
            return ConstructSimulationFrame(timeSeconds);
        }
        var simFrame = _simulationFramePool[_simulationFramePool.Count-1];
        ResetSimulationFrame (simFrame);
        simFrame.timeSeconds = timeSeconds;
        _simulationFramePool.RemoveAt(_simulationFramePool.Count-1);
        return simFrame;
    }

    void ReturnSimulationFrameToPool (SimulationFrame toRelease) {
        _simulationFramePool.Add(toRelease);
    }
    #endregion

    public override IEnumerator Step (Vector2 trackPos, float trackDir, float deltaTime) {
        _trackPos = trackPos;
        _trackDir = trackDir;
        _worldPos = UnityToRedirectionPlanePos(trackSpace.TrackToWorldPosition(trackPos));
        var worldDirVec = trackSpace.TrackToWorldDirection(trackDir);
        var worldDirVec2 = new Vector2(worldDirVec.x,worldDirVec.z);
        _worldDir = RDWMethodUtil.VecToAngle(worldDirVec2);

        if (_firstFrame) {
            _prevTrackPos = _trackPos;
            _prevTrackDir = _trackDir;
            _prevWorldPos = _worldPos;
            _prevWorldDir = _worldDir;
            _time = 0.0f;
            pathPredictor.Clear();
            _redirector.Clear();
            _currentBestRedirectInstruction = new RDWRedirector.RedirectInstruction();
            for (int i = 0; i < _currentRedirects.Count; i++) {
                _currentRedirects[i].simulationCount = 0;
                _currentRedirects[i].value = 0.0f;
            }
            for (int i = 0; i < _simulationFrames.Count; i++) {
                ReturnSimulationFrameToPool(_simulationFrames[i]);
            }
            _simulationFrames.Clear();
            // _velocityWindow.Clear(velocityEstimationWindowSeconds,0.0f);
            _firstFrame = false;
        }

        _time += deltaTime;

        // _velocityWindow.AdvanceDelta((_trackPos - _prevTrackPos)/deltaTime,deltaTime);
        pathPredictor.SubmitSample(trackPos,trackDir,deltaTime);
        pathPredictor.Predict(out float predTrackDirection, out float predConfidence);

        RunSimulations(predTrackDirection, predConfidence, deltaTime);

        _redirector.Apply(_currentBestRedirectInstruction,_trackPos,_prevTrackPos,
            _trackDir,_prevTrackDir,_worldPos,_prevWorldPos,_time);

        _prevTrackPos = _trackPos;
        _prevTrackDir = _trackDir;
        // Should update world pos/dir? (can change through redirection)
        _prevWorldPos = _worldPos;
        _prevWorldDir = _worldDir;

        yield break;
    }

    List<ConnectedGraph.Node> _startNodes = new List<ConnectedGraph.Node>();
    List<float> _startNodeProbabilities = new List<float>();
    void RunSimulations (float predTrackDir, float predConfidence, float deltaTime) {
        // Find possible starting nodes
        _startNodes.Clear();
        GetImmediatelyWalkableNodes(_startNodes,minAngleDifferenceForWalkableNodesDegrees,
            out ConnectedGraph.Node closestNode);
        // GetStraightLineWalkableNodes(_startNodes);

        if (_startNodes.Count == 0) {
            return;
        }

        // Update node weights for backtrack
        // _closestNodeDebug = GetClosestNode(_startNodes);
        _closestNodeDebug = closestNode;
        _weighter.probabilityDecayPerSecond = backtrackProbabilityDecayPerSecond;
        _weighter.probabilityRecoverPerSecond = backtrackProbabilityRecoverPerSecond;
        _weighter.probabilityMinimum = backtrackProbabilityMinimum;
        _weighter.Visit(closestNode,deltaTime);

        // Get starting node probabilities: use path predictor, ignore backtrack
        _startNodeProbabilities.Clear();
        GetNodeProbabilitiesPredictionWeighted(_startNodeProbabilities,
            _startNodes,predTrackDir,predConfidence);

        //

        // // Run simulations until time elapsed
        // var frame = AcquireSimulationFrame(_time);
        // var calcTimeMilliseconds = calcTimePerStep * 1000;
        // _stopwatch.Restart();
        // while (_stopwatch.ElapsedMilliseconds < calcTimeMilliseconds) {
        //     var node = GetRandomNodeWeighted(_startNodeProbabilities,
        //         _startNodes);
        //     RunSimulation (predTrackDir,predConfidence,node,frame);
        // }
        // _stopwatch.Stop();

        //

        var frame = AcquireSimulationFrame(_time);

        // Set tasks running
        for (int taskI = 0; taskI < _simulationTasks.Count; taskI++) {
            var redirects = _simulationRedirects[taskI];
            for (int redI = 0; redI < redirects.Count; redI++) {
                redirects[redI].simulationCount = 0;
                redirects[redI].value = 0;
            }
            if (mode == SimulationLimitMode.PathLength) {
                if (_random == null) {
                    _random = new System.Random(pathLengthModeSeed);
                }

                _simulationTasks[taskI].Set(
                    maxComputedPathSeconds: pathLengthModePathLengthPerStep,
                    seed: _random.Next(),
                    redirects: _simulationRedirects[taskI],
                    redirector: _redirector,
                    predTrackDir: predTrackDir,
                    predConfidence: predConfidence,
                    connectedGraph: connectedGraph,
                    startNodes: _startNodes,
                    startNodeProbabilities: _startNodeProbabilities,
                    initialPathGenWeighter: _weighter,
                    walkableTrackSpaceBounds: GetWalkableTrackSpaceBounds(),
                    trackPos: _trackPos,
                    trackDir: _trackDir,
                    worldPos: _worldPos,
                    worldDir: _worldDir,
                    maxLookaheadTimeSeconds: maxSimulationTime,
                    walkSpeedMetersPerSecond: walkSpeedMetersPerSecond,
                    turnSpeedDegreesPerSecond: turnSpeedDegreesPerSecond
                );
            } else {
                _simulationTasks[taskI].Set(
                    runTimeMilliseconds:(int)(calcTimePerStep*1000),
                    redirects:_simulationRedirects[taskI],
                    redirector: _redirector,
                    predTrackDir: predTrackDir,
                    predConfidence: predConfidence,
                    connectedGraph: connectedGraph,
                    startNodes: _startNodes,
                    startNodeProbabilities: _startNodeProbabilities,
                    initialPathGenWeighter: _weighter,
                    walkableTrackSpaceBounds: GetWalkableTrackSpaceBounds(),
                    trackPos: _trackPos,
                    trackDir: _trackDir,
                    worldPos: _worldPos,
                    worldDir: _worldDir,
                    maxLookaheadTimeSeconds: maxSimulationTime,
                    walkSpeedMetersPerSecond: walkSpeedMetersPerSecond,
                    turnSpeedDegreesPerSecond: turnSpeedDegreesPerSecond
                );
            }
            _simulationTasks[taskI].Start();
            // _simulationTasks[taskI].Resume();
        }

        // Wait for all times to complete (use their time)
        for (int taskI = 0; taskI < _simulationTasks.Count; taskI++) {
            _simulationTasks[taskI].Wait();
        }
        // Task.WaitAll(_simulationTasksArr);

        // Gather results from all tasks into current frame
        for (int taskI = 0; taskI < _simulationTasks.Count; taskI++) {
            var redirects = _simulationRedirects[taskI];
            for (int redI = 0; redI < redirects.Count; redI++) {
                frame.redirects[redI].value += redirects[redI].value;
                frame.redirects[redI].simulationCount += redirects[redI].simulationCount;
            }
        }

        //

        if (_currentRedirects.Count == 0) {
            for (int i = 0; i < redirectOptions.Count; i++) {
                _currentRedirects.Add(new Redirect{
                    option = redirectOptions[i],
                    simulationCount = 0,
                    value = 0.0f
                });
            }
        } else {
            for (int i = 0; i < _currentRedirects.Count; i++) {
                _currentRedirects[i].value = 0.0f;
                _currentRedirects[i].simulationCount = 0;
            }
        }

        // Manage frames, select best redirect instruction by average
        // Add linear weighting
        var totalWeight = 0.0f;
        _simulationFrames.Add(frame);
        for (int i = 0; i < _simulationFrames.Count; i++) {
            var diffTime = _time - _simulationFrames[i].timeSeconds;
            if (diffTime > frameWindowSeconds) {
                // Frame is too old for the window, release it
                var toReleaseFrame = _simulationFrames[i];
                _simulationFrames.RemoveAt(i);
                i--;
                ReturnSimulationFrameToPool(toReleaseFrame);
            } else {
                // Frame is recent enough to include, calc weight
                var weight = 1.0f - diffTime / frameWindowSeconds;
                if (weight <= 0) {
                    continue;
                }

                totalWeight += weight;

                // Go through each redirect, add to main score list
                for (int j = 0; j < _simulationFrames[i].redirects.Count; j++) {
                    var simFrameRedirect = _simulationFrames[i].redirects[j];
                    if (simFrameRedirect.simulationCount > 0) {
                        var avgValue = simFrameRedirect.value / simFrameRedirect.simulationCount;
                        _currentRedirects[j].value += avgValue * weight;
                        _currentRedirects[j].simulationCount += simFrameRedirect.simulationCount;

                        // var value = simFrameRedirect.value / simFrameRedirect.simulationCount;
                        // _currentRedirects[j].value += value;
                        // _currentRedirects[j].simulationCount += simFrameRedirect.simulationCount;
                    }
                }
            }
        }

        var bestValue = -1.0f;
        for (int i = 0; i < _currentRedirects.Count; i++) {
            _currentRedirects[i].value /= totalWeight;
            if (_currentRedirects[i].value > bestValue) {
                _currentBestRedirectInstruction =
                    _currentRedirects[i].option.instruction;
                bestValue = _currentRedirects[i].value;
            }
        }

        // UnityEngine.Debug.Log(
        //     "MC Frame Complete. Simulations: " + simulationCountThisFrame);
    }

    void GetStraightLineWalkableNodes (List<ConnectedGraph.Node> outWalkableNodes) {
        var wpos3 = RedirectionPlaneToUnityPos(_worldPos);
        for (int i = 0; i < connectedGraph.nodes.Count; i++) {
            var node = connectedGraph.nodes[i];
            if (visibilityTester.Visible(wpos3,node.position)) {
                outWalkableNodes.Add(node);
            }
        }
    }

    void GetSqrDistanceSortedStraightLineWalkableNodes (SortedList<float,ConnectedGraph.Node> outWalkableNodes) {
        var wpos3 = RedirectionPlaneToUnityPos(_worldPos);
        for (int i = 0; i < connectedGraph.nodes.Count; i++) {
            var node = connectedGraph.nodes[i];
            if (visibilityTester.Visible(wpos3,node.position)) {
                outWalkableNodes.Add((wpos3-node.position).sqrMagnitude,node);
            }
        }
    }

    static int CompareNodesByDistance(DistanceNode x, DistanceNode y)
    {
        if (x.distance < y.distance) {
            return -1;
        } else if (x.distance == y.distance) {
            return 0;
        } else {
            return 1;
        }
    }

    struct DistanceNode {
        public float distance;
        public ConnectedGraph.Node node;
    }

    List<float> _anglesTmp = new List<float>();
    List<ConnectedGraph.Node> _straightLineNodesTmp = new List<ConnectedGraph.Node>();
    List<DistanceNode> _distanceNodesTmp = new List<DistanceNode>();
    void GetImmediatelyWalkableNodes (List<ConnectedGraph.Node> outWalkableNodes,
        float minAngleDifference, out ConnectedGraph.Node closest) {
        // GetStraightLineWalkableNodes(outWalkableNodes);
        // return;
        _anglesTmp.Clear();
        _straightLineNodesTmp.Clear();
        _distanceNodesTmp.Clear();

        // Get walkable nodes
        GetStraightLineWalkableNodes(_straightLineNodesTmp);

        // Add distance to walkable nodes
        for (int i = 0; i < _straightLineNodesTmp.Count; i++) {
            var nodeRPos = UnityToRedirectionPlanePos(_straightLineNodesTmp[i].position);
            _distanceNodesTmp.Add(new DistanceNode {
                distance = (_worldPos - nodeRPos).sqrMagnitude,
                node = _straightLineNodesTmp[i]
            });
        }

        // Sort by distance
        _distanceNodesTmp.Sort(CompareNodesByDistance);

        // Test that angle difference is large enough to include
        for (int ni = 0; ni < _distanceNodesTmp.Count; ni++) {
            var nodeRPos = UnityToRedirectionPlanePos(_distanceNodesTmp[ni].node.position);
            var angle = RDWMethodUtil.VecToAngle(nodeRPos - _worldPos);
            var toAdd = true;

            if (_distanceNodesTmp[ni].distance < 0.2f) {
                continue;
            }

            for (int ai = 0; ai < _anglesTmp.Count; ai++) {
                if (Mathf.Abs(Mathf.DeltaAngle(angle,_anglesTmp[ai])) < minAngleDifference) {
                    toAdd = false;
                    // UnityEngine.Debug.Log(Time.frameCount + " failed: angle: " + angle + " other: " + _anglesTmp[ai]);
                    break;
                }
            }

            if (toAdd) {
                _anglesTmp.Add(angle);
                outWalkableNodes.Add(_distanceNodesTmp[ni].node);
            }

            // if (outWalkableNodes.Count < 4) {

            // if (_distanceNodesTmp[ni].distance < 5f) {
            //     _anglesTmp.Add(angle);
            //     outWalkableNodes.Add(_distanceNodesTmp[ni].node);
            // }
        }

        if (_distanceNodesTmp.Count > 0) {
            closest = _distanceNodesTmp[0].node;
        } else {
            closest = null;
        }
    }

    // Simulation is just a (literal) random walk starting from the leaf
    // Walk is constrained with a walk model. Let's use a simple model:
    // 1. Turn to a random graph neighbour
    // 2. Walk towards the neighbour
    // 3. Check that we're in the track space (apply no redirection)
    // 4. If we leave the track space, quit, otherwise goto 1.
    // ConnectedGraphWeightedPathGenerator _pathGen = new ConnectedGraphWeightedPathGenerator();
    // ConnectedGraphTimeBacktrackNodeWeighter _pathGenWeighter = new ConnectedGraphTimeBacktrackNodeWeighter();
    // void RunSimulation (float predTrackDir, float predConfidence,
    //     ConnectedGraph.Node startingNode, SimulationFrame frame) {

    //     if (frame.redirects.Count <= 0) {
    //         return;
    //     }

    //     // Prepare path generator weights
    //     _pathGenWeighter.Reset();
    //     _pathGenWeighter.probabilityDecayPerSecond = backtrackProbabilityDecayPerSecond;
    //     _pathGenWeighter.probabilityRecoverPerSecond = backtrackProbabilityRecoverPerSecond;
    //     _pathGenWeighter.probabilityMinimum = backtrackProbabilityMinimum;
    //     _pathGenWeighter.CopyWeights(_weighter);

    //     var bounds = GetWalkableTrackSpaceBounds();

    //     // Currently just pick a redirection randomly (uniformly)
    //     var redirect = frame.redirects[Random.Range(0,frame.redirects.Count)];

    //     var trackPos = _trackPos;
    //     var trackDir = _trackDir;
    //     var worldPos = _worldPos;
    //     var worldDir = _worldDir;
    //     var redirectInstr = redirect.option.instruction;
    //     var simulationTime = 0.0f;
    //     var collision = false;
    //     var targetGraphNode = startingNode;
    //     var pathGenIsInit = false;
    //     var turnTime = 0.0f;
    //     var walkTime = 0.0f;

    //     // Are we already out of bounds?
    //     // if (!trackSpace.bounds.Contains(trackPos)) {
    //     //     collision = true;
    //     // }

    //     // Simulate initial turn-to-face
    //     {
    //         var targetWorldPos = UnityToRedirectionPlanePos(
    //             targetGraphNode.position);
    //         var targetWorldDir = RDWMethodUtil.VecToAngle(
    //             targetWorldPos - worldPos);
    //         WorldWalkDirInfo(worldDir, targetWorldDir,
    //             out float worldDirDelta,
    //             out RDWRedirector.RedirectInstruction.RotationDirection worldDirDeltaDirection);
    //         var trackDirDelta = CalculateTrackDirDelta(
    //             redirectInstr.rotationDirection,worldDirDeltaDirection,
    //             worldDirDelta);

    //         trackDir += trackDirDelta;
    //         worldDir = targetWorldDir;
    //         simulationTime += CalculateTurnDuration(worldDirDelta);
    //     }

    //     while (simulationTime < maxSimulationTime && !collision) {

    //         // Walk towards target graph node, applying scale ONLY (currently)
    //         {
    //             var targetWorldPos = UnityToRedirectionPlanePos(
    //                 targetGraphNode.position);
    //             WorldWalkPosInfo(worldPos, targetWorldPos,
    //                 out _, out float worldPosDeltaDistance);
    //             var trackPosDeltaDistance = CalculateTrackPosDeltaDistance(
    //                 redirectInstr.scaleDirection,worldPosDeltaDistance);
    //             var trackPosInitDirVec = RDWMethodUtil.AngleToVec(trackDir);

    //         //     // Break arc into line segments with max 45 degree curvature
    //         //     // Work out max length of a line segment
    //         //     var clockwise = redirectInstr.rotationDirection == RDWRedirector.RedirectInstruction.RotationDirection.Right;
    //         //     var circleRadius = (1.0f/(_redirector.curvatureGainDegreesPerMeter/360.0f))/(2*Mathf.PI);
    //         //     var circleCircumference = 2 * Mathf.PI * circleRadius;
    //         //     var arcProportion = trackPosDeltaDistance / circleCircumference;
    //         //     // var maxLineSegmentLength = circleCircumference / 8; // 45 degree segments
    //         //     var trackPosToCircleCenterAngle = trackDir + (clockwise ? 90.0f : 270.0f);
    //         //     var circleCenter = trackPos + circleRadius * RDWMethodUtil.AngleToVec(trackPosToCircleCenterAngle);
    //         //     var arcDegrees = arcProportion * 360.0f;
    //         //     var maxDegreesPerSegment = 45.0f;
    //         //     // var circleZeroAngle = RDWMethodUtil.VecToAngle(trackPos - circleCenter) + 90.0f;
    //         //     var circleZeroAngle = trackPosToCircleCenterAngle - 180.0f;

    //         //     var workingTrackPos = trackPos;
    //         //     var workingTrackDir = trackDir;
    //         //     for (var degreesTurned = 0.0f; degreesTurned < arcDegrees; degreesTurned += maxDegreesPerSegment) {

    //         //         var degreesThisSegment = Mathf.Min(maxDegreesPerSegment,arcDegrees - degreesTurned);
    //         //         var totalDegrees = degreesTurned + degreesThisSegment;
    //         //         var nextTrackDir = trackDir + totalDegrees * (clockwise ? 1 : -1);
    //         //         var nextTrackPos = circleCenter + circleRadius * RDWMethodUtil.AngleToVec(circleZeroAngle + totalDegrees * (clockwise ? 1 : -1));

    //         //         // var angleSign = redirectInstr.rotationDirection == RDWRedirector.RedirectInstruction.RotationDirection.Left ? -1 : 1;
    //         //         // var nextTrackDir = workingTrackDir + distanceThisStep * _redirector.curvatureGainDegreesPerMeter * angleSign;
    //         //         // var nextTrackPos = workingTrackPos + distanceThisStep * RDWMethodUtil.AngleToVec(nextTrackDir);

    //         //         // Check we're still in the track space and < time limit
    //         //         if (!trackSpace.bounds.Contains(nextTrackPos)) {
    //         //             // Rough estimate of when we would hit the bounds
    //         //             // Use GetHit because we know current is in, next is out
    //         //             collision = CollideLineSegmentWithRect(workingTrackPos,
    //         //                 nextTrackPos,trackSpace.bounds,out Vector2 collisionPoint);
    //         //             if (collision) {
    //         //                 // Correct simulationTime
    //         //                 var collidedWalk = collisionPoint - workingTrackPos;
    //         //                 simulationTime += CalculateWalkDuration(
    //         //                     CalculateWorldPosDeltaDistance(
    //         //                         redirectInstr.scaleDirection,
    //         //                         collidedWalk.magnitude
    //         //                     ));
    //         //                 break;
    //         //             }
    //         //         }

    //         //         workingTrackDir = nextTrackDir;
    //         //         workingTrackPos = nextTrackPos;
    //         //     }

    //         //     if (collision) {
    //         //         break;
    //         //     }

    //         //     trackPos = workingTrackPos;
    //         //     trackDir = workingTrackDir;
    //         //     worldPos = targetWorldPos;
    //         //     walkTime = CalculateWalkDuration(worldPosDeltaDistance);
    //         //     simulationTime += walkTime;
    //         // }

    //         // var nextRedirect = frame.redirects[Random.Range(0,frame.redirects.Count)];
    //         // redirectInstr = nextRedirect.option.instruction;

    //             var targetTrackPos = trackPos +
    //                 (trackPosInitDirVec * trackPosDeltaDistance);

    //             // Check we're still in the track space and < time limit
    //             if (!bounds.Contains(targetTrackPos)) {
    //                 // Rough estimate of when we would hit the bounds
    //                 // Use GetHit because we know current is in, next is out
    //                 collision = CollideLineSegmentWithRect(trackPos,
    //                     targetTrackPos,bounds,out Vector2 collisionPoint);
    //                 if (collision) {
    //                     // Correct simulationTime
    //                     var collidedWalk = collisionPoint - trackPos;
    //                     simulationTime += CalculateWalkDuration(
    //                         CalculateWorldPosDeltaDistance(
    //                             redirectInstr.scaleDirection,
    //                             collidedWalk.magnitude
    //                         ));
    //                     break;
    //                 }
    //             }

    //             trackPos = targetTrackPos;
    //             worldPos = targetWorldPos;
    //             walkTime = CalculateWalkDuration(worldPosDeltaDistance);
    //             simulationTime += walkTime;
    //         }

    //         // var nextRedirect = frame.redirects[Random.Range(0,frame.redirects.Count)];
    //         // redirectInstr = nextRedirect.option.instruction;

    //         var nextRedirect = frame.redirects[Random.Range(0,frame.redirects.Count)];
    //         redirectInstr = nextRedirect.option.instruction;

    //         // Pick a new graph node from those connected to the current node
    //         // If path gen isn't ready, set it up
    //         if (!pathGenIsInit) {
    //             // Update weights for first walk
    //             _pathGenWeighter.Visit(startingNode,simulationTime);
    //             // Setup path gen
    //             _pathGen.Setup(_random,connectedGraph,_pathGenWeighter,startingNode);
    //             pathGenIsInit = true;
    //         } else {
    //             _pathGen.Advance(turnTime + walkTime*0.5f,walkTime*0.5f);
    //         }

    //         _pathGen.Predict();
    //         targetGraphNode = _pathGen.nextNode;

    //         // Turn towards target graph node, applying rotation
    //         {
    //             var targetWorldPos = UnityToRedirectionPlanePos(
    //                 targetGraphNode.position);
    //             var targetWorldDir = RDWMethodUtil.VecToAngle(
    //                 targetWorldPos - worldPos);
    //             WorldWalkDirInfo(worldDir, targetWorldDir,
    //                 out float worldDirDelta,
    //                 out RDWRedirector.RedirectInstruction.RotationDirection worldDirDeltaDirection);
    //             var trackDirDelta = CalculateTrackDirDelta(
    //                 redirectInstr.rotationDirection,worldDirDeltaDirection,
    //                 worldDirDelta);

    //             trackDir += trackDirDelta;
    //             worldDir = targetWorldDir;
    //             turnTime = CalculateTurnDuration(worldDirDelta);
    //             simulationTime += turnTime;
    //         }



    //         // Select new redirect instruction
    //         // redirectInstr =
    //         //     redirectOptions[Random.Range(0,redirectOptions.Count)].instruction;
    //     }

    //     // Score the simulation - higher is better
    //     // Let's say the score is the proportion of available time used without
    //     // a collision. So if collided immediately, score is 0. If no collision,
    //     // score is 1.
    //     redirect.simulationCount++;

    //     var thisValue = Mathf.Clamp01(simulationTime / maxSimulationTime);
    //     thisValue = easeOutExpo(thisValue);// Mathf.Log(thisValue+1,2);
    //     // var thisValue = simulationTime > maxSimulationTime ? 1 : 0;
    //     redirect.value += thisValue;
    // }

    Rect GetWalkableTrackSpaceBounds () {
        var bounds = trackSpace.bounds;
        return new Rect (
            x: bounds.x + trackSpaceWalkableOffsetMeters,
            y: bounds.y + trackSpaceWalkableOffsetMeters,
            width: bounds.width - trackSpaceWalkableOffsetMeters * 2,
            height: bounds.height - trackSpaceWalkableOffsetMeters * 2);
    }

    // float easeOutExpo(float x) {
    //     return x == 1 ? 1 : 1 - Mathf.Pow (2, -10 * x);
    // }

    // void WorldWalkPosInfo (Vector2 startWorldPos, Vector2 endWorldPos,
    //     out Vector2 worldPosDelta, out float worldPosDeltaDistance) {

    //     worldPosDelta = endWorldPos - startWorldPos;
    //     worldPosDeltaDistance = worldPosDelta.magnitude;
    // }

    // void WorldWalkDirInfo (float startWorldDir, float endWorldDir,
    //     out float worldDirDelta,
    //     out RDWRedirector.RedirectInstruction.RotationDirection worldDirDeltaDirection) {

    //     worldDirDelta = Mathf.DeltaAngle(startWorldDir,endWorldDir);
    //     worldDirDeltaDirection = worldDirDelta < 0
    //         ? RDWRedirector.RedirectInstruction.RotationDirection.Left
    //         : RDWRedirector.RedirectInstruction.RotationDirection.Right;
    // }

    // For walking model
    // float CalculateTrackDirDelta (
    //     RDWRedirector.RedirectInstruction.RotationDirection redirectRotationDirection,
    //     RDWRedirector.RedirectInstruction.RotationDirection worldTurnDirection,
    //     float worldDirDelta) {
    //     // Redirection while turning (rotation gain)
    //     // turn_red_dir := left | right
    //     // virt_turn = track_turn * turn_factor
    //     // turn_factor = co_turn_mul   :: dir(track_turn) == turn_red_dir
    //     // turn_factor = anti_turn_mul :: otherwise
    //     return worldDirDelta /
    //         GetTurnScaleFactor(redirectRotationDirection,
    //         worldTurnDirection,worldDirDelta);
    // }

    // float GetTurnScaleFactor (
    //     RDWRedirector.RedirectInstruction.RotationDirection redirectRotationDirection,
    //     RDWRedirector.RedirectInstruction.RotationDirection worldTurnDirection,
    //     float worldDirDelta) {

    //     // if (redirectRotationDirection == RDWRedirector.RedirectInstruction.RotationDirection.None) {
    //     //     return 1.0f;
    //     // } else {
    //     //     return redirectRotationDirection == worldTurnDirection
    //     //         ? _redirector.angularCoRotMultiplier
    //     //         : _redirector.angularAntiRotMultiplier;
    //     // }
    //     if (redirectRotationDirection == RDWRedirector.RedirectInstruction.RotationDirection.None) {
    //         return 1.0f;
    //     } else {
    //         return redirectRotationDirection == worldTurnDirection
    //             ? Random.Range(1.0f,_redirector.angularCoRotMultiplier)
    //             : Random.Range(_redirector.angularAntiRotMultiplier,1.0f);
    //     }
    // }

    // For walking model - concept is that walk/look direction is the same
    // float CalculateTrackPosDeltaDistance (
    //     RDWRedirector.RedirectInstruction.ScaleDirection redirectScaleDirection,
    //     float worldWalkDistance) {
    //     // Redirection while walking (translation gain)
    //     // scale_red_dir := magnify | reduce
    //     // virt_walk = track_walk * scale_factor
    //     // scale_factor = magnify_scale_mul :: scale_red_dir == magnify
    //     // scale_factor = reduce_scale_mul  :: otherwise
    //     return worldWalkDistance / GetWalkDistanceScaleFactor(redirectScaleDirection);
    // }

    // float CalculateWorldPosDeltaDistance (
    //     RDWRedirector.RedirectInstruction.ScaleDirection redirectScaleDirection,
    //     float trackWalkDistance) {

    //     return trackWalkDistance * GetWalkDistanceScaleFactor(redirectScaleDirection);
    // }

    // float GetWalkDistanceScaleFactor (
    //     RDWRedirector.RedirectInstruction.ScaleDirection redirectScaleDirection) {

    //     // switch(redirectScaleDirection) {
    //     //     case RDWRedirector.RedirectInstruction.ScaleDirection.Magnify :
    //     //         return _redirector.transGainMagnifyMultiplier;
    //     //     case RDWRedirector.RedirectInstruction.ScaleDirection.Reduce :
    //     //         return _redirector.transGainReduceMultiplier;
    //     // }
    //     // return 1.0f;
    //     switch(redirectScaleDirection) {
    //         case RDWRedirector.RedirectInstruction.ScaleDirection.Magnify :
    //             return Random.Range(1.0f,_redirector.transGainMagnifyMultiplier);
    //         case RDWRedirector.RedirectInstruction.ScaleDirection.Reduce :
    //             return Random.Range(_redirector.transGainReduceMultiplier, 1.0f);
    //     }
    //     return 1.0f;
    // }

    // // Returns number of hits in {0,2}
    // int CollideCircleWithLineSegment (Vector2 circleCenter, float circleRadius,
    //     Vector2 line0, Vector2 line1, out Vector2 hit0, out Vector2 hit1) {

    //     var d = line1 - line0;
    //     var f = line0 - circleCenter;
    //     var r = circleRadius;

    //     // https://stackoverflow.com/a/1084899
    //     float a = Vector2.Dot(d,d) ;
    //     float b = 2*Vector2.Dot(f,d) ;
    //     float c = Vector2.Dot(f,f) - r*r;

    //     float discriminant = b*b-4*a*c;
    //     if( discriminant < 0 )
    //     {
    //         hit0 = Vector2.zero;
    //         hit1 = Vector2.zero;
    //         return 0;
    //     }
    //     // ray didn't totally miss sphere,
    //     // so there is a solution to
    //     // the equation.

    //     discriminant = Mathf.Sqrt( discriminant );

    //     // either solution may be on or off the ray so need to test both
    //     // t1 is always the smaller value, because BOTH discriminant and
    //     // a are nonnegative.
    //     float t1 = (-b - discriminant)/(2*a);
    //     float t2 = (-b + discriminant)/(2*a);

    //     // 3x HIT cases:
    //     //          -o->             --|-->  |            |  --|->
    //     // Impale(t1 hit,t2 hit), Poke(t1 hit,t2>1), ExitWound(t1<0, t2 hit),

    //     // 3x MISS cases:
    //     //       ->  o                     o ->              | -> |
    //     // FallShort (t1>1,t2>1), Past (t1<0,t2<0), CompletelyInside(t1<0, t2>1)

    //     if( t1 >= 0 && t1 <= 1 )
    //     {
    //         // t1 is the intersection, and it's closer than t2
    //         // (since t1 uses -b - discriminant)
    //         // Impale, Poke
    //         hit0 = line0 + d*t1;
    //         if (t2 >= 0 && t2 <= 1) {
    //             hit1 = line0 + d*t1;
    //             return 2;
    //         } else {
    //             hit1 = Vector2.zero;
    //             return 1;
    //         }
    //     }

    //     // here t1 didn't intersect so we are either started
    //     // inside the sphere or completely past it
    //     if( t2 >= 0 && t2 <= 1 )
    //     {
    //         // ExitWound
    //         hit0 = line0 + d*t2;
    //         hit1 = Vector2.zero;
    //         return 1;
    //     }

    //     // no intn: FallShort, Past, CompletelyInside
    //     hit0 = Vector2.zero;
    //     hit1 = Vector2.zero;
    //     return 0;
    // }

    // // Arc angles in degrees in Unity's strange co-ord system, direction clockwise
    // // Match unity's weird coordinate system. (0,1) is 0 degrees, (1,0) is 90 degrees defined by unit circle (cos(theta),sin(theta))
    // // i.e., (1,0) is at 0 degrees, (0,1) is at 90
    // int CollideArcWithLineSegment (Vector2 arcCenter, float arcRadius,
    //     float arcAngle0, float arcAngle1,
    //     Vector2 line0, Vector2 line1, out Vector2 hit0, out Vector2 hit1) {

    //     arcAngle0 = ((arcAngle0 + 180) % 360) - 180;

    //     var circleHitCount = CollideCircleWithLineSegment(arcCenter,arcRadius,line0,
    //         line1,out Vector2 circleHit0, out Vector2 circleHit1);

    //     if (circleHitCount <= 0) {
    //         hit0 = Vector2.zero;
    //         hit1 = Vector2.zero;
    //         return 0;
    //     }

    //     if (circleHitCount == 1) {
    //         var hitAngle = RDWMethodUtil.VecToAngle(circleHit0 - arcCenter);
    //         if (DeltaAngle(arcAngle0,hitAngle) < DeltaAngle(arcAngle0,arcAngle1)) {
    //             hit0 = circleHit0;
    //             hit1 = Vector2.zero;
    //             return 1;
    //         }
    //     }

    //     hit0 = Vector2.zero;
    //     hit1 = Vector2.zero;

    //     var arcDelta01 = DeltaAngle(arcAngle0,arcAngle1);
    //     var arcHitCount = 0;
    //     var hitAngle0 = RDWMethodUtil.VecToAngle(circleHit0 - arcCenter);
    //     if (DeltaAngle(arcAngle0,hitAngle0) < arcDelta01) {
    //         hit0 = circleHit0;
    //         arcHitCount++;
    //     }

    //     var hitAngle1 = RDWMethodUtil.VecToAngle(circleHit0 - arcCenter);
    //     if (DeltaAngle(arcAngle0,hitAngle1) < arcDelta01) {
    //         if (arcHitCount == 0) {
    //             hit0 = circleHit1;
    //             hit1 = Vector2.zero;
    //             return 1;
    //         } else {
    //             hit1 = circleHit1;
    //             return 2;
    //         }
    //     }
    // }

    // int CollideArcWithRect (Vector2 arcCenter, float arcRadius,
    //     float arcAngle0, float arcAngle1, Rect rect, out Vector2 hit0, ) {

    // }

    // bool CollideLineSegmentWithLineSegment (Vector2 line00, Vector2 line01,
    //     Vector2 line10, Vector2 line11, out Vector2 hit) {

    //     // https://stackoverflow.com/a/1968345
    //     Vector2 s0 = line01 - line00;
    //     Vector2 s1 = line11 - line10;

    //     float denom = -s1.x * s0.y + s0.x * s1.y;
    //     float s = (-s0.y * (line00.x - line10.x) + s0.x * (line00.y - line10.y)) / denom;
    //     float t = ( s1.x * (line00.y - line10.y) - s1.y * (line00.x - line10.x)) / denom;

    //     if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {

    //         hit = line00 + (t * s0);
    //         return true;
    //     }

    //     hit = Vector2.zero;
    //     return false;
    // }

    // Check if line segment collides with rect and return collision point
    // bool CollideLineSegmentWithRect (Vector2 line0, Vector2 line1,
    //     Rect rect, out Vector2 hit) {

    //     var r0 = rect.min;
    //     var r1 = new Vector2(rect.xMax,rect.yMin);
    //     var r2 = rect.max;
    //     var r3 = new Vector2(rect.xMin,rect.yMax);

    //     return
    //         CollideLineSegmentWithLineSegment(line0,line1,r0,r3,out hit) ||
    //         CollideLineSegmentWithLineSegment(line0,line1,r3,r2,out hit) ||
    //         CollideLineSegmentWithLineSegment(line0,line1,r2,r1,out hit) ||
    //         CollideLineSegmentWithLineSegment(line0,line1,r1,r0,out hit);
    // }

    // ConnectedGraph.Node GetRandomNodeWeighted (List<float> weights,
    //     List<ConnectedGraph.Node> nodes) {

    //     // rval is between 0 and 1 inclusive
    //     var rval = Random.value;
    //     for (int i = 0; i < nodes.Count; i++) {
    //         if (weights[i] > rval) {
    //             return nodes[i];
    //         } else {
    //             rval -= weights[i];
    //         }
    //     }

    //     // Handle case where rval is exactly 1
    //     if (nodes.Count > 0) {
    //         return nodes[nodes.Count-1];
    //     } else {
    //         return null;
    //     }
    // }

    // Return normalized node probs, weighted by similarity to predicted path
    void GetNodeProbabilitiesPredictionWeighted (List<float> outProbabilities,
        List<ConnectedGraph.Node> possibleNodes, float predTrackDir,
        float predConfidence) {

        // Update possible node probabilities from prediction
        var predWorldDir = RDWMethodUtil.VecToAngle(
            UnityToRedirectionPlaneDir(
            trackSpace.TrackToWorldDirection(predTrackDir)));
        var totalSimilarity = 0.0f;
        for (int i = 0; i < possibleNodes.Count; i++) {
            var toNodeWorldDir = RDWMethodUtil.VecToAngle(
                UnityToRedirectionPlanePos(possibleNodes[i].position) - _worldPos);
            var deltaAngle = Mathf.DeltaAngle(predWorldDir,toNodeWorldDir);
            var similarity = 1.0f - (Mathf.Abs(deltaAngle) * (1.0f/180));
            // var probability = _backtrackProbabilities[node] * predProbability;
            outProbabilities.Add(similarity);

            totalSimilarity += similarity;
        }

        // Correct similarities for total, compensate for low confidence
        var baseProbability = 1.0f/possibleNodes.Count;
        var totalSimilarityRecip = 1.0f/totalSimilarity;
        for (int i = 0; i < possibleNodes.Count; i++) {
            var similarity = outProbabilities[i] * totalSimilarityRecip;
            var similarityScore = similarity*predConfidence;
            var baseScore = baseProbability*(1-predConfidence);
            outProbabilities[i] = similarityScore + baseScore;
        }
    }

    // Return normalized node probs, weighted uniformly
    void GetNodeProbabilitiesUniformWeighted (List<float> outProbabilities,
        List<ConnectedGraph.Node> nodes) {

        var probability = 1.0f / nodes.Count;
        for (int i = 0; i < nodes.Count; i++) {
            outProbabilities.Add(probability);
        }
    }

    // Ensure node probabilities sum to 1
    void NormalizeNodeProbabilities (List<float> probabilities) {
        var sum = 0.0f;
        for (int i = 0; i < probabilities.Count; i++) {
            sum += probabilities[i];
        }

        var recip = 1.0f / sum;
        for (int i = 0; i < probabilities.Count; i++) {
            probabilities[i] *= recip;
        }
    }

    ConnectedGraph.Node GetClosestNode () {
        return GetClosestNode(connectedGraph.nodes);
    }

    ConnectedGraph.Node GetClosestNode (
        List<ConnectedGraph.Node> possibleNodes) {
        var closest = null as ConnectedGraph.Node;
        var closestDistanceSqr = Mathf.Infinity;

        for (int i = 0; i < possibleNodes.Count; i++) {
            var nodePos = UnityToRedirectionPlanePos(
                possibleNodes[i].position);
            var to = nodePos - _worldPos;
            var distanceSqr = to.sqrMagnitude;
            if (distanceSqr < closestDistanceSqr) {
                closest = possibleNodes[i];
                closestDistanceSqr = distanceSqr;
            }
        }

        return closest;
    }

    float CalculateTurnDuration (float worldDirDelta) {
        return Mathf.Abs(worldDirDelta) / turnSpeedDegreesPerSecond;
    }

    float CalculateWalkDuration (float worldWalkDistance) {
        return worldWalkDistance / walkSpeedMetersPerSecond;
    }

    Vector3 RedirectionPlaneToUnityPos (Vector2 redPlaneWorldPos) {
        return new Vector3(redPlaneWorldPos.x,0.0f,redPlaneWorldPos.y);
    }

    Vector2 UnityToRedirectionPlanePos (Vector3 worldPos) {
        return new Vector2(worldPos.x,worldPos.z);
    }

    Vector2 UnityToRedirectionPlaneDir (Vector3 worldDir) {
        return new Vector2(worldDir.x,worldDir.z).normalized;
    }
}