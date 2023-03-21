using System.Collections.Generic;
using UnityEngine;

public class ConnectedGraphWeightedPathGenerator {

    public ConnectedGraph.Node currentNode { get; private set; }
    public ConnectedGraph.Node nextNode { get; private set; }
    public ConnectedGraphNodeWeighter weighter { get; private set; }
    public ConnectedGraph graph { get; private set; }
    public System.Random random { get; private set; }

    public void Setup (System.Random random, ConnectedGraph graph,
        ConnectedGraphNodeWeighter weighter, ConnectedGraph.Node startNode) {

        this.random = random;
        this.graph = null;
        this.weighter = null;
        this.currentNode = null;
        this.nextNode = null;

        if (graph == null || weighter == null) {
            return;
        }

        this.graph = graph;
        this.weighter = weighter;
        this.currentNode = startNode;

        if (startNode == null) {
            startNode = GetRandomNode(random,graph.nodes);
        }
    }

    List<ConnectedGraph.Node> _neighboursTmp = new List<ConnectedGraph.Node>();
    public bool Advance (float secondsAtCurrent, float secondsAtNext) {
        if (nextNode == null) {
            return false;
        }

        weighter.Visit(currentNode,secondsAtCurrent);
        currentNode = nextNode;
        nextNode = null;
        weighter.Visit(currentNode,secondsAtNext);

        return true;
    }

    public bool Predict () {
        if (nextNode != null) {
            return true;
        }

        _neighboursTmp.Clear();
        graph.GetNeighboursNoAlloc(currentNode,_neighboursTmp);
        if (_neighboursTmp.Count == 0) {
            return false;
        }

        nextNode = GetRandomNodeWeighted(random,_neighboursTmp);
        return true;
    }

    static ConnectedGraph.Node GetRandomNode (
        System.Random random,
        List<ConnectedGraph.Node> nodes) {
        return nodes[random.Next(nodes.Count)];
    }

    ConnectedGraph.Node GetRandomNodeWeighted (
        System.Random random,
        List<ConnectedGraph.Node> nodes) {

        var weightSum = 0.0f;
        for (int i = 0; i < nodes.Count; i++) {
            weightSum += weighter.GetWeight(nodes[i]);
        }

        // rval is between 0 (inclusive) 1 (exclusive)
        var rval = random.NextDouble();
        var weightSumRecip = 1.0f / weightSum;
        for (int i = 0; i < nodes.Count; i++) {
            var p = weighter.GetWeight(nodes[i]) * weightSumRecip;
            if (p > rval) {
                return nodes[i];
            } else {
                rval -= p;
            }
        }

        // Handle case where rval is exactly 1 or empty nodes
        if (nodes.Count > 0) {
            return nodes[nodes.Count-1];
        } else {
            return null;
        }
    }

}