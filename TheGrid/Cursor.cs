using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.DirectX;
using Gas.Graphics;
using Gas.Input;

namespace TheGrid
{
    public class Cursor
    {
        #region Variables
        private MouseDevice mouse = null;
        private Surface surface = null;
        private Size size = new Size();

        private Renderer renderer = null;

        private Vector3 pos = new Vector3();
        private float movementScaleFactor = 1.0f;

        private bool clicked = false;

        private bool clickedLastUpdate = false;
        #endregion

        #region Properties
        public bool LeftButtonPressed
        {
            get
            {
                return mouse.LeftButtonPressed;
            }
        }

        public bool RightButtonPressed
        {
            get
            {
                return mouse.RightButtonPressed;
            }
        }

        public bool LeftButtonJustPressed
        {
            get
            {
                return ( !clickedLastUpdate && mouse.LeftButtonPressed );
            }
        }

        public Vector3 Position
        {
            get
            {
                return pos + new Vector3( surface.Size.Width * 2, surface.Size.Height * 2, 0.0f );
            }
            set
            {
                pos = value;
            }
        }

        public Vector2 Center
        {
            get
            {
                return new Vector2( pos.X + surface.Size.Width, pos.Y + surface.Size.Height );
            }
            set
            {
                pos = new Vector3( value.X - surface.Size.Width, value.Y - surface.Size.Height, 0.0f );
            }
        }

        public Size Size
        {
            get
            {
                return size;
            }
        }
        #endregion

        #region Events
        public event EventHandler Clicked;
        public event EventHandler Unclicked;
        #endregion

        #region Constructor
        public Cursor( Renderer renderer, string materialName, Size size )
        {
            this.renderer = renderer;

            this.size = size;
            surface = new Surface( renderer, materialName, size );

            mouse = new MouseDevice( null );
        }

        public Cursor( Renderer renderer, string materialName, Size size,
            float movementScaleFactor )
        {
            this.renderer = renderer;

            this.size = size;
            this.movementScaleFactor = movementScaleFactor;
            surface = new Surface( renderer, materialName, size );

            mouse = new MouseDevice( null );
        }
        #endregion

        #region Public methods
        public void Update( float moveFactor )
        {
            clickedLastUpdate = mouse.LeftButtonPressed;
            mouse.Update();

            pos.X -= mouse.MovementVector.X * movementScaleFactor;
            pos.Y += mouse.MovementVector.Y * movementScaleFactor;
            pos.Z += mouse.MovementVector.Z * movementScaleFactor;

            Matrix inverseProjView = Matrix.Invert( renderer.ProjectionMatrix ) * Matrix.Invert( renderer.ViewMatrix );
            Vector3 screenPos = Vector3.TransformCoordinate( pos, renderer.ViewMatrix * renderer.ProjectionMatrix );
            Vector2 worldSpaceUpRightBounds = Vector2.TransformCoordinate( new Vector2( 1, 1 ), inverseProjView );
            Vector2 worldSpaceBottomLeftBounds = Vector2.TransformCoordinate( new Vector2( -1, -1 ), inverseProjView );

            if ( screenPos.X < -1 )
                pos.X = worldSpaceBottomLeftBounds.X;
            else if ( screenPos.X > 1 )
                pos.X = worldSpaceUpRightBounds.X;

            if ( screenPos.Y < -1 )
                pos.Y = worldSpaceBottomLeftBounds.Y;
            else if ( screenPos.Y > 1 )
                pos.Y = worldSpaceUpRightBounds.Y;

            if ( mouse.LeftButtonPressed )
            {
                if ( !clicked && Clicked != null )
                    Clicked( this, null );

                clicked = true;
            }
            else
            {
                if ( clicked && Clicked != null )
                    Unclicked( this, null );

                clicked = false;
            }
        }

        public void Render()
        {
            surface.Render( new Vector3( pos.X + surface.Size.Width,
                pos.Y + surface.Size.Height, -1.0f ) );
        }
        #endregion
    }
}