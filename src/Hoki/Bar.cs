using System;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// A general purpose progress bar
	/// </summary>
	public class Bar : SpriteObject, Updateable {
		private const float
			changeRate=4;	//Rate of change in percent, %dist/sec

		private SpriteObject
			left,			//Bar bounds
			middle,
			right,
			bar;			//Scaled bar

		private float
			percent,		//Percent full the bar is
			targetPercent,	//percent the bar is approaching 
			fullWidth,		//Width of the bar at 100% full
			totalWidth;		//Width of the bar holder

		public override float Height {
			get { return middle.Height; }
		}

		public override float Width {
			get { return totalWidth; }
		}


		public Bar(Device device,SpriteTexture leftTex,SpriteTexture middleTex,SpriteTexture rightTex,SpriteTexture barTex,int width) : base(device,null) {
			totalWidth=width;

			left=new SpriteObject(device,leftTex);
			Add(left);

			middle=new SpriteObject(device,middleTex);
			middle.X=leftTex.Width;
			middle.Width=fullWidth=width-leftTex.Width-rightTex.Width;
			Add(middle);

			right=new SpriteObject(device,rightTex);
			right.X=middle.X+middle.Width;
			Add(right);

			bar=new SpriteObject(device,barTex);
			bar.X=middle.X;
			bar.Y=(middleTex.Height-barTex.Height)/2;
			Add(bar);
		}

		public float TargetPercent {
			get { return targetPercent; }
			set { targetPercent=value; }
		}

		public float Percent {
			get { return percent; }
			set { percent=targetPercent=value; }
		}

		#region Updateable Members

		public void Update(float elapsedTime) {
			//Move towards targetPercent and scale the bar
			percent+=(targetPercent-percent)*changeRate*elapsedTime;
			bar.Width=fullWidth*percent;
		}

		#endregion
	}
}
