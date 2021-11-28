using System.IO;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest.CustomNodes
{
    internal class questSceneNodeDefinition : GraphNode
    {
        internal questSceneNodeDefinition(CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
        }

        // TODO: [CP77] add/remove sockets on UpdateNode for questSceneNodeDefinition (Entry/Exit/Interrupt (INT/RET))
        public override void UpdateNode(bool manual = false)
        {
            var sceneNode = (CP77Types.questSceneNodeDefinition)NodeDef;
            Description = Path.GetFileName(sceneNode.SceneFile.DepotPath);

            if (manual)
            {
                // TODO: [CP77] load the .scene and update the node
            }
        }
    }
}
