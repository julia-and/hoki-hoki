using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;
using FloatMath;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// Catches mines sent by a Launcher
	/// </summary>
	public class Catcher : SpriteObject {
		private int turns;		//Number of 45° rotations
		private Segment seg;	//Represents the front face, acts as a wall

		/// <summary>
		/// Number of 45° rotations
		/// </summary>
		public int Turns {
			get { return turns; }
			set {
				turns=value%8;
				Rotation=value*FMath.PI/4;
			}
		}

		public Segment Segment {
			get { return seg; }
		}

		public Catcher(Device device,SpriteTexture tex) : base(device,tex) {
            //Center the origin in the texture
			origin.X=tex.Width/2;
			origin.Y=tex.Height/2;
		}

		/// <summary>
		/// Updates the Segment returned by Seg() to reflect the current transformations
		/// </summary>
		public void UpdateSeg() {
			//Get half dimensions
			float halfWidth=tex.Width/2;
			float halfHeight=tex.Height/2;

			//Get corners in local space
			Vector2 p1=new Vector2(halfWidth,halfHeight);
			Vector2 p2=new Vector2(halfWidth,-halfHeight);

			//Convert them to the parent (map's/zero'd layer's) coordinate space
			LocalToParent(ref p1);
			LocalToParent(ref p2);

			//Put them in the seg
			seg=new Segment(p1,p2,null);
		}
	}
}
