using System;
using System.Collections.Generic;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Scene.CustomNodes
{
    internal sealed class scnQuestNode : GraphNode
    {
        internal scnQuestNode(CP77Types.CHandle<CP77Types.scnSceneGraphNode> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
        }

        public override string GenerateNodeName()
        {
            var questNode = ((CP77Types.scnQuestNode)NodeDef).QuestNode.GetInstance();
            if (questNode != null)
            {
                var context = Editor.GraphContext.CurrentContext;

                if (context.UseFriendlyNodeName)
                {
                    if (Quest.GraphNode.GetFriendlyName(questNode.REDType, out var friendlyName))
                    {
                        return context.IncludeIDInNodeName ? $"{friendlyName} #{Id}" : friendlyName;
                    }
                }

                return context.IncludeIDInNodeName ? $"{questNode.REDType} #{Id}" : questNode.REDType;
            }
            else
            {
                return base.GenerateNodeName();
            }
        }

        // TODO: [CP77] add/remove sockets on UpdateNode for scnQuestNode
        public override void UpdateNode(bool manual = false)
        {
            var scnQuestNode = (CP77Types.scnQuestNode)NodeDef;
            void SetQuestSocketNames(IReadOnlyDictionary<long, Editor.IGraphSocket> sockets, CP77Types.CArray<CP77Types.CName> socketMappings, Action<Editor.IGraphSocket> addSocket)
            {
                for (var i = 0; i != socketMappings.Count; ++i)
                {
                    var socketID = GenerateSocketID(0, (ushort)i);
                    if (sockets.TryGetValue(socketID, out var socket))
                        socket.Name = socketMappings[i].Value;
                    else
                        addSocket(new GraphSocket(socketID, socketMappings[i].Value));
                }
            }
            SetQuestSocketNames(InSockets, scnQuestNode.IsockMappings, AddInputSocket);
            SetQuestSocketNames(OutSockets, scnQuestNode.OsockMappings, AddOutputSocket);
        }

        public override void Draw()
        {
            // Hack - [CP77] make scnQuestNodes more readable
            var questNode = ((CP77Types.scnQuestNode)NodeDef).QuestNode.GetInstance();
            if (questNode == null)
            {
                _altName = NodeDef.REDType;
                Name = GenerateNodeName();
            }
            // if the quest node was changed (weird check, i know)
            else if (_altName != questNode.REDType)
            {
                _altName = questNode.REDType;
                Name = GenerateNodeName();
            }

            var style = Editor.GraphContext.CurrentContext.Style;
            style.PushColor(Editor.GraphStyle.Color.NodeNameplate, 0xFF800080); // Purple
            base.Draw();
            style.PopColor();
        }
    }
}
