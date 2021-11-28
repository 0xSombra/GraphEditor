using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77.Scene
{
    internal class GraphNode : Editor.GraphNode
    {
        internal CP77Types.CHandle<CP77Types.scnSceneGraphNode> NodeHandle { get; private set; }

        internal CP77Types.scnSceneGraphNode NodeDef => NodeHandle.GetInstance();

        public GraphNode(CP77Types.CHandle<CP77Types.scnSceneGraphNode> nodeHandle, long id, string name)
            : base(id, name)
        {
            NodeHandle = nodeHandle;
        }

        public static bool GetFriendlyName(string fullTypeName, out string friendlyName)
        {
            var match = Regex.Match(fullTypeName, "^[a-z]+(.+)");//Node$");
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

        public static long GenerateSocketID(long nodeID, ushort name, ushort ordinal)
        {
            long socketID = (int)nodeID;

            socketID <<= 16;
            socketID |= name;

            socketID <<= 16;
            socketID |= ordinal;
            return socketID;
        }

        public static void ParseSocketID(long socketID, out long nodeID, out ushort name, out ushort ordinal)
        {
            ordinal = (ushort)socketID;
            socketID >>= 16;

            name = (ushort)socketID;
            socketID >>= 16;

            nodeID = (int)socketID;
        }

        // TODO: [CP77] figure out scene socket names
        /*
        * not for scnQuestNode
        * 666 reserved for Deletion Marker Node? both ordinal 0 and 1
        * IDA says there are Name: 1 to 6 output sockets (maybe wrong)
        * Section nodes can use Name_1 and above for scnEventSocket (or any socket)
        * Choice nodes have sockets Name_1 through Name_6
        [Input]
        Name: 0, Ordinal: N = Reserved for custom sockets per node?
        Name: 1, Ordinal: 0 = CutDestination
        * Name_1026 = disable
        * if CutDestination then cancel else start

        [Output]
        Name: 0, Ordinal: N = Reserved for custom sockets per node?
        Name: 1 till 6, Ordinal: ? = ???

        Choice node always has output 2 till 6
        Section node can have Name: 2, Ordinal: N (mq000_01_apartment.scene #498)
        */
        public override void AddInputSocket(Editor.IGraphSocket socket)
        {
            if (this is not CustomNodes.scnQuestNode)
            {
                ParseSocketID(socket.Id, out long _, out ushort name, out ushort ordinal);
                if (name == 0)
                {
                    if (ordinal == 0)
                    {
                        // will be replaced later if needed
                        socket.Name = "In"; // aka start
                    }
                }
                else if (name == 1)
                {
                    Debug.Assert(ordinal == 0, "Unexpected input socket ordinal. assumed Name_1 is reserved for CutDestination");
                    if (ordinal == 0)
                    {
                        socket.Name = "Cancel";
                    }
                }
                else if (name == 666)
                {
                    //Debug.Assert(ordinal == 0, "Unexpected input socket ordinal. assumed Name_666 is reserved for DeletionMarker");
                    //if (ordinal == 0)
                    {
                        socket.Name = $"In_Deleted_{ordinal}";
                    }
                }
                else if (name == 1026)
                {
                    Debug.Assert(ordinal == 0, "Unexpected input socket ordinal. assumed Name_1026 is reserved for Disable");
                    if (ordinal == 0)
                    {
                        socket.Name = "Disable";
                    }
                }
            }
            base.AddInputSocket(socket);
        }

        public override void AddOutputSocket(Editor.IGraphSocket socket)
        {
            if (this is not CustomNodes.scnQuestNode)
            {
                ParseSocketID(socket.Id, out long _, out ushort name, out ushort ordinal);
                if (name == 0)
                {
                    if (ordinal == 0)
                    {
                        // will be replaced later if needed
                        socket.Name = "Out"; // aka End
                    }
                }
                else if (name == 1)
                {
                    Debug.Assert(ordinal == 0, "Unexpected output socket ordinal. assumed Name_1 is reserved for CutSource");
                    if (ordinal == 0)
                    {
                        socket.Name = "CutSource";
                    }
                }
                else if (name == 666)
                {
                    //Debug.Assert(ordinal == 1, "Unexpected output socket ordinal. assumed Name_666 is reserved for DeletionMarker");
                    //if (ordinal == 1)
                    {
                        socket.Name = $"Out_Deleted_{ordinal}";
                    }
                }
            }
            base.AddOutputSocket(socket);
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
            // display scnSceneResource because why not
            PropertyReflectionHelper.DrawTypeProps(NodeDef.Cr2wFile.Chunks[0].data, typeof(CP77Types.scnSceneResource));

            if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
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
                    if (ImGui.InputTextMultiline("##Comment", ref comment, 256, new Vector2(-1.0f, 35.0f)))
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

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var ffStrategy = NodeDef.FfStrategy.Value;
                    if (Editor.PropertyHelper.Draw("ffStrategy", NodeDef.FfStrategy.GetEnumType(), ref ffStrategy))
                    {
                        NodeDef.FfStrategy.Value = ffStrategy;
                        NodeDef.FfStrategy.IsSerialized = true;
                    }

                    ImGui.EndTable();
                }
            }

            PropertyReflectionHelper.Draw(NodeDef, typeof(CP77Types.scnSceneGraphNode));

            if (NodeDef is CP77Types.scnQuestNode scnQuestNode)
            {
                var questNode = scnQuestNode.QuestNode.GetInstance();
                if (questNode != null)
                {
                    // Hack - [CP77] shortcut to the quest node
                    ImGui.PushID("QuestNode");
                    PropertyReflectionHelper.Draw(questNode, typeof(CP77Types.questNodeDefinition));
                    ImGui.PopID();
                }
            }
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

        public override void Remove_DONOTUSE_ONLYGRAPH()
        {
            base.Remove_DONOTUSE_ONLYGRAPH();
            NodeHandle.ClearHandle();
            NodeHandle = null;
        }

        public long GenerateSocketID(ushort name, ushort ordinal)
            => GenerateSocketID(Id, name, ordinal);
    }
}
