using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Scene.CustomNodes
{
    internal sealed class scnChoiceNode : GraphNode
    {
        internal scnChoiceNode(CP77Types.CHandle<CP77Types.scnSceneGraphNode> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
        }

        // TODO: [CP77] add/remove sockets on UpdateNode for scnChoiceNode
        public override void UpdateNode(bool manual = false)
        {
            var choiceNode = (CP77Types.scnChoiceNode)NodeDef;
            for (var i = 0; i != choiceNode.Options.Count; ++i)
            {
                var socketID = GenerateSocketID(0, (ushort)i);
                if (OutSockets.TryGetValue(socketID, out var socket))
                    socket.Name = $"Options[{i}]";
            }
        }
    }
}
