using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities {
	/// <summary>
	/// A class containing a textured quad and transformations of it. May contain nested child sprites to which
	/// the parent's transformations as well as their own are applied.
	/// </summary>
	public class SpriteObject : EffectObject {
		#region vars
		protected PositionColoredTextured[] verts;	//Textured quad (4 vertices/triangle strip)
		protected SpriteTexture tex;	//Texture with width, height, and frame count

		//Filtering
		protected TextureFilter filter;	//Filter to draw with, if setFilter is true
		protected bool setFilter;		//Whether the filter should be changed to draw

		//State
		protected int frame;			//Current frame of the texture
		#endregion

		#region getset
		/// <summary>
		/// The scaled width of the sprite
		/// </summary>
		virtual public float Width {
			get {
				if (tex==null) return 0;
				return scale.X*tex.Width;
			}
			set { if (tex!=null) scale.X=value/tex.Width; }
		}

		/// <summary>
		/// The scaled height of the sprite
		/// </summary>
		virtual public float Height {
			get {
				if (tex==null) return 0;
				return scale.Y*tex.Height;
			}
			set { if (tex!=null) scale.Y=value/tex.Height; }
		}

		/// <summary>
		/// Frame to draw, counting from 0
		/// </summary>
		virtual public int Frame {
			get { return frame; }
			set {
				//Set the frame
				frame=value;
				setFrame(value);
			}
		}

		virtual public float AbsoluteXScale {
			get {
				if (parent==null) return XScale;
				else return parent.XScale*XScale;
			}
		}

		virtual public float AbsoluteYScale {
			get {
				if (parent==null) return YScale;
				else return parent.YScale*YScale;
			}
		}

		virtual public float AbsoluteWidth {
			get {
				if (tex==null) return 0;
				else return AbsoluteXScale*tex.Width;
			}
		}

		virtual public float AbsoluteHeight {
			get {
				if (tex==null) return 0;
				else return AbsoluteYScale*tex.Height;
			}
		}

		virtual public TextureFilter DrawFilter {
			get { return filter; }
			set {
				filter=value;
				setFilter=true;
			}
		}
		#endregion

		#region constructor
		public SpriteObject(GraphicsDevice device,SpriteTexture tex) : base(device) {
			//Store the texture
			this.tex=tex;

			if (tex!=null) {
				//Create vertices for a textured quad (4 vertices/triangle strip)
				verts=new PositionColoredTextured[4];

				verts[0]=new PositionColoredTextured(0.0f,0.0f,1.0f,Color.White,0.0f,1.0f);			//Top left
				verts[1]=new PositionColoredTextured(0.0f,tex.Height,1.0f,Color.White,0.0f,0.0f);		//Bottom left
				verts[2]=new PositionColoredTextured(tex.Width,0.0f,1.0f,Color.White,1.0f,1.0f);		//Top right
				verts[3]=new PositionColoredTextured(tex.Width,tex.Height,1.0f,Color.White,1.0f,0.0f);	//Bottom right
			}

			//Set the frame
			frame=0;
			setFrame(0);
		}

		#endregion

		#region device
		public static void SetRenderStates(GraphicsDevice device) {
			//States are applied per draw call by Renderer.Apply; nothing to latch globally.
		}

		public static void SetupCamera(GraphicsDevice device,int screenWidth,int screenHeight) {
			if (Renderer.Effect==null) Renderer.Init(device,screenWidth,screenHeight);
			else Renderer.SetProjection(screenWidth,screenHeight);
		}
		#endregion

		#region buffer

		protected override void checkUpdates() {
			base.checkUpdates();

			if (colorNeedsUpdate) setColor(ColorX.FromArgb((int)alpha,color));	//Apply the color to the vertices
		}

		private void setFrame(int frame) {
			if (tex==null) return;

			//Update the vertices' texture coordinates
			Vector4 coords=tex.TexCoords(frame);
			verts[0].Tu=verts[1].Tu=coords.X; //Left
			verts[0].Tv=verts[2].Tv=coords.Y; //Top
			verts[2].Tu=verts[3].Tu=coords.Z; //Right
			verts[1].Tv=verts[3].Tv=coords.W; //Bottom
		}

		private void setColor(Color color) {
			if (tex==null) return;

			//Set vertex colors
			verts[0].Color=verts[1].Color=verts[2].Color=verts[3].Color=color;
		}
		#endregion

		#region drawing
		public override void Draw(Matrix parentMatrix,Vector2 parentShift) {
			TextureFilter oldFilter=Renderer.Filter;

			if (setFilter && filter!=oldFilter) Renderer.Filter=filter;

			base.Draw(parentMatrix,parentShift);

			if (setFilter && filter!=oldFilter) Renderer.Filter=oldFilter;
		}

		protected override void deviceDraw(Matrix trans) {
			base.deviceDraw(trans);
			if (tex!=null) {
				if (current!=null) { //Method 1: use an effect (if the base class set one)
					//Pass constant values to the effect
					foreach (FXConstant fxc in allConstants) sendFXC(fxc,trans);

					//Draw with the effect
					foreach (EffectPass pass in current.CurrentTechnique.Passes) {
						pass.Apply();
						device.DrawUserPrimitives(PrimitiveType.TriangleStrip,verts,0,2);
					}
				} else
					//Method 2: the shared BasicEffect stands in for the fixed function pipeline
					Renderer.DrawStrip(trans,tex.Tex,verts);
			}
		}

		protected override void sendFXC(FXConstant fxc,Matrix trans) {
			if (current!=null) {
				switch (fxc.Type) {
					case ConstType.Color:
						current.Parameters[fxc.Name].SetValue(new Vector4(color.R,color.G,color.B,color.A));
						break;
					case ConstType.Texture:
						if (tex!=null) current.Parameters[fxc.Name].SetValue(tex.Tex); //Can't set with a null tex, but it doesn't matter because the effect routine won't be run
						break;
					case ConstType.WorldMatrix:
						current.Parameters[fxc.Name].SetValue(trans);
						break;
				}
			}
		}
		#endregion
	}
}
