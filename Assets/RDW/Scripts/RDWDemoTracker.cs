using UnityEngine;
using System.IO;
using System;
using System.Collections;

[RequireComponent(typeof(RDWTrackSpace))]
public class RDWDemoTracker : MonoBehaviour {

#if UNITY_EDITOR
    public float timeStep;

    public RDWWaypointWalker walker;

    public int bufferLength;

    public const char ENTRY_SEPARATOR = ';';

    void Awake () {
        _trackSpace = GetComponent<RDWTrackSpace>();
        _bufferCount = 0;
        _stepCount = 0;
        _buffer = new string[bufferLength > 0? bufferLength : 1];

        walker.enabled = false;

        // Rough duration estimate
        _duration = 0;
        for (int i = 0; i < walker.waypoints.Count; i++) {
            var prev = i == 0 ? walker.transform.position : walker.waypoints[i - 1];
            _duration += (walker.waypoints[i] - prev).magnitude / walker.forwardSpeed;
            _duration += 90.0f / walker.turnSpeed;
        }

        _fileName = Application.dataPath + Path.DirectorySeparatorChar +
            DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + ".txt";
    }

    void OnDisable () {
        StopAllCoroutines();
    }

    void Update () {

        if (Input.GetKeyDown(KeyCode.R)) {
            StartCoroutine(Record());
        }
    }

    IEnumerator Record () {
        while (walker.waypoints.Count > 0) {
            var time = _stepCount * timeStep;

            walker.Step(timeStep);
            // This can take a long time (i.e., brute force) need to let UI refresh so system doesn't lock
            yield return StartCoroutine(_trackSpace.Step(walker.transform.localPosition, walker.transform.localRotation, timeStep));

            _buffer[_bufferCount++] =
                time + "," +
                walker.transform.position.ToStringEx() + "," +
                walker.transform.rotation.ToStringEx() + "," +
                _trackSpace.transform.position.ToStringEx() + "," +
                _trackSpace.transform.rotation.ToStringEx() + ENTRY_SEPARATOR;

            if (_bufferCount >= _buffer.Length) {
                WriteBufferAndClear();
            }

            Debug.Log("Step: " + _stepCount + " Time: " + time + " Progress: " + ((time / _duration) * 100.0f) + "%");

            _stepCount++;
            yield return null;
        }

        WriteBufferAndClear();
        UnityEditor.EditorApplication.isPlaying = false;
    }

    void WriteBufferAndClear () {
        using (var sw = new StreamWriter(_fileName, append: true)) {
            for (int i = 0; i < _bufferCount; i++) {
                sw.Write(_buffer[i]);
            }
        }
        _bufferCount = 0;
    }

    string[] _buffer;
    int _bufferCount;
    int _stepCount;
    RDWTrackSpace _trackSpace;
    float _duration;
    string _fileName;

#endif
}