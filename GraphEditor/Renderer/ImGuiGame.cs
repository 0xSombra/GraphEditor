using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using ImGuiNET;

namespace GraphEditor.Renderer
{
    /// <summary>
    /// Simple FNA + ImGui example
    /// See https://github.com/Sombra0xCC/ImGui.NET/blob/master/src/ImGui.NET.SampleProgram.XNA/SampleGame.cs
    /// </summary>
    public class ImGuiGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;

        public ImGuiGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += Window_ClientSizeChanged;
            IsMouseVisible = true;
            IsFixedTimeStep = false;
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.PreferMultiSampling = true;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void Draw(GameTime gameTime)
        {
            var clearColor = ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg];
            GraphicsDevice.Clear(new Color(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W));

            _imGuiRenderer.BeforeLayout(gameTime);
            MainWindow.Draw();
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }
    }
}
