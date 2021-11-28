using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using WolvenKit.RED4.CR2W;
using WolvenKit.Common.Model.Cr2w;
using WolvenKit.RED4.CR2W.Reflection;
using ImGuiNET;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest
{
    internal sealed class Graph : Editor.Graph
    {
        internal CR2WFile CR2WFile { get; private set; }
        internal CP77Types.graphGraphDefinition GraphDef { get; private set; }
        internal bool HideCutControlNode;

        internal Graph(string filename)
            : base(filename)
        {
        }

        internal void LoadGraphDefinition(CR2WFile cr2wFile, CP77Types.graphGraphDefinition graphDef)
        {
            // We need to do this or subgraph's nodes will try to access main graph's context
            using var _ = new Editor.GraphContext.ScopedContext(Context);

            // Populate Nodes
            foreach (var nodeHandle in graphDef.Nodes)
            {
                var nodeInstance = nodeHandle.GetInstance<CP77Types.questNodeDefinition>();
                var node = NodeFactory.CreateGraphNodeByReflection(nodeHandle);
                foreach (var socketHandle in nodeInstance.Sockets)
                {
                    var socketInstance = socketHandle.GetInstance<CP77Types.questSocketDefinition>();
                    switch (socketInstance.Type.Value)
                    {
                        case CP77Types.Enums.questSocketType.Input:
                        case CP77Types.Enums.questSocketType.CutDestination:
                            node.AddInputSocket(NodeFactory.CreateGraphSocket(node.GetNextInputSocketID(), socketInstance.Name.Value, socketHandle));
                            break;
                        case CP77Types.Enums.questSocketType.Output:
                        case CP77Types.Enums.questSocketType.CutSource:
                            node.AddOutputSocket(NodeFactory.CreateGraphSocket(node.GetNextOutputSocketID(), socketInstance.Name.Value, socketHandle));
                            break;
                    }
                }
                AddNode(node);
            }

            // Populate Links
            foreach (var nodeHandle in graphDef.Nodes)
            {
                var nodeInstance = nodeHandle.GetInstance<CP77Types.questNodeDefinition>();
                var node = (GraphNode)_nodes[nodeInstance.Id.Value];
                foreach (var socketHandle in nodeInstance.Sockets)
                {
                    var socketInstance = socketHandle.GetInstance<CP77Types.questSocketDefinition>();
                    if (socketInstance.Type.Value == CP77Types.Enums.questSocketType.Input
                        || socketInstance.Type.Value == CP77Types.Enums.questSocketType.CutDestination)
                    {
                        continue;
                    }

                    Editor.IGraphSocket GetSocketByDef(CP77Types.questSocketDefinition socketDef)
                    {
                        var socketType = socketDef.Type.Value;
                        var isInputSocket = socketType == CP77Types.Enums.questSocketType.Input
                            || socketType == CP77Types.Enums.questSocketType.CutDestination;


                        if (isInputSocket)
                        {
                            foreach (var (_, n) in _nodes)
                            {
                                foreach (var (_, s) in n.InSockets)
                                {
                                    if (((GraphSocket)s).SocketDef.VarChunkIndex == socketDef.VarChunkIndex)
                                    {
                                        return s;
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var (_, s) in node.OutSockets)
                            {
                                if (((GraphSocket)s).SocketDef.VarChunkIndex == socketDef.VarChunkIndex)
                                {
                                    return s;
                                }
                            }
                        }

                        // this is very bad
                        throw new Exception("Socket not found");
                    }

                    foreach (var connectionHandle in socketInstance.Connections)
                    {
                        var connectionInstance = connectionHandle.GetInstance();
                        var sourceSocket = GetSocketByDef(connectionInstance.Source.GetInstance<CP77Types.questSocketDefinition>());
                        var destinationSocket = GetSocketByDef(connectionInstance.Destination.GetInstance<CP77Types.questSocketDefinition>());

                        AddLink(CreateNewLink(GetNextLinkID(), sourceSocket, destinationSocket, connectionHandle));
                    }
                }
            }

            CR2WFile = cr2wFile;
            GraphDef = graphDef;
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

            var graphRes = cr2w.As<CP77Types.graphGraphResource>();
            if (graphRes == null)
                return LoadFileErrorCode.UnexpectedChunkType;

            var graphDef = graphRes.Graph.GetInstance();
            if (graphDef == null)
                return LoadFileErrorCode.UnexpectedChunkType;

            LoadGraphDefinition(cr2w, graphDef);
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

        public override long GetNextNodeID()
        {
            if (_nodes.Count == 0)
                return 1;
            else
            {
                var lastNode = _nodes.Values.Last();
                if (((GraphNode)lastNode).NodeDef is CP77Types.questDeletionMarkerNodeDefinition deletionNode)
                {
                    if (deletionNode.DeletedNodeIds.Count != 0)
                    {
                        var newNodeID = lastNode.Id + 1;
                        foreach (var nodeID in deletionNode.DeletedNodeIds)
                        {
                            if (nodeID.Value >= newNodeID)
                                newNodeID = nodeID.Value + 1;
                        }

                        return newNodeID;
                    }
                }

                return lastNode.Id + 1;
            }
        }

        public override bool CanCreateLink(Editor.IGraphSocket source, Editor.IGraphSocket destination)
        {
            if (!base.CanCreateLink(source, destination))
                return false;

            // CutSource can only connect to CutDestination and vice-versa
            var sourceSocketType = ((GraphSocket)source).SocketType;
            var destinationSocketType = ((GraphSocket)destination).SocketType;
            if (sourceSocketType == CP77Types.Enums.questSocketType.CutSource)
                return destinationSocketType == CP77Types.Enums.questSocketType.CutDestination;
            else if (destinationSocketType == CP77Types.Enums.questSocketType.CutDestination)
                return sourceSocketType == CP77Types.Enums.questSocketType.CutSource;

            return true;
        }

        public override Editor.IGraphLink CreateNewLink(long id, Editor.IGraphSocket source, Editor.IGraphSocket destination, object handle = null)
        {
            if (CR2WFile != null && handle == null && id != -1)
            {
                var sourceSocket = (GraphSocket)source;
                var destinationSocket = (GraphSocket)destination;

                var connectionChunk = CR2WFile.CreateChunkEx("graphGraphConnectionDefinition");
                var connection = (CP77Types.graphGraphConnectionDefinition)connectionChunk.data;

                connection.Source.SetReference(sourceSocket.SocketHandle.Reference);
                connection.Destination.SetReference(destinationSocket.SocketHandle.Reference);

                var sourceConnection = new CP77Types.CHandle<CP77Types.graphGraphConnectionDefinition>(CR2WFile, sourceSocket.SocketDef.Connections, "");
                sourceConnection.SetReference(connectionChunk);
                sourceSocket.SocketDef.Connections.Add(sourceConnection);

                var destinationConnection = new CP77Types.CHandle<CP77Types.graphGraphConnectionDefinition>(CR2WFile, destinationSocket.SocketDef.Connections, "");
                destinationConnection.SetReference(connectionChunk);
                destinationSocket.SocketDef.Connections.Add(destinationConnection);

                handle = sourceConnection;
            }

            return new GraphLink(id, source, destination, handle);
        }

        public override void AddNode(Editor.IGraphNode node)
        {
            if (CR2WFile == null)
            {
                base.AddNode(node);
                return;
            }

            GraphDef.Nodes.Add(((GraphNode)node).NodeHandle);
            base.AddNode(node);
        }

        public override void RemoveNode(Editor.IGraphNode node)
        {
            if (CR2WFile == null)
            {
                base.RemoveNode(node);
                return;
            }

            var graphNode = (GraphNode)node;

            if (graphNode.NodeDef is not CP77Types.questDeletionMarkerNodeDefinition
                && graphNode.NodeDef is not CP77Types.questStartEndNodeDefinition
                && graphNode.NodeDef is not CP77Types.questIONodeDefinition)
            {
                // replace with deletion marker
                var deletionGraphNode = NodeFactory.CreateGraphNode(nameof(CP77Types.questDeletionMarkerNodeDefinition), node.Position);

                if (!string.IsNullOrEmpty(node.CustomName))
                    deletionGraphNode.CustomName = node.CustomName + " #Deleted";
                deletionGraphNode.Comment = node.Comment;
                // add node ID to 'DeletedNodeIds'
                {
                    var deletionNode = ((CP77Types.questDeletionMarkerNodeDefinition)deletionGraphNode.NodeDef);
                    var nodeID = new CP77Types.CUInt16(deletionNode.cr2w, deletionNode.DeletedNodeIds, "");
                    nodeID.IsSerialized = true;
                    nodeID.Value = (ushort)node.Id;
                    deletionNode.DeletedNodeIds.IsSerialized = true;
                    deletionNode.DeletedNodeIds.Add(nodeID);
                }

                foreach (var (_, inputSocket) in node.InSockets)
                {
                    var graphSocket = (GraphSocket)inputSocket;

                    Editor.IGraphSocket destSocket;
                    if (graphSocket.SocketType == CP77Types.Enums.questSocketType.CutDestination)
                    {
                        // Should CutDestination be ignored?
                        //continue;

                        // CutDestination
                        destSocket = deletionGraphNode.InSockets.Values.ElementAt(0);
                    }
                    else
                    {
                        // In
                        destSocket = deletionGraphNode.InSockets.Values.ElementAt(1);
                    }

                    foreach (var connection in graphSocket.Connections)
                    {
                        AddLink(CreateNewLink(connection.Source, destSocket));
                    }
                }

                foreach (var (_, outputSocket) in node.OutSockets)
                {
                    var graphSocket = (GraphSocket)outputSocket;

                    // Out
                    var srcSocket = deletionGraphNode.OutSockets.Values.ElementAt(0);
                    foreach (var connection in graphSocket.Connections)
                    {
                        AddLink(CreateNewLink(srcSocket, connection.Destination));
                    }
                }

                AddNode(deletionGraphNode);
            }

            GraphDef.Nodes.Remove(graphNode.NodeHandle);
            base.RemoveNode(node);
        }

        public override void RemoveNodes(List<Editor.IGraphNode> nodes)
        {
            // TODO: [CP77-Quest] merge multiple deleted nodes into a single deletion marker
            base.RemoveNodes(nodes);
        }

        public override void Close()
        {
            base.Close();
            GraphDef = null;
            CR2WFile = null;
        }

        protected override IReadOnlyList<Editor.IGraphNodeCreateParams> GetNodeCreationList()
            => NodeFactory.NodesCreateParamsList;

        protected override void DrawMenuBar()
        {
            base.DrawMenuBar();

            ImGui.SameLine();
            if (ImGui.Button(HideCutControlNode ? "Show CutControl nodes" : "Hide CutControl nodes"))
            {
                HideCutControlNode ^= true;
                foreach (var (_, node) in _nodes)
                {
                    if (((GraphNode)node).NodeDef is CP77Types.questCutControlNodeDefinition)
                        node.IsHidden = HideCutControlNode;
                }
            }
        }

        protected override void SortNodes_Auto(Func<Editor.IGraphNode, bool> isStartNode = null)
        {
            base.SortNodes_Auto((node) =>
            {
                var nodeDef = ((GraphNode)node).NodeDef;
                return nodeDef is CP77Types.questStartNodeDefinition
                    || nodeDef is CP77Types.questInputNodeDefinition;
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
            void LoadStuff(SortedDictionary<long, Editor.IGraphNode> nodes)
            {
                var nodesCount = br.ReadInt32();
                for (var i = 0; i != nodesCount; ++i)
                {
                    var nodeID = br.ReadInt32();
                    var x = br.ReadSingle();
                    var y = br.ReadSingle();
                    var customName = br.ReadString();
                    var comment = br.ReadString();

                    if (nodes != null && nodes.TryGetValue(nodeID, out var node))
                    {
                        node.Position = new Vector2(x, y);
                        node.CustomName = customName;
                        node.Comment = comment;
                    }
                }

                var phaseNodesCount = br.ReadInt32();
                for (var i = 0; i != phaseNodesCount; ++i)
                {
                    var parentNodeID = br.ReadInt32();
                    if (nodes != null && nodes.TryGetValue(parentNodeID, out var node))
                    {
                        if (node is CustomNodes.questPhaseNodeDefinition phaseNode)
                        {
                            if (phaseNode.LoadSubgraph())
                            {
                                LoadStuff(phaseNode.SubGraph._nodes);
                                continue;
                            }
                        }
                    }

                    // should still read from file
                    // even if node wasn't found or not PhaseNode
                    LoadStuff(null);
                }
            }
            LoadStuff(_nodes);
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

            bw.Write(0xFFFFFFFF); // marker for new format (> version 0)
            bw.Write(2); // version


            // version 2 uses 'int' node IDs
            void SaveStuff(SortedDictionary<long, Editor.IGraphNode> nodes)
            {
                bw.Write(nodes.Count);
                foreach (var (nodeID, node) in nodes)
                {
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

                var phaseNodesCountPos = ms.Position;
                bw.Write((int)0);
                var phaseNodesCount = 0;
                foreach (var (nodeID, node) in nodes)
                {
                    if (node is not CustomNodes.questPhaseNodeDefinition phaseNode)
                        continue;
                    
                    if (phaseNode.SubGraph == null)
                        continue;

                    ++phaseNodesCount;
                    bw.Write((int)nodeID);
                    SaveStuff(phaseNode.SubGraph._nodes);
                }
                var pos = ms.Position;
                ms.Position = phaseNodesCountPos;
                bw.Write(phaseNodesCount);
                ms.Position = pos;
            }
            SaveStuff(_nodes);

            if (File.Exists(filename))
                File.Delete(filename);

            using var fs = File.Create(filename);
            ms.Position = 0;
            ms.CopyTo(fs);
        }
    }
}
