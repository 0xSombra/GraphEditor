using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Quest
{
    internal class GraphNode : Editor.GraphNode
    {
        internal CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> NodeHandle { get; private set; }

        internal CP77Types.questNodeDefinition NodeDef => NodeHandle.GetInstance<CP77Types.questNodeDefinition>();

        public GraphNode(CP77Types.CHandle<CP77Types.graphGraphNodeDefinition> nodeHandle, long id, string name)
            : base(id, name)
        {
            NodeHandle = nodeHandle;
        }

        public static bool GetFriendlyName(string fullTypeName, out string friendlyName)
        {
            var match = Regex.Match(fullTypeName, "^[a-z]+(.+)NodeDefinition$");
            if (match.Success)
            {
                friendlyName = match.Groups[1].Value;
                friendlyName = Regex.Replace(friendlyName, "([A-Z][a-z0-9]+|[A-Z0-9]{1,}(?=[A-Z]|$))", "$1 ");
                friendlyName = friendlyName.TrimEnd();
                return true;
            }

            friendlyName = fullTypeName;
            return false;
        }

        public override string GenerateNodeName()
        {
            var context = Editor.GraphContext.CurrentContext;

            if (context.UseFriendlyNodeName)
            {
                if (GetFriendlyName(_altName, out var friendlyName))
                {
                    return context.IncludeIDInNodeName ? $"{friendlyName} #{Id}" : friendlyName;
                }
            }

            return context.IncludeIDInNodeName ? $"{_altName} #{Id}" : _altName;
        }
        
        public override void DrawProperties()
        {
            if (ImGui.CollapsingHeader("questNodeDefinition", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable("property_table", 2, PropertyReflectionHelper.TableFlags))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText("Name");
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText(_name);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText("Comment");
                    ImGui.TableNextColumn();
                    var comment = _comment ?? string.Empty;
                    if (ImGui.InputTextMultiline("##Comment", ref comment, 255, new Vector2(-1.0f, 35.0f)))
                    {
                        Comment = comment;
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText("CustomName");
                    ImGui.TableNextColumn();
                    var customName = _customName ?? string.Empty;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputText("##CustomName", ref customName, 256))
                    {
                        CustomName = customName;
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText("Type");
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText(NodeDef.REDType);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText("HandleID");
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText(NodeHandle.Reference.ChunkIndex.ToString());

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText("ID");
                    ImGui.TableNextColumn();
                    ImGuiExtensions.AlignedText(Id.ToString());

                    ImGui.EndTable();
                }
            }

            PropertyReflectionHelper.Draw(NodeDef, typeof(CP77Types.questNodeDefinition));
        }

        public override void Draw()
        {
            bool isStyled = false;
            if (TmpNodeStyling.TryGetStyling(NodeDef.GetType(), out var stylingColors))
            {
                var style = Editor.GraphContext.CurrentContext.Style;
                foreach (var stylingColor in stylingColors)
                    style.PushColor(stylingColor.Key, stylingColor.Value);

                isStyled = true;
            }

            base.Draw();

            if (isStyled)
            {
                Editor.GraphContext.CurrentContext.Style.PopColor(stylingColors.Count);
            }
        }

        public override void AddInputSocket(Editor.IGraphSocket socket)
        {
            var graph = (Graph)Editor.GraphContext.CurrentContext?.Graph;
            if (graph?.CR2WFile == null)
            {
                base.AddInputSocket(socket);
                return;
            }

            NodeDef.Sockets.Add(((GraphSocket)socket).SocketHandle);
            base.AddInputSocket(socket);
        }

        public override void RemoveInputSocket(Editor.IGraphSocket socket)
        {
            var graph = (Graph)Editor.GraphContext.CurrentContext?.Graph;
            if (graph?.CR2WFile == null)
            {
                base.RemoveInputSocket(socket);
                return;
            }

            NodeDef.Sockets.Remove(((GraphSocket)socket).SocketHandle);
            base.RemoveInputSocket(socket);
        }

        public override void AddOutputSocket(Editor.IGraphSocket socket)
        {
            var graph = (Graph)Editor.GraphContext.CurrentContext?.Graph;
            if (graph?.CR2WFile == null)
            {
                base.AddOutputSocket(socket);
                return;
            }

            NodeDef.Sockets.Add(((GraphSocket)socket).SocketHandle);
            base.AddOutputSocket(socket);
        }

        public override void RemoveOutputSocket(Editor.IGraphSocket socket)
        {
            var graph = (Graph)Editor.GraphContext.CurrentContext?.Graph;
            if (graph?.CR2WFile == null)
            {
                base.RemoveOutputSocket(socket);
                return;
            }

            NodeDef.Sockets.Remove(((GraphSocket)socket).SocketHandle);
            base.RemoveOutputSocket(socket);
        }

        public override void UpdateNode(bool manual = false)
        {
            // if not dynamic sockets
            if (NodeDef is not CP77Types.questSceneNodeDefinition
                && NodeDef is not CP77Types.questPhaseNodeDefinition
                && NodeDef is not CP77Types.questCombatNodeDefinition
                && NodeDef is not CP77Types.questMiscAICommandNode
                && NodeDef is not CP77Types.questMovePuppetNodeDefinition
                && NodeDef is not CP77Types.questLogicalBaseNodeDefinition
                && NodeDef is not CP77Types.questJournalNodeDefinition
                && NodeDef is not CP77Types.questPhoneManagerNodeDefinition
                && NodeDef is not CP77Types.questUseWorkspotNodeDefinition
                && NodeDef is not CP77Types.questSwitchNodeDefinition
                && NodeDef is not CP77Types.questPlaceholderNodeDefinition
                && NodeDef is not CP77Types.questRandomizerNodeDefinition)
            {
                return;
            }

            // Update dynamic sockets
            var inputSocketsToBeDeleted = _inSockets.Keys.ToList();
            var outputSocketsToBeDeleted = _outSockets.Keys.ToList();
            NodeFactory.AddNodeSockets(NodeDef,
                // Input
                (socketName, socketType) =>
                {
                    foreach (var (socketID, socket) in InSockets)
                    {
                        if (socket.Name == socketName)
                        {
                            inputSocketsToBeDeleted.Remove(socketID);
                            return;
                        }
                    }

                    AddInputSocket(NodeFactory.CreateGraphSocket(GetNextInputSocketID(), socketName, socketType, NodeDef));
                },
                // Output
                (socketName, socketType) =>
                {
                    foreach (var (socketID, socket) in OutSockets)
                    {
                        if (socket.Name == socketName)
                        {
                            outputSocketsToBeDeleted.Remove(socketID);
                            return;
                        }
                    }

                    AddOutputSocket(NodeFactory.CreateGraphSocket(GetNextOutputSocketID(), socketName, socketType, NodeDef));
                });

            inputSocketsToBeDeleted.ForEach(x => RemoveInputSocket(x));
            outputSocketsToBeDeleted.ForEach(x => RemoveOutputSocket(x));
        }

        public override void Remove_DONOTUSE_ONLYGRAPH()
        {
            base.Remove_DONOTUSE_ONLYGRAPH();
            NodeHandle.ClearHandle();
            NodeHandle = null;
        }

        public long GetNextInputSocketID()
        {
            if (_inSockets.Count == 0)
            {
                return (Id << 32) | 1;
            }
            else
            {
                return _inSockets.Keys.Last() + 1;
            }
        }

        public long GetNextOutputSocketID()
        {
            if (_outSockets.Count == 0)
                return (Id << 32) | 1;
            else
                return _outSockets.Keys.Last() + 1;
        }
    }
}
