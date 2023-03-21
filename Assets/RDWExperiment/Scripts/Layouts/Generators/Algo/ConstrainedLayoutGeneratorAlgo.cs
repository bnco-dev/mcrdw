using System.Collections.Generic;
using UnityEngine;

namespace RDWMCExperiment {
public class ConstrainedLayoutGeneratorAlgo : ILayoutGeneratorAlgo {

class Grid {

    bool[] grid;
    public int xDim { get; private set; }
    public int yDim { get; private set; }

    public Grid (int xDim, int yDim, bool initialValue) {
        this.xDim = xDim;
        this.yDim = yDim;
        grid = new bool[xDim*yDim];
        SetAll(initialValue);
    }

    public bool InBounds (int x, int y) {
        return Index(x,y) >= 0;
    }

    public void Set (int x, int y, bool value) {
        var i = Index(x,y);
        if (i < 0) {
            return;
        }

        grid[i] = value;
    }

    public void SetAll (bool value) {
        for (int i = 0; i < grid.Length; i++) {
            grid[i] = value;
        }
    }

    public bool Get (int x, int y) {
        var i = Index(x,y);
        if (i < 0) {
            return false;
        }

        return grid[i];
    }

    int Index (int x, int y) {
        // Bounds check
        if (x < 0 || x >= xDim || y < 0 || y >= yDim) {
            return -1;
        }

        return x * yDim + y;
    }
}

static readonly Vector2Int[] NEIGHBOUR_NODES = {
    new Vector2Int(2,0),
    new Vector2Int(-2,0),
    new Vector2Int(0,2),
    new Vector2Int(0,-2),
};
static readonly Vector2Int[] NEIGHBOUR_EDGES = {
    new Vector2Int(1,0),
    new Vector2Int(-1,0),
    new Vector2Int(0,1),
    new Vector2Int(0,-1),
};

class Node {
    public List<Node> connectedNodes = new List<Node>();
    public Vector2Int position;
    public Node(Vector2Int position) {
        this.position = position;
    }
}

public ConstrainedLayoutGeneratorAlgo () {}

public int xDim;
public int yDim;
public float edgeFactor;
public float nodeSpacingMeters;
public float pathLengthMeters;
public float maxStraightMeters;
public float maxPathStraightMeters;
public System.Random random;

public void Set (int xDim, int yDim, float edgeFactor, float nodeSpacingMeters,
    float pathLengthMeters, float maxStraightMeters, float maxPathStraightMeters,
    System.Random random) {

    this.xDim = xDim;
    this.yDim = yDim;
    this.edgeFactor = edgeFactor;
    this.nodeSpacingMeters = nodeSpacingMeters;
    this.pathLengthMeters = pathLengthMeters;
    this.maxStraightMeters = maxStraightMeters;
    this.maxPathStraightMeters = maxPathStraightMeters;
    this.random = random;
}

public Layout Generate () {
    var layout = new Layout();
    if (Generate(layout)) {
        return layout;
    }
    return null;
}

List<Vector2Int> _path = new List<Vector2Int>();
List<Vector2Int> _candidateEdges = new List<Vector2Int>();
public bool Generate (Layout layout) {

    if (random == null) {
        random = new System.Random();
    }

    // Nodes are (even,even), edges are (even,odd) and (odd,even)
    // Initially everything is unset
    var grid = new Grid(xDim*2-1,yDim*2-1,initialValue:false);

    // Pick a starting node
    var current = new Vector2Int(random.Next(xDim)*2,random.Next(yDim)*2);
    var straightDirection = Vector2Int.zero;
    var straightDistance = 0.0f;

    grid.Set(current.x,current.y,true);
    while (true) {
        // Conditions for candidate edges:
        // unset, connected to unset node, not straight path > constraint
        _candidateEdges.Clear();
        for (int ei = 0; ei < NEIGHBOUR_EDGES.Length; ei++) {
            var eiOff = NEIGHBOUR_EDGES[ei];

            if (// in bounds?
                grid.InBounds(current.x+eiOff.x,current.y + eiOff.y) &&
                // edge unset?
                !grid.Get(current.x+eiOff.x,current.y + eiOff.y) &&
                // node unset?
                !grid.Get(current.x+2*eiOff.x,current.y + 2*eiOff.y) &&
                // straight line limit not reached?
                !(eiOff == straightDirection && straightDistance + nodeSpacingMeters > maxStraightMeters)) {

                // Conditions satisfied, edge is a candidate
                _candidateEdges.Add(eiOff);
            }
        }

        if (_candidateEdges.Count == 0) {
            // No possible further connections - now to check for correctness
            break;
        }

        var edgeOff = _candidateEdges[random.Next(_candidateEdges.Count)];

        // Update straight distance
        if (edgeOff == straightDirection) {
            straightDistance += nodeSpacingMeters;
        } else {
            straightDistance = nodeSpacingMeters;
            straightDirection = edgeOff;
        }

        grid.Set(current.x + edgeOff.x,current.y + edgeOff.y,true);
        current = current + edgeOff*2;
        grid.Set(current.x,current.y,true);
    }

    // Check for correctness (just make sure every node is set)
    for (int x = 0; x < grid.xDim; x += 2) {
        for (int y = 0; y < grid.yDim; y += 2) {
            if (!grid.Get(x,y)) {
                return false;
            }
        }
    }

    // We have a correct minimal layout

    // Gather missing edges
    _candidateEdges.Clear();
    // Vertical edges
    for (int x = 0; x < grid.xDim; x += 2) {
        for (int y = 1; y < grid.yDim; y += 2) {
            if (!grid.Get(x,y)) {
                _candidateEdges.Add(new Vector2Int(x,y));
            }
        }
    }
    // Horizontal edges
    for (int x = 1; x < grid.xDim; x += 2) {
        for (int y = 0; y < grid.yDim; y += 2) {
            if (!grid.Get(x,y)) {
                _candidateEdges.Add(new Vector2Int(x,y));
            }
        }
    }

    // Add some missing edges based on edge-factor
    Mathf.Clamp01(edgeFactor);
    var edgesToAddCount = Mathf.FloorToInt(_candidateEdges.Count * edgeFactor);
    for (int i = 0; i < edgesToAddCount; i++) {
        var ei = random.Next(_candidateEdges.Count);
        var edge = _candidateEdges[ei];
        _candidateEdges.RemoveAt(ei);
        grid.Set(edge.x,edge.y,true);
    }

    // Convert to graph form
    var nodes = new List<Node>();
    var nodeFromGridPos = new Dictionary<Vector2Int,Node>();
    for (int x = 0; x < grid.xDim; x += 2) {
        for (int y = 0; y < grid.yDim; y += 2) {
            // var pos = new Vector3(x/2,0,y/2)*spaceBetweenNodesMeters;
            var node = new Node(new Vector2Int(x/2,y/2));
            nodes.Add(node);
            nodeFromGridPos[new Vector2Int(x,y)] = node;
        }
    }
    // Add edges
    for (int ni = 0; ni < nodes.Count; ni++) {
        var node = nodes[ni];
        for (int ei = 0; ei < NEIGHBOUR_EDGES.Length; ei++) {
            var edgeIOffs = NEIGHBOUR_EDGES[ei];
            var xy = new Vector2Int(node.position.x*2,node.position.y*2);
            if (!grid.Get(xy.x+edgeIOffs.x,xy.y+edgeIOffs.y)) {
                continue;
            }

            var nodeIOffs = NEIGHBOUR_NODES[ei];
            var idx = new Vector2Int(xy.x+nodeIOffs.x,xy.y+nodeIOffs.y);
            node.connectedNodes.Add(nodeFromGridPos[idx]);
        }
    }
    //Remove unnecessary nodes (nodes on straight lines) and cleanup
    for (var i = 0; i < nodes.Count; i++) {
        var node = nodes[i];
        if (node.connectedNodes.Count == 2) {
            var nodeA = node.connectedNodes[0];
            var nodeB = node.connectedNodes[1];
            var delta = nodeA.position - nodeB.position;
            if (delta.x == 0 || delta.y == 0) {
                // We're on a straight line, remove the node
                nodeA.connectedNodes.Remove(node);
                nodeA.connectedNodes.Add(nodeB);
                nodeB.connectedNodes.Remove(node);
                nodeB.connectedNodes.Add(nodeA);
                nodes.RemoveAt(i);
                i--;
            }
        }
    }

    // Generate path
    // (pick a start node randomly)
    // (then randomly choose a new node and generate shortest path)
    // (walk along path until time runs out or node reached)
    // (if time out, stop. if node reached, choose a new node, repeat)
    var currentNodeI = random.Next(0,nodes.Count);
    var targetNodeI = random.Next(0,nodes.Count-1);

    if (targetNodeI >= currentNodeI) {
        targetNodeI++;
    }

    var currentNode = nodes[currentNodeI];
    var targetNode = nodes[targetNodeI];
    var length = 0.0f;
    var path = new List<Vector2Int>();
    path.Add(currentNode.position);
    var pathLegNodes = new List<Node>();
    while (length < pathLengthMeters) {
        OptimalPath(pathLegNodes,nodes,currentNode,targetNode);

        // Pop start node (already added)
        pathLegNodes.RemoveAt(pathLegNodes.Count-1);
        while (pathLegNodes.Count > 0 && length < pathLengthMeters) {
            var pathLegNode = pathLegNodes[pathLegNodes.Count - 1];
            path.Add(pathLegNode.position);
            pathLegNodes.RemoveAt(pathLegNodes.Count-1);
            // Calculate distance
            var xDelta = pathLegNode.position.x - currentNode.position.x;
            var yDelta = pathLegNode.position.y - currentNode.position.y;
            length += nodeSpacingMeters*(Mathf.Abs(xDelta) + Mathf.Abs(yDelta));
            currentNode = pathLegNode;
        }

        currentNodeI = targetNodeI;
        targetNodeI = random.Next(0,nodes.Count-1);
        if (targetNodeI >= currentNodeI) {
            targetNodeI++;
        }

        currentNode = nodes[currentNodeI];
        targetNode = nodes[targetNodeI];
    }

    // Apply path straight line constraint
    var pathStraightDirection = Vector2Int.zero;
    var pathStraightDistance = 0.0f;
    for (int i = 0; i < path.Count-1; i++) {
        var to = path[i+1] - path[i];
        var dir = new Vector2Int(Sign(to.x),Sign(to.y));
        var dist = nodeSpacingMeters *
            (to.x != 0 ? Mathf.Abs(to.x) : Mathf.Abs(to.y));

        if (dir == pathStraightDirection) {
            pathStraightDistance += dist;
        } else {
            pathStraightDistance = dist;
            pathStraightDirection = dir;
        }

        if (pathStraightDistance > maxPathStraightMeters) {
            return false;
        }
    }

    // Build edge list
    var edges = new List<Vector2Int>();
    for (int ni = 0; ni < nodes.Count; ni++) {
        var node = nodes[ni];
        for (int ei = 0; ei < node.connectedNodes.Count; ei++) {
            var otheri = nodes.IndexOf(node.connectedNodes[ei]);
            if (!ContainsEdge(edges,ni,otheri)) {
                AddEdge(edges,ni,otheri);
            }
        }
    }

    // Build wall list
    // Walls are just line segments (pairs of points)
    // Wherever two nodes are not connected, there should be a wall
    // To build list, scan down every edge row and column
    // Combine adjacent wall segments into one wall
    var walls = new List<Vector2>();
    var writing = false;
    // Horizontal edges
    for (int y = 1; y < grid.yDim; y+= 2) {
        for (int x = 0; x < grid.xDim; x += 2) {
            var edgePresent = grid.Get(x,y);
            if (!edgePresent && !writing) {
                writing = true;
                walls.Add(new Vector2((x/2)-0.5f,(y/2)+0.5f));
            } else if (edgePresent && writing) {
                writing = false;
                walls.Add(new Vector2((x/2)-0.5f,(y/2)+0.5f));
            }
        }
        if (writing) {
            writing = false;
            walls.Add(new Vector2(((grid.xDim+1)/2)-0.5f,(y/2)+0.5f));
        }
    }
    // Vertical edges
    for (int x = 1; x < grid.xDim; x+= 2) {
        for (int y = 0; y < grid.yDim; y += 2) {
            var edgePresent = grid.Get(x,y);
            if (!edgePresent && !writing) {
                writing = true;
                walls.Add(new Vector2((x/2)+0.5f,(y/2)-0.5f));
            } else if (edgePresent && writing) {
                writing = false;
                walls.Add(new Vector2((x/2)+0.5f,(y/2)-0.5f));
            }
        }
        if (writing) {
            writing = false;
            walls.Add(new Vector2((x/2)+0.5f,((grid.yDim+1)/2)-0.5f));
        }
    }

    // We have everything we need now, assemble layout class
    layout.nodePositions = new float[3*nodes.Count];
    for (int i = 0; i < nodes.Count; i++) {
        var node = nodes[i];
        layout.nodePositions[i*3]   = node.position.x * nodeSpacingMeters;
        layout.nodePositions[i*3+1] = 0.0f;
        layout.nodePositions[i*3+2] = node.position.y * nodeSpacingMeters;
    }
    layout.edgeNodeIndexes = new uint[2*edges.Count];
    for (int i = 0; i < edges.Count; i++) {
        layout.edgeNodeIndexes[i*2]   = (uint)edges[i].x;
        layout.edgeNodeIndexes[i*2+1] = (uint)edges[i].y;
    }
    layout.wallPositions = new float[3*walls.Count];
    for (int i = 0; i < walls.Count; i++) {
        var wallPosition = walls[i];
        layout.wallPositions[i*3]   = wallPosition.x * nodeSpacingMeters;
        layout.wallPositions[i*3+1] = 0.0f;
        layout.wallPositions[i*3+2] = wallPosition.y * nodeSpacingMeters;
    }
    layout.pathPositions = new float[3*path.Count];
    for (int i = 0; i < path.Count; i++) {
        var pathPosition = path[i];
        layout.pathPositions[i*3]   = pathPosition.x * nodeSpacingMeters;
        layout.pathPositions[i*3+1] = 0.0f;
        layout.pathPositions[i*3+2] = pathPosition.y * nodeSpacingMeters;
    }
    return true;
}

static int Sign (int x) {
    if (x > 0) {
        return 1;
    } else if (x == 0) {
        return 0;
    } else {
        return -1;
    }
}

bool ContainsEdge (List<Vector2Int> edges, int node1Idx, int node2Idx) {
    var minIdx = Mathf.Min((int)node1Idx,(int)node2Idx);
    var maxIdx = Mathf.Max((int)node1Idx,(int)node2Idx);
    return edges.Contains(new Vector2Int(minIdx,maxIdx));
}

void AddEdge (List<Vector2Int> edges, int node1Idx, int node2Idx) {
    var minIdx = Mathf.Min((int)node1Idx,(int)node2Idx);
    var maxIdx = Mathf.Max((int)node1Idx,(int)node2Idx);
    edges.Add(new Vector2Int(minIdx,maxIdx));
}

class PathBuildNode {
    public Node node;
    public PathBuildNode parent;
    public float cost;
    public PathBuildNode (Node node, PathBuildNode parent, float cost) {
        this.node = node;
        this.parent = parent;
        this.cost = cost;
    }
}

bool OptimalPath(List<Node> outPath, List<Node> nodes,
    Node startNode, Node destNode) {
    outPath.Clear();
    var unvisitedPathBuildNodes = new List<PathBuildNode>();
    var nodeToPathBuildNode = new Dictionary<Node,PathBuildNode>();

    // Dumb version of Djikstra's
    unvisitedPathBuildNodes.Add(new PathBuildNode (startNode,null,0));
    nodeToPathBuildNode[startNode] = unvisitedPathBuildNodes[0];

    while (true) {
        if (unvisitedPathBuildNodes.Count == 0) {
            return false;
        }

        var minCost = Mathf.Infinity;
        var minCostIdx = -1;
        for (int i = 0; i < unvisitedPathBuildNodes.Count; i++) {
            var node = unvisitedPathBuildNodes[i];
            if (node.cost < minCost) {
                minCost = node.cost;
                minCostIdx = i;
            }
        }

        var nextPbn = unvisitedPathBuildNodes[minCostIdx];
        unvisitedPathBuildNodes.RemoveAt(minCostIdx);

        if (nextPbn.node == destNode) {
            // Reached the destination, build (reversed) path and return
            for (var pbn = nextPbn; pbn != null; pbn = pbn.parent) {
                outPath.Add(pbn.node);
            }
            return true;
        }

        // f.e. neighbour of new node, update costs/add to unvisited
        for (int i = 0; i < nextPbn.node.connectedNodes.Count; i++) {
            var n = nextPbn.node.connectedNodes[i];

            if (!nodeToPathBuildNode.TryGetValue(n,out PathBuildNode pbn)) {
                // Node not yet seen, add to node dict and unvisited list
                var newPbn = new PathBuildNode(n,nextPbn,nextPbn.cost+1);
                unvisitedPathBuildNodes.Add(newPbn);
                nodeToPathBuildNode.Add(n,newPbn);
            } else {
                // Node seen before, revise cost/path if lower
                if (pbn.cost > nextPbn.cost+1) {
                    pbn.parent = nextPbn;
                    pbn.cost = nextPbn.cost+1;
                }
                // Shouldn't need to revise if we've visited before
                // But shouldn't do any harm (as will never be lower)
            }
        }
    }
}

}
}