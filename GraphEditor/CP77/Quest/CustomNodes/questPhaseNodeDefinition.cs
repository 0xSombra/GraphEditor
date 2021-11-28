using System.IO;
using ImGuiNET;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest.CustomNodes
{
    internal class questPhaseNodeDefinition : GraphNode
    {
        internal Graph SubGraph;

        internal questPhaseNodeDefinition(CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
        }

        internal bool LoadSubgraph()
        {
            if (SubGraph != null)
            {
                SubGraph.Name = Name;
                return true;
            }

            var phaseNode = (CP77Types.questPhaseNodeDefinition)NodeDef;
            if (!phaseNode.PhaseGraph.ChunkHandle)
                return false;
            var phaseNodeSubgraph = phaseNode.PhaseGraph.GetInstance();
            //if (phaseNodeSubgraph.Nodes.Count == 0)
            //    return false;

            var currentGraph = (Graph)Editor.GraphContext.CurrentContext.Graph;
            SubGraph = new Graph(Name) { IsSubgraph = true };
            SubGraph.LoadGraphDefinition(currentGraph.CR2WFile, phaseNodeSubgraph);
            return true;
        }

        // TODO: [CP77] add/remove sockets on UpdateNode for questPhaseNodeDefinition (input/output nodes)
        public override void UpdateNode(bool manual = false)
        {
            var phaseNode = (CP77Types.questPhaseNodeDefinition)NodeDef;
            Description = Path.GetFileName(phaseNode.PhaseResource.DepotPath);

            if (string.IsNullOrEmpty(phaseNode.PhaseResource.DepotPath))
            {
                base.UpdateNode(manual);
            }
            else if (manual)
            {
                // TODO: [CP77] load the .questphase and update the node
            }
        }

        protected override void HandleMouseKeyboardEvents()
        {
            base.HandleMouseKeyboardEvents();
            if (!_isHovered)
                return;

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                if (LoadSubgraph())
                {
                    Editor.GraphContext.CurrentContext.Graph.ActiveSubgraph = SubGraph;
                }
            }
        }
    }
}
