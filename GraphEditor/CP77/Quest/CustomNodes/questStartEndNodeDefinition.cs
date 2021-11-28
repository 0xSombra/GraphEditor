using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest.CustomNodes
{
    internal class questStartEndNodeDefinition : Common.NodeShapes.QuestArrow
    {
        protected bool isStartNode;

        internal questStartEndNodeDefinition(CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
            var nodeInstance = nodeHandle.GetInstance();
            if (nodeInstance is CP77Types.questStartNodeDefinition)
            {
                Name = "Start";
                isStartNode = true;
            }
            else if (nodeInstance is CP77Types.questEndNodeDefinition)
            {
                Name = "End";
            }
        }

        public override void Draw()
        {
            var context = Editor.GraphContext.CurrentContext;
            var style = context.Style;
            if (isStartNode)
            {
                style.PushColor(Editor.GraphStyle.Color.NodeNameplate, 0xFF00FF00); // Green
                style.PushColor(Editor.GraphStyle.Color.NodeName, 0xFF000000); // Black
            }

            base.Draw();

            if (isStartNode)
            {
                style.PopColor(2);
            }
        }
    }
}
