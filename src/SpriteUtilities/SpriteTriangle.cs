using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities {
	/// <summary>
	/// A textured, screen-aligned triangle
	/// </summary>
	public class SpriteTriangle : TransformedObject {
		protected PositionColoredTextured[] verts;	//Triangle vertices
		protected Texture2D tex;	//Texture to apply

		public SpriteTriangle(GraphicsDevice device,Texture2D tex) : base(device) {
			//Store the texture
			this.tex=tex;

			//Create the vertices
			verts=new PositionColoredTextured[3];
			for (int i=0;i<verts.Length;i++)
				verts[i]=new PositionColoredTextured(0,0,1,Color.White,0,0);
		}


		#region vertex control
		/// <summary>
		/// Sets the position of a vertex
		/// </summary>
		/// <param name="index">The vertex's index, 0 to 2</param>
		/// <param name="position">The vertex's new position</param>
		public void SetPosition(int index,Vector2 position) {
			verts[index].X=position.X;
			verts[index].Y=position.Y;
		}

		/// <summary>
		/// Gets the position of a vertex
		/// </summary>
		/// <param name="index">The vertex's index, 0 to 2</param>
		public Vector2 GetPosition(int index) {
			return new Vector2(verts[index].X,verts[index].Y);
		}

		/// <summary>
		/// Sets the texture coordinates of a vertex
		/// </summary>
		/// <param name="index">The vertex's index, 0 to 2</param>
		/// <param name="coords">The vertex's new texture coordinates</param>
		public void SetTexCoords(int index,Vector2 coords) {
			verts[index].Tu=coords.X;
			verts[index].Tv=coords.Y;
		}

		/// <summary>
		/// Gets the texture coordinates of a vertex
		/// </summary>
		/// <param name="index">The vertex's index, 0 to 2</param>
		public Vector2 GetTexCoords(int index) {
			return new Vector2(verts[index].Tu,verts[index].Tv);
		}
		#endregion

		#region buffer
		protected override void checkUpdates() {
			base.checkUpdates();
			if (colorNeedsUpdate) setColor(ColorX.FromArgb((int)alpha,color));	//Apply the color to the vertices
		}

		protected void setColor(Color color) {
			verts[0].Color=verts[1].Color=verts[2].Color=color;
		}
		#endregion

		#region drawing
		protected override void deviceDraw(Matrix trans) {
			Renderer.DrawList(trans,tex,verts);
		}
		#endregion
	}
}
