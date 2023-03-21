using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace RDWMCExperiment {
public class TrialConsoleLogger : MonoBehaviour {

public TrialRunner runner;

void Awake () {
    if (runner) {
        runner.starting += TrialRunner_OnStarting;
        runner.stopped += TrialRunner_OnStopped;
        runner.completed += TrialRunner_OnCompleted;
    }
}

void TrialRunner_OnStarting (TrialRunner runner, Layout layout, Condition condition) {
    Debug.Log("Trial Starting: " + layout + " " + condition.name);
}

void TrialRunner_OnStopped (TrialRunner runner, Layout layout, Condition condition) {
    Debug.Log("Trial Stopped: " + layout + " " + condition.name);
}

void TrialRunner_OnCompleted (TrialRunner runner, Layout layout, Condition condition, TrialRunner.Results results) {
    Debug.Log("Trial Completed: " + layout + " " + condition.name + " " + results.collisions);
}

}
}