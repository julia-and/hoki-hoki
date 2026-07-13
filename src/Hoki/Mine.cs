using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;
using FloatMath;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// Projectile that moves from a Launcher to a Catcher
	/// </summary>
	public class Mine : SpriteObject,Updateable {
		public const int speed=100;				//Speed of movement, px/sec
		private const float borderDist=0.7f;	//Distance of borders from center, decimal% of half-dimension
		
		private Vector2
			direction;	//Direction of motion
		private Catcher
			catcher;	//Launcher that will catch this

		/// <summary>
		/// Fired when the mine should be removed
		/// </summary>
		public event EventHandler Die;

		/// <summary>
		/// Fired when the mine hits the heli
		/// </summary>
		public event EventHandler Explode;

		public Vector2 Direction {
			get { return direction; }
		}

		public Mine(Device device,SpriteTexture tex,Vector2 direction,Catcher catcher) : base(device,tex) {
			//Store the direction vector
			this.direction=direction;

			//Center origin in the texture
			float halfWidth=tex.Width/2;
			float halfHeight=tex.Height/2;
			origin.X=halfWidth;
			origin.Y=halfHeight;

			//Scale sizes for borders
			halfWidth*=borderDist;
			halfHeight*=borderDist;
		}

		public Catcher Catcher {
			get { return catcher; }
			set { catcher=value; }
		}

		public void Hit() {
			Explode(this,new EventArgs());
		}

		#region Updateable Members
		public void Update(float elapsedTime) {
			//Move
			X+=direction.X*speed*elapsedTime;
			Y+=direction.Y*speed*elapsedTime;

			if ((Math.Abs(direction.X)>0.01f && FMath.Sgn(direction.X)!=FMath.Sgn(catcher.X-X)) || (Math.Abs(direction.Y)>0.01f && FMath.Sgn(direction.Y)!=FMath.Sgn(catcher.Y-Y))) Die(this,new EventArgs());
		}
		#endregion

	}

}
