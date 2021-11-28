using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Scene.CustomNodes
{
    internal class scnStartNode : Common.NodeShapes.SceneArrow
    {
        protected bool isStartNode;

        internal scnStartNode(CP77Types.CHandle<CP77Types.scnSceneGraphNode> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
            isStartNode = nodeHandle.GetInstance() is CP77Types.scnStartNode;
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
