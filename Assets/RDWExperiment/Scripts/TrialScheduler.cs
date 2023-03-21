using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace RDWMCExperiment {
public class TrialScheduler : MonoBehaviour {

[System.Serializable]
public class TrialSet {
    public ALayoutProvider layoutProvider;
    public List<Condition> conditions;
}

[HorizontalLine]

[SerializeField] public bool runTrialsOnStart;// { get; private set; }


[HorizontalLine]

[SerializeField, ReadOnly]
private int _currentTrialSetIndex;
public int currentTrialSetIndex { get { return _currentTrialSetIndex; } }
[SerializeField, ReadOnly]
private int _currentLayoutIndex;
public int currentLayoutIndex { get { return _currentLayoutIndex; } }
[SerializeField, ReadOnly]
private int _currentConditionIndex;
public int currentConditionIndex { get { return _currentConditionIndex; } }
[SerializeField, ReadOnly]
private float _currentProgress;
public float currentProgress { get { return _currentProgress; } }

[HorizontalLine]

[DisableIf("isInPlayMode")]
public TrialRunner runner;
[DisableIf("isInPlayMode")]
public TrialResultRecorder resultRecorder;

public List<TrialSet> trialSets;

// public List<Condition> conditions;

// public List<Layout> layouts = new List<Layout>();

// public string saveDirectory;

// [HorizontalLine]
// public string saveDir = "";

// Layout _layout;
// Condition _condition;
Coroutine _coroutine;
int _totalTrialNum;
float _progressPerTrial;

bool isInPlayMode { get { return Application.isPlaying; }}

// [Button("Choose Save Directory")]
// void ChooseSaveDirectoryButton () {
//     saveDir = UnityEditor.EditorUtility.SaveFolderPanel("Save results to folder","","");
// }

[Button("Run Trials",EButtonEnableMode.Playmode)]
void RunTrialsButton () {
    if (_coroutine == null) {
        _coroutine = StartCoroutine(CoRunAllTrialSets());
    }
}

[Button("Stop Trials",EButtonEnableMode.Playmode)]
void StopTrialsButton() {
    Stop();
}

void Stop () {
    if (_coroutine != null) {
        runner.Stop();
        StopCoroutine(_coroutine);
        _coroutine = null;
    }
}

IEnumerator CoRunAllTrialSets () {
    // Sum all trials for progress counter
    _totalTrialNum = 0;
    for (int i = 0; i < trialSets.Count; i++) {
        _totalTrialNum += trialSets[i].conditions.Count *
            trialSets[i].layoutProvider.GetCollectionLayoutCount();
    }
    _progressPerTrial = 1.0f / _totalTrialNum;

    for (int i = 0; i < trialSets.Count; i++) {
        _currentTrialSetIndex = i;
        var enumerator = CoRunTrialSet(trialSets[i]);
        yield return null;
        while (enumerator.MoveNext()) {
            yield return null;
        }
    }

    _coroutine = null;
}

IEnumerator CoRunTrialSet (TrialSet trialSet) {
    var layouts = new List<Layout>();
    trialSet.layoutProvider.Import(layouts);

    resultRecorder.EndRecord();
    resultRecorder.BeginRecord(trialSet.layoutProvider.GetCollectionDescriptor(),trialSet.conditions);

    // Do header
    resultRecorder.AddField("Layout Num");
    for (int ci = 0; ci < trialSet.conditions.Count; ci++) {
        resultRecorder.AddField(trialSet.conditions[ci].name + "_TotalScaleModMeters");
        resultRecorder.AddField(trialSet.conditions[ci].name + "_TotalTurnModDegrees");
        resultRecorder.AddField(trialSet.conditions[ci].name + "_TotalCollisions");
    }
    resultRecorder.NextEntry();
    // Do entries
    for (var layoutI = 0; layoutI < layouts.Count; layoutI++) {
        _currentLayoutIndex = layoutI;
        resultRecorder.AddField(layoutI.ToString());
        // _layout = layouts[trialI];
        for (var condI = 0; condI < trialSet.conditions.Count; condI++) {
            _currentConditionIndex = condI;
            _currentProgress += _progressPerTrial;

            var enumerator = runner.Run(layouts[layoutI],trialSet.conditions[condI]);
            yield return null;
            while (enumerator.MoveNext()) {
                yield return null;
            }
        }
        resultRecorder.NextEntry();
    }
    resultRecorder.EndRecord();
}

void TrialRunner_Completed (TrialRunner tr,Layout l,Condition c,
    TrialRunner.Results results) {
    resultRecorder.AddField(results.totalScaleModMeters.ToString());
    resultRecorder.AddField(results.totalTurnModMeters.ToString());
    resultRecorder.AddField(results.collisions.ToString());
}

void Awake () {
    if (runner) {
        runner.completed += TrialRunner_Completed;
    }
}

void Start () {
    if (runTrialsOnStart)
    {
        RunTrialsButton();
    }
}

void OnDestroy () {
    if (runner) {
        runner.completed -= TrialRunner_Completed;
    }
}

}
}