using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// Decorative sprite
	/// </summary>
	public class MapSprite : SpriteObject, Updateable {
		private Map
			map;

		private float
			invParallax,
			animRate,
			accumulator;

		private Vector2
			basePosition;

		public MapSprite(Device device,SpriteTexture tex,Map map,Vector2 basePosition,float depth,float animRate) : base(device,tex) {
			invParallax=1-depth;
			this.animRate=animRate;
			this.basePosition=basePosition-new Vector2(tex.Width/2,tex.Height/2);
			this.map=map;
		}

		public void Scroll() {
			position=basePosition-new Vector2((int)(invParallax*map.Position.X),(int)(invParallax*map.Position.Y));
		}

		#region Updateable Members
		public void Update(float elapsedTime) {
			if (animRate>0) {
				accumulator+=elapsedTime;
				while (accumulator>animRate) {
					accumulator-=animRate;
					if (Frame==tex.Frames-1) Frame=0;
					else Frame++;
				}
			}
		}
		#endregion
	}
}
