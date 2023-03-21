using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Unity.EditorCoroutines.Editor;

namespace RDWMCExperiment {
public class LayoutCollectionBuilder : MonoBehaviour {

    public ALayoutGenerator generator;
    public int layoutCount = 5;

#if UNITY_EDITOR
    private bool _running;
    private EditorCoroutine _coroutine;

    static bool IsDirectoryEmpty(string path) {
        return !System.IO.Directory.EnumerateFileSystemEntries(path).Any();
    }

    [Button("Build Collection")]
    void BuildCollectionButton () {
        if (!_running) {
            _coroutine = EditorCoroutineUtility.StartCoroutine(BuildCollectionCoroutine(),this);
        }
    }

    static string EncodeDateTime (DateTime dt) {
        return dt.ToString("s").Replace(":","-");
    }

    IEnumerator BuildCollectionCoroutine () {
        _running = true;

        var folderPath = UnityEditor.EditorUtility.SaveFolderPanel("Collection Path",
            "","layouts");
        if (folderPath.Length > 0) {
            if (Directory.Exists(folderPath) && !IsDirectoryEmpty(folderPath)) {
                Debug.LogError("Abandoned as folder already contains files: please provide an empty folder");
                yield break;
            }

            Directory.CreateDirectory(folderPath);

            var masterSeed = new System.Random().Next();
            var masterRandom = new System.Random(masterSeed);

            // Create desc files
            var collectionDescPath = Path.Combine(folderPath,"LayoutCollectionDesc.txt");
            using (var sw = new StreamWriter(collectionDescPath)) {
                sw.Write(string.Format(
                    "id: {0},\n" +
                    "seed: {1},\n" +
                    "date: {2},\n" +
                    "count: {3}",
                    Guid.NewGuid(),
                    masterSeed,
                    EncodeDateTime(DateTime.Now),
                    layoutCount
                ));
            }
            var generatorParamsPath = Path.Combine(folderPath,"GeneratorParams.txt");
            using (var sw = new StreamWriter(generatorParamsPath)) {
                sw.Write(generator.GenerateParamsString());
            }

            // Create layout files
            var layoutPath = Path.Combine(folderPath,"LayoutFiles");
            Directory.CreateDirectory(layoutPath);

            var layouts = new List<Layout>();
            var iter = generator.Generate(layouts,masterRandom,layoutCount);

            while (iter.MoveNext()) {
                Debug.Log("Building layout collection. Progress: " + generator.GetProgress());
                yield return null;
            }

            for (int i = 0; i < layouts.Count; i++) {
                var path = Path.Combine(layoutPath,i.ToString() + ".json");
                LayoutUtil.ExportToFile(layouts[i],path);
            }

            UnityEditor.AssetDatabase.Refresh();
        }

        _running = false;
    }

    [Button("Cancel Build")]
    void CancelBuildButton () {
        if (_running) {
            EditorCoroutineUtility.StopCoroutine(_coroutine);
            _running = false;

            // Let generator know to stop
            if (generator) {
                generator.Cancel();
            }
        }
    }
#endif

}
}
