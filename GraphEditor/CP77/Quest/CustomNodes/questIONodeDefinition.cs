using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest.CustomNodes
{
    internal class questIONodeDefinition : questStartEndNodeDefinition
    {
        internal questIONodeDefinition(CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
            isStartNode = nodeHandle.GetInstance() is CP77Types.questInputNodeDefinition;
        }

        public override void Draw()
        {
            var _socketName = ((CP77Types.questIONodeDefinition)NodeDef).SocketName.Value;
            if (NodeDef is CP77Types.questOutputNodeDefinition outputNode)
                Name = $"{_socketName} ({outputNode.Type.Value})";
            else
                Name = _socketName;

            base.Draw();
        }
    }
}
