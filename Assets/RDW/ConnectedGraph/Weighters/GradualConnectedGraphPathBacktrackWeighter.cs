using System.Collections.Generic;
using UnityEngine;

public class ConnectedGraphTimeBacktrackNodeWeighter : ConnectedGraphNodeWeighter {
    public float probabilityDecayPerSecond { get; set; }
    public float probabilityRecoverPerSecond { get; set; }
    public float probabilityMinimum { get; set; }

    List<ConnectedGraph.Node> memory = new List<ConnectedGraph.Node>();

    void RefreshMemory () {
        memory.Clear();
        foreach (var kvp in weights) {
            memory.Add(kvp.Key);
        }
    }

    public override void Reset () {
        base.Reset();
        memory.Clear();
    }

    public override void SetAllWeights (Dictionary<ConnectedGraph.Node,float> newWeights) {
        base.SetAllWeights(newWeights);
        RefreshMemory();
    }

    public override void CopyWeights (ConnectedGraphNodeWeighter other) {
        base.CopyWeights(other);
        RefreshMemory();
    }

    public override void Visit (ConnectedGraph.Node node, float seconds) {

        for (int i = 0; i < memory.Count; i++) {
            if (memory[i] == node) {
                continue;
            }

            var recover = probabilityRecoverPerSecond * seconds;
            var newWeight = weights[memory[i]] + recover;
            weights[memory[i]] = newWeight;

            if (newWeight >= 1.0f) {
                weights.Remove(memory[i]);
                memory.RemoveAt(i);
                i--;
            }
        }

        if (!memory.Contains(node)) {
            memory.Add(node);
            weights.Add(node,1.0f);
        }

        var decay = probabilityDecayPerSecond * seconds;
        weights[node] = Mathf.Max(weights[node] - decay,probabilityMinimum);
    }
}

