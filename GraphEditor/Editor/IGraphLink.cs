namespace GraphEditor.Editor
{
    public interface IGraphLink
    {
        long Id { get; }
        bool IsHighlighted { get; set; }
        IGraphSocket Source { get; set; }
        IGraphSocket Destination { get; set; }

        void Draw();
        void Remove_DONOTUSE_ONLYGRAPH();
    }
}
