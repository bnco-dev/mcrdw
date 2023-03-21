using System.Collections.Generic;

public class ConnectedGraphStepBacktrackNodeWeighter : ConnectedGraphNodeWeighter {
    public float visitProbabilityMultiplier { get; set; }
    public float probabilityRecoverRate { get; set; }

    List<ConnectedGraph.Node> memory = new List<ConnectedGraph.Node>();

    void RefreshMemory () {
        memory.Clear();
        foreach (var kvp in weights) {
            memory.Add(kvp.Key);
        }
    }

    public override void Reset () {
        weights.Clear();
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

        if (probabilityRecoverRate > 0.0f) {
            for (int i = 0; i < memory.Count; i++) {
                var newWeight = weights[memory[i]] + probabilityRecoverRate;
                weights[memory[i]] = newWeight;

                if (newWeight >= 1.0f) {
                    memory.RemoveAt(i);
                    i--;
                }
            }
        }

        if (visitProbabilityMultiplier < 1.0f &&
            !memory.Contains(node)) {

            weights[node] = visitProbabilityMultiplier;
            memory.Add(node);
        }
    }
}

