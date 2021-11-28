using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest
{
    internal sealed class GraphSocket : Editor.GraphSocket
    {
        internal CP77Types.CHandle<CP77Types.graphGraphSocketDefinition> SocketHandle { get; private set; }
        internal CP77Types.questSocketDefinition SocketDef => SocketHandle.GetInstance<CP77Types.questSocketDefinition>();
        internal CP77Types.Enums.questSocketType SocketType => SocketDef.Type.Value;

        internal GraphSocket(CP77Types.CHandle<CP77Types.graphGraphSocketDefinition> socketHandle, long id, string name)
            : base(id, name)
        {
            SocketHandle = socketHandle;
        }

        public override void Remove_DONOTUSE_ONLYNODE()
        {
            base.Remove_DONOTUSE_ONLYNODE();
            SocketHandle.ClearHandle();
            SocketHandle = null;
        }
    }
}
