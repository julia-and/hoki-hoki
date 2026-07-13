using System;
using SpriteUtilities;
using FloatMath;
using Microsoft.Xna.Framework.Graphics;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// Updateable SpriteObject that can gradually fade itself to a given transparency
	/// </summary>
	public class Fader : SpriteObject, Updateable {
		private const float rate=6;	//Rate of change in alpha, dec%diff/sec
		private float
			target,
			rateScale=1;

		public float FadeTarget {
			get { return target; }
			set { target=FMath.Clamp(value,0,SpriteObject.MaxAlpha); }
		}

		/// <summary>
		/// Modifies the fade rate
		/// </summary>
		public float RateScale {
			get { return rateScale; }
			set { rateScale=value; }
		}

		public Fader(Device device,SpriteTexture tex) : base(device,tex) {
			target=Alpha=0;
		}

		#region Updateable Members

		public void Update(float elapsedTime) {
			float diff=target-Alpha;
			if (Math.Abs(diff)<1) Alpha=target;
			else Alpha+=(target-Alpha)*rate*rateScale*elapsedTime;
		}

		#endregion
	}
}
