using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Gas.Graphics;
using Gas.Helpers;

namespace TheGrid
{
    public partial class TheGridForm : GraphicsForm
    {
        public class GridNode
        {
            public Vector2 Pos = new Vector2();
            public Vector2 Vel = new Vector2();
            public Vector2 Forces = new Vector2();
            public float Mass = 0.0f;
        }

        #region Variables
        private Config config = new Config( "Config.txt" );

        private bool showFPS = false;
        private Gas.Graphics.Font fpsFont = null;

        private Cursor cursor = null;

        private Gas.Graphics.Texture sceneTex = null;
        private BloomPostProcessor bloomProcessor = null;
        private bool useBloom = true;

        private Color gridColor = Color.FromArgb( 0, 85, 175 );
        private int colorChangeRate = 1;
        private int redDir = 1;
        private int greenDir = 1;
        private int blueDir = 1;

        private int gridSize = 0;
        private GridNode[ , ] grid = null;
        private float nodeMass = 0.0f;
        private float springConstant = 0.0f;
        private float springXEquilibrium = 0.0f;
        private float springYEquilibrium = 0.0f;
        private float springDamping = 0.0f;
        private int cursorInfluenceMagnitude = 0;
        #endregion

        #region Constructor
        public TheGridForm()
        {
            InitializeComponent();
        }
        #endregion

        #region GraphicsForm Methods
        protected override void InitializeGame()
        {
            System.Windows.Forms.Cursor.Hide();

            renderer.ProjectionMode = ProjectionMode.Orthogonal;
            renderer.ViewMatrix = Matrix.LookAtLH( new Vector3( 0, 0, -5.0f ), new Vector3(), new Vector3( 0, 1, 0 ) );
            renderer.Device.RenderState.Lighting = false;

            showFPS = config.GetSetting<bool>( "ShowFPS" );

            fpsFont = renderer.CreateFont( "Arial", 16 );
            fpsFont.ShadowColor = Color.Gray;

            cursor = new Cursor( renderer, "cursor", new Size( 10, 10 ) );

            useBloom = config.GetSetting<bool>( "UseBloom" );

            sceneTex = new Gas.Graphics.Texture( renderer, renderer.FullscreenSize.Width, renderer.FullscreenSize.Height,
                true );
            bloomProcessor = new BloomPostProcessor( renderer );
            bloomProcessor.Blur = config.GetSetting<float>( "BloomBlur" );
            bloomProcessor.BloomScale = config.GetSetting<float>( "BloomScale" );
            bloomProcessor.BrightPassThreshold = config.GetSetting<float>( "BloomBrightPassThreshold" );

            cursorInfluenceMagnitude = config.GetSetting<int>( "CursorInfluenceMagnitude" );

            InitializeGrid();

            this.KeyDown += new KeyEventHandler( OnKeyDown );
        }

        protected override void UpdateEnvironment()
        {
            if ( !renderer.Windowed )
            {
                System.Windows.Forms.Cursor.Position = new Point( renderer.FullscreenSize.Width,
                    renderer.FullscreenSize.Height );
            }

            cursor.Update( timer.MoveFactorPerSecond );
            UpdateGrid( timer.MoveFactorPerSecond );
        }

        protected override void Render3DEnvironment()
        {
            renderer.Clear( ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0 );

            if ( useBloom )
            {
                renderer.SaveRenderTarget();
                sceneTex.SetAsRenderTarget();
                renderer.Clear();
            }

            renderer.Begin( null );
            renderer.WorldMatrix = Matrix.Identity;
            RenderGrid();
            renderer.End();

            cursor.Render();
            renderer.Render( false );

            if ( useBloom )
            {
                renderer.SetScreenAsRenderTarget();

                Matrix oldView = renderer.ViewMatrix;
                renderer.ViewMatrix = Matrix.LookAtLH( new Vector3( 0, 0, -5.0f ), new Vector3(),
                    new Vector3( 0, 1, 0 ) );

                bloomProcessor.SceneImage = sceneTex;
                bloomProcessor.Render();

                renderer.ViewMatrix = oldView;
            }

            if ( showFPS )
            {
                renderer.Begin( null );
                fpsFont.RenderText( new Vector2( 20, 20 ), "FPS: " + timer.FramesPerSecond.ToString(), Color.White, true );
                renderer.End();
            }

            renderer.Present();
        }

        void OnKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Escape )
                running = false;
        }

        private void InitializeGrid()
        {
            gridSize = config.GetSetting<int>( "Resolution" );
            nodeMass = config.GetSetting<float>( "NodeMass" );
            springConstant = config.GetSetting<float>( "SpringConstant" );
            springDamping = config.GetSetting<float>( "SpringDamping" );
            springXEquilibrium = renderer.FullscreenSize.Width / gridSize;
            springYEquilibrium = renderer.FullscreenSize.Height / gridSize;

            grid = new GridNode[ gridSize, gridSize ];

            for ( int x = 0; x < gridSize; ++x )
            {
                for ( int y = 0; y < gridSize; ++y )
                {
                    grid[ x, y ] = new GridNode();
                    grid[ x, y ].Mass = nodeMass;
                }
            }

            int dx = ( int )springXEquilibrium;
            int dy = -( int )springYEquilibrium;

            int startX = -renderer.FullscreenSize.Width / 2 + dx / 2;
            int startY = renderer.FullscreenSize.Height / 2 + dy / 2;

            int px = startX;
            int py = startY;

            for ( int x = 0; x < gridSize; ++x )
            {
                for ( int y = 0; y < gridSize; ++y )
                {
                    grid[ x, y ].Pos = new Vector2( ( float )px, ( float )py );

                    py += dy;
                }

                py = startY;
                px += dx;
            }
        }

        private void RenderGrid()
        {
            int numVertices = 4 * ( 1 - gridSize + gridSize * gridSize );
            CustomVertex.PositionColored[] vertices = new CustomVertex.PositionColored[ numVertices ];

            int index = 0;

            for ( int x = 0; x < gridSize; ++x )
            {
                for ( int y = 0; y < gridSize; ++y )
                {
                    if ( x < gridSize - 1 )
                    {
                        vertices[ index ].Position = new Vector3( grid[ x, y ].Pos.X, grid[ x, y ].Pos.Y, 1.0f );
                        vertices[ index ].Color = gridColor.ToArgb();

                        index++;

                        vertices[ index ].Position = new Vector3( grid[ x + 1, y ].Pos.X, grid[ x + 1, y ].Pos.Y, 1.0f );
                        vertices[ index ].Color = gridColor.ToArgb();

                        index++;
                    }

                    if ( y < gridSize - 1 )
                    {
                        vertices[ index ].Position = new Vector3( grid[ x, y ].Pos.X, grid[ x, y ].Pos.Y, 1.0f );
                        vertices[ index ].Color = gridColor.ToArgb();

                        index++;

                        vertices[ index ].Position = new Vector3( grid[ x, y + 1 ].Pos.X, grid[ x, y + 1 ].Pos.Y, 1.0f );
                        vertices[ index ].Color = gridColor.ToArgb();

                        index++;
                    }
                }
            }

            renderer.Device.VertexFormat = CustomVertex.PositionColored.Format;
            renderer.Device.DrawUserPrimitives( PrimitiveType.LineList, numVertices / 2, vertices );
        }

        private void UpdateGrid( float moveFactor )
        {
            UpdateGridColor();

            float dampingMultiplier = ( float )Math.Exp( -moveFactor * springDamping );

            for ( int x = 1; x < gridSize - 1; ++x )
            {
                for ( int y = 1; y < gridSize - 1; ++y )
                {
                    if ( cursor.LeftButtonPressed )
                    {
                        grid[ x, y ].Vel -= Vector2.Normalize( cursor.Center - grid[ x, y ].Pos ) *
                            ( cursorInfluenceMagnitude / Vector2.Length( cursor.Center - grid[ x, y ].Pos ) ) * moveFactor;
                    }
                    else if ( cursor.RightButtonPressed )
                    {
                        grid[ x, y ].Vel += Vector2.Normalize( cursor.Center - grid[ x, y ].Pos ) *
                            ( cursorInfluenceMagnitude / Vector2.Length( cursor.Center - grid[ x, y ].Pos ) ) * moveFactor;
                    }

                    Vector2 averageNeighborPos =
                        ( grid[ x - 1, y - 1 ].Pos + grid[ x, y - 1 ].Pos + grid[ x + 1, y - 1 ].Pos +
                          grid[ x - 1, y ].Pos + grid[ x + 1, y ].Pos +
                          grid[ x - 1, y + 1 ].Pos + grid[ x, y + 1 ].Pos + grid[ x + 1, y + 1 ].Pos ) * 0.125f;

                    grid[ x, y ].Forces += -springConstant * ( grid[ x, y ].Pos - averageNeighborPos );
                    grid[ x, y ].Vel += grid[ x, y ].Forces * ( moveFactor / grid[ x, y ].Mass );
                    grid[ x, y ].Vel *= dampingMultiplier;
                    grid[ x, y ].Pos += grid[ x, y ].Vel * moveFactor;

                    grid[ x, y ].Forces = new Vector2();
                }
            }
        }

        private void UpdateGridColor()
        {
            if ( gridColor.R == 255 )
                redDir = -1;
            else if ( gridColor.R == 0 )
                redDir = 1;

            if ( gridColor.G == 255 )
                greenDir = -1;
            else if ( gridColor.G == 0 )
                greenDir = 1;

            if ( gridColor.B == 255 )
                blueDir = -1;
            else if ( gridColor.B == 0 )
                blueDir = 1;

            gridColor = Color.FromArgb(
                gridColor.R + redDir * colorChangeRate,
                gridColor.G + greenDir * colorChangeRate,
                gridColor.B + blueDir * colorChangeRate );
        }
        #endregion
    }
}