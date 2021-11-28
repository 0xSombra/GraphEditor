using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using WolvenKit.RED4.CR2W;
using WolvenKit.Common.Model.Cr2w;
using WolvenKit.RED4.CR2W.Reflection;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77
{
    internal static class PropertyReflectionHelper
    {
        static string HandleSearchInput = string.Empty;
        static string TweakDBSearchInput = string.Empty;
        static float TweakDBFilterTimer = 0.0f;
        static IReadOnlyList<Tuple<string, ulong>> FilteredTweakDBRecords;
        static uint PopupHandleID;
        static object _popupObject;
        static Type _popupObjectType;
        internal static ImGuiTableFlags TableFlags = ImGuiTableFlags.BordersInner | ImGuiTableFlags.NoSavedSettings
                                                   | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg;

        static void DrawPropMembers(IEditableVariable obj)
        {
            var members = obj.accessor.GetMembers();
            foreach (var member in members)
            {
                if (member.Ordinal == -1)
                    continue;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawProp((IEditableVariable)obj.accessor[obj, member.Name], member.Name, member.Type);
            }
        }

        static bool TryDrawPropPrimitive(IEditableVariable obj, string propName, Type propType)
        {
            switch (obj)
            {
                case CP77Types.CRUID cVar:
                {
                    var value = cVar.Id.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Id.Value = value;
                        cVar.IsSerialized = true;
                        cVar.Id.IsSerialized = true;
                    }
                }
                return true;
                case CP77Types.CColor cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CName cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.TweakDBID cVar:
                {
                    var value = cVar.Id.Value;
                    bool nodeOpen = Editor.PropertyHelper.DrawPropName(propName, propType, isTreeNode: true);

                    if (ImGui.TableNextColumn())
                    {
                        if (FilteredTweakDBRecords == null || TweakDBFilterTimer != 0.0f)
                        {
                            TweakDBFilterTimer -= ImGui.GetIO().DeltaTime;
                            if (TweakDBFilterTimer <= 0.0f)
                            {
                                if (!string.IsNullOrEmpty(TweakDBSearchInput))
                                {
                                    FilteredTweakDBRecords = TweakDB.RecordsSorted.Where(
                                        x => x.Item1.Contains(TweakDBSearchInput, StringComparison.OrdinalIgnoreCase)).ToList();
                                }
                                else
                                {
                                    FilteredTweakDBRecords = TweakDB.RecordsSorted;
                                }
                                TweakDBFilterTimer = 0.0f;
                            }
                        }

                        ImGui.PushID(propName);
                        unsafe
                        {
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.BeginCombo("##Dropdown", TweakDB.RecordToString(value), ImGuiComboFlags.HeightLargest))
                            {
                                ImGui.SetNextItemWidth(-1);
                                if (ImGui.InputTextWithHint("##Search", "Search", ref TweakDBSearchInput, 256))
                                    TweakDBFilterTimer = 0.25f;

                                if (ImGui.BeginChild("Scrollable", new Vector2(0.0f, 300.0f)))
                                {
                                    // Can't use clipper because we need SetItemDefaultFocus
                                    for (var i = 0; i != FilteredTweakDBRecords.Count; ++i)
                                    {
                                        var record = FilteredTweakDBRecords[i];
                                        var isSelected = record.Item2 == value;

                                        if (ImGuiExtensions.NextItemVisible(new Vector2(1.0f, ImGui.GetTextLineHeight())) &&
                                            ImGui.Selectable(record.Item1, isSelected))
                                        {
                                            cVar.Id.Value = record.Item2;
                                            cVar.IsSerialized = true;
                                            cVar.Id.IsSerialized = true;
                                            ImGui.CloseCurrentPopup();
                                            break;
                                        }

                                        if (isSelected)
                                        {
                                            // not working for some reasons
                                            //ImGui.SetItemDefaultFocus();
                                            if (ImGui.IsWindowAppearing())
                                                ImGui.SetScrollHereY();
                                        }
                                    }
                                }
                                ImGui.EndChild();
                                ImGui.EndCombo();
                            }
                        }
                        ImGui.PopID();
                    }

                    if (nodeOpen)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if (Editor.PropertyHelper.Draw("Hex", typeof(ulong), ref value, "%016llX"))
                        {
                            cVar.Id.Value = value;
                            cVar.IsSerialized = true;
                            cVar.Id.IsSerialized = true;
                        }

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if (Editor.PropertyHelper.Draw("Decimal", typeof(ulong), ref value))
                        {
                            cVar.Id.Value = value;
                            cVar.IsSerialized = true;
                            cVar.Id.IsSerialized = true;
                        }

                        ImGui.TreePop();
                    }
                }
                return true;

                case CP77Types.NodeRef cVar:
                {
                    var value = cVar.Value.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value.Value = value;
                        cVar.IsSerialized = true;
                        cVar.Value.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.LocalizationString cVar:
                {
                    bool nodeOpen = Editor.PropertyHelper.DrawPropName(propName, propType, isTreeNode: true);

                    if (ImGui.TableNextColumn())
                    {
                        ImGuiExtensions.AlignedText(cVar.ToString());
                    }

                    if (nodeOpen)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var unk1 = cVar.Unk1.Value;
                        if (Editor.PropertyHelper.Draw("Unk1", typeof(CP77Types.CUInt64), ref unk1))
                        {
                            cVar.Unk1.Value = unk1;
                            cVar.IsSerialized = true;
                            cVar.Unk1.IsSerialized = true;
                        }

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var value = cVar.Value.Value;
                        if (Editor.PropertyHelper.Draw("Value", typeof(CP77Types.CString), ref value))
                        {
                            cVar.Value.Value = value;
                            cVar.IsSerialized = true;
                            cVar.Value.IsSerialized = true;
                        }

                        ImGui.TreePop();
                    }
                }
                return true;

                case CP77Types.CBool cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CByteArray cVar:
                {
                    var value = cVar.Bytes;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Bytes = value;
                        cVar.IsSerialized = true;

                    }
                }
                return true;

                case CP77Types.CByteArray2 cVar:
                {
                    var value = cVar.GetBytes();
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.SetValue(value);
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CBytes cVar:
                {
                    var value = cVar.Bytes;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Bytes = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CDateTime cVar:
                {
                    var value = cVar.DValue;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        // TODO: [CP77-PropertyWindow] wait for wkit to implement a setter
                        // obj.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CGUID cVar:
                {
                    // string for now. should be fine
                    var value = cVar.GuidString;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.GuidString = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CString cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                #region GeneratedIntTypes_InputScalar

                case CP77Types.CInt8 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CUInt8 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CInt16 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CUInt16 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CInt32 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CUInt32 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CInt64 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CUInt64 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CDynamicInt cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CVLQInt32 cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CFloat cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                case CP77Types.CDouble cVar:
                {
                    var value = cVar.Value;
                    if (Editor.PropertyHelper.Draw(propName, propType, ref value))
                    {
                        cVar.Value = value;
                        cVar.IsSerialized = true;
                    }
                }
                return true;

                #endregion

                default:
                    return false;
            }
        }

        static bool TryDrawPropGeneric(IEditableVariable obj, string propName, Type propType)
        {
            switch (obj)
            {
                case IEnumAccessor enumAccessor:
                {
                    var enumValue = (Enum)obj.accessor[obj, "Value"];
                    if (Editor.PropertyHelper.Draw(propName, enumValue.GetType(), ref enumValue, enumAccessor.IsFlag))
                    {
                        obj.accessor[obj, "Value"] = enumValue;
                        obj.IsSerialized = true;
                    }
                }
                return true;

                case IHandleAccessor handleAccessor:
                {
                    bool nodeOpen = Editor.PropertyHelper.DrawPropName(propName, propType, isTreeNode: true);

                    if (ImGui.TableNextColumn())
                    {
                        if (handleAccessor.ChunkHandle && handleAccessor.Reference != null)
                        {
                            // Editor.PropertyHelper.GetTypeName(handleAccessor.Reference.data.GetType())
                            ImGuiExtensions.AlignedText(handleAccessor.Reference.REDName);
                        }
                        else
                        {
                            ImGuiExtensions.AlignedText($"<Empty> {Editor.PropertyHelper.GetTypeName(propType.GenericTypeArguments[0])}");
                        }

                        ImGui.SameLine();
                        if (ImGuiExtensions.IconButton_Add(propName, true, 0))
                        {
                            _popupObject = obj;
                            _popupObjectType = propType.GenericTypeArguments[0];
                            ImGui.OpenPopup(PopupHandleID);
                        }

                        ImGui.SameLine();
                        if (ImGuiExtensions.IconButton_Remove(propName, true, 1))
                        {
                            handleAccessor.ClearHandle();
                        }
                    }

                    if (nodeOpen)
                    {
                        if (handleAccessor.Reference != null)
                        {
                            DrawPropMembers(handleAccessor.Reference.data);
                        }
                        ImGui.TreePop();
                    }
                }
                return true;

                case IArrayAccessor arrayAccessor:
                {
                    var isStatic = arrayAccessor.IsStatic();
                    var scrollToIndex = -1;

                    bool nodeOpen = Editor.PropertyHelper.DrawPropName(propName, propType, isTreeNode: true);
                    if (ImGui.TableNextColumn())
                    {
                        ImGuiExtensions.AlignedText($"{Editor.PropertyHelper.GetTypeName(arrayAccessor.InnerType)}[{arrayAccessor.Count}]");

                        if (!isStatic)
                        {
                            ImGui.SameLine();
                            if (ImGuiExtensions.IconButton_Add(propName))
                            {
                                scrollToIndex = arrayAccessor.Count;
                                var element = CP77Types.CR2WTypeManager.Create(arrayAccessor.Elementtype, "", (CR2WFile)obj.Cr2wFile, (CP77Types.CVariable)arrayAccessor);
                                element.IsSerialized = true;
                                arrayAccessor.IsSerialized = true;
                                arrayAccessor.Add(element);
                            }
                        }
                        else
                        {
                            // make sure static arrays are serialized by default
                            // we have no way of knowing if one of their elements was modified
                            arrayAccessor.IsSerialized = true;
                        }
                    }

                    if (nodeOpen)
                    {
                        var indexToBeRemoved = -1;
                        for (var i = 0; i != arrayAccessor.Count; ++i)
                        {
                            var element = (IEditableVariable)arrayAccessor[i];

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            // if static and not primitive, show the remove button to clear the type
                            if ((!isStatic || element is not IREDPrimitive))
                            {
                                if (ImGuiExtensions.IconButton_Remove(i, false))
                                {
                                    indexToBeRemoved = i;
                                }

                                ImGui.SameLine();
                            }

                            if (scrollToIndex == i)
                            {
                                ImGui.SetScrollHereY();
                            }

                            DrawProp(element, $"[{i}]", element.GetType());
                        }

                        if (indexToBeRemoved != -1)
                        {
                            var element = (IEditableVariable)arrayAccessor[indexToBeRemoved];
                            element.ClearVariable();

                            if (!isStatic)
                            {
                                arrayAccessor.Remove(element);
                            }
                        }

                        ImGui.TreePop();
                    }
                }
                return true;

                case CP77Types.ISoftAccessor softAccessor:
                {
                    bool nodeOpen = Editor.PropertyHelper.DrawPropName(propName, propType, isTreeNode: true);
                    if (ImGui.TableNextColumn())
                    {
                        ImGuiExtensions.AlignedText(softAccessor.ToString());
                    }

                    if (nodeOpen)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var depotPath = softAccessor.DepotPath;
                        if (Editor.PropertyHelper.Draw("DepotPath", typeof(string), ref depotPath))
                        {
                            softAccessor.DepotPath = depotPath;
                            obj.IsSerialized = true;
                        }

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var importFlags = softAccessor.Flags;
                        if (Editor.PropertyHelper.Draw("Flags", typeof(WolvenKit.Common.EImportFlags), ref importFlags, isFlag: true))
                        {
                            softAccessor.Flags = importFlags;
                            obj.IsSerialized = true;
                        }

                        ImGui.TreePop();
                    }
                }
                return true;
            }

            return false;
        }

        static void DrawProp(IEditableVariable obj, string propName, Type propType)
        {
            if (TryDrawPropPrimitive(obj, propName, propType))
                return;

            if (TryDrawPropGeneric(obj, propName, propType))
                return;


            var isInlineType = obj is not IREDPrimitive;
            isInlineType &= obj is CP77Types.CVariable cVar && (cVar.ChildrEditableVariables?.Count ?? 0) != 0;
            if (isInlineType)
            {
                bool nodeOpen = Editor.PropertyHelper.DrawPropName(propName, propType, isTreeNode: true);
                if (ImGui.TableNextColumn())
                {
                    ImGuiExtensions.AlignedText(propType.Name);
                }

                if (nodeOpen)
                {
                    DrawPropMembers(obj);
                    ImGui.TreePop();
                }
            }
            else
            {
                Editor.PropertyHelper.DrawPropName(propName, propType);
                if (ImGui.TableNextColumn())
                {
                    if (obj == null)
                    {
                        ImGuiExtensions.AlignedText($"!! NULL !! {Editor.PropertyHelper.GetTypeName(propType)}");
                    }
                    else
                    {
                        ImGuiExtensions.AlignedText("!! UNHANDLED !!");
                        ImGui.SameLine();

                        ImGui.PushID(propName);
                        var str = obj.ToString();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputText("", ref str, (uint)str.Length, ImGuiInputTextFlags.ReadOnly);
                        ImGui.PopID();
                    }
                }
            }
        }

        static void DrawPopups()
        {
            if (PopupHandleID == 0)
                PopupHandleID = ImGui.GetID("Create/Select a chunk");

            var opened = true;
            ImGui.SetNextWindowSize(new Vector2(400.0f), ImGuiCond.Once);
            if (ImGui.BeginPopupModal("Create/Select a chunk", ref opened, ImGuiWindowFlags.NoSavedSettings))
            {
                var handleAccessor = (IHandleAccessor)_popupObject;
                if (ImGui.BeginTabBar("handle_choices"))
                {
                    if (ImGui.BeginTabItem("New"))
                    {
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputTextWithHint("##search-box", "Search", ref HandleSearchInput, 256);
                        if (ImGui.BeginChild("Scrollable"))
                        {
                            ImGui.BeginGroup();
                            var subclasses = AssemblyDictionary.GetSubClassesOf(_popupObjectType);
                            IEnumerable<Type> GetSubclassesOrCurrent()
                            {
                                if (!subclasses.Any())
                                {
                                    yield return _popupObjectType;
                                }
                                else
                                {
                                    foreach (var subclass in subclasses)
                                        yield return subclass;
                                }
                            }

                            // how fast is the string search?
                            // does it need a filtered list?
                            // need ImGuiClipper?
                            foreach (var subclass in GetSubclassesOrCurrent())
                            {
                                if (!string.IsNullOrEmpty(HandleSearchInput)
                                    && !subclass.Name.Contains(HandleSearchInput, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                if (ImGui.Selectable(subclass.Name))
                                {
                                    var cr2w = (CR2WFile)handleAccessor.Cr2wFile;
                                    var chunk = cr2w.CreateChunkEx(subclass.Name); // always null parent?
                                    handleAccessor.SetReference(chunk);
                                    ImGui.CloseCurrentPopup();
                                }
                            }
                            ImGui.EndGroup();
                        }
                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Existing"))
                    {
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputTextWithHint("##search-box", "Search", ref HandleSearchInput, 256);
                        if (ImGui.BeginChild("Scrollable"))
                        {
                            // how fast is the string search?
                            // does it need a filtered list?
                            // need ImGuiClipper?
                            var cr2w = handleAccessor.Cr2wFile;
                            var currentChunkIndex = handleAccessor.Reference?.ChunkIndex ?? -1;
                            foreach (var chunk in cr2w.Chunks)
                            {
                                var isCurrentChunk = chunk.ChunkIndex == currentChunkIndex;
                                if (!isCurrentChunk && !chunk.data.GetType().IsSubclassOf(_popupObjectType))
                                    continue;

                                if (!string.IsNullOrEmpty(HandleSearchInput)
                                    && !chunk.REDName.Contains(HandleSearchInput, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                if (ImGui.Selectable(chunk.REDName, isCurrentChunk))
                                {
                                    handleAccessor.SetReference(chunk);
                                    ImGui.CloseCurrentPopup();
                                    break;
                                }
                            }
                        }
                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndPopup();
            }
        }

        internal static void DrawTypeProps(IEditableVariable obj, Type objType)
        {
            var props = objType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (props.Length == 0)
                return;

            if (ImGui.CollapsingHeader(objType.Name))
            {
                // using the same name somehow breaks tree nodes. previously: "property_table"
                if (ImGui.BeginTable(objType.Name, 2, TableFlags))
                {
                    foreach (var prop in props)
                    {
                        var propObj = (IEditableVariable)obj.accessor[obj, prop.Name];

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        DrawProp(propObj, prop.Name, propObj?.GetType() ?? prop.PropertyType);
                    }

                    ImGui.EndTable();
                }
            }
        }

        internal static void Draw(IEditableVariable nodeDef, Type stopAtType = null)
        {
            if (stopAtType == null)
                stopAtType ??= typeof(CP77Types.ISerializable);

            var nodeDefTypes = new Stack<Type>();
            for (var nodeDefType = nodeDef.GetType(); nodeDefType != null && nodeDefType != stopAtType; nodeDefType = nodeDefType.BaseType)
            {
                nodeDefTypes.Push(nodeDefType);
            }

            while (nodeDefTypes.Count != 0)
            {
                var nodeDefType = nodeDefTypes.Pop();
                DrawTypeProps(nodeDef, nodeDefType);
            }

            DrawPopups();
        }
    }
}
