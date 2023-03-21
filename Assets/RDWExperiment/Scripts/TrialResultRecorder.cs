using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace RDWMCExperiment {
public class TrialResultRecorder : MonoBehaviour {

    public string resultsRootPath = "TrialResults";
    public string fieldDelimeter = ",";
    public string entryDelimeter = "\n";

    StreamWriter _writer;
    int _fieldsThisEntry;

    void OnDestroy () {
        if (_writer != null) {
            _writer.Dispose();
            _writer = null;
        }
    }

    static string EncodeDateTime (DateTime dt) {
        return dt.ToString("s").Replace(":","-");
    }

    public void BeginRecord (string layoutCollectionDesc, List<Condition> conditions) {
        if (_writer != null) {
            UnityEngine.Debug.LogError("Trial recorder already writing. Abandoning...");
            Application.Quit();
        }

        // var assetsPath = Path.Combine(Application.dataPath,"Assets");
        var rootPath = Path.Combine(Application.dataPath,resultsRootPath);
        var folderPath = Path.Combine(rootPath,EncodeDateTime(DateTime.Now));
        if (Directory.Exists(folderPath)) {
            UnityEngine.Debug.LogError("Path " + folderPath + " exists. Abandoning...");
            Application.Quit();
        }
        Directory.CreateDirectory(folderPath);

        var collectionDescPath = Path.Combine(folderPath,"LayoutCollectionDesc.txt");
        using (var sw = new StreamWriter(collectionDescPath)) {
            sw.Write(layoutCollectionDesc);
        }

        var conditionsPath = Path.Combine(folderPath,"Conditions.csv");
        using (var sw = new StreamWriter(conditionsPath)) {
            _fieldsThisEntry = 0;
            AddField(sw,"Name");
            AddField(sw,"Time Step");
            AddField(sw,"Walk Speed (m/s)");
            AddField(sw,"Turn Speed (deg/s)");
            AddField(sw,"Track Space Dimensions (w,h)");

            NextEntry(sw);

            for (int i = 0; i < conditions.Count; i++) {
                AddField(sw,conditions[i].name);
                AddField(sw,conditions[i].timeStep.ToString());
                AddField(sw,conditions[i].walkSpeedMetersPerSecond.ToString());
                AddField(sw,conditions[i].turnSpeedDegreesPerSecond.ToString());
                AddField(sw,conditions[i].trackSpaceDimensions.ToString());

                NextEntry(sw);
            }
        }

        var recordPath = Path.Combine(folderPath,"Record.csv");
        _writer = new StreamWriter(recordPath);
        _fieldsThisEntry = 0;
    }


    public void EndRecord () {
        if (_writer != null) {
            _writer.Close();
            _writer.Dispose();
            _writer = null;
        }
    }

    private void AddField (StreamWriter writer, string field) {
        if (_fieldsThisEntry > 0) {
            writer.Write(fieldDelimeter);
        }
        writer.Write(field);
        _fieldsThisEntry++;
    }


    public void AddField (string field) {
        AddField(_writer,field);
    }

    private void NextEntry (StreamWriter writer) {
        writer.Write(entryDelimeter);
        _fieldsThisEntry = 0;
    }

    public void NextEntry () {
        NextEntry(_writer);
    }
}
}