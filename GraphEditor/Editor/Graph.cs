using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace GraphEditor.Editor
{
    public abstract class Graph
    {
        public enum LoadFileErrorCode
        {
            Success,
            FileNotFound,
            NoCR2W,
            UnsupportedCR2WVersion,
            UnexpectedChunkType
        }

        public enum SaveFileErrorCode
        {
            Success,
            Error
        }

        protected enum OpenedMenu
        {
            None,
            Settings,
            Help
        }

        public readonly GraphContext Context;
        public string Filename; 
        public string Name;
        public bool IsSubgraph { get; set; }
        public Graph ActiveSubgraph;
        public IGraphNode ActiveNode { get; private set; }
        public IReadOnlyDictionary<long, IGraphNode> Nodes => _nodes;
        public IReadOnlyDictionary<long, IGraphLink> Links => _links;
        protected SortedDictionary<long, IGraphNode> _nodes = new SortedDictionary<long, IGraphNode>();
        protected SortedDictionary<long, IGraphLink> _links = new SortedDictionary<long, IGraphLink>();
        protected Vector2 _regionMin;
        protected Vector2 _regionMax;
        protected Vector2 _regionSize => _regionMax - _regionMin;
        protected Vector2 _selectionStart;
        protected Vector2 _selectionEnd;
        protected OpenedMenu _openedMenu;
        bool _isDragStarted;
        string _nodeCreationSearchInput = string.Empty;
        Vector2 _contextMenuOpenPos;
        float _nodeUpdateTimer = 0.0f;

        public Graph(string filename)
        {
            Name = Path.GetFileName(filename);
            Filename = filename;
            Context = new GraphContext(this);
        }

        public abstract LoadFileErrorCode Load(string filename);

        public abstract SaveFileErrorCode Save(string filename);

        public virtual void Close()
        {
            _nodes.Clear();
            _links.Clear();
        }

        public virtual bool IsSavable() => true;

        public virtual void AddNode(IGraphNode node)
        {
            _nodes.Add(node.Id, node);
        }

        public virtual void RemoveNode(IGraphNode node)
        {
            node.Remove_DONOTUSE_ONLYGRAPH();
            _nodes.Remove(node.Id);

            if (ActiveNode != null && ActiveNode.Id == node.Id)
                ActiveNode = null;
        }

        public virtual void RemoveNodes(List<IGraphNode> nodes)
        {
            foreach (var node in nodes)
                RemoveNode(node);
        }

        public virtual void AddLink(IGraphLink link)
        {
            _links.Add(link.Id, link);
            link.Source.AddLink_DONOTUSE_ONLYGRAPH(link);
            link.Destination.AddLink_DONOTUSE_ONLYGRAPH(link);
        }

        public virtual void RemoveLink(IGraphLink link)
        {
            link.Remove_DONOTUSE_ONLYGRAPH();
            _links.Remove(link.Id);
        }

        public virtual long GetNextNodeID()
        {
            if (_nodes.Count == 0)
                return 1;
            else
                return _nodes.Keys.Last() + 1;
        }

        public virtual long GetNextLinkID()
        {
            if (_links.Count == 0)
                return 1;
            else
                return _links.Keys.Last() + 1;
        }

        public virtual bool CanCreateLink(IGraphSocket source, IGraphSocket destination)
        {
            // not fake socket (the mouse)
            if (source.Id == -1 || destination.Id == -1)
                return false;

            // not same type
            if (source.Type == destination.Type)
                return false;

            // not same node
            if (source.Parent.Id == destination.Parent.Id)
                return false;

            // doesnt already exist
            if (source.Connections.Any(x => x.Destination.Id == destination.Id))
                return false;

            return true;
        }

        public virtual IGraphLink CreateNewLink(long id, IGraphSocket source, IGraphSocket destination, object extraData = null)
        {
            return new GraphLink(id, source, destination);
        }

        public IGraphLink CreateNewLink(IGraphSocket source, IGraphSocket destination, object extraData = null)
            => CreateNewLink(GetNextLinkID(), source, destination, extraData);

        public void RemoveSelectedNodes()
        {
            var selectedNodes = new List<IGraphNode>();
            selectedNodes.AddRange(Nodes.Values.Where(x => x.IsSelected));

            if (selectedNodes.Count != 0)
                RemoveNodes(selectedNodes);
        }

        // Clears selection of any other node
        public void SelectNode(IGraphNode selectNode)
        {
            ActiveNode = selectNode;
            foreach (var (_, node) in Nodes)
            {
                if (selectNode != null && node.Id == selectNode.Id)
                {
                    node.SelectionType = GraphNodeSelectionType.Single;
                }
                else
                {
                    node.SelectionType = GraphNodeSelectionType.None;
                }
            }
        }

        public void ScrollToNode(IGraphNode node)
        {
            // puts the node in the center of the view:
            Context.ViewOffset = -(node.Position * Context.UIScale);
            Context.ViewOffset -= node.Size / 2;
            Context.ViewOffset += _regionSize / 2;
        }

        public void SetScale(float scale)
        {
            // This zooms to top-left of graph.
            // Should probably zoom to cursor if using CTRL+MouseWheel
            Context.ViewOffset *= scale / Context.UIScale;
            Context.UIScale = scale;
            Context.ForceAllDirty = true;
        }

        // returns ActiveSubgraph if any
        public Graph GetGraph()
        {
            return ActiveSubgraph?.GetGraph() ?? this;
        }

        public void Draw()
        {
            using var _ = new GraphContext.ScopedContext(Context);

            if (_nodeUpdateTimer <= 0.0f)
            {
                foreach (var (_, node) in _nodes)
                    node.UpdateNode();

                _nodeUpdateTimer = Context.UpdateNodeEverySecs;
            }
            else
            {
                _nodeUpdateTimer -= ImGui.GetIO().DeltaTime;
            }

            var drawList = ImGui.GetWindowDrawList();
            // 0 - background
            // 1 - foreground
            // 2 - rendered over foreground, for menus/info/status and such
            drawList.ChannelsSplit(3);

            DrawGrid();

            // Only render if there is no Subgraph shown
            // This prevents a bunch of ugly bugs + makes the graph look better
            if (ActiveSubgraph == null)
            {
                ImGui.SetWindowFontScale(Context.UIScale);

                DrawNodes();
                DrawLinks();

                Context.ForceAllDirty = false;

                ImGui.SetWindowFontScale(1.0f);

                // Menu bar stuff
                {
                    drawList.ChannelsSetCurrent(2); // Foreground - Menus (on top of normal foreground)
                    ImGui.SetCursorScreenPos(_regionMin);
                    ImGui.BeginGroup();
                    DrawMenuBar();
                    DrawActiveMenus();
                    ImGui.EndGroup();
                    drawList.ChannelsSetCurrent(1); // Foreground
                    var padding = ImGui.GetStyle().FramePadding;
                    drawList.AddRectFilled(ImGui.GetItemRectMin() - padding, ImGui.GetItemRectMax() + padding, 0x7F000000);
                }

                HandleMouseKeyboardEvents();

                DrawContextMenu();
            }

            drawList.ChannelsMerge();

            // Must be called after drawList.ChannelsMerge()
            DrawSubgraph();
        }

        protected virtual void DrawSubgraph()
        {
            // recursively close all subgraphs of 'graph'
            static void CloseAllSubgraphs(Graph graph)
            {
                while (graph.ActiveSubgraph != null)
                {
                    var subgraph = graph.ActiveSubgraph;
                    graph.ActiveSubgraph = null;
                    graph = subgraph;
                }
            }

            if (ActiveSubgraph == null)
                return;

            var windowPadding = ImGui.GetStyle().WindowPadding;
            Graph currentGraph = ActiveSubgraph;

            // Draw Subgraph navigation
            ImGui.SetCursorScreenPos(_regionMin + new Vector2(windowPadding.X, 0.0f));
            ImGui.BeginGroup();
            {
                Graph curGraph = this;
                while (curGraph != null)
                {
                    var isRoot = curGraph == this;
                    var graphName = isRoot ? "Root" : curGraph.Name;

                    // Move cursor to a new line if we're going to render out of view
                    {
                        ImGui.SameLine();
                        if (ImGui.GetCursorScreenPos().X + ImGui.CalcTextSize(graphName).X >= _regionMax.X)
                        {
                            ImGui.NewLine();
                        }
                    }

                    if (ImGuiExtensions.AlignedSelectableText(graphName))
                    {
                        CloseAllSubgraphs(curGraph);
                        break;
                    }

                    curGraph = curGraph.ActiveSubgraph;
                    if (curGraph != null)
                    {
                        ImGui.SameLine();
                        ImGuiExtensions.AlignedText("-");

                        currentGraph = curGraph;
                    }
                }
            }
            ImGui.EndGroup();
            if (ActiveSubgraph == null)
                return;

            var subGraphOffsetTop = ImGui.GetItemRectSize().Y + ImGui.GetStyle().FramePadding.Y; // NavBar group
            var subGraphSize = _regionSize - new Vector2(windowPadding.X * 2, subGraphOffsetTop + windowPadding.Y);
            ImGui.PushStyleColor(ImGuiCol.Border, 0xFF0000FF);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, 0xFF000000);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 4);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ImGui.GetStyle().WindowPadding - new Vector2(4, 4));
            ImGui.SetCursorScreenPos(_regionMin + new Vector2(windowPadding.X, subGraphOffsetTop));
            if (ImGui.BeginChild("Subgraph", subGraphSize, true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // don't affect anything drawn in .Draw()
                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor(2);

                currentGraph.Draw();
            }
            ImGui.EndChild();            
        }

        protected virtual void DrawLinks()
        {
            var drawList = ImGui.GetWindowDrawList();
            drawList.ChannelsSetCurrent(0); // Background

            foreach (var (_, link) in _links)
            {
                if (link.Source.Parent.IsHidden || link.Destination.Parent.IsHidden)
                    continue;

                link.Draw();
            }

            if (Context.TMP_CreatedLink != null)
            {
                Context.TMP_CreatedLink.Draw();
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    Context.TMP_CreatedLink = null;
            }
        }

        protected virtual void DrawNodes()
        {
            var isAnyNodeActive = false;
            var drawList = ImGui.GetWindowDrawList();
            drawList.ChannelsSetCurrent(1); // Foreground

            foreach (var (_, node) in _nodes)
            {
                if (Context.ForceAllDirty)
                    node.Dirty = true;

                if (node.IsHidden)
                    continue;

                if (string.IsNullOrEmpty(node.Name))
                {
                    // needs GraphContext.CurrentContext set
                    node.Name = node.GenerateNodeName();
                }

                ImGuiExtensions.PushID(node.Id);
                node.Draw();
                ImGui.PopID();

                if (node.SelectionType == GraphNodeSelectionType.Single)
                {
                    ActiveNode = node;
                    isAnyNodeActive = true;
                }
            }

            // Fixes an issue with multi-select nodes
            if (!isAnyNodeActive)
                ActiveNode = null;
        }

        // If you can call this a 'MenuBar'...
        protected virtual void DrawMenuBar()
        {
            var io = ImGui.GetIO();

            ImGuiExtensions.AlignedText($"{1000.0f / io.Framerate:0.000} ms/frame ({io.Framerate:0.0} FPS)");
            ImGui.SameLine();
            ImGuiExtensions.ToggleButton("Settings", ref _openedMenu, OpenedMenu.Settings, OpenedMenu.None);
            ImGui.SameLine();
            ImGuiExtensions.ToggleButton("Help", ref _openedMenu, OpenedMenu.Help, OpenedMenu.None);
        }

        protected virtual void DrawActiveMenus()
        {
            var style = Context.Style;

            if (_openedMenu == OpenedMenu.Help)
            {
                ImGuiExtensions.AlignedText("Use RightClick to drag viewport");
                ImGuiExtensions.AlignedText("Use RightClick->CreateNode to create nodes");
                ImGuiExtensions.AlignedText("Use CTRL+MouseWheel to zoom in");
                ImGuiExtensions.AlignedText("Use Shift+Mousewheel to scroll horizontally (or horizontal mouse wheel)");
                ImGuiExtensions.AlignedText("Use 'Delete' to delete selected nodes");
                ImGuiExtensions.AlignedText("Use RightClick->Sort->[Load/Save] to load/save the graph sorting. or 'Auto' for automatic sorting");
                ImGuiExtensions.AlignedText("  !! Custom names and comments are saved in the sort file for now !!");
            }
            else if (_openedMenu == OpenedMenu.Settings)
            {
#if DEBUG
                ImGui.Checkbox("DrawHitbox", ref Context.DrawHitbox);
#endif
                if (ImGui.Checkbox("UseFriendlyNodeName", ref Context.UseFriendlyNodeName))
                {
                    foreach (var (_, node) in _nodes)
                        node.Name = node.GenerateNodeName();
                }

                if (ImGui.Checkbox("IncludeIDInNodeName", ref Context.IncludeIDInNodeName))
                {
                    foreach (var (_, node) in _nodes)
                        node.Name = node.GenerateNodeName();
                }

                ImGui.SetNextItemWidth(200.0f);
                if (ImGui.InputFloat("UpdateNodeEverySecs", ref Context.UpdateNodeEverySecs))
                    Context.UpdateNodeEverySecs = Math.Max(0.0f, Context.UpdateNodeEverySecs);
                ImGui.SameLine();
                if (ImGui.Button("Normal (5s)"))
                    Context.UpdateNodeEverySecs = 5.0f;
                ImGui.SameLine();
                if (ImGui.Button("Fast (2s)"))
                    Context.UpdateNodeEverySecs = 2.0f;
                ImGui.SameLine();
                if (ImGui.Button("Instant"))
                    Context.UpdateNodeEverySecs = 0.0f;

                ImGui.SetNextItemWidth(200.0f);
                ImGui.InputFloat("ScrollSpeed", ref Context.ScrollSpeed);

                ImGui.SetNextItemWidth(200.0f);
                var scale = Context.UIScale;
                if (ImGui.SliderFloat("UIScale", ref scale, style.UIScaleMin, style.UIScaleMax))
                {
                    if (scale <= 0.0f)
                        scale = style.UIScaleMin;

                    SetScale(scale);
                }
                ImGui.SameLine();
                if (ImGui.Button("Reset##Scale"))
                {
                    SetScale(1.0f);
                }

                ImGui.SetNextItemWidth(200.0f);
                ImGui.InputFloat2("ViewOffset", ref Context.ViewOffset);
                ImGui.SameLine();
                if (ImGui.Button("Reset##View"))
                {
                    Context.ViewOffset = Vector2.Zero;
                }
            }
        }

        protected virtual void HandleMouseKeyboardEvents()
        {
            var style = Context.Style;
            var io = ImGui.GetIO();
            var isHoveringGraph = ImGui.IsWindowHovered();
            var isHoveringAnyItem = ImGui.IsAnyItemHovered();

            if (!_isDragStarted)
            {
                if (isHoveringGraph)
                {
                    if (!isHoveringAnyItem && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        _isDragStarted = true;
                        _selectionStart = ImGui.GetMousePos();
                        _selectionEnd = _selectionStart;
                    }
                    else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                    {
                        _isDragStarted = true;
                    }
                }
            }
            else if (isHoveringGraph && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _selectionEnd += io.MouseDelta;
            }
            else if (isHoveringGraph && ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                Context.ViewOffset += io.MouseDelta;
            }
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                var selectionRect = new ImRect(_selectionStart, _selectionEnd);
                // Fix rect Min/Max
                {
                    if (selectionRect.Min.X > selectionRect.Max.X)
                    {
                        float tmp = selectionRect.Min.X;
                        selectionRect.Min.X = selectionRect.Max.X;
                        selectionRect.Max.X = tmp;
                    }
                    if (selectionRect.Min.Y > selectionRect.Max.Y)
                    {
                        float tmp = selectionRect.Min.Y;
                        selectionRect.Min.Y = selectionRect.Max.Y;
                        selectionRect.Max.Y = tmp;
                    }
                }

                foreach (var (_, node) in _nodes)
                {
                    var nodeStart = node.GetScreenPosition();
                    var nodeEnd = nodeStart + node.Size;
                    var nodeRect = new ImRect(nodeStart, nodeEnd);

                    if (ImGuiExtensions.RectOverlaps(selectionRect, nodeRect))
                    {
                        node.SelectionType = GraphNodeSelectionType.Multi;
                    }
                }

                _isDragStarted = false;
                _selectionStart = Vector2.Zero;
                _selectionEnd = Vector2.Zero;
            }
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                _isDragStarted = false;

                if (!isHoveringAnyItem && ImGuiExtensions.IsMouseClickedNoDrag(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup("graph_context_menu");
                }
            }

            if (isHoveringGraph && ImGui.IsKeyPressed((int)Keys.Delete))
            {
                if (ActiveNode != null)
                {
                    // only a single node is selected
                    RemoveNode(ActiveNode);
                }
                else
                {
                    RemoveSelectedNodes();
                }
            }

            if (isHoveringGraph)
            {
                if (io.MouseWheel != 0.0f)
                {
                    // Zoom
                    if (io.KeyCtrl)
                    {
                        SetScale(Math.Clamp(Context.UIScale + io.MouseWheel * 0.1f, style.UIScaleMin, style.UIScaleMax));
                    }
                    // ScrollH
                    else if (io.KeyShift)
                    {
                        Context.ViewOffset.X += io.MouseWheel * Context.ScrollSpeed;
                    }
                    // ScrollV
                    else
                    {
                        Context.ViewOffset.Y += io.MouseWheel * Context.ScrollSpeed;
                    }

                }
                else if (io.MouseWheelH != 0.0f)
                {
                    Context.ViewOffset.X += io.MouseWheelH * Context.ScrollSpeed;
                }
            }

            if (_isDragStarted && _selectionStart != _selectionEnd)
            {
                var drawList = ImGui.GetWindowDrawList();
                drawList.ChannelsSetCurrent(0);
                drawList.AddRectFilled(_selectionStart, _selectionEnd, style.GetColor(GraphStyle.Color.SelectionBg));
                drawList.AddRect(_selectionStart, _selectionEnd, style.GetColor(GraphStyle.Color.SelectionBorder));
                drawList.ChannelsSetCurrent(1);
            }
        }

        protected virtual void DrawContextMenu()
        {
            var createNodePopupID = ImGui.GetID("Create a new node");

            if (ImGui.BeginPopup("graph_context_menu"))
            {
                _contextMenuOpenPos = ImGui.GetMousePosOnOpeningCurrentPopup();
                if (ImGui.MenuItem("Reset view"))
                {
                    Context.ViewOffset = Vector2.Zero;
                    SetScale(1.0f);
                }

                if (ImGui.BeginMenu("Sort Nodes"))
                {
                    if (ImGui.MenuItem("Auto"))
                        SortNodes_Auto();
                    if (ImGui.MenuItem("Load", !IsSubgraph))
                        SortNodes_Load();
                    if (ImGui.MenuItem("Save", !IsSubgraph))
                        SortNodes_Save();
                    ImGui.EndMenu();
                }

                if (ImGui.Selectable("Create New Node"))
                {
                    ImGui.OpenPopup(createNodePopupID);
                }

                ImGui.EndPopup();
            }

            var opened = true;
            ImGui.SetNextWindowSize(new Vector2(400.0f), ImGuiCond.Once);
            if (ImGui.BeginPopupModal("Create a new node", ref opened, ImGuiWindowFlags.NoSavedSettings))
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##search-box", "Search", ref _nodeCreationSearchInput, 256);
                if (ImGui.BeginChild("node_list"))
                {
                    var nodeCreationParamsList = GetNodeCreationList();
                    if (nodeCreationParamsList.Count == 0)
                    {
                        ImGuiExtensions.AlignedText("no nodes found!");
                    }

                    // how fast is the string search?
                    // does it need a filtered list?
                    // need ImGuiClipper?
                    for (var i = 0; i != nodeCreationParamsList.Count; ++i)
                    {
                        var nodeCreator = nodeCreationParamsList[i];

                        if (!string.IsNullOrEmpty(_nodeCreationSearchInput)
                            && !nodeCreator.DisplayName.Contains(_nodeCreationSearchInput, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        ImGui.PushID(i);
                        if (ImGui.Selectable(nodeCreator.DisplayName))
                        {
                            var position = ((_contextMenuOpenPos - _regionMin) - Context.ViewOffset) / Context.UIScale;
                            nodeCreator.CreateNew(position);
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.PopID();
                    }
                }
                ImGui.EndChild();
                ImGui.EndPopup();
            }
        }

        protected virtual void SortNodes_Auto(Func<IGraphNode, bool> isStartNode = null)
        {
            var dict = new Dictionary<long, SortedNode>();
            foreach (var (nodeID, node) in _nodes)
                dict.Add(nodeID, new SortedNode((GraphNode)node));
            var sortedNodesDict = SortNodes(dict, isStartNode);

            float nodeSpacingX = 100.0f * Context.UIScale;
            float nodeSpacingY = 100.0f * Context.UIScale;
            float currentXPos = 0.0f;
            foreach (var (_, sortedNodes) in sortedNodesDict.OrderBy(x => x.Key))
            {
                if (sortedNodes.Count == 0) continue;
                float maxNodeWidth = 0.0f;
                float currentYPos = 0.0f;

                foreach (var sortedNode in sortedNodes)
                {
                    sortedNode.node.Position = new Vector2(currentXPos, currentYPos) / Context.UIScale;
                    currentYPos += sortedNode.node.Size.Y + nodeSpacingY;
                    maxNodeWidth = Math.Max(maxNodeWidth, sortedNode.node.Size.X);
                }

                currentXPos += nodeSpacingX + maxNodeWidth;
            }
        }

        protected abstract void SortNodes_Load();
        protected abstract void SortNodes_Save();

        protected abstract IReadOnlyList<IGraphNodeCreateParams> GetNodeCreationList();

        void DrawGrid()
        {
            var style = Context.Style;
            var drawList = ImGui.GetWindowDrawList();
            drawList.ChannelsSetCurrent(0); // Background

            _regionMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
            _regionMax = ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos();

            drawList.AddRectFilled(_regionMin, _regionMin + _regionMax, style.GetColor(GraphStyle.Color.GridBg));

            for (float x = Context.ViewOffset.X % style.GridSize; x < _regionMax.X; x += style.GridSize)
            {
                drawList.AddLine(new Vector2(_regionMin.X + x, _regionMin.Y),
                    new Vector2(_regionMin.X + x, _regionMax.Y + _regionMax.Y),
                    style.GetColor(GraphStyle.Color.GridLine), 1.0f);
            }

            for (float y = Context.ViewOffset.Y % style.GridSize; y < _regionMax.Y; y += style.GridSize)
            {
                drawList.AddLine(new Vector2(_regionMin.X, _regionMin.Y + y),
                    new Vector2(_regionMin.X + _regionMax.X, _regionMin.Y + y),
                    style.GetColor(GraphStyle.Color.GridLine), 1.0f);
            }
        }

        // ------------------------ TEMP SOLUTION FOR SORT ------------------------
        // TODO: Fix auto node sorting
        // ------------------------------------------------------------------------
        class SortedNode
        {
            internal IGraphNode node;
            internal bool IsSorted;
            internal bool IsBeingSorted;
            internal int Column;
            //internal int Row;

            internal SortedNode(GraphNode node)
            {
                this.node = node;
            }
        };

        // what am i doing
        // If A connects to B, B is put 1 column after A. that's pretty much it.
        // big graph = possible stack overflow
        static Dictionary<int, List<SortedNode>> SortNodes(Dictionary<long, SortedNode> nodes, Func<IGraphNode, bool> isStartNode = null)
        {
            var sortedNodes = new Dictionary<int, List<SortedNode>>();

            IEnumerable<SortedNode> startNodes = null;
            if (isStartNode != null)
                startNodes = nodes.Values.Where(x => isStartNode(x.node));

            if (startNodes == null || !startNodes.Any())
                startNodes = nodes.Values;

            void SetColumn(SortedNode node, int column, int atIndex = -1)
            {
                if (sortedNodes.TryGetValue(node.Column, out var oldColumnNodes))
                {
                    oldColumnNodes.Remove(node);
                }

                if (sortedNodes.TryGetValue(column, out var columnNodes))
                {
                    if (atIndex == -1 || atIndex > columnNodes.Count)
                        columnNodes.Add(node);
                    else
                        columnNodes.Insert(atIndex, node);
                }
                else
                {
                    sortedNodes.Add(column, new List<SortedNode>() { node });
                }

                node.Column = column;
                node.IsSorted = true;
            }

            foreach (var node in startNodes)
            {
                void Sort(SortedNode node)
                {
                    // dont handle recursive loops
                    if (node.IsBeingSorted) return;
                    node.IsBeingSorted = true;
                    foreach (var (_, socket) in node.node.OutSockets)
                    {
                        foreach (var connection in socket.Connections)
                        {
                            var linkedNode = nodes[connection.Destination.Parent.Id];
                            if (linkedNode.Column > node.Column) continue;
                            SetColumn(linkedNode, node.Column + 1);
                            Sort(linkedNode);
                        }
                    }
                    node.IsBeingSorted = false;
                    SetColumn(node, node.Column);
                }
                Sort(node);
            }

            // Puts start node behind the first node it links to and at the same row
            foreach (var node in startNodes)
            {
                SortedNode closestNode = null;
                foreach (var (_, socket) in node.node.OutSockets)
                {
                    foreach (var connection in socket.Connections)
                    {
                        var linkedNode = nodes[connection.Destination.Parent.Id];
                        if (closestNode == null)
                            closestNode = linkedNode;
                        else if (linkedNode.Column < closestNode.Column)
                            closestNode = linkedNode;
                        else if (linkedNode.Column == closestNode.Column)
                        {
                            // check row
                        }
                    }
                }

                if (closestNode != null && closestNode.Column != (node.Column + 1))
                {
                    SetColumn(node, closestNode.Column - 1, sortedNodes[closestNode.Column].IndexOf(closestNode));
                }
            }

            // any leftover nodes are put in the first column
            foreach (var (_, node) in nodes.Where(x => !x.Value.IsSorted))
                SetColumn(node, 0);

            return sortedNodes;
        }
    }
}
