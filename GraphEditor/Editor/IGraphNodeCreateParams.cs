using System.Numerics;

namespace GraphEditor.Editor
{
    public interface IGraphNodeCreateParams
    {
        string DisplayName { get; }

        void CreateNew(Vector2 position);
    }
}
