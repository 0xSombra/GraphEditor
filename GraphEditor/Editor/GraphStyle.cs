using System;
using System.Collections.Generic;
using System.Numerics;

namespace GraphEditor.Editor
{
    public sealed class GraphStyle
    {
        public enum Color
        {
            GridBg,
            GridLine,
            SelectionBg,
            SelectionBorder,
            Socket,
            SocketName,
            UnusedSocket,
            HoveredSocket,
            Link,
            ActiveLink,
            NodeBg,
            NodeBorder,
            NodeName,
            NodeComment,
            NodeDescription,
            NodeNameplate,
            ActiveNodeBorder,
            COUNT
        }

        public enum Var
        {
            GridSize,
            UIScaleMin,
            UIScaleMax,
            BorderThickness,
            BorderRoundness,
            NodeTextPadding,
            NodeSocketStartX,
            NodeSocketRadius,
            NodeSocketLabelSpacing,
            NodeSocketSpacing,
            NodeBoxPadding,
            COUNT
        }

        readonly uint[] _colors;
        readonly object[] _vars;
        Stack<Tuple<Color, uint>> _backupColors = new Stack<Tuple<Color, uint>>();
        Stack<Tuple<Var, object>> _backupVars = new Stack<Tuple<Var, object>>();
        public IReadOnlyList<uint> Colors => _colors;
        public float GridSize => (float)_vars[(int)Var.GridSize];
        public float UIScaleMin => (float)_vars[(int)Var.UIScaleMin];
        public float UIScaleMax => (float)_vars[(int)Var.UIScaleMax];
        public float BorderThickness => (float)_vars[(int)Var.BorderThickness];
        public float BorderRoundness => (float)_vars[(int)Var.BorderRoundness];
        public Vector2 NodeTextPadding => (Vector2)_vars[(int)Var.NodeTextPadding];
        public float NodeSocketStartX => (float)_vars[(int)Var.NodeSocketStartX]; // ??
        public float NodeSocketRadius => (float)_vars[(int)Var.NodeSocketRadius];
        public float NodeSocketLabelSpacing => (float)_vars[(int)Var.NodeSocketLabelSpacing];
        public Vector2 NodeSocketSpacing => (Vector2)_vars[(int)Var.NodeSocketSpacing];
        public float NodeBoxPadding => (float)_vars[(int)Var.NodeBoxPadding]; // extra space at the bottom / min box size

        static uint ToRGBA(byte r, byte g, byte b, byte a)
        {
            return (uint)((a << 24) | (b << 16) | (g << 8) | r);
        }

        public GraphStyle()
        {
            _colors = new uint[(int)Color.COUNT];
            _colors[(int)Color.GridBg] = ToRGBA(50, 50, 50, 255);
            _colors[(int)Color.GridLine] = ToRGBA(202, 204, 206, 40);

            _colors[(int)Color.SelectionBg] = ToRGBA(0, 120, 215, 80);
            _colors[(int)Color.SelectionBorder] = ToRGBA(0, 120, 215, 255);

            _colors[(int)Color.Socket] = ToRGBA(255, 255, 255, 255);
            _colors[(int)Color.SocketName] = ToRGBA(255, 255, 255, 255);
            _colors[(int)Color.UnusedSocket] = ToRGBA(0, 0, 0, 255);
            _colors[(int)Color.HoveredSocket] = ToRGBA(255, 0, 0, 255);

            _colors[(int)Color.Link] = ToRGBA(0, 0, 0, 255);
            _colors[(int)Color.ActiveLink] = ToRGBA(0, 0, 0, 255);

            _colors[(int)Color.NodeBg] = ToRGBA(100, 100, 100, 255);
            _colors[(int)Color.NodeBorder] = ToRGBA(255, 255, 255, 255);
            _colors[(int)Color.NodeName] = ToRGBA(255, 255, 255, 255);
            _colors[(int)Color.NodeComment] = ToRGBA(255, 255, 255, 255);
            _colors[(int)Color.NodeDescription] = ToRGBA(255, 255, 255, 255);
            _colors[(int)Color.NodeNameplate] = ToRGBA(255, 0, 0, 255);
            _colors[(int)Color.ActiveNodeBorder] = ToRGBA(255, 255, 0, 255);

            _vars = new object[(int)Var.COUNT];
            _vars[(int)Var.GridSize] = 32.0f;
            _vars[(int)Var.UIScaleMin] = 0.1f;
            _vars[(int)Var.UIScaleMax] = 2.5f;
            _vars[(int)Var.BorderThickness] = 1.5f;
            _vars[(int)Var.BorderRoundness] = 4.0f;
            _vars[(int)Var.NodeTextPadding] = new Vector2(8.0f, 4.0f);
            _vars[(int)Var.NodeSocketStartX] = 12.0f;
            _vars[(int)Var.NodeSocketRadius] = 4.5f;
            _vars[(int)Var.NodeSocketLabelSpacing] = 4.0f;
            _vars[(int)Var.NodeSocketSpacing] = new Vector2(20.0f, 16.0f);
            _vars[(int)Var.NodeBoxPadding] = 26.0f;

        }

        public uint GetColor(Color colorIdx)
        {
            return Colors[(int)colorIdx];
        }

        public void PushColor(Color colorIdx, Vector4 color)
        {
            byte r = (byte)(Math.Clamp(color.X, 0.0f, 1.0f) * 255.0f);
            byte g = (byte)(Math.Clamp(color.Y, 0.0f, 1.0f) * 255.0f);
            byte b = (byte)(Math.Clamp(color.Z, 0.0f, 1.0f) * 255.0f);
            byte a = (byte)(Math.Clamp(color.W, 0.0f, 1.0f) * 255.0f);
            PushColor(colorIdx, ToRGBA(r, g, b, a));
        }

        public void PushColor(Color colorIdx, uint color)
        {
            var backup = _colors[(int)colorIdx];
            _backupColors.Push(Tuple.Create(colorIdx, backup));
            _colors[(int)colorIdx] = color;
        }

        void PushVar(Var varIdx, object objVar)
        {
            var backup = _vars[(int)varIdx];
            _backupVars.Push(Tuple.Create(varIdx, backup));
            _vars[(int)varIdx] = objVar;
        }

        public void PushVar(Var varIdx, Vector2 vec2Var)
            => PushVar(varIdx, (object)vec2Var);

        public void PushVar(Var varIdx, float floatVar)
            => PushVar(varIdx, (object)floatVar);

        public void PopColor(int count = 1)
        {
            if (count == 0)
                return;

            for (var i = 0; i != count; ++i)
            {
                var backupColor = _backupColors.Pop();
                _colors[(int)backupColor.Item1] = backupColor.Item2;
            }
        }

        public void PopVar(int count = 1)
        {
            if (count == 0)
                return;

            for (var i = 0; i != count; ++i)
            {
                var backupVar = _backupVars.Pop();
                _vars[(int)backupVar.Item1] = backupVar.Item2;
            }
        }
    }
}
