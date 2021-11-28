using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace GraphEditor.Editor
{
    class FakeMouseSocket : IGraphSocket
    {
        internal static FakeMouseSocket Instance = new FakeMouseSocket();
        public long Id => -1;
        public string Name { get; set; } = string.Empty;
        public IGraphNode Parent { get; set; }
        public SocketType Type { get; set; } = SocketType.Input;
        public Vector2 ScreenPosition => ImGui.GetMousePos();
        public Vector2 RelativePosition { get; set; }
        public IReadOnlyList<IGraphLink> Connections { get; set; }
        public bool IsHovered => false;

        public void AddLink_DONOTUSE_ONLYGRAPH(IGraphLink link)
        {
        }

        public void Draw(bool drawSocketNames = true)
        {
        }

        public void Remove_DONOTUSE_ONLYNODE()
        {
        }

        public void RemoveLink_DONOTUSE_ONLYGRAPH(IGraphLink link)
        {
        }
    }

    public enum SocketType
    {
        Input,
        Output
    };

    public class GraphSocket : IGraphSocket
    {
        public long Id => _id;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                // recalculate node size to account for the new name's length
                if (Parent != null)
                    Parent.Dirty = true;
            }
        }
        public IGraphNode Parent { get; set; }
        public SocketType Type { get; set; }
        public Vector2 ScreenPosition => RelativePosition + Parent.GetScreenPosition();
        public Vector2 RelativePosition { get; set; } = Vector2.Zero;
        public IReadOnlyList<IGraphLink> Connections => _connections;
        public bool IsHovered { get; protected set; }
        protected readonly long _id;
        protected string _name;
        protected List<IGraphLink> _connections = new List<IGraphLink>();
        protected bool _isBeingRemoved; // dont remove from _connections, we will clear it later.

        public GraphSocket(long id, string name)
        {
            _id = id;
            _name = name;
        }

        // Just an InvisibleButton with a check to make it visible or not
        protected bool DrawHitboxArea(Vector2 size, uint color = 0xFF00FF00)
        {
            var context = GraphContext.CurrentContext;

            if (context.DrawHitbox)
            {
                var drawList = ImGui.GetWindowDrawList();
                var position = ImGui.GetCursorScreenPos();
                drawList.AddRectFilled(position, position + size, color);
            }

            return ImGui.InvisibleButton("##HitBox", size);
        }

        protected virtual void DrawSocketArea(Vector2 socketPosition, bool isCircle = true)
        {
            var context = GraphContext.CurrentContext;
            var style = context.Style;
            float NodeSocketRadius = style.NodeSocketRadius * context.UIScale;
            float BorderThickness = style.BorderThickness * context.UIScale;

            var drawList = ImGui.GetWindowDrawList();
            uint socketColor;
            if (IsHovered)
                socketColor = style.GetColor(GraphStyle.Color.HoveredSocket);
            else
                socketColor = style.GetColor(Connections.Count == 0 ? GraphStyle.Color.UnusedSocket : GraphStyle.Color.Socket);

            if (isCircle)
            {
                drawList.AddCircleFilled(socketPosition, NodeSocketRadius,
                    socketColor);
                drawList.AddCircle(socketPosition, NodeSocketRadius + BorderThickness,
                    style.GetColor(Parent.IsSelected ? GraphStyle.Color.ActiveNodeBorder : GraphStyle.Color.NodeBorder),
                    0, BorderThickness);
            }
            else
            {
                drawList.AddRectFilled(socketPosition - (new Vector2(NodeSocketRadius, NodeSocketRadius) * 1.25f),
                    socketPosition + (new Vector2(NodeSocketRadius, NodeSocketRadius) * 1.25f),
                    socketColor);
                drawList.AddRect(socketPosition - (new Vector2(NodeSocketRadius, NodeSocketRadius) * 1.25f),
                    socketPosition + (new Vector2(NodeSocketRadius, NodeSocketRadius) * 1.25f),
                    style.GetColor(Parent.IsSelected ? GraphStyle.Color.ActiveNodeBorder : GraphStyle.Color.NodeBorder),
                    0.0f, ImDrawFlags.None, BorderThickness);
            }

            ImGuiExtensions.PushID(Id);
            if (isCircle)
            {
                ImGui.SetCursorScreenPos(socketPosition - new Vector2(NodeSocketRadius, NodeSocketRadius));
                DrawHitboxArea(new Vector2(NodeSocketRadius, NodeSocketRadius) * 2.0f);
            }
            else
            {
                ImGui.SetCursorScreenPos(socketPosition - (new Vector2(NodeSocketRadius, NodeSocketRadius) * 1.25f));
                DrawHitboxArea(new Vector2(NodeSocketRadius, NodeSocketRadius) * 2.5f);
            }
            ImGui.PopID();

            IsHovered = ImGuiExtensions.IsItemHovered_Overlap();
            if (IsHovered && ImGui.IsItemClicked(ImGuiMouseButton.Left) && context.TMP_CreatedLink == null)
            {
                if (Type == SocketType.Output)
                {
                    FakeMouseSocket.Instance.Type = SocketType.Input;
                    context.TMP_CreatedLink = context.Graph.CreateNewLink(-1, this, FakeMouseSocket.Instance);
                }
                else
                {
                    FakeMouseSocket.Instance.Type = SocketType.Output;
                    context.TMP_CreatedLink = context.Graph.CreateNewLink(-1, FakeMouseSocket.Instance, this);
                }

                // clears node selection to fix a bug
                context.Graph.SelectNode(null);
            }
            else if (IsHovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && context.TMP_CreatedLink != null)
            {
                if (Type == SocketType.Input)
                {
                    if (context.Graph.CanCreateLink(context.TMP_CreatedLink.Source, this))
                    {
                        var newLink = context.Graph.CreateNewLink(context.TMP_CreatedLink.Source, this);
                        context.Graph.AddLink(newLink);
                    }
                }
                else
                {
                    if (context.Graph.CanCreateLink(this, context.TMP_CreatedLink.Destination))
                    {
                        var newLink = context.Graph.CreateNewLink(this, context.TMP_CreatedLink.Destination);
                        context.Graph.AddLink(newLink);
                    }
                }
            }
        }

        public virtual void AddLink_DONOTUSE_ONLYGRAPH(IGraphLink link)
        {
            _connections.Add(link);
        }

        public virtual void RemoveLink_DONOTUSE_ONLYGRAPH(IGraphLink link)
        {
            if (_isBeingRemoved)
                return;

            _connections.Remove(link);
        }

        public virtual void Draw(bool drawSocketNames = true)
        {
            var context = GraphContext.CurrentContext;
            var style = context.Style;
            float NodeSocketLabelSpacing = style.NodeSocketLabelSpacing * context.UIScale;
            float NodeSocketRadius = style.NodeSocketRadius * context.UIScale;

            var renderPosition = Parent.GetScreenPosition() + RelativePosition;
            DrawSocketArea(renderPosition);

            if (drawSocketNames)
            {
                var socketNameSize = ImGui.CalcTextSize(Name);
                var nameOffset = new Vector2(NodeSocketRadius + NodeSocketLabelSpacing, -(socketNameSize.Y / 2));
                if (Type == SocketType.Output)
                {
                    nameOffset.X = -(nameOffset.X + socketNameSize.X);
                }
                ImGui.GetWindowDrawList().AddText(renderPosition + nameOffset, style.GetColor(GraphStyle.Color.SocketName), Name);
            }
        }

        public virtual void Remove_DONOTUSE_ONLYNODE()
        {
            _isBeingRemoved = true;
            foreach (var connection in _connections)
            {
                GraphContext.CurrentContext.Graph.RemoveLink(connection);
            }

            _connections.Clear();
            Parent = null;
        }
    }
}
