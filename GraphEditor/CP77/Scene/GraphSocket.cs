using System.Numerics;
using ImGuiNET;

namespace GraphEditor.CP77.Scene
{
    internal sealed class GraphSocket : Editor.GraphSocket
    {
        public GraphSocket(long id, string name)
            : base(id, name)
        {
        }

        protected override void DrawSocketArea(Vector2 socketPosition, bool isCircle = true)
        {
            base.DrawSocketArea(socketPosition, isCircle);

            if (IsHovered)
            {
                GraphNode.ParseSocketID(Id, out long _, out ushort stampName, out ushort stampOrdinal);
                ImGui.SetTooltip($"Name: {stampName}, Ordinal: {stampOrdinal}");
            }
        }
    }
}
