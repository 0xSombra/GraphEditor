using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Scene.CustomNodes
{
    internal sealed class scnEndNode : scnStartNode
    {
        public override string ArrowText => $"{Name} ({((CP77Types.scnEndNode)NodeDef).Type.Value})";

        internal scnEndNode(CP77Types.CHandle<CP77Types.scnSceneGraphNode> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
        }
    }
}
