#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NaughtyAttributes;
using System.Threading;
using System.Threading.Tasks;

namespace RDWMCExperiment {
public class ConstrainedLayoutGenerator : ALayoutGenerator {

    public Vector2Int dimensions = new Vector2Int(5,5);
    public float edgeFactor = 0.5f;
    public float nodeSpacingMeters = 1.0f;
    public float pathLengthMeters = 10.0f;
    public float maxStraightMeters = 2.2f;
    public float maxPathStraightMeters = 4.4f;

    [HorizontalLine]

    public int maxTaskCount = 200;

    bool _running = false;
    List<Task<Layout>> _tasks = new List<Task<Layout>>();
    float _progress = 0;
    CancellationTokenSource _cts = null;

    public override string GenerateParamsString () {
        return string.Format(
            "generator: Constrained,\n" +
            "dimensions: {0},\n" +
            "edge factor: {1},\n" +
            "node spacing (meters): {2},\n" +
            "path length (meters): {3},\n" +
            "max straight (meters): {4},\n" +
            "max path straight (meters): {5}",
            dimensions,
            edgeFactor,
            nodeSpacingMeters,
            pathLengthMeters,
            maxStraightMeters,
            maxPathStraightMeters);
    }

    public override IEnumerator Generate (List<Layout> layouts,
        System.Random masterRandom, int count) {
        if (_running) {
            Debug.LogWarning("Attempting to start generation while busy. Abandoning...");
            yield break;
        }

        _running = true;
        _tasks.Clear();
        _cts = new CancellationTokenSource();
        var remainingCount = count;
        while (_tasks.Count > 0 || remainingCount > 0) {
            // Store results from finished tasks and remove them
            for (int i = 0; i < _tasks.Count; i++) {
                if (_tasks[i].IsCompleted) {
                    layouts.Add(_tasks[i].Result);
                    _tasks[i].Dispose();
                    _tasks.RemoveAt(i);
                    i--;
                }
            }

            // Add new tasks if required
            while (remainingCount > 0 && _tasks.Count < maxTaskCount) {
                var random = new System.Random(masterRandom.Next());
                var algo = new ConstrainedLayoutGeneratorAlgo();
                algo.Set(dimensions.x,dimensions.y,edgeFactor,nodeSpacingMeters,
                    pathLengthMeters,maxStraightMeters,maxPathStraightMeters,random);

                _tasks.Add(Task.Factory.StartNew(() => GenerateLayout(algo,_cts.Token),_cts.Token));
                remainingCount--;
            }

            _progress = 1 - ((_tasks.Count + remainingCount) / (float)count);
            yield return null;
        }

        _running = false;
    }

    public override float GetProgress() {
        return _progress;
    }

    public override bool InProgress() {
        return _running;
    }

    public override void Cancel () {
        if (_running) {
            // Send out cancel event and wait for all tasks to resolve
            _cts.Cancel();
            Task.WaitAll(_tasks.ToArray());

            // Dispose the cancellation token source and tasks
            _cts.Dispose();
            for (int i = 0; i < _tasks.Count; i++) {
                _tasks[i].Dispose();
            }

            _tasks.Clear();
            _running = false;
            _cts = null;
            _progress = 0;
        }
    }

    // public override async Task GenerateAsync (List<Layout> layouts, string desc) {
    //     var masterRandom = null as System.Random;
    //     if (useStaticSeed) {
    //         masterRandom = new System.Random(seed);
    //     } else {
    //         masterRandom = new System.Random();
    //     }

    //     var totalRemainingLayouts = count;
    //     var layoutsPerThread = count / threadCount;
    //     var tasks = new List<Task<List<Layout>>>();
    //     for (int i = 0; i < threadCount; i++) {
    //         var layoutsForThisThread = layoutsPerThread;

    //         if (i == threadCount - 1) {
    //             layoutsForThisThread = totalRemainingLayouts;
    //         }

    //         totalRemainingLayouts -= layoutsForThisThread;

    //         var algo = new ConstrainedLayoutGeneratorAlgo();
    //         var random = new System.Random(masterRandom.Next());
    //         algo.Set(dimensions.x,dimensions.y,edgeFactor,nodeSpacingMeters,pathLengthMeters,
    //             maxStraightMeters,random);
    //         tasks.Add(Task.Factory.StartNew(() => GenerateLayout(algo,layoutsForThisThread)));
    //     }

    //     await Task.WhenAll(tasks);

    //     for (int i = 0; i < tasks.Count; i++) {
    //         var threadLayouts = tasks[i].Result;
    //         for (int ti = 0; ti < threadLayouts.Count; ti++) {
    //             layouts.Add(threadLayouts[ti]);
    //         }
    //     }
    // }

    static Layout GenerateLayout (ConstrainedLayoutGeneratorAlgo algo,
        CancellationToken ct) {

        var layout = new Layout();
        while (!algo.Generate(layout)) {
            if (ct.IsCancellationRequested) {
                return null;
            }
        }
        return layout;
    }
}
}

#endif