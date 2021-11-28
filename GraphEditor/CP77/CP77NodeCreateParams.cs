using System;
using System.Numerics;

namespace GraphEditor.CP77
{
    internal sealed class CP77NodeCreateParams : Editor.IGraphNodeCreateParams
    {
        public string DisplayName { get; set; }
        internal Type NodeType;
        internal Action<Vector2, CP77NodeCreateParams> CreateNewFn;

        internal CP77NodeCreateParams(Type nodeType, Action<Vector2, CP77NodeCreateParams> createNewFn)
        {
            DisplayName = nodeType.Name;
            NodeType = nodeType;
            CreateNewFn = createNewFn;
        }

        public void CreateNew(Vector2 position)
        {
            CreateNewFn(position, this);
        }
    }
}
