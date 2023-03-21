using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RDWMCExperiment {

public static class LayoutUtil {

static int CalculateMaxLinkCount (int xNodeCount, int zNodeCount) {
    return xNodeCount * (zNodeCount-1) + zNodeCount * (xNodeCount-1);
}

public static Layout ImportFromText(string text) {
    var layout = new Layout();
    ImportFromText(text,layout);
    return layout;
}

public static void ImportFromText(string text, Layout layout) {
    JsonUtility.FromJsonOverwrite(text,layout);
}

public static Layout ImportFromFile(string path) {
    var layout = new Layout();
    ImportFromFile(path,layout);
    return layout;
}

public static void ImportFromFile(string path, Layout layout) {
    using (var sr = new StreamReader(path)) {
        ImportFromText(sr.ReadToEnd(),layout);
    }
}

public static string ExportToText(Layout layout) {
    return JsonUtility.ToJson(layout,true);
}

public static void ExportToFile(Layout layout, string path) {
    using (var sw = new StreamWriter(path)) {
        sw.Write(JsonUtility.ToJson(layout,true));
    }
}
}

}
