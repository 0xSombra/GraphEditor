using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using ImGuiNET;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using GraphEditor.Editor;

namespace GraphEditor
{
    internal static class MainWindow
    {
        enum FileFilterIndex
        {
            None,
            CP77_QuestResource,
            CP77_SceneResource
        }

        static OpenFileDialog OFileDialog;
        static SaveFileDialog SFileDialog;
        static List<Graph> Graphs = new List<Graph>();
        static Graph ActiveGraph;
        // ------ ImGui stuff ------
        static bool ShowGraphs = true;
        static bool ShowProperties = true;
        static bool ShowNodeList = true;
        static bool InitializedDockspace;
        const ImGuiDockNodeFlags RootDockFlags = ImGuiDockNodeFlags.PassthruCentralNode;
        static uint DockID_Root;
        static uint DockID_Root_Top;
        static uint DockID_Root_Bottom;
        static uint DockID_Root_Top_Left;
        static uint DockID_Root_Top_Right;
        static uint __PopupMessageID; // set inside DrawPopups
        static string PopupMessage;

        static MainWindow()
        {
            var sbFilter = new StringBuilder();
            sbFilter.Append("CP77 QuestResource|*.quest;*.questphase");
            sbFilter.Append("|CP77 SceneResource|*.scene");

            OFileDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter = sbFilter.ToString(),
                Multiselect = true
            };

            SFileDialog = new SaveFileDialog()
            {
                OverwritePrompt = true
            };
        }

        internal static void ShowPopup(string message)
        {
            PopupMessage = message;
            ImGui.OpenPopup(__PopupMessageID);
        }

        internal static bool CreateAndLoadGraph<T>(string filename) where T : Graph
        {
            Graph graph;
            // new T(string);
            {
                var bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                graph = (Graph)Activator.CreateInstance(typeof(T), bindings, null, new object[1] { filename }, null);
            }

            var errorCode = graph.Load(filename);
            if (errorCode == Graph.LoadFileErrorCode.Success)
            {
                Graphs.Add(graph);
                return true;
            }
            else
            {
                ShowPopup($"Failed to load '{Path.GetFileName(filename)}'\nErrorCode: {errorCode}");
                return false;
            }
        }

        static void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Open"))
                    {
                        if (OFileDialog.ShowDialog() == true)
                        {
                            if (OFileDialog.FilterIndex == (int)FileFilterIndex.CP77_QuestResource)
                            {
                                foreach (var filename in OFileDialog.FileNames)
                                    CreateAndLoadGraph<CP77.Quest.Graph>(filename);
                            }
                            else if (OFileDialog.FilterIndex == (int)FileFilterIndex.CP77_SceneResource)
                            {
                                foreach (var filename in OFileDialog.FileNames)
                                    CreateAndLoadGraph<CP77.Scene.Graph>(filename);
                            }
                        }
                    }

                    if (ImGui.MenuItem("Save As", ActiveGraph?.IsSavable() ?? false))
                    {
                        SFileDialog.FileName = ActiveGraph.Name;
                        if (SFileDialog.ShowDialog() == true)
                        {
                            var errorCode = ActiveGraph.Save(SFileDialog.FileName);
                            if (errorCode != Graph.SaveFileErrorCode.Success)
                            {
                                ShowPopup($"Failed to save '{Path.GetFileName(SFileDialog.FileName)}'\nErrorCode: {errorCode}");
                            }
                        }
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Window"))
                {
                    ImGui.Checkbox("Show Graphs", ref ShowGraphs);
                    ImGui.Checkbox("Show Properties", ref ShowProperties);
                    ImGui.Checkbox("Show Node List", ref ShowNodeList);

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Cyberpunk 2077"))
                {
                    if (ImGui.MenuItem("Load styling.json"))
                    {
                        var path = "styling.json";
                        if (!File.Exists(path))
                        {
                            var ofd = new OpenFileDialog()
                            {
                                CheckFileExists = true,
                                Filter = "Styling.json|*.json"
                            };

                            if (ofd.ShowDialog() == true)
                            {
                                path = ofd.FileName;
                            }
                        }
                        CP77.TmpNodeStyling.Reload(path);
                    }

                    if (ImGui.MenuItem("Load tweakdb.str"))
                    {
                        var path = "tweakdb.str";
                        if (!File.Exists(path))
                        {
                            var ofd = new OpenFileDialog()
                            {
                                CheckFileExists = true,
                                Filter = "TweakDB.str|tweakdb.str"
                            };

                            if (ofd.ShowDialog() == true)
                            {
                                path = ofd.FileName;
                            }
                        }
                        CP77.TweakDB.Reload(path);
                    }

                    ImGui.EndMenu();
                }

#if DEBUG
                if (ImGui.BeginMenu("Debug"))
                {
                    if (ImGui.MenuItem("GC.Collect()"))
                    {
                        GC.Collect();
                    }

                    ImGui.EndMenu();
                }
#endif

                var link = "https://github.com/0xSombra/GraphEditor";
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(link).X);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y - 1);
                if (ImGuiExtensions.AlignedSelectableText(link))
                {
                    Process.Start(new ProcessStartInfo("https://github.com/0xSombra/GraphEditor") { UseShellExecute = true });
                }

                ImGui.EndMenuBar();
            }
        }

        static void DrawProperties()
        {
            if (ActiveGraph == null)
                return;

            ActiveGraph.GetGraph().ActiveNode?.DrawProperties();
        }

        unsafe static void DrawNodeList()
        {
            if (ActiveGraph == null)
                return;

            // HACK - This is fine :)
            var actualGraph = ActiveGraph.GetGraph();
            using var _ = new GraphContext.ScopedContext(actualGraph.Context);

            var oFriendlyName = actualGraph.Context.UseFriendlyNodeName;
            var oIncludeIDName = actualGraph.Context.IncludeIDInNodeName;
            actualGraph.Context.UseFriendlyNodeName = false;
            actualGraph.Context.IncludeIDInNodeName = true;

            var clipperStruct = ImGuiExtensions.NewImGuiListClipper();
            var clipper = new ImGuiListClipperPtr(&clipperStruct);
            clipper.Begin(actualGraph.Nodes.Count);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i != clipper.DisplayEnd; ++i)
                {
                    var node = actualGraph.Nodes.ElementAt(i).Value;
                    var nodeName = string.IsNullOrEmpty(node.CustomName) ? node.GenerateNodeName() : node.CustomName;
                    if (node.IsHidden)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, 0x8F0000FF);
                    }

                    if (ImGui.Selectable(nodeName, node.IsSelected))
                    {
                        actualGraph.SelectNode(node);
                        actualGraph.ScrollToNode(node);
                    }

                    if (node.IsHidden)
                    {
                        ImGui.PopStyleColor();
                    }
                }
            }
            clipper.End();

            // Revert hack
            actualGraph.Context.UseFriendlyNodeName = oFriendlyName;
            actualGraph.Context.IncludeIDInNodeName = oIncludeIDName;
        }

        static void DrawPopups()
        {
            if (__PopupMessageID == 0)
                __PopupMessageID = ImGui.GetID("Messagebox");
            var opened = true;
            if (ImGui.BeginPopupModal("Messagebox", ref opened, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize))
            {
                ImGuiExtensions.AlignedText(PopupMessage);
                if (ImGui.Button("OK"))
                    ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
        }

        internal static void Draw()
        {
            if (Graphs.Count == 0 || !ShowGraphs)
                ActiveGraph = null;

            var io = ImGui.GetIO();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X, io.DisplaySize.Y), ImGuiCond.Always);
            if (ImGui.Begin("Root", ImGuiWindowFlags.NoSavedSettings
                                    | ImGuiWindowFlags.NoTitleBar
                                    | ImGuiWindowFlags.NoBackground
                                    | ImGuiWindowFlags.NoResize
                                    | ImGuiWindowFlags.NoScrollbar
                                    | ImGuiWindowFlags.NoScrollWithMouse
                                    | ImGuiWindowFlags.NoBringToFrontOnFocus
                                    | ImGuiWindowFlags.MenuBar))
            {
                DrawMenuBar();

                if (!InitializedDockspace)
                {
                    DockID_Root = ImGui.GetID("Dock_Root");
                    bool dockNodeExists;

                    unsafe
                    {
                        dockNodeExists = ImGui.DockBuilderGetNode(DockID_Root) != (IntPtr*)IntPtr.Zero;
                    }

                    if (!dockNodeExists)
                    {
                        ImGui.DockBuilderAddNode(DockID_Root, RootDockFlags);
                        ImGui.DockBuilderSplitNode(DockID_Root, ImGuiDir.Up, 0.375f, out DockID_Root_Top, out DockID_Root_Bottom);
                        ImGui.DockBuilderSplitNode(DockID_Root_Top, ImGuiDir.Left, 0.174f, out DockID_Root_Top_Left, out DockID_Root_Top_Right);
                        ImGui.DockBuilderFinish(DockID_Root);
                    }
                    InitializedDockspace = true;
                }
                ImGui.DockSpace(DockID_Root, Vector2.Zero, RootDockFlags);
                if (ShowGraphs)
                {
                    ImGui.SetNextWindowDockID(DockID_Root_Bottom, ImGuiCond.FirstUseEver);
                    if (ImGui.Begin("Graphs", ref ShowGraphs))
                    {
                        if (ImGui.BeginTabBar("Tabs", ImGuiTabBarFlags.Reorderable
                                                    | ImGuiTabBarFlags.AutoSelectNewTabs
                                                    | ImGuiTabBarFlags.TabListPopupButton
                                                    | ImGuiTabBarFlags.NoTooltip))
                        {
                            Graph graphToBeClosed = null;
                            for (var i = 0; i != Graphs.Count; ++i)
                            {
                                var graph = Graphs[i];

                                // This allows opening same file twice
                                ImGui.PushID(i);

                                var opened = true;
                                var tab_active = ImGui.BeginTabItem(graph.Name, ref opened);

                                if (ImGui.IsItemHovered())
                                    ImGui.SetTooltip(graph.Filename);

                                if (tab_active)
                                {
                                    ActiveGraph = graph;
                                    if (ImGui.BeginChild("graph-content-area", ImGui.GetContentRegionAvail(), false,
                                        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                                    {
                                        graph.Draw();
                                    }
                                    ImGui.EndChild();
                                    ImGui.EndTabItem();
                                }

                                if (!opened)
                                    graphToBeClosed = graph;

                                ImGui.PopID();
                            }

                            if (graphToBeClosed != null)
                            {
                                graphToBeClosed.Close();
                                Graphs.Remove(graphToBeClosed);
                            }
                        }
                        ImGui.EndTabBar();
                    }
                    ImGui.End();
                }

                if (ShowNodeList)
                {
                    ImGui.SetNextWindowDockID(DockID_Root_Top_Left, ImGuiCond.FirstUseEver);
                    if (ImGui.Begin("Node List", ref ShowNodeList))
                    {
                        DrawNodeList();
                    }
                    ImGui.End();
                }

                if (ShowProperties)
                {
                    ImGui.SetNextWindowDockID(DockID_Root_Top_Right, ImGuiCond.FirstUseEver);
                    if (ImGui.Begin("Properties", ref ShowProperties))
                    {
                        DrawProperties();
                    }
                    ImGui.End();
                }

                DrawPopups();
            }
            ImGui.End();
        }
    }
}
