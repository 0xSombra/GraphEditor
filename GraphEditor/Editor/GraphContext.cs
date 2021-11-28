using System;
using System.Numerics;

namespace GraphEditor.Editor
{
    public sealed class GraphContext
    {
        // RAII where are you
        // Usage: using var _ = new GraphContext.ScopedContext(Context);
        public struct ScopedContext : IDisposable
        {
            GraphContext _oldContext;

            public ScopedContext(GraphContext context)
            {
                _oldContext = CurrentContext;
                CurrentContext = context;
            }

            public void Dispose()
            {
                CurrentContext = _oldContext;
            }
        }

        public static GraphContext CurrentContext { get; private set; }
        public Graph Graph;
        public bool ForceAllDirty = false;
        public bool DrawHitbox = false;
        public bool UseFriendlyNodeName = true;
        public bool IncludeIDInNodeName = true;
        public float UIScale = 1.0f;
        public float ScrollSpeed = 15.0f;
        public float UpdateNodeEverySecs = 5.0f;
        public Vector2 ViewOffset = Vector2.Zero;
        public GraphStyle Style { get; } = new GraphStyle();
        public IGraphLink TMP_CreatedLink;

        public GraphContext(Graph graph)
        {
            Graph = graph;
        }
    }
}
