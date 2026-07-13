using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// Summary description for AALayer.
	/// </summary>
	public class AASpriteObject : SpriteObject {

		private bool antiAlias;

		public AASpriteObject(Device device,SpriteTexture tex) : base(device,tex) {}

		public override void Draw(Matrix parentMatrix, Vector2 parentShift) {
			//ponytail: per-draw MSAA toggling is a D3D9 concept; multisampling is backbuffer-wide now
			base.Draw (parentMatrix, parentShift);
		}

		public bool AntiAlias {
			get { return antiAlias; }
			set { antiAlias=value; }
		}
	}
}