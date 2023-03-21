using UnityEngine;
using System;
using System.Collections.Generic;

public class RDWDemoPlayback : MonoBehaviour {

# if UNITY_EDITOR
    public bool playing;
    public Transform walker;
    public TextAsset recording;

    void OnEnable () {
        LoadRecording();
    }

    void Update () {

        if (_times.Count <= 0) {
            playing = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            playing = !playing;
        }

        if (playing) {
            _time += Time.deltaTime;

            var i = 0;
            while (i < _times.Count) {
                if (_times[i] > _time) {
                    break;
                }
                i++;
            }

            if (i <= 0) {
                walker.position = _walkerPositions[0];
                walker.rotation = _walkerRotations[0];
                transform.position = _trackSpacePositions[0];
                transform.rotation = _trackSpaceRotations[0];
            } else if (i >= _times.Count) {
                walker.position = _walkerPositions[_times.Count - 1];
                walker.rotation = _walkerRotations[_times.Count - 1];
                transform.position = _trackSpacePositions[_times.Count - 1];
                transform.rotation = _trackSpaceRotations[_times.Count - 1];
            } else {
                var t = (_time - _times[i - 1]) / (_times[i] - _times[i - 1]);
                walker.position = Vector3.Lerp(_walkerPositions[i - 1], _walkerPositions[i], t);
                walker.rotation = Quaternion.Lerp(_walkerRotations[i - 1], _walkerRotations[i], t);
                transform.position = Vector3.Lerp(_trackSpacePositions[i - 1], _trackSpacePositions[i], t);
                transform.rotation = Quaternion.Lerp(_trackSpaceRotations[i - 1], _trackSpaceRotations[i], t);
            }
        }
    }

    void LoadRecording () {
        var entries = recording.text.Split(new[] { RDWDemoTracker.ENTRY_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

        _times = new List<float>(entries.Length);
        _walkerPositions = new List<Vector3>(entries.Length);
        _walkerRotations = new List<Quaternion>(entries.Length);
        _trackSpacePositions = new List<Vector3>(entries.Length);
        _trackSpaceRotations = new List<Quaternion>(entries.Length);

        for (int i = 0; i < entries.Length; i++) {
            var attribs = entries[i].Split(',');
            _times.Add(float.Parse(attribs[0]));
            _walkerPositions.Add(ParseVector3(attribs[1], attribs[2], attribs[3]));
            _walkerRotations.Add(ParseQuaternion(attribs[4], attribs[5], attribs[6], attribs[7]));
            _trackSpacePositions.Add(ParseVector3(attribs[8], attribs[9], attribs[10]));
            _trackSpaceRotations.Add(ParseQuaternion(attribs[11], attribs[12], attribs[13], attribs[14]));
        }

    }

    static Vector3 ParseVector3 (string x, string y, string z) {
        // Substring to remove parens
        return new Vector3(
            float.Parse(x.Substring(1)),
            float.Parse(y),
            float.Parse(z.Substring(0,z.Length-1))
        );
    }

    static Quaternion ParseQuaternion (string x, string y, string z, string w) {
        // Substring to remove parens
        return new Quaternion(
            float.Parse(x.Substring(1)),
            float.Parse(y),
            float.Parse(z),
            float.Parse(w.Substring(0, w.Length - 1))
        );
    }

    float _time;
    List<float> _times;
    List<Vector3> _walkerPositions;
    List<Quaternion> _walkerRotations;
    List<Vector3> _trackSpacePositions;
    List<Quaternion> _trackSpaceRotations;
#endif
}
