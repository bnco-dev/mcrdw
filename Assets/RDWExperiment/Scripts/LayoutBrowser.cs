using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;

namespace RDWMCExperiment {
public class LayoutBrowser : MonoBehaviour {

    public ALayoutProvider layoutProvider;
    public TrialVisualizer visualizer;

    public List<Layout> layouts = new List<Layout>();

    public int layoutIndex;

    static bool IsDirectoryEmpty(string path) {
        return !System.IO.Directory.EnumerateFileSystemEntries(path).Any();
    }

    [Button("Save Layout")]
    void SaveLayoutButton () {
#if UNITY_EDITOR
        var path = UnityEditor.EditorUtility.SaveFilePanel("Save Layout","",
            "layout.json","json");
        if (path.Length > 0 && layoutIndex > 0 && layoutIndex < layouts.Count) {
            LayoutUtil.ExportToFile(layouts[layoutIndex],path);
        }
#endif
    }

    [Button("Save All Layouts")]
    void SaveAllLayoutsButton () {
#if UNITY_EDITOR
        var folderPath = UnityEditor.EditorUtility.SaveFolderPanel("Save All Layouts",
            "","layouts");
        if (folderPath.Length > 0) {
            if (System.IO.Directory.Exists(folderPath) && !IsDirectoryEmpty(folderPath)) {
                Debug.LogError("Abandoned as folder already contains files: please provide an empty folder");
                return;
            }

            System.IO.Directory.CreateDirectory(folderPath);
            for (int i = 0; i < layouts.Count; i++) {
                var path = System.IO.Path.Combine(folderPath,i.ToString() + ".json");
                LayoutUtil.ExportToFile(layouts[layoutIndex],path);
            }
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
    }

    [Button("Import Layouts")]
    void ImportLayoutsButton () {
        ImportLayouts(additive: false);
        UpdateLayoutVisualizer();
    }

    [Button("Import Layouts Additive")]
    void ImportLayoutsAdditiveButton () {
        ImportLayouts(additive: true);
        UpdateLayoutVisualizer();
    }

    [Button("Clear Layouts")]
    void ClearLayoutsButton () {
        layouts.Clear();
        UpdateLayoutVisualizer();
    }

    [Button("Next Layout")]
    void NextLayoutButton () {
        layoutIndex++;

        if (layoutIndex >= layouts.Count) {
            layoutIndex = 0;
        }
        UpdateLayoutVisualizer();
    }

    [Button("Prev Layout")]
    void PrevLayoutButton () {
        layoutIndex--;

        if (layoutIndex < 0) {
            layoutIndex = layouts.Count-1;
        }
        UpdateLayoutVisualizer();
    }

    void ImportLayouts (bool additive) {
        if (!layoutProvider) {
            return;
        }

        if (additive) {
            layoutProvider.ImportAdditive(layouts);
        } else {
            layoutProvider.Import(layouts);
        }
    }

    void OnValidate () {
        UpdateLayoutVisualizer();
    }

    void UpdateLayoutVisualizer () {
        if (!visualizer) {
            return;
        }

        visualizer.layout = null;
        visualizer.mode = TrialVisualizer.Mode.LayoutOnly;


        if (layoutIndex >= 0 && layoutIndex < layouts.Count) {
            visualizer.layout = layouts[layoutIndex];
        }
    }
}
}
