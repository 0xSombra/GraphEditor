using System.Linq;
using GraphEditor.Editor;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest
{
    internal class GraphLink : Editor.GraphLink
    {
        internal CP77Types.CHandle<CP77Types.graphGraphConnectionDefinition> LinkHandle { get; private set; }
        internal CP77Types.graphGraphConnectionDefinition LinkDef => LinkHandle.GetInstance();

        public GraphLink(long id, IGraphSocket source, IGraphSocket destination, object obj = null)
            : base(id, source, destination)
        {
            LinkHandle = (CP77Types.CHandle<CP77Types.graphGraphConnectionDefinition>)obj;
        }

        public override void Remove_DONOTUSE_ONLYGRAPH()
        {
            base.Remove_DONOTUSE_ONLYGRAPH();

            var sourceSocket = LinkDef.Source.GetInstance();
            var destinationSocket = LinkDef.Destination.GetInstance();

            // two handle objects that point to the same chunk (LinkDef)
            // need to destroy both handles.
            var sourceHandle = sourceSocket.Connections.First(x => x.Reference.ChunkIndex == LinkDef.VarChunkIndex);
            var destinationHandle = destinationSocket.Connections.First(x => x.Reference.ChunkIndex == LinkDef.VarChunkIndex);

            sourceHandle.ClearHandle();
            sourceSocket.Connections.Remove(sourceHandle);

            destinationHandle.ClearHandle();
            destinationSocket.Connections.Remove(destinationHandle);

            LinkHandle = null;
        }
    }
}
