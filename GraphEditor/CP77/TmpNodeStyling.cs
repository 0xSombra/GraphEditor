using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Newtonsoft.Json.Linq;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77
{
    // temporarily till we figure out what to color each node
    internal static class TmpNodeStyling
    {
        static Dictionary<Type, Dictionary<Editor.GraphStyle.Color, uint>> TypeColors = new Dictionary<Type, Dictionary<Editor.GraphStyle.Color, uint>>();

        static uint ToRGBA(byte r, byte g, byte b, byte a)
        {
            uint rgba = a;
            rgba <<= 8;
            rgba |= b;
            rgba <<= 8;
            rgba |= g;
            rgba <<= 8;
            rgba |= r;
            return rgba;
        }

        internal static bool TryGetStyling(Type type, out IReadOnlyDictionary<Editor.GraphStyle.Color, uint> styling)
        {
            if (TypeColors.TryGetValue(type, out var stylingColors))
            {
                styling = stylingColors;
                return true;
            }

            styling = null;
            return false;
        }

        internal static void Reload(string filename)
        {
            if (!File.Exists(filename))
                return;

            TypeColors.Clear();
            var jArray = JArray.Parse(File.ReadAllText(filename));
            /*
            [
                {
                    "Type": "scnCutControlNode",
                    "Colors": {
                        "NodeText": [ 255, 0, 0, 255 ],
                        "NodeNameplate": [ 255, 255, 0, 255 ]
                    }
                }
            ]
             */
            var wolvenkitAssembly = typeof(CP77Types.scnActorId).Assembly;
            foreach (var element in jArray)
            {
                var nodeTypeStr = element.Value<string>("Type");
                if (string.IsNullOrEmpty(nodeTypeStr)) continue;
                var nodeType = wolvenkitAssembly.GetType($"WolvenKit.RED4.CR2W.Types.{nodeTypeStr}", false, true);
                if (nodeType == null) continue;
                var colorsObj = element.Value<JObject>("Colors");

                Dictionary<Editor.GraphStyle.Color, uint> colors;
                if (!TypeColors.TryGetValue(nodeType, out colors))
                {
                    colors = new Dictionary<Editor.GraphStyle.Color, uint>();
                    TypeColors.Add(nodeType, colors);
                }

                foreach (JProperty jStyle in colorsObj.Children())
                {
                    Editor.GraphStyle.Color styleColor;
                    if (string.Equals(jStyle.Name, "NodeBody", StringComparison.OrdinalIgnoreCase))
                        styleColor = Editor.GraphStyle.Color.NodeBg;
                    else if (string.Equals(jStyle.Name, "NodeText", StringComparison.OrdinalIgnoreCase))
                        styleColor = Editor.GraphStyle.Color.NodeName;
                    else if (string.Equals(jStyle.Name, "SocketText", StringComparison.OrdinalIgnoreCase))
                        styleColor = Editor.GraphStyle.Color.SocketName;
                    else if (string.Equals(jStyle.Name, "NodeNameplate", StringComparison.OrdinalIgnoreCase))
                        styleColor = Editor.GraphStyle.Color.NodeNameplate;
                    else
                        continue;

                    uint rgba;                    
                    if (jStyle.Value is JArray rgbaArray && rgbaArray.Count == 4)
                    {
                        rgba = ToRGBA(rgbaArray[0].Value<byte>(), rgbaArray[1].Value<byte>(), rgbaArray[2].Value<byte>(), rgbaArray[3].Value<byte>());
                    }
                    else
                    {
                        var rgbaHex = colorsObj.Value<string>(jStyle.Name);
                        if (string.IsNullOrEmpty(rgbaHex))
                            continue;

                        if (rgbaHex[0] == '#')
                            rgbaHex = rgbaHex.Substring(1);

                        if (!uint.TryParse(rgbaHex, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint rgbaReversed))
                            continue;

                        var a = (byte)rgbaReversed;
                        rgbaReversed >>= 8;
                        var b = (byte)rgbaReversed;
                        rgbaReversed >>= 8;
                        var g = (byte)rgbaReversed;
                        rgbaReversed >>= 8;
                        var r = (byte)rgbaReversed;
                        rgba = ToRGBA(r, g, b, a);
                    }

                    if (styleColor == Editor.GraphStyle.Color.NodeName)
                    {
                        colors.Add(Editor.GraphStyle.Color.NodeName, rgba);
                        colors.Add(Editor.GraphStyle.Color.NodeDescription, rgba);
                    }
                    else
                    {
                        colors.Add(styleColor, rgba);
                    }
                }
            }
        }
    }
}
