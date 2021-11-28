using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using WolvenKit.RED4.CR2W;
using WolvenKit.Common.Model.Cr2w;
using WolvenKit.RED4.CR2W.Reflection;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest
{
    using AddSocketFn = Action<string /* name */, CP77Types.Enums.questSocketType /* socketType */>;

    static partial class NodeFactory
    {
        public static IReadOnlyList<Editor.IGraphNodeCreateParams> NodesCreateParamsList => _nodesCreateParamsList;
        static List<Editor.IGraphNodeCreateParams> _nodesCreateParamsList;

        static NodeFactory()
        {
            _nodesCreateParamsList = new List<Editor.IGraphNodeCreateParams>();

            var allQuestNodes = AssemblyDictionary.GetSubClassesOf(typeof(CP77Types.questNodeDefinition));
            foreach (var questNodeType in allQuestNodes)
            {
                if (questNodeType == typeof(CP77Types.questDisableableNodeDefinition)
                    || questNodeType == typeof(CP77Types.questStartEndNodeDefinition)
                    || questNodeType == typeof(CP77Types.questIONodeDefinition)
                    || questNodeType == typeof(CP77Types.questSignalStoppingNodeDefinition)
                    || questNodeType == typeof(CP77Types.questTypedSignalStoppingNodeDefinition)
                    || questNodeType == typeof(CP77Types.questEmbeddedGraphNodeDefinition)
                    || questNodeType == typeof(CP77Types.questAICommandNodeBase)
                    || questNodeType == typeof(CP77Types.questLogicalBaseNodeDefinition)
                    || questNodeType == typeof(CP77Types.questConfigurableAICommandNode)
                    || questNodeType == typeof(CP77Types.questBaseObjectNodeDefinition))
                {
                    continue;
                }

                _nodesCreateParamsList.Add(new CP77NodeCreateParams(questNodeType, CreateGraphNode));
            }
        }

        public static CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> CreateQuestNode(long id, string typeName, CR2WFile cr2wFile, CP77Types.CVariable parent = null, string uniqueName = null)
        {
            if (id > ushort.MaxValue)
            {
                MainWindow.ShowPopup("Can't create more than 65535 nodes");
                return null;
            }

            var nodeChunk = cr2wFile.CreateChunkEx(typeName);
            var nodeHandle = new CP77Types.CHandle<CP77Types.graphGraphNodeDefinition>(cr2wFile, parent, uniqueName ?? string.Empty);
            nodeHandle.SetReference(nodeChunk);

            var questNode = (CP77Types.questNodeDefinition)nodeHandle.GetInstance();
            questNode.Id.IsSerialized = true;
            questNode.Id.Value = (ushort)id;

            return nodeHandle;
        }

        public static CP77Types.CHandle<CP77Types.graphGraphSocketDefinition> CreateQuestSocket(string name, CP77Types.Enums.questSocketType socketType, CR2WFile cr2wFile, CP77Types.CVariable parent = null, string uniqueName = null)
        {
            var socketChunk = cr2wFile.CreateChunkEx("questSocketDefinition");
            var socketHandle = new CP77Types.CHandle<CP77Types.graphGraphSocketDefinition>(cr2wFile, parent, uniqueName ?? string.Empty);
            socketHandle.SetReference(socketChunk);

            var questSocket = socketHandle.GetInstance<CP77Types.questSocketDefinition>();
            questSocket.Name.IsSerialized = true;
            questSocket.Name.Value = name;
            questSocket.Type.IsSerialized = true;
            questSocket.Type.Value = socketType;

            return socketHandle;
        }

        public static void AddNodeSockets(IEditableVariable obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            var bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;
            //for (Type currentType = obj.GetType(); currentType != null && currentType != typeof(CP77Types.questNodeDefinition); currentType = currentType.BaseType)
            {
                Type currentType = obj.GetType();
                var method = typeof(NodeFactory).GetMethod($"AddNodeSockets_{currentType.Name}", bindingFlags);
                if (method != null)
                {
                    method.Invoke(null, new object[] { Convert.ChangeType(obj, currentType), addInputSocket, addOutputSocket });
                    return;
                }
            }            
        }

        public static GraphNode CreateGraphNodeByReflection(CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> nodeHandle)
        {
            var nodeDef = nodeHandle.GetInstance<CP77Types.questNodeDefinition>();
            var id = (long)nodeDef.Id.Value;
            var name = nodeDef.REDType;
            GraphNode node = null;

            var currentAssembly = Assembly.GetExecutingAssembly();
            for (var nodeDefType = nodeDef.GetType(); nodeDefType != typeof(CP77Types.graphIGraphObjectDefinition); nodeDefType = nodeDefType.BaseType)
            {
                var nodeType = currentAssembly.GetType($"{typeof(Graph).Namespace}.CustomNodes.{nodeDefType.Name}");
                if (nodeType == null)
                    continue;

                var @params = new object[3] { nodeHandle, id, name };
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                node = (GraphNode)Activator.CreateInstance(nodeType, bindingFlags, null, @params, null);
            }

            if (node == null)
            {
                node = new GraphNode(nodeHandle, id, name);
            }
            return node;
        }

        public static GraphSocket CreateGraphSocket(long id, string name, CP77Types.Enums.questSocketType socketType, CP77Types.graphGraphNodeDefinition parentNode)
        {
            var socketHandle = CreateQuestSocket(name, socketType, (CR2WFile)parentNode.Cr2wFile, parentNode.Sockets);
            return CreateGraphSocket(id, name, socketHandle);
        }

        public static GraphSocket CreateGraphSocket(long id, string name, CP77Types.CHandle<CP77Types.graphGraphSocketDefinition> socketHandle)
        {
            return new GraphSocket(socketHandle, id, name);
        }

        public static GraphNode CreateGraphNode(string typeName, Vector2 position)
        {
            var graph = (Graph)Editor.GraphContext.CurrentContext.Graph;
            var nodeHandle = CreateQuestNode(graph.GetNextNodeID(), typeName, graph.CR2WFile, graph.GraphDef.Nodes);
            if (nodeHandle == null)
                return null;

            var node = CreateGraphNodeByReflection(nodeHandle);
            AddNodeSockets(nodeHandle.Reference.data,
                (name, socketType) => node.AddInputSocket(CreateGraphSocket(node.GetNextInputSocketID(), name, socketType, node.NodeDef)),
                (name, socketType) => node.AddOutputSocket(CreateGraphSocket(node.GetNextOutputSocketID(), name, socketType, node.NodeDef)));
            node.Position = position;
            return node;
        }

        static void CreateGraphNode(Vector2 position, CP77NodeCreateParams @params)
        {
            var graph = (Graph)Editor.GraphContext.CurrentContext.Graph;
            var node = CreateGraphNode(@params.NodeType.Name, position);
            graph.AddNode(node);
        }
    }
}
