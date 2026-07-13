using System;
using FloatMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities {
	/// <summary>
	/// An unfilled rectangle drawn with four SpriteLines.
	/// </summary>
	public class SpriteBox : SpriteObject {
		private SpriteLine[]
			lines;		//Hold all lines
		private float
			width,		//Box dimensions
			height,
			thickness;	//Line width

		#region getset
		public override float Width {
			get { return width; }
			set {
				width=value;
				recalculate();
			}
		}

		public override float Height {
			get { return height; }
			set {
				height=value;
				recalculate();
			}
		}

		public float Thickness {
			get { return thickness; }
			set {
				thickness=value;
				foreach (SpriteLine l in lines) l.Thickness=value;
			}
		}

		public override Color Tint {
			get { return base.Tint; }
			set {
				base.Tint=value;
				foreach (SpriteLine l in lines) l.Tint=value;
			}
		}
		#endregion

		public SpriteBox(GraphicsDevice device) : base(device,null) {
			lines=new SpriteLine[4];
			for (int i=0;i<lines.Length;i++) {
				lines[i]=new SpriteLine(device);//Construct lines
				lines[i].Angle=-i*FMath.PI/2;	//Set angles in order: bottom line, right, top, left
				lines[i].Length=0;
				this.Add(lines[i]);
			}
		}

		/// <summary>
		/// Reconfigures the box so that its x and y coordinates are in the top-left corner and its width and height are positive.
		/// </summary>
		public void Normalize() {
			if (width<0) {
				X+=width;
				width=-width;
			}
			if (height<0) {
				Y+=height;
				height=-height;
			}
		}

		/// <summary>
		/// Determines whether a point in the parent's coordinate space is within the box
		/// </summary>
		/// <param name="pt"></param>
		public bool PtInBox(Vector2 pt) {
			return (((width>0 && pt.X>X && pt.X<X+width) || (width<0 && pt.X<X && pt.X>X+width)) && ((height>0 && pt.Y>Y && pt.Y<Y+height) || (height<0 && pt.Y<Y && pt.Y>Y+height)));
		}

		/// <summary>
		/// Updates lines to reflect current width and height specifications
		/// </summary>
		protected void recalculate() {
			float[] dimensions={width,height};	//Put the dimensions in a temporary array for convenient access
			for (int i=0;i<lines.Length;i++) {
				lines[i].Length=dimensions[i%2];
				if (i>0 && i<3) {
					lines[i].X=dimensions[0];
					lines[i].Y=dimensions[1];
				}
			}
		}
	}
}
