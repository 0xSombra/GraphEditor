using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.Editor
{
    public static class PropertyHelper
    {
        const uint DEFAULT_MAX_STRING_LENGTH = 256;
        const string DEFAULT_FLOAT_FORMAT = "%.10f";
        const string DEFAULT_DOUBLE_FORMAT = "%.10f";

        public static string GetTypeName(Type propType)
        {
            if (propType.IsGenericType)
            {
                var sb = new StringBuilder();
                sb.Append(propType.Name);
                // removes `1 (no way a type has more than 9 args.. right?)
                sb.Remove(sb.Length - 2, 2);
                sb.Append('<');
                sb.Append(string.Join(',', propType.GenericTypeArguments.Select(x => GetTypeName(x))));
                sb.Append('>');
                return sb.ToString();
            }
            else
            {
                return propType.Name;
            }
        }

        public static bool DrawPropName(string propName, Type propType, bool isTreeNode = false)
        {
            bool ret = true;

            if (isTreeNode)
            {
                ImGui.AlignTextToFramePadding();
                ret = ImGui.TreeNodeEx(propName, ImGuiTreeNodeFlags.SpanAvailWidth);
            }
            else
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Bullet();
                ImGui.SameLine();
                ImGuiExtensions.AlignedText(propName);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(GetTypeName(propType));
            }

            return ret;
        }

        public static bool Draw<TEnum>(string propName, Type propType, ref TEnum value, bool isFlag = false) where TEnum : Enum
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                var currentEnum = value.ToString();
                var enumNames = Enum.GetNames(propType);

                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = false;
                if (ImGui.BeginCombo("##ValueInput", currentEnum))
                {
                    foreach (string enumName in enumNames)
                    {
                        var enumValue = (TEnum)Enum.Parse(propType, enumName);
                        if (isFlag)
                        {
                            dynamic curFlags = value;
                            dynamic newFlag = enumValue;
                            if (curFlags != 0 && newFlag == 0)
                            {
                                // [Flags] enum ABC { None = 0, A = 1, B = 2, C = 4 }
                                // var myEnum = ABC.A;
                                // ------
                                // This code will highlight both 'None' and 'A'
                                // This check fixes that.
                                continue;
                            }

                            var isSelected = value.HasFlag(enumValue);
                            if (ImGui.Selectable(enumName, isSelected, ImGuiSelectableFlags.DontClosePopups))
                            {
                                modified = true;
                                if (isSelected)
                                    value = curFlags & ~newFlag;
                                else
                                    value = curFlags | newFlag;
                            }
                        }
                        else
                        {
                            var isSelected = Equals(value, enumValue);
                            if (ImGui.Selectable(enumName, isSelected))
                            {
                                modified = true;
                                value = enumValue;
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                    }

                    ImGui.EndCombo();
                }
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref Color value)
        {
            Color Vec4ToColor(Vector4 vec4)
            {
                byte r = (byte)(Math.Clamp(vec4.X, 0.0f, 1.0f) * 255.0f);
                byte g = (byte)(Math.Clamp(vec4.Y, 0.0f, 1.0f) * 255.0f);
                byte b = (byte)(Math.Clamp(vec4.Z, 0.0f, 1.0f) * 255.0f);
                byte a = (byte)(Math.Clamp(vec4.W, 0.0f, 1.0f) * 255.0f);

                return Color.FromArgb(a, r, g, b);
            }

            Vector4 ColorToVec4(Color value)
            {
                return new Vector4(value.R / 255.0f, value.G / 255.0f, value.B / 255.0f, value.A / 255.0f);
            }

            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                var vec4Color = ColorToVec4(value);
                bool modified = ImGui.ColorEdit4("##ValueInput", ref vec4Color, ImGuiColorEditFlags.InputRGB);
                if (modified)
                    value = Vec4ToColor(vec4Color);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref bool value)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                bool modified = ImGui.Checkbox("##ValueInput", ref value);
                ImGui.SameLine();
                ImGuiExtensions.AlignedText(value ? "true" : "false");
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref byte[] value)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                // TODO: [CP77] draw byte array
                ImGuiExtensions.AlignedText($"!! UNHANDLED !! byte[{value.Length}]");
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref DateTime value)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                // TODO: draw DateTime
                ImGuiExtensions.AlignedText($"!! UNHANDLED !! {value}");
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref Guid value)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                // TODO: draw GUID
                ImGuiExtensions.AlignedText($"!! UNHANDLED !! {value}");
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref string value, uint maxLength = DEFAULT_MAX_STRING_LENGTH)
        {
            value ??= string.Empty;
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputText("##ValueInput", ref value, maxLength);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref sbyte value, string format = null)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref byte value, string format = null)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref int value, string format = null)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref uint value, string format = null)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref short value, string format = null)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref ushort value, string format = null)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref long value, string format = null)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref ulong value, string format = null)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref float value, string format = DEFAULT_FLOAT_FORMAT)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }

        public static bool Draw(string propName, Type propType, ref double value, string format = DEFAULT_DOUBLE_FORMAT)
        {
            DrawPropName(propName, propType);

            if (ImGui.TableNextColumn())
            {
                ImGui.PushID(propName);
                ImGui.SetNextItemWidth(-1);
                bool modified = ImGui.InputScalar("##ValueInput", ref value, null, null, format);
                ImGui.PopID();

                return modified;
            }

            return false;
        }
    }
}
