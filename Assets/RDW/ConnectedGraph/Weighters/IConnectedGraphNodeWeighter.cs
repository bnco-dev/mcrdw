using System.Collections.Generic;

public abstract class ConnectedGraphNodeWeighter {

    protected Dictionary<ConnectedGraph.Node,float> weights =
        new Dictionary<ConnectedGraph.Node, float>();

    public virtual void Reset () {
        weights.Clear();
    }

    public virtual float GetWeight (ConnectedGraph.Node node) {
        if (weights.TryGetValue (node, out float value)) {
            return value;
        }
        return 1.0f;
    }

    public virtual void CopyWeights (ConnectedGraphNodeWeighter other) {
        weights.Clear();
        other.GetAllWeights(weights);
    }

    public virtual void GetAllWeights (Dictionary<ConnectedGraph.Node,float> outWeights) {
        foreach (var kvp in weights) {
            outWeights.Add(kvp.Key,kvp.Value);
        }
    }

    public virtual void SetAllWeights (Dictionary<ConnectedGraph.Node,float> newWeights) {
        weights.Clear();
        foreach (var kvp in newWeights) {
            weights.Add(kvp.Key,kvp.Value);
        }
    }

    public abstract void Visit (ConnectedGraph.Node node, float seconds);
}