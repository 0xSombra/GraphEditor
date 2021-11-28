using System.Collections.Generic;
using System.Numerics;

namespace GraphEditor.Editor
{
    public interface IGraphSocket
    {
        long Id { get; }
        string Name { get; set; }
        IGraphNode Parent { get; set; }
        SocketType Type { get; set; }
        Vector2 ScreenPosition { get; }
        Vector2 RelativePosition { get; set; }
        IReadOnlyList<IGraphLink> Connections { get; }
        bool IsHovered { get; }

        void AddLink_DONOTUSE_ONLYGRAPH(IGraphLink link);
        void RemoveLink_DONOTUSE_ONLYGRAPH(IGraphLink link);
        void Draw(bool drawSocketNames = true);
        void Remove_DONOTUSE_ONLYNODE();
    }
}
