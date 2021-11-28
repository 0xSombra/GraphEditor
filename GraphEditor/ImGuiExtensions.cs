using System;
using System.Numerics;

namespace ImGuiNET
{
    public struct ImRect
    {
        public Vector2 Min;
        public Vector2 Max;

        public ImRect(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }
    }

    public static class ImGuiExtensions
    {
        public static bool NextItemVisible(Vector2 size, bool claimSpaceIfInvisible = true)
        {
            Vector2 rectMin = ImGui.GetCursorScreenPos();
            Vector2 rectMax = new Vector2(rectMin.X + size.X, rectMin.Y + size.Y);
            bool visible = ImGui.IsRectVisible(rectMin, rectMax);
            if (!visible && claimSpaceIfInvisible)
            {
                ImGui.Dummy(size);
            }
            return visible;
        }

        public static ImGuiListClipper NewImGuiListClipper()
        {
            return new ImGuiListClipper()
            {
                DisplayStart = 0,
                DisplayEnd = 0,
                ItemsCount = 0,
                StepNo = 0,
                ItemsFrozen = 0,
                ItemsHeight = 0,
                StartPosY = 0
            };
        }

        public static bool RightAlignedButton(string label)
            => RightAlignedButton(label, Vector2.Zero);

        public static bool RightAlignedButton(string label, Vector2 minSize, float offset = 0.0f)
        {
            var buttonSize = ImGui.CalcTextSize(label) + ImGui.GetStyle().FramePadding * 2;
            if (minSize.X > buttonSize.X)
                buttonSize.X = minSize.X;
            if (minSize.Y > buttonSize.Y)
                buttonSize.Y = minSize.Y;

            // Expand area if needed to fit
            ImGui.Dummy(buttonSize);
            ImGui.SameLine();
            ImGui.SetCursorPosX((ImGui.GetContentRegionMax().X - buttonSize.X) - offset);
            return ImGui.Button(label, buttonSize);
        }

        public static bool IconButton_Add(int id, bool isRightAligned = true, int rightAlignIndex = 0)
        {
            ImGui.PushID(id);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));
            bool pressed;
            if (isRightAligned)
                pressed = RightAlignedButton("+", new Vector2(20), 24.0f * rightAlignIndex);
            else
                pressed = ImGui.Button("+", new Vector2(20));
            ImGui.PopStyleColor();
            ImGui.PopID();

            return pressed;
        }

        public static bool IconButton_Add(string id, bool isRightAligned = true, int rightAlignIndex = 0)
            => IconButton_Add((int)ImGui.GetID(id), isRightAligned, rightAlignIndex);

        public static bool IconButton_Remove(int id = 0, bool isRightAligned = true, int rightAlignIndex = 0)
        {
            ImGui.PushID(id);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255, 0, 0, 255));
            bool pressed;
            if (isRightAligned)
                pressed = RightAlignedButton("-", new Vector2(20), 24.0f * rightAlignIndex);
            else
                pressed = ImGui.Button("-", new Vector2(20));
            ImGui.PopStyleColor();
            ImGui.PopID();

            return pressed;
        }

        public static bool IconButton_Remove(string id, bool isRightAligned = true, int rightAlignIndex = 0)
            => IconButton_Remove((int)ImGui.GetID(id), isRightAligned, rightAlignIndex);

        public static void PushID(long id)
        {
            // alternatively, could call PushID twice with low/high
            ImGui.PushID(new IntPtr(id));
        }

        // Uses 'GetCursorScreenPos' as starting point
        public static bool IsMouseHoveringRect(Vector2 rectSize)
        {
            var rectMin = ImGui.GetCursorScreenPos();
            var rectMax = rectMin + rectSize;
            return ImGui.IsMouseHoveringRect(rectMin, rectMax);
        }

        public static bool AlignedSelectableText(string text)
        {
            var textSize = ImGui.CalcTextSize(text);
            var isHovered = IsMouseHoveringRect(textSize);

            if (isHovered)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFA9629);
            }

            AlignedText(text);

            if (isHovered)
            {
                ImGui.PopStyleColor();
            }

            return ImGui.IsItemClicked();
        }

        public static void AlignedDisabledText(string text)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled(text);
        }

        public static void AlignedText(string text)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(text);
        }

        public static bool IsItemHovered_Overlap()
        {
            // Hacky version because adding ImGuiContext to ImGui.NET is hell.
            // equivalent to: return ImGui::IsItemHovered() && ImGui::GetCurrentContext()->HoveredIdPreviousFrame == ImGui::GetItemID()
            // Make sure your item is "SetItemAllowOverlap"

            if (!ImGui.IsItemHovered())
                return false;

            var itemID = ImGui.GetItemID();
            // Set hovered ID to 0 so the next GetHoveredID returns the last frame's
            // and sets HoveredIdAllowOverlap to false. (enable later with SetItemAllowOverlap)
            ImGui.SetHoveredID(0);
            var hoveredIdPreviousFrame = ImGui.GetHoveredID();
            // We know our item was the one hovered, let's revert what we did
            ImGui.SetHoveredID(itemID);
            ImGui.SetItemAllowOverlap();

            return hoveredIdPreviousFrame == itemID;
        }

        public static bool RectOverlaps(ImRect a, ImRect b)
        {
            return b.Min.Y < a.Max.Y && b.Max.Y > a.Min.Y && b.Min.X < a.Max.X && b.Max.X > a.Min.X;
        }

        public static bool IsMouseClickedNoDrag(ImGuiMouseButton button, float dragThreshold = 5.0f)
        {
            if (ImGui.IsMouseReleased(button))
            {
                return ImGui.GetMouseDragDelta(button, dragThreshold) == Vector2.Zero;
            }
            return false;
        }

        public static bool ToggleButton(string label, ref bool toggleable)
            => ToggleButton(label, Vector2.Zero, ref toggleable);

        public static bool ToggleButton(string label, Vector2 size, ref bool toggleable)
            => ToggleButton(label, size, ref toggleable, true, false);

        public static bool ToggleButton<T>(string label, ref T toggleable, T toggledValue, T untoggledValue)
            => ToggleButton(label, Vector2.Zero, ref toggleable, toggledValue, untoggledValue);

        public static bool ToggleButton<T>(string label, Vector2 size, ref T toggleable, T enabledValue, T disabledValue)
        {
            var isEnabled = toggleable.Equals(enabledValue);

            if (isEnabled)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);

            var pressed = ImGui.Button(label, size);

            if (isEnabled)
                ImGui.PopStyleColor();

            if (pressed)
            {
                toggleable = isEnabled ? disabledValue : enabledValue;
                return true;
            }
            return false;
        }
    }
}
