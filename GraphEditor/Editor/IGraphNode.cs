using System.Collections.Generic;
using System.Numerics;

namespace GraphEditor.Editor
{
    public interface IGraphNode
    {
        bool Dirty { get; set; }
        long Id { get; }
        Vector2 Position { get; set; }
        Vector2 Size { get; }
        string Name { get; set; }
        string CustomName { get; set; }
        string Comment { get; set; }
        string Description { get; set; }
        GraphNodeSelectionType SelectionType { get; set; }
        IGraphSocket ActiveSocket { get; }
        IReadOnlyDictionary<long, IGraphSocket> InSockets { get; }
        IReadOnlyDictionary<long, IGraphSocket> OutSockets { get; }
        bool IsSelected { get; }
        bool IsHidden { get; set; }

        Vector2 GetLocalPosition();
        Vector2 GetScreenPosition();
        string GenerateNodeName();
        void UpdateNode(bool manual = false);
        void DrawProperties();
        void Draw();
        void Remove_DONOTUSE_ONLYGRAPH();
    }
}
