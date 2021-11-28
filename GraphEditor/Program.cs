using System;

namespace GraphEditor
{
    static class Program
    {
        static void InitializeCyberpunkStuff()
        {
            CP77.TmpNodeStyling.Reload("styling.json");
            CP77.TweakDB.Reload("tweakdb.str");
        }

        [STAThread]
        static void Main()
        {
            InitializeCyberpunkStuff();
            using var game = new Renderer.ImGuiGame();
            game.Run();
        }
    }
}
