using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RDWMonteCarloMethodVisualizer : MonoBehaviour {

    [System.Serializable]
    public class RedirectVisualObjects {
        public Text rotationText;
        public Text scaleText;
        public Text simulationCountText;
        public Text valueText;
        public UnityEngine.UI.Image selectionIndicator;
    }

    public RDWMonteCarloMethod method;
    public List<RedirectVisualObjects> visualObjects;
    public Color lowValueColor = Color.red;
    public Color highValueColor = Color.green;

    void Update () {
        for (int i = 0; i < method._currentRedirects.Count; i++) {
            var redirect = method._currentRedirects[i];
            var col = Color.Lerp(lowValueColor,highValueColor,redirect.value);
            visualObjects[i].rotationText.color = col;
            visualObjects[i].scaleText.color = col;
            visualObjects[i].simulationCountText.color = col;

            // Allocates memory
            var simCount = redirect.simulationCount.ToString();
            visualObjects[i].simulationCountText.text = "Sims: " + simCount;
            var value = redirect.value.ToString();
            visualObjects[i].valueText.text = "Value: " + value;
            // Debug.Log(redirect.ToString() + " " + simCount.ToString() + " " + value.ToString());

            visualObjects[i].selectionIndicator.enabled = false;
        }

        var bestValue = -1.0f;
        var bestRedirectIdx = -1;
        for (int i = 0; i < method._currentRedirects.Count; i++) {
            var redirect = method._currentRedirects[i];
            if (redirect.value > bestValue) {
                bestValue = redirect.value;
                bestRedirectIdx = i;
            }
        }

        if (bestRedirectIdx >= 0) {
            visualObjects[bestRedirectIdx].selectionIndicator.enabled = true;
        }
    }
}


//     public Color nodeConnectColor = Color.white;
//     public Color rootNodeColor = Color.white;
//     public Color walkNodeColor = Color.cyan;
//     public Color redirectNodeColor = Color.blue;
//     public Color zeroValueColor = Color.red;
//     public Color oneValueColor = Color.green;

//     public RDWMonteCarloTreeMethod method;
//     public float width;
//     public float heightBetweenNodes;
//     public float nodeScale = 0.2f;

//     List<RDWMonteCarloTreeMethodVisualizeNode> _nodes =
//         new List<RDWMonteCarloTreeMethodVisualizeNode>();
//     int _activeNodeCount;

//     // Stack<RDWMonteCarloMethod.INode> _toVisitStack =
//     //     new Stack<RDWMonteCarloMethod.INode>();
//     Stack<NodeInfo> _toVisitStack = new Stack<NodeInfo>();

//     Material _rootMaterial;
//     Material _walkMaterial;
//     Material _redirectMaterial;

//     void OnDestroy () {
//         ReleaseMaterials();
//         ReleaseNodes();
//     }

//     void Update () {
//         GatherMaterials ();
//         ReturnNodesToPool();

//         if ()

//         // Add root node info
//         _toVisitStack.Clear();
//         _toVisitStack.Push(new NodeInfo {
//             node = method.treeRoot,
//             position = Vector3.zero,
//             columnWidth = width
//         });

//         while (_toVisitStack.Count > 0) {
//             var nodeInfo = _toVisitStack.Pop();

//             // Setup viznode
//             var vizNode = ObtainVizNode();
//             var vizNodeTransform = vizNode.transform;
//             SetVizNode (vizNode,nodeInfo.node);

//             // Position viznode
//             vizNodeTransform.parent = transform;
//             vizNodeTransform.localPosition = nodeInfo.position;
//             var vizNodeScale = Mathf.Min(
//                 nodeScale * nodeInfo.columnWidth,
//                 nodeScale * heightBetweenNodes);
//             vizNodeTransform.localScale = Vector3.one * vizNodeScale;

//             // Setup children
//             var childCount = nodeInfo.node.GetChildCount();
//             var childColumnWidth = nodeInfo.columnWidth / childCount;
//             var childHeight = nodeInfo.position.y - heightBetweenNodes;
//             var minX = nodeInfo.position.x - (nodeInfo.columnWidth/2.0f);
//             for (int i = 0; i < childCount; i++) {
//                 var childPos = new Vector3 (
//                     x: minX + childColumnWidth * (i+0.5f),
//                     y: childHeight,
//                     z: 0
//                 );
//                 _toVisitStack.Push(new NodeInfo {
//                     node = nodeInfo.node.GetChild(i),
//                     position = childPos,
//                     parentVizNodeTransform = vizNodeTransform,
//                     columnWidth = childColumnWidth
//                 });
//             }

//             // Connect to parent visually
//             if (nodeInfo.parentVizNodeTransform != null) {
//                 var col = Color.Lerp(zeroValueColor,oneValueColor,
//                     GetNodeValue(nodeInfo.node));
//                 Debug.DrawLine(vizNodeTransform.position,
//                     nodeInfo.parentVizNodeTransform.position,col,0.0f);
//             }
//         }
//     }

//     void GatherMaterials () {
//         if (!_rootMaterial) {
//             _rootMaterial = ConstructMaterial(rootNodeColor);
//         }
//         if (!_walkMaterial) {
//             _walkMaterial = ConstructMaterial(walkNodeColor);
//         }
//         if (!_redirectMaterial) {
//             _redirectMaterial = ConstructMaterial(redirectNodeColor);
//         }
//     }

//     void ReleaseMaterials () {
//         if (_rootMaterial != null) {
//             DestroyImmediate(_rootMaterial);
//         }
//         if (_walkMaterial != null) {
//             DestroyImmediate(_walkMaterial);
//         }
//         if (_redirectMaterial != null) {
//             DestroyImmediate(_redirectMaterial);
//         }
//     }

//     void ReleaseNodes () {
//         for (int i = 0; i < _nodes.Count; i++) {
//             if (_nodes[i] && _nodes[i] != null) {
//                 Destroy(_nodes[i]);
//             }
//         }
//         _nodes.Clear();
//     }

//     Material ConstructMaterial (Color color) {
//         var material = new Material(Shader.Find("Standard"));
//         material.color = color;
//         material.hideFlags = HideFlags.DontSave;
//         return material;
//     }

//     void SetVizNode (
//         RDWMonteCarloTreeMethodVisualizeNode vizNode,
//         RDWMonteCarloTreeMethod.INode node) {

//         var nodeType = GetNodeType(node);
//         if (nodeType == RDWMonteCarloTreeMethodVisualizeNode.NodeType.Root) {
//             SetVizNode(vizNode,(RDWMonteCarloTreeMethod.RootNode)node);
//         } else if (nodeType == RDWMonteCarloTreeMethodVisualizeNode.NodeType.Walk) {
//             SetVizNode(vizNode,(RDWMonteCarloTreeMethod.WalkNode)node);
//         } else {
//             SetVizNode(vizNode,(RDWMonteCarloTreeMethod.RedirectNode)node);
//         }
//     }

//     void SetVizNode (
//         RDWMonteCarloTreeMethodVisualizeNode vizNode,
//         RDWMonteCarloTreeMethod.RootNode node) {

//         vizNode.nodeType = RDWMonteCarloTreeMethodVisualizeNode.NodeType.Root;
//         vizNode.rootNodeVars.time = node.time;
//         vizNode.rootNodeVars.graphNodePos = node.graphNode.position;
//         vizNode.rootNodeVars.worldPos = node.worldPos;
//         vizNode.rootNodeVars.worldDir = node.worldDir;
//         vizNode.rootNodeVars.trackPos = node.trackPos;
//         vizNode.rootNodeVars.trackDir = node.trackDir;
//         vizNode.SetMaterial(_rootMaterial);
//     }

//     void SetVizNode (
//         RDWMonteCarloTreeMethodVisualizeNode vizNode,
//         RDWMonteCarloTreeMethod.WalkNode node) {

//         vizNode.nodeType = RDWMonteCarloTreeMethodVisualizeNode.NodeType.Walk;
//         vizNode.walkNodeVars.time = node.time;
//         vizNode.walkNodeVars.probability = node.probability;
//         vizNode.walkNodeVars.graphNodePos = node.graphNode.position;
//         vizNode.SetMaterial(_walkMaterial);
//     }

//     void SetVizNode (
//         RDWMonteCarloTreeMethodVisualizeNode vizNode,
//         RDWMonteCarloTreeMethod.RedirectNode node) {

//         vizNode.nodeType = RDWMonteCarloTreeMethodVisualizeNode.NodeType.Redirect;
//         vizNode.redirectNodeVars.rotationDirection= node.instructionOption.instruction.rotationDirection;
//         vizNode.redirectNodeVars.scaleDirection = node.instructionOption.instruction.scaleDirection;
//         vizNode.redirectNodeVars.instructionValue = node.instructionOption.value;
//         vizNode.redirectNodeVars.trackPos = node.trackPos;
//         vizNode.redirectNodeVars.trackDir = node.trackDir;
//         vizNode.redirectNodeVars.simulationCount = node.simulationCount;
//         vizNode.redirectNodeVars.simulationValue = node.simulationValue;
//         vizNode.redirectNodeVars.simulationValueSum = node.simulationValueSum;
//         vizNode.SetMaterial(_redirectMaterial);
//     }

//     float GetNodeValue (RDWMonteCarloTreeMethod.INode node) {
//         if (node is RDWMonteCarloTreeMethod.RootNode) {
//             return 1.0f;
//         } else if (node is RDWMonteCarloTreeMethod.WalkNode) {
//             return ((RDWMonteCarloTreeMethod.WalkNode)node).probability;
//         } else if (node is RDWMonteCarloTreeMethod.RedirectNode) {
//             return ((RDWMonteCarloTreeMethod.RedirectNode)node).simulationValue;
//         } else {
//             Debug.LogError("Unknown node type");
//             return 0.0f;
//         }
//     }

//     RDWMonteCarloTreeMethodVisualizeNode.NodeType GetNodeType (
//         RDWMonteCarloTreeMethod.INode node) {

//         if (node is RDWMonteCarloTreeMethod.RootNode) {
//             return RDWMonteCarloTreeMethodVisualizeNode.NodeType.Root;
//         } else if (node is RDWMonteCarloTreeMethod.WalkNode) {
//             return RDWMonteCarloTreeMethodVisualizeNode.NodeType.Walk;
//         } else if (node is RDWMonteCarloTreeMethod.RedirectNode) {
//             return RDWMonteCarloTreeMethodVisualizeNode.NodeType.Redirect;
//         } else {
//             Debug.LogError("Unknown node type");
//             return RDWMonteCarloTreeMethodVisualizeNode.NodeType.Root;
//         }
//     }

//     void ReturnNodesToPool () {
//         for (int i = 0; i < _activeNodeCount; i++) {
//             if (_nodes[i] && _nodes[i] != null) {
//                 Hide(_nodes[i]);
//             }
//         }
//         _activeNodeCount = 0;
//     }

//     RDWMonteCarloTreeMethodVisualizeNode ObtainVizNode () {
//         _activeNodeCount++;
//         if (_activeNodeCount > _nodes.Count) {
//             _nodes.Add(ConstructVizNode());
//         }

//         if (!_nodes[_activeNodeCount-1] || _nodes[_activeNodeCount-1] == null) {
//             _nodes[_activeNodeCount-1] = ConstructVizNode();
//         }

//         Show(_nodes[_activeNodeCount-1]);
//         return _nodes[_activeNodeCount-1];
//     }

//     RDWMonteCarloTreeMethodVisualizeNode ConstructVizNode () {
//         var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//         go.name = "RDWMCM Viz Node";
//         go.hideFlags = HideFlags.DontSave;
//         // go.transform.parent = transform;
//         return go.AddComponent<RDWMonteCarloTreeMethodVisualizeNode>();
//     }

//     void Hide (RDWMonteCarloTreeMethodVisualizeNode vizNode) {
//         vizNode.gameObject.SetActive(false);
//     }

//     void Show (RDWMonteCarloTreeMethodVisualizeNode vizNode) {
//         vizNode.gameObject.SetActive(true);
//     }

//     // int FindMaxDepth (RDWMonteCarloMethod.RootNode root) {

//     //     while ()
//     //     return 0;
//     // }
// }

// #if UNITY_EDITOR
// [UnityEditor.CustomEditor(typeof(RDWMonteCarloTreeMethodVisualizer))]
// public class RDWMonteCarloMethodVisualizerEditor : UnityEditor.Editor {

//     public override void OnInspectorGUI () {
//         DrawDefaultInspector();
//         // HandleSelection();

//         // if (Selection)

//         // var up = Input.GetKeyDown(KeyCode.UpArrow);
//         // var down = Input.GetKeyDown(KeyCode.DownArrow);
//         // var left = Input.GetKeyDown(KeyCode.LeftArrow);
//         // var right = Input.GetKeyDown(KeyCode.RightArrow);

//         // if (Input.GetKeyDown(KeyCode.UpArrow)) {

//         // }

//         // if (UnityEditor.Selection.activeGameObject)
//     }


// }
// #endif