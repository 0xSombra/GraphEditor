using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace GraphEditor.Editor
{
    public enum GraphNodeSelectionType
    {
        None,
        Single,
        Multi
    }

    public class GraphNode : IGraphNode
    {
        public bool Dirty { get; set; } = true;
        public long Id => _id;
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size => _size;
        public string Name
        {
            get => string.IsNullOrEmpty(CustomName) ? _name : CustomName;
            set { _name = value; Dirty = true; }
        }
        public string CustomName
        {
            get => _customName;
            set { _customName = value; Dirty = true; }
        }
        public string Comment
        {
            get => _comment;
            set { _comment = value; Dirty = true; }
        }
        public string Description
        {
            get => _description;
            set { _description = value; Dirty = true; }
        }
        public GraphNodeSelectionType SelectionType
        {
            get => _selectionType;
            set
            {
                if (!IsHidden)
                    _selectionType = value;

                if (value == GraphNodeSelectionType.None)
                    _isDragStarted = false;
            }
        }
        public IGraphSocket ActiveSocket { get; protected set; }
        public IReadOnlyDictionary<long, IGraphSocket> InSockets => _inSockets;
        public IReadOnlyDictionary<long, IGraphSocket> OutSockets => _outSockets;
        public bool IsSelected => SelectionType != GraphNodeSelectionType.None;
        public bool IsHidden { get; set; }
        protected readonly long _id;
        protected string _altName;
        protected string _name;
        protected string _customName;
        protected string _comment;
        protected string _description; // under name, dont know what to call it
        protected Vector2 _size = Vector2.Zero;
        protected bool _isHovered;
        protected bool _isDragStarted;
        protected bool _isAnySocketHovered;
        protected SortedDictionary<long, IGraphSocket> _inSockets = new SortedDictionary<long, IGraphSocket>();
        protected SortedDictionary<long, IGraphSocket> _outSockets = new SortedDictionary<long, IGraphSocket>();
        protected IGraphSocket _contextMenuSocket;
        GraphNodeSelectionType _selectionType;
        // ---- For default CalculateSize and DrawNode ----
        Vector2 _namePlateSize;

        public GraphNode(long id, string name = null)
        {
            _id = id;
            _altName = string.IsNullOrEmpty(name) ? "Node" : name;
        }

        public virtual void AddInputSocket(IGraphSocket socket)
        {
            Dirty = true;
            socket.Parent = this;
            socket.Type = SocketType.Input;
            _inSockets.Add(socket.Id, socket);
        }

        public virtual void RemoveInputSocket(IGraphSocket socket)
        {
            Dirty = true;
            _inSockets.Remove(socket.Id);
            socket.Remove_DONOTUSE_ONLYNODE();

            if (ActiveSocket != null && ActiveSocket.Id == socket.Id)
                ActiveSocket = null;
        }

        public virtual void AddOutputSocket(IGraphSocket socket)
        {
            Dirty = true;
            socket.Parent = this;
            socket.Type = SocketType.Output;
            _outSockets.Add(socket.Id, socket);
        }

        public virtual void RemoveOutputSocket(IGraphSocket socket)
        {
            Dirty = true;
            _outSockets.Remove(socket.Id);
            socket.Remove_DONOTUSE_ONLYNODE();

            if (ActiveSocket != null && ActiveSocket.Id == socket.Id)
                ActiveSocket = null;
        }

        public void RemoveInputSocket(long id)
            => RemoveInputSocket(_inSockets[id]);

        public void RemoveOutputSocket(long id)
            => RemoveOutputSocket(_outSockets[id]);

        public Vector2 GetLocalPosition()
        {
            var context = GraphContext.CurrentContext;
            return (Position * context.UIScale) + context.ViewOffset + ImGui.GetWindowContentRegionMin();
        }

        public Vector2 GetScreenPosition()
        {
            return GetLocalPosition() + ImGui.GetWindowPos();
        }

        public virtual void Draw()
        {
            _isHovered = false;
            _isAnySocketHovered = false;

            if (Dirty)
            {
                CalculateSize();
                Dirty = false;
            }

            ImGuiExtensions.PushID(Id);
            ImGui.SetCursorPos(GetLocalPosition());
            DrawHitboxArea(Size);
            ImGui.PopID();
            if (ImGui.IsItemVisible())
            {
                _isHovered = ImGuiExtensions.IsItemHovered_Overlap();
                DrawNode();
                DrawSockets();
            }

            if (!_isAnySocketHovered)
                ActiveSocket = null;

            // hacked together so context menus arent scaled
            // alternatively, could let graph call this function after rendering all nodes
            // but it would loop over the nodes twice.
            ImGui.SetWindowFontScale(1.0f);
            DrawContextMenu();
            ImGui.SetWindowFontScale(GraphContext.CurrentContext.UIScale);

            HandleMouseKeyboardEvents();
        }

        public virtual void Remove_DONOTUSE_ONLYGRAPH()
        {
            foreach (var (_, socket) in InSockets)
                socket.Remove_DONOTUSE_ONLYNODE();

            foreach (var (_, socket) in OutSockets)
                socket.Remove_DONOTUSE_ONLYNODE();

            _inSockets.Clear();
            _outSockets.Clear();
            ActiveSocket = null;
        }

        public virtual string GenerateNodeName()
        {
            var context = GraphContext.CurrentContext;
            return context.IncludeIDInNodeName ? $"{_altName} #{Id}" : _altName;
        }

        public virtual void UpdateNode(bool manual = false)
        {
            // Useful if node definition was manually modified
        }

        public virtual void DrawProperties()
        {
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

        protected virtual void DrawContextMenu()
        {
            if (ImGui.BeginPopup("node_context_menu"))
            {
                if (ImGui.MenuItem("Update"))
                {
                    UpdateNode(manual: true);
                }
                ImGui.EndPopup();
            }

            if (ImGui.BeginPopup("socket_context_menu"))
            {
                var linksToBeDeleted = new List<IGraphLink>();

                ImGuiExtensions.AlignedDisabledText(_contextMenuSocket.Name);
                ImGui.SameLine();
                if (ImGuiExtensions.RightAlignedButton("Clear"))
                {
                    linksToBeDeleted.AddRange(_contextMenuSocket.Connections);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.Separator();

                if (_contextMenuSocket.Connections.Count != 0)
                {
                    var isInputSocket = _contextMenuSocket.Type == SocketType.Input;
                    for (var i = 0; i != _contextMenuSocket.Connections.Count; ++i)
                    {
                        var connection = _contextMenuSocket.Connections[i];

                        IGraphSocket connectedToSocket;
                        if (isInputSocket)
                            connectedToSocket = connection.Source;
                        else
                            connectedToSocket = connection.Destination;

                        if (ImGuiExtensions.AlignedSelectableText($"{connectedToSocket.Parent.Name}::{connectedToSocket.Name}"))
                        {
                            connection.IsHighlighted = false;
                            var graph = GraphContext.CurrentContext.Graph;
                            graph.SelectNode(connectedToSocket.Parent);
                            graph.ScrollToNode(connectedToSocket.Parent);

                            ImGui.CloseCurrentPopup();
                        }
                        else
                        {
                            connection.IsHighlighted = ImGui.IsItemHovered();
                        }

                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
                        if (ImGuiExtensions.IconButton_Remove(i))
                        {
                            linksToBeDeleted.Add(connection);
                            //ImGui.CloseCurrentPopup();
                        }
                        ImGui.PopStyleColor();
                    }
                }

                foreach (var connection in linksToBeDeleted)
                {
                    GraphContext.CurrentContext.Graph.RemoveLink(connection);
                }

                ImGui.EndPopup();
            }
        }

        protected virtual void HandleMouseKeyboardEvents()
        {
            var context = GraphContext.CurrentContext;
            var io = ImGui.GetIO();
            var isHoveringGraph = ImGui.IsWindowHovered();
            var isHoveringAnyItem = ImGui.IsAnyItemHovered();

            // Off by 1 frame
            // If a node is single-selected and it's not us
            // Fixes an issue with multi-selection
            if (context.Graph.ActiveNode != null && context.Graph.ActiveNode.Id != Id)
            {
                SelectionType = GraphNodeSelectionType.None;
            }

            if (!_isHovered && isHoveringGraph && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (SelectionType == GraphNodeSelectionType.Single ||
                    (SelectionType == GraphNodeSelectionType.Multi && !isHoveringAnyItem))
                {
                    SelectionType = GraphNodeSelectionType.None;
                }
            }

            if (!_isDragStarted)
            {
                if (isHoveringGraph && (_isHovered || IsSelected) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _isDragStarted = true;
                    if (!IsSelected)
                        SelectionType = GraphNodeSelectionType.Single;
                }
            }
            else if (isHoveringGraph && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                Position += io.MouseDelta / new Vector2(context.UIScale);
            }
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                _isDragStarted = false;
            }
            
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                if (ImGuiExtensions.IsMouseClickedNoDrag(ImGuiMouseButton.Right))
                {
                    if (ActiveSocket != null)
                    {
                        _contextMenuSocket = ActiveSocket;
                        ImGui.OpenPopup("socket_context_menu");
                    }
                    else if (_isHovered)
                    {
                        ImGui.OpenPopup("node_context_menu");
                    }
                }
            }

        }

        protected virtual void CalculateSize()
        {
            var context = GraphContext.CurrentContext;
            var style = context.Style;
            float NodeTextPaddingX = style.NodeTextPadding.X * context.UIScale;
            float NodeTextPaddingY = style.NodeTextPadding.Y * context.UIScale;
            float NodeSocketLabelSpacing = style.NodeSocketLabelSpacing * context.UIScale;
            float NodeSocketRadius = style.NodeSocketRadius * context.UIScale;
            float NodeSocketStartX = style.NodeSocketStartX * context.UIScale;
            float NodeSocketSpacingX = style.NodeSocketSpacing.X * context.UIScale;
            float NodeSocketSpacingY = style.NodeSocketSpacing.Y * context.UIScale;
            float NodeBoxPadding = style.NodeBoxPadding * context.UIScale;
            float BorderThickness = style.BorderThickness * context.UIScale;

            var nameSize = ImGui.CalcTextSize(Name);
            var descriptionSize = string.IsNullOrEmpty(Description) ? Vector2.Zero : ImGui.CalcTextSize(Description);

            float widthRequiredForSockets = 0.0f;
            float heightRequiredForSockets = NodeSocketSpacingY * Math.Max(InSockets.Count, OutSockets.Count);
            for (var i = 0; i != Math.Max(InSockets.Count, OutSockets.Count); ++i)
            {
                float inputSocketNameWidth = 0.0f;
                if (i < InSockets.Count)
                    inputSocketNameWidth = NodeSocketRadius + NodeSocketLabelSpacing + ImGui.CalcTextSize(InSockets.ElementAt(i).Value.Name).X;

                float outputSocketNameWidth = 0.0f;
                if (i < OutSockets.Count)
                    outputSocketNameWidth = NodeSocketRadius + NodeSocketLabelSpacing + ImGui.CalcTextSize(OutSockets.ElementAt(i).Value.Name).X;

                float width = inputSocketNameWidth + NodeSocketSpacingX + outputSocketNameWidth;
                widthRequiredForSockets = Math.Max(widthRequiredForSockets, width);
            }

            var textSizeX = Math.Max(nameSize.X, descriptionSize.X);
            var textSizeY = nameSize.Y;
            if (!string.IsNullOrEmpty(Description))
            {
                textSizeY += NodeTextPaddingY + descriptionSize.Y;
            }

            var hasSockets = widthRequiredForSockets != 0.0f;
            var bottomPadding = NodeBoxPadding - (hasSockets ? NodeSocketSpacingY : 0.0f);
            _size = new Vector2(Math.Max(textSizeX + NodeTextPaddingX * 2, widthRequiredForSockets),
                textSizeY + NodeTextPaddingY * 2
                + heightRequiredForSockets
                + bottomPadding);

            _namePlateSize = new Vector2(Size.X, textSizeY + NodeTextPaddingY * 2) - new Vector2(BorderThickness / 2);
            var socketsYOffset = _namePlateSize.Y + NodeSocketStartX;

            void UpdateSocketPosition(IReadOnlyDictionary<long, IGraphSocket> sockets, bool isRightAligned)
            {
                var socketPosition = new Vector2(isRightAligned ? Size.X : 0.0f, socketsYOffset);
                foreach (var (_, socket) in sockets)
                {
                    socket.RelativePosition = socketPosition;
                    socketPosition.Y += NodeSocketSpacingY;
                }
            }

            UpdateSocketPosition(InSockets, false);
            UpdateSocketPosition(OutSockets, true);
        }

        protected virtual void DrawNode()
        {
            var context = GraphContext.CurrentContext;
            var style = context.Style;
            float BorderRoundness = style.BorderRoundness * context.UIScale;
            float BorderThickness = style.BorderThickness * context.UIScale;

            var drawList = ImGui.GetWindowDrawList();
            var renderPosition = GetScreenPosition();

            {
                var nodeBoxStart = renderPosition;
                var nodeBoxEnd = nodeBoxStart + Size;

                drawList.AddRectFilled(nodeBoxStart, nodeBoxEnd, style.GetColor(GraphStyle.Color.NodeBg), style.BorderRoundness);
                var borderColor = style.GetColor(IsSelected ? GraphStyle.Color.ActiveNodeBorder : GraphStyle.Color.NodeBorder);
                drawList.AddRect(nodeBoxStart, nodeBoxEnd, borderColor, BorderRoundness, ImDrawFlags.None, BorderThickness);
            }

            {
                float NodeTextPaddingY = style.NodeTextPadding.Y * context.UIScale;

                var nameSize = ImGui.CalcTextSize(Name);
                var namePos = new Vector2(Size.X / 2 - nameSize.X / 2, NodeTextPaddingY);

                drawList.AddRectFilled(renderPosition + new Vector2(BorderThickness / 2), renderPosition + _namePlateSize, style.GetColor(GraphStyle.Color.NodeNameplate), BorderRoundness, ImDrawFlags.RoundCornersTop);
                drawList.AddText(renderPosition + namePos, style.GetColor(GraphStyle.Color.NodeName), Name);

                if (!string.IsNullOrEmpty(Description))
                {
                    var descriptionPosY = namePos.Y + nameSize.Y + NodeTextPaddingY;
                    var splitDescriptions = Description.Split('\n');
                    foreach (var splitDescription in splitDescriptions)
                    {
                        var descriptionSize = ImGui.CalcTextSize(splitDescription);
                        var descriptionPos = new Vector2(Size.X / 2 - descriptionSize.X / 2, descriptionPosY);
                        drawList.AddText(renderPosition + descriptionPos, style.GetColor(GraphStyle.Color.NodeDescription), splitDescription);
                        descriptionPosY += descriptionSize.Y; // padded
                    }
                }
            }

            if (!string.IsNullOrEmpty(Comment))
            {
                float NodeTextPaddingY = style.NodeTextPadding.Y * context.UIScale;

                var commentSize = ImGui.CalcTextSize(Comment);
                var commentPosition = renderPosition - new Vector2(0.0f, commentSize.Y + NodeTextPaddingY);
                drawList.AddText(commentPosition, style.GetColor(GraphStyle.Color.NodeComment), Comment);
            }
        }

        protected virtual void DrawSockets(bool drawSocketNames = true)
        {
            void DrawSocketsDict(IReadOnlyDictionary<long, IGraphSocket> sockets)
            {
                foreach (var (_, socket) in sockets)
                {
                    socket.Draw(drawSocketNames);
                    if (socket.IsHovered)
                    {
                        ActiveSocket = socket;
                        _isAnySocketHovered = true;
                    }
                }
            }

            DrawSocketsDict(InSockets);
            DrawSocketsDict(OutSockets);
        }
    }
}
