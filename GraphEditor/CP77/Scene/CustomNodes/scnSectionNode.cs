using System.Diagnostics;
using System.Text;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Scene.CustomNodes
{
    internal sealed class scnSectionNode : GraphNode
    {
        internal scnSectionNode(CP77Types.CHandle<CP77Types.scnSceneGraphNode> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
        }

        // TODO: [CP77] add/remove sockets on UpdateNode for scnChoiceNode
        public override void UpdateNode(bool manual = false)
        {
            var sectionNode = (CP77Types.scnSectionNode)NodeDef;
            for (var i = 0; i != sectionNode.Events.Count; ++i)
            {
                var @event = sectionNode.Events[i];
                var socketEvent = @event.GetInstance<CP77Types.scneventsSocket>();
                if (socketEvent == null)
                    continue;

                var stampName = socketEvent.OsockStamp.Name.Value;
                var stampOrdinal = socketEvent.OsockStamp.Ordinal.Value;
                if (stampName == 0)
                    continue; // ??

                var socketID = GenerateSocketID(stampName, stampOrdinal);
                if (OutSockets.TryGetValue(socketID, out var socket))
                {
                    socket.Name = $"Events[{i}]@{socketEvent.StartTime.Value}";
                }
                else
                {
                    Debug.Assert(false, "This can happen?");
                }
            }

            Description = $"Duration: {sectionNode.SectionDuration.Stu.Value}";
        }
    }
}
