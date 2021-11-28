using System.Numerics;
using ImGuiNET;

namespace GraphEditor.Editor
{
    public class GraphLink : IGraphLink
    {
        public long Id => _id;
        public bool IsHighlighted { get; set; }
        public IGraphSocket Source { get; set; }
        public IGraphSocket Destination { get; set; }
        protected readonly long _id;

        public GraphLink(long id, IGraphSocket source, IGraphSocket destination)
        {
            _id = id;
            Source = source;
            Destination = destination;
        }

        /* temp solution to link hell */
        static uint U32FromRGBA(byte r, byte g, byte b, byte a = 255)
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
        static readonly uint[] RandomColors = new uint[]
        {
            U32FromRGBA(0, 0, 0),
            U32FromRGBA(255, 0, 0),
            U32FromRGBA(0, 255, 0),
            U32FromRGBA(0, 0, 255),

            U32FromRGBA(0, 255, 255),         
            U32FromRGBA(255, 0, 255),
            U32FromRGBA(255, 255, 0),

            U32FromRGBA(170, 255, 0),
            U32FromRGBA(170, 0, 255),
            U32FromRGBA(0, 170, 255),
            U32FromRGBA(255, 0, 170),
            U32FromRGBA(255, 170, 0),

            U32FromRGBA(255, 127, 39),
        };
        static System.Random _rnd = new System.Random();
        static uint GetRandomLinkColor()
        {
            return RandomColors[_rnd.Next(RandomColors.Length)];
        }
        protected uint _randomLinkColor = GetRandomLinkColor();
        /* ========================== */

        public virtual void Draw()
        {
            var context = GraphContext.CurrentContext;
            var style = context.Style;
            float LinkCurve = 75.0f * context.UIScale;
            float LinkThickness = 1.75f * context.UIScale;

            var drawList = ImGui.GetWindowDrawList();
            var sourcePosition = Source.ScreenPosition;
            var destinationPosition = Destination.ScreenPosition;

            drawList.AddBezierCubic(sourcePosition, sourcePosition + new Vector2(LinkCurve, 0.0f)
                , destinationPosition - new Vector2(LinkCurve, 0.0f), destinationPosition
                , IsHighlighted ? 0xFFFFFFFF : _randomLinkColor // style.GetColor(GraphStyle.Color.Link)
                , LinkThickness);
        }

        public virtual void Remove_DONOTUSE_ONLYGRAPH()
        {
            Source.RemoveLink_DONOTUSE_ONLYGRAPH(this);
            Destination.RemoveLink_DONOTUSE_ONLYGRAPH(this);
            Source = null;
            Destination = null;
        }
    }
}
