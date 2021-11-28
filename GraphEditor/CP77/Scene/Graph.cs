using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using WolvenKit.RED4.CR2W;
using ImGuiNET;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Scene
{
    internal sealed class Graph : Editor.Graph
    {
        readonly static List<Editor.IGraphNodeCreateParams> _nodesCreateParamsList;
        internal CR2WFile CR2WFile { get; private set; }
        internal CP77Types.scnSceneGraph GraphDef { get; private set; }
        internal bool HideCutControlNode;

        static Graph()
        {
            // TODO: [CP77] fill _nodesCreateParamsList
            _nodesCreateParamsList = new List<Editor.IGraphNodeCreateParams>();
        }

        static GraphNode CreateNodeByReflection(CP77Types.CHandle<CP77Types.scnSceneGraphNode> nodeHandle)
        {
            var nodeDef = nodeHandle.GetInstance();
            var id = (long)nodeDef.NodeId.Id.Value;
            var name = nodeDef.REDType;
            GraphNode node = null;

            var currentAssembly = Assembly.GetExecutingAssembly();
            for (var nodeDefType = nodeDef.GetType(); nodeDefType != typeof(CP77Types.ISerializable); nodeDefType = nodeDefType.BaseType)
            {
                var nodeType = currentAssembly.GetType($"{typeof(Graph).Namespace}.CustomNodes.{nodeDefType.Name}");
                if (nodeType == null) continue;

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

        internal Graph(string filename)
            : base(filename)
        {
        }

        public override bool IsSavable() => CR2WFile != null;

        public override LoadFileErrorCode Load(string filename)
        {
            if (!File.Exists(filename))
                return LoadFileErrorCode.FileNotFound;

            using var fs = File.OpenRead(filename);

            var cr2w = new CR2WFile();
            var cr2wError = cr2w.Read(fs);
            switch (cr2wError)
            {
                case WolvenKit.Common.EFileReadErrorCodes.NoCr2w:
                    return LoadFileErrorCode.NoCR2W;
                case WolvenKit.Common.EFileReadErrorCodes.UnsupportedVersion:
                    return LoadFileErrorCode.UnsupportedCR2WVersion;
            }

            // check version?
            var sceneRes = cr2w.As<CP77Types.scnSceneResource>();
            if (sceneRes == null)
                return LoadFileErrorCode.UnexpectedChunkType;

            var graphDef = sceneRes.SceneGraph.GetInstance();
            if (graphDef == null)
                return LoadFileErrorCode.UnexpectedChunkType;

            // Populate Nodes and output sockets
            foreach (var nodeHandle in graphDef.Graph)
            {
                var nodeInstance = nodeHandle.GetInstance();
                var node = CreateNodeByReflection(nodeHandle);
                foreach (var socketInstance in nodeInstance.OutputSockets)
                {
                    var stampName = socketInstance.Stamp.Name.Value;
                    var stampOrdinal = socketInstance.Stamp.Ordinal.Value;
                    var socketID = node.GenerateSocketID(stampName, stampOrdinal);

                    var socket = new GraphSocket(socketID, $"Out_{stampName}_{stampOrdinal}");
                    node.AddOutputSocket(socket);
                }

                AddNode(node);
            }

            // Populate Links and input sockets
            foreach (var nodeHandle in graphDef.Graph)
            {
                var nodeInstance = nodeHandle.GetInstance();
                var node = (GraphNode)_nodes[nodeInstance.NodeId.Id.Value];
                foreach (var srcSocketInstance in nodeInstance.OutputSockets)
                {
                    var srcStampName = srcSocketInstance.Stamp.Name.Value;
                    var srcStampOrdinal = srcSocketInstance.Stamp.Ordinal.Value;
                    var srcSocketID = node.GenerateSocketID(srcStampName, srcStampOrdinal);

                    var srcSocket = node.OutSockets[srcSocketID];
                    foreach (var destSocketInstance in srcSocketInstance.Destinations)
                    {
                        var destNodeID = destSocketInstance.NodeId.Id.Value;
                        var destNode = (GraphNode)_nodes[destNodeID];

                        var destStampName = destSocketInstance.IsockStamp.Name.Value;
                        var destStampOrdinal = destSocketInstance.IsockStamp.Ordinal.Value;
                        var destSocketID = destNode.GenerateSocketID(destStampName, destStampOrdinal);

                        Editor.IGraphSocket destSocket;
                        if (!destNode.InSockets.TryGetValue(destSocketID, out destSocket))
                        {
                            destSocket = new GraphSocket(destSocketID, $"In_{destStampName}_{destStampOrdinal}");
                            destNode.AddInputSocket(destSocket);
                        }
                        AddLink(CreateNewLink(GetNextLinkID(), srcSocket, destSocket));
                    }
                }
            }

            // set Name for entry nodes
            foreach (var entry in sceneRes.EntryPoints)
            {
                var node = _nodes[entry.NodeId.Id.Value];
                node.Name = entry.Name.Value;
            }

            // set Name for exit nodes
            foreach (var exit in sceneRes.ExitPoints)
            {
                var node = _nodes[exit.NodeId.Id.Value];
                node.Name = exit.Name.Value;
            }

            // set Description for notable nodes
            foreach (var notable in sceneRes.NotablePoints)
            {
                var node = (GraphNode)_nodes[notable.NodeId.Id.Value];
                node.Description = notable.Name.Value;

                Debug.Assert(node.NodeDef is not CP77Types.scnQuestNode, "scnQuestNode will override 'NotablePoints' Description");
            }

            CR2WFile = cr2w;
            GraphDef = graphDef;

            return LoadFileErrorCode.Success;
        }

        public override SaveFileErrorCode Save(string filename)
        {
            try
            {
                if (File.Exists(filename))
                    File.Delete(filename);

                using var fs = File.Create(filename);
                using var bw = new BinaryWriter(fs);
                CR2WFile.Write(bw);

                return SaveFileErrorCode.Success;
            }
            catch (Exception)
            {
                return SaveFileErrorCode.Error;
            }
        }

        public override void AddLink(Editor.IGraphLink link)
        {
            if (CR2WFile == null)
            {
                base.AddLink(link);
                return;
            }

            GraphNode.ParseSocketID(link.Source.Id, out long _, out ushort sourceName, out ushort sourceOrdinal);
            var sourceNode = (GraphNode)link.Source.Parent;
            foreach (var outputSocket in sourceNode.NodeDef.OutputSockets)
            {
                if (outputSocket.Stamp.Name.Value != sourceName || outputSocket.Stamp.Ordinal.Value != sourceOrdinal)
                    continue;

                GraphNode.ParseSocketID(link.Destination.Id, out long destinationNodeID, out ushort destinationName, out ushort destinationOrdinal);
                var inputSocket = new CP77Types.scnInputSocketId(CR2WFile, outputSocket.Destinations, "");
                inputSocket.IsSerialized = true;

                inputSocket.NodeId.IsSerialized = true;
                inputSocket.NodeId.Id.IsSerialized = true;
                inputSocket.NodeId.Id.Value = (uint)destinationNodeID;

                inputSocket.IsockStamp.IsSerialized = true;
                inputSocket.IsockStamp.Name.IsSerialized = true;
                inputSocket.IsockStamp.Name.Value = destinationName;
                inputSocket.IsockStamp.Ordinal.IsSerialized = true;
                inputSocket.IsockStamp.Ordinal.Value = destinationOrdinal;

                outputSocket.Destinations.Add(inputSocket);
                base.AddLink(link);
                break;
            }
        }

        public override void RemoveNode(Editor.IGraphNode node)
        {
            // .. no
            //base.RemoveNode(node);
        }

        public override void RemoveLink(Editor.IGraphLink link)
        {
            GraphNode.ParseSocketID(link.Source.Id, out long _, out ushort sourceName, out ushort sourceOrdinal);
            var sourceNode = (GraphNode)link.Source.Parent;
            foreach (var outputSocket in sourceNode.NodeDef.OutputSockets)
            {
                if (outputSocket.Stamp.Name.Value != sourceName || outputSocket.Stamp.Ordinal.Value != sourceOrdinal)
                    continue;

                GraphNode.ParseSocketID(link.Destination.Id, out long destinationNodeID, out ushort destinationName, out ushort destinationOrdinal);
                foreach (var inputSocket in outputSocket.Destinations)
                {
                    if (inputSocket.NodeId.Id.Value != destinationNodeID
                        || inputSocket.IsockStamp.Name.Value != destinationName
                        || inputSocket.IsockStamp.Ordinal.Value != destinationOrdinal)
                    {
                        continue;
                    }

                    outputSocket.Destinations.Remove(inputSocket);
                    base.RemoveLink(link);
                    break;
                }
                break;
            }
        }

        public override void Close()
        {
            base.Close();
            GraphDef = null;
            CR2WFile = null;
        }

        protected override IReadOnlyList<Editor.IGraphNodeCreateParams> GetNodeCreationList()
        {
            return _nodesCreateParamsList;
        }

        protected override void DrawMenuBar()
        {
            base.DrawMenuBar();

            ImGui.SameLine();
            if (ImGui.Button(HideCutControlNode ? "Show CutControl nodes" : "Hide CutControl nodes"))
            {
                HideCutControlNode ^= true;
                foreach (var node in _nodes)
                {
                    if (((GraphNode)node.Value).NodeDef is CP77Types.scnCutControlNode)
                        node.Value.IsHidden = HideCutControlNode;
                }
            }
        }

        protected override void SortNodes_Auto(Func<Editor.IGraphNode, bool> isStartNode = null)
        {
            base.SortNodes_Auto((node) =>
            {
                return ((GraphNode)node).NodeDef is CP77Types.scnStartNode;
            });
        }

        void SortNodes_Load_v0(BinaryReader br)
        {
            var nodesCount = br.ReadInt32();
            for (var i = 0; i != nodesCount; ++i)
            {
                var nodeID = br.ReadInt32();
                var x = br.ReadSingle();
                var y = br.ReadSingle();

                if (_nodes.TryGetValue(nodeID, out var node))
                {
                    node.Position = new Vector2(x, y);
                }
            }
        }

        void SortNodes_Load_v1(BinaryReader br)
        {
            var nodesCount = br.ReadInt32();
            for (var i = 0; i != nodesCount; ++i)
            {
                var nodeID = br.ReadInt32();
                var x = br.ReadSingle();
                var y = br.ReadSingle();
                var customName = br.ReadString();

                if (_nodes.TryGetValue(nodeID, out var node))
                {
                    node.Position = new Vector2(x, y);
                    node.CustomName = customName;
                }
            }
        }

        void SortNodes_Load_v2(BinaryReader br)
        {
            var nodesCount = br.ReadInt32();
            for (var i = 0; i != nodesCount; ++i)
            {
                var nodeID = br.ReadInt32();
                var x = br.ReadSingle();
                var y = br.ReadSingle();
                var customName = br.ReadString();
                var comment = br.ReadString();

                if (_nodes.TryGetValue(nodeID, out var node))
                {
                    node.Position = new Vector2(x, y);
                    node.CustomName = customName;
                    node.Comment = comment;
                }
            }
        }

        protected override void SortNodes_Load()
        {
            var filename = $"GraphSort_CP77/{Name}.bin";
            if (!File.Exists(filename))
                return;

            using var fs = File.OpenRead(filename);
            using var br = new BinaryReader(fs);

            var marker = br.ReadUInt32(); // version 0 had no 'version'
            if (marker != 0xFFFFFFFF)
            {
                fs.Position = 0;
                SortNodes_Load_v0(br);
            }
            else
            {
                var version = br.ReadInt32();
                switch (version)
                {
                    case 1:
                        SortNodes_Load_v1(br);
                        break;
                    case 2:
                        SortNodes_Load_v2(br);
                        break;
                }
            }
        }

        protected override void SortNodes_Save()
        {
            var filename = $"GraphSort_CP77/{Name}.bin";
            Directory.CreateDirectory("GraphSort_CP77");

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            // marker for new format
            bw.Write(0xFFFFFFFF);
            bw.Write(2); // version


            bw.Write(_nodes.Count);
            foreach (var (nodeID, node) in _nodes)
            {
                // version 2 uses 'int' node IDs
                bw.Write((int)nodeID);
                bw.Write(node.Position.X);
                bw.Write(node.Position.Y);

                if (string.IsNullOrEmpty(node.CustomName))
                {
                    bw.Write(string.Empty);
                }
                else
                {
                    bw.Write(node.CustomName);
                }

                if (string.IsNullOrEmpty(node.Comment))
                {
                    bw.Write(string.Empty);
                }
                else
                {
                    bw.Write(node.Comment);
                }
            }

            if (File.Exists(filename))
                File.Delete(filename);

            using var fs = File.Create(filename);
            ms.Position = 0;
            ms.CopyTo(fs);
        }
    }
}
