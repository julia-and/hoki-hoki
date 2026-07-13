using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities {
	/// <summary>
	/// Shared rendering state for the sprite scene graph. Replaces the D3D9 fixed-function
	/// pipeline (world transform + modulate texture stage) with a shared BasicEffect.
	/// </summary>
	public static class Renderer {
		public static GraphicsDevice Device;
		public static BasicEffect Effect;
		public static SpriteBatch Batch;

		//Current magnification filter; SpriteObject.DrawFilter temporarily overrides this (D3D9 sampler state emulation)
		public static TextureFilter Filter=TextureFilter.Linear;

		//Current blend state; particle effects temporarily switch this to Additive (D3D9 render state emulation)
		public static BlendState Blend=BlendState.NonPremultiplied;

		public static void Init(GraphicsDevice device,int screenWidth,int screenHeight) {
			Device=device;
			Effect=new BasicEffect(device);
			Effect.VertexColorEnabled=true;
			Effect.World=Matrix.Identity;
			Effect.View=Matrix.Identity;
			SetProjection(screenWidth,screenHeight);
			Batch=new SpriteBatch(device);
		}

		//Logical scene size; the viewport may be larger (resizable window renders at native res)
		public static int LogicalWidth=640,LogicalHeight=480;

		public static void SetProjection(int width,int height) {
			//Matches D3DX OrthoOffCenterLH(0.5,w+0.5,h+0.5,0.5,0,1): y-down pixel coordinates.
			//Wide z range because legacy vertices carry z=1 and depth testing is off anyway.
			LogicalWidth=width;
			LogicalHeight=height;
			Effect.Projection=Matrix.CreateOrthographicOffCenter(0.5f,width+0.5f,height+0.5f,0.5f,-10f,10f);
		}

		/// <summary>
		/// Applies blend/sampler/raster state and the world transform, then leaves the
		/// device ready for a DrawUserPrimitives call. Mirrors SpriteObject.SetRenderStates.
		/// </summary>
		public static void Apply(Matrix world,Texture2D tex) {
			Device.BlendState=Blend;
			Device.DepthStencilState=DepthStencilState.None;
			Device.RasterizerState=RasterizerState.CullNone;
			Device.SamplerStates[0]=Filter==TextureFilter.Point?SamplerState.PointWrap:SamplerState.LinearWrap;
			Effect.World=world;
			Effect.Texture=tex;
			Effect.TextureEnabled=tex!=null;
			Effect.CurrentTechnique.Passes[0].Apply();
		}

		public static void DrawStrip(Matrix world,Texture2D tex,PositionColoredTextured[] verts) {
			Apply(world,tex);
			Device.DrawUserPrimitives(PrimitiveType.TriangleStrip,verts,0,verts.Length-2);
		}

		public static void DrawList(Matrix world,Texture2D tex,PositionColoredTextured[] verts) {
			Apply(world,tex);
			Device.DrawUserPrimitives(PrimitiveType.TriangleList,verts,0,verts.Length/3);
		}

		public static void DrawIndexed(Matrix world,Texture2D tex,PositionColoredTextured[] verts,short[] indices) {
			Apply(world,tex);
			Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,verts,0,verts.Length,indices,0,indices.Length/3);
		}

		public static void DrawLineList(Matrix world,PositionColored[] verts,short[] indices) {
			Apply(world,null);
			Device.DrawUserIndexedPrimitives(PrimitiveType.LineList,verts,0,verts.Length,indices,0,indices.Length/2);
		}

		/// <summary>
		/// Draws a triangle fan (dead primitive in modern APIs) as an indexed triangle list.
		/// </summary>
		public static void DrawFan(Matrix world,Texture2D tex,PositionColoredTextured[] verts) {
			int tris=verts.Length-2;
			if (tris<1) return;
			short[] indices=new short[tris*3];
			for (int i=0;i<tris;i++) {
				indices[i*3]=0;
				indices[i*3+1]=(short)(i+1);
				indices[i*3+2]=(short)(i+2);
			}
			DrawIndexed(world,tex,verts,indices);
		}
	}

	/// <summary>
	/// Replacements for D3DX matrix helpers used by the scene graph.
	/// </summary>
	public static class MatrixUtil {
		/// <summary>
		/// D3DX Matrix.Transformation2D with scalingRotation always 0 (the only form this codebase uses).
		/// Row-vector composition: scale about scalingCenter, rotate about rotationCenter, then translate.
		/// </summary>
		public static Matrix Transformation2D(Vector2 scalingCenter,float scalingRotation,Vector2 scaling,Vector2 rotationCenter,float rotation,Vector2 translation) {
			Matrix m=Matrix.CreateTranslation(-scalingCenter.X,-scalingCenter.Y,0)
				*Matrix.CreateScale(scaling.X,scaling.Y,1)
				*Matrix.CreateTranslation(scalingCenter.X,scalingCenter.Y,0);
			if (rotation!=0) {
				m=m*Matrix.CreateTranslation(-rotationCenter.X,-rotationCenter.Y,0)
					*Matrix.CreateRotationZ(rotation)
					*Matrix.CreateTranslation(rotationCenter.X,rotationCenter.Y,0);
			}
			return m*Matrix.CreateTranslation(translation.X,translation.Y,0);
		}
	}

	/// <summary>
	/// System.Drawing.Color.FromArgb replacements for XNA Color.
	/// </summary>
	public static class ColorX {
		public static Color FromArgb(int alpha,Color baseColor) {
			return new Color(baseColor.R,baseColor.G,baseColor.B,(byte)MathHelper.Clamp(alpha,0,255));
		}

		public static Color FromArgb(int r,int g,int b) {
			return new Color(r,g,b);
		}

		public static Color FromArgb(int a,int r,int g,int b) {
			return new Color(r,g,b,a);
		}
	}

	[Flags]
	public enum FontDrawFlags {
		None=0,
		Center=1,
		VerticalCenter=2,
		Right=4,
		WordBreak=8,
		NoClip=16
	}
}
