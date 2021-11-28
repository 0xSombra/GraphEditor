using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using CP77Types = WolvenKit.RED4.CR2W.Types;

// sometimes C# is very annoying
// wanted: class Arrow<TGraphNode> : TGraphNode where TGraphNode : Editor.IGraphNode
namespace GraphEditor.CP77.Common.NodeShapes
{
    internal class QuestArrow : Quest.GraphNode, IArrowNode
    {
        public virtual string ArrowText => Name;
        public Vector2 ArrowBoxSize { get; set; }
        public Vector2 FinalNodeSize { get; set; }

        public QuestArrow(CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
        }

        protected override void CalculateSize()
        {
            Arrow.CalculateSize(this);
            _size = FinalNodeSize;
        }

        protected override void DrawNode()
        {
            Arrow.DrawNode(this);
        }

        protected override void DrawSockets(bool drawSocketNames = true)
        {
            base.DrawSockets(false);

            if (ActiveSocket != null)
            {
                ImGui.SetTooltip(ActiveSocket.Name);
            }
        }
    }

    internal class SceneArrow : Scene.GraphNode, IArrowNode
    {
        public virtual string ArrowText => Name;
        public Vector2 ArrowBoxSize { get; set; }
        public Vector2 FinalNodeSize { get; set; }

        public SceneArrow(CP77Types.CHandle<CP77Types.scnSceneGraphNode> nodeHandle, long id, string name)
            : base(nodeHandle, id, name)
        {
        }

        protected override void CalculateSize()
        {
            Arrow.CalculateSize(this);
            _size = FinalNodeSize;
        }

        protected override void DrawNode()
        {
            Arrow.DrawNode(this);
        }

        protected override void DrawSockets(bool drawSocketNames = true)
        {
            base.DrawSockets(false);

            if (ActiveSocket != null)
            {
                ImGui.SetTooltip(ActiveSocket.Name);
            }
        }
    }

    interface IArrowNode : Editor.IGraphNode
    {
        string ArrowText { get; }
        Vector2 ArrowBoxSize { get; set; }
        Vector2 FinalNodeSize { get; set; }
    }

    static class Arrow
    {
        // ------ ImGui constants ------
        const float BaseArrowHeadXOffset = 35.0f;
        const float BaseArrowBodyYOffset = 10.0f;
        static readonly Vector2 BaseMinArrowBoxSize = new Vector2(45, 30);

        internal static void CalculateSize(IArrowNode arrowNode)
        {
            var context = Editor.GraphContext.CurrentContext;
            var style = context.Style;
            float NodeSocketSpacingY = style.NodeSocketSpacing.Y * context.UIScale;
            float ArrowHeadXOffset = BaseArrowHeadXOffset * context.UIScale;
            float ArrowBodyYOffset = BaseArrowBodyYOffset * context.UIScale;
            Vector2 MinArrowBoxSize = BaseMinArrowBoxSize * context.UIScale;
            Vector2 NodeTextPadding = style.NodeTextPadding * context.UIScale;

            var ArrowBoxSize = MinArrowBoxSize;
            var NodeSize = ArrowBoxSize + new Vector2(ArrowHeadXOffset, ArrowBodyYOffset * 2);

            var requiredSizeForName = ImGui.CalcTextSize(arrowNode.ArrowText) + new Vector2(NodeTextPadding.X * 3, NodeTextPadding.Y * 2);
            if (NodeSize.X < requiredSizeForName.X)
            {
                var triangleSize = NodeSize.X - ArrowBoxSize.X;
                NodeSize.X = requiredSizeForName.X;
                ArrowBoxSize.X = requiredSizeForName.X - triangleSize;
            }

            void UpdateSocketPosition(IReadOnlyDictionary<long, Editor.IGraphSocket> sockets, bool isRightAligned)
            {
                var socketPosition = new Vector2(isRightAligned ? NodeSize.X : 0.0f, NodeSize.Y / 2);

                if (!isRightAligned && sockets.Count > 1)
                {
                    // TODO: [CP77] render cut destination properly on arrows :)
                    socketPosition.Y -= NodeSocketSpacingY;
                }

                foreach (var (_, socket) in sockets)
                {
                    socket.RelativePosition = socketPosition;

                    // shouldn't happen, but we will still try to draw.
                    socketPosition.Y += NodeSocketSpacingY;
                }
            }

            UpdateSocketPosition(arrowNode.InSockets, false);
            UpdateSocketPosition(arrowNode.OutSockets, true);

            arrowNode.ArrowBoxSize = ArrowBoxSize;
            arrowNode.FinalNodeSize = NodeSize;
        }

        internal static void DrawNode(IArrowNode graphNode)
        {
            var context = Editor.GraphContext.CurrentContext;
            var style = context.Style;
            float ArrowHeadXOffset = BaseArrowHeadXOffset * context.UIScale;
            float ArrowBodyYOffset = BaseArrowBodyYOffset * context.UIScale;
            float NodeSocketLabelSpacing = style.NodeSocketLabelSpacing * context.UIScale;
            float BorderThickness = style.BorderThickness * context.UIScale;

            var drawList = ImGui.GetWindowDrawList();
            var renderPosition = graphNode.GetScreenPosition();
            var color = style.GetColor(Editor.GraphStyle.Color.NodeNameplate);
            {
                var boxSize = graphNode.ArrowBoxSize;
                var boxPos = renderPosition + new Vector2(0.0f, ArrowBodyYOffset);
                var borderColor = style.GetColor(graphNode.IsSelected ? Editor.GraphStyle.Color.ActiveNodeBorder : Editor.GraphStyle.Color.NodeBorder);
                drawList.AddRectFilled(boxPos, boxPos + boxSize, color);
                drawList.AddRect(boxPos, boxPos + boxSize, borderColor, 0.0f, ImDrawFlags.None, BorderThickness);
                drawList.AddTriangleFilled(renderPosition + new Vector2(boxSize.X, 0.0f)
                    , renderPosition + boxSize + (new Vector2(0.0f, ArrowBodyYOffset * 2))
                    , renderPosition + boxSize + (new Vector2(ArrowHeadXOffset, -(boxSize.Y / 2) + ArrowBodyYOffset))
                    , color);
                drawList.AddTriangle(renderPosition + new Vector2(boxSize.X, 0.0f)
                    , renderPosition + boxSize + (new Vector2(0.0f, ArrowBodyYOffset * 2))
                    , renderPosition + boxSize + (new Vector2(ArrowHeadXOffset, -(boxSize.Y / 2) + ArrowBodyYOffset))
                    , borderColor, BorderThickness);

                // cover up ugly border inside the arrow
                // maybe we should render borders with lines instead of rect
                {
                    var lineStart = boxPos + new Vector2(boxSize.X - BorderThickness / 2, BorderThickness / 2);
                    var lineEnd = lineStart + new Vector2(0.0f, boxSize.Y - BorderThickness);
                    drawList.AddLine(lineStart, lineEnd, color, BorderThickness * 2);
                }
            }

            {
                var nameSize = ImGui.CalcTextSize(graphNode.ArrowText);
                var namePos = (graphNode.Size / 2) - (nameSize / 2);
                namePos.X -= NodeSocketLabelSpacing;
                drawList.AddText(renderPosition + namePos, style.GetColor(Editor.GraphStyle.Color.NodeName), graphNode.ArrowText);
            }
        }
    }
}
