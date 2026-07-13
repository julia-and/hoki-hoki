using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;
using FloatMath;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// Summary description for Slider.
	/// </summary>
	public class Slider : MenuElement {
		private SpriteObject barLeft,barMiddle,barRight,slider;
		private float rate=0.05f,percent=0;
		private int sliderWidth,sliderHeight;

		public event EventHandler Change,Press;

		public Slider(Device device,SpriteTexture barLeftTex,SpriteTexture barMiddleTex,SpriteTexture barRightTex,SpriteTexture sliderTex,int width,int height) : base(device,null) {
			sliderWidth=width;
			sliderHeight=height;

			Vector2 barOrigin=new Vector2(0,barMiddleTex.Height/2);

			barLeft=new SpriteObject(device,barLeftTex);
			barLeft.Origin=barOrigin;
			barLeft.Y=height/2;
			Add(barLeft);			

			barMiddle=new SpriteObject(device,barMiddleTex);
			barMiddle.Width=width-barLeftTex.Width-barRightTex.Width;
			barMiddle.X=barLeft.Width;
			barMiddle.Y=height/2;
			barMiddle.Origin=barOrigin;
			Add(barMiddle);

			barRight=new SpriteObject(device,barRightTex);
			barRight.X=barMiddle.X+barMiddle.Width;
			barRight.Y=height/2;
			barRight.Origin=barOrigin;
			Add(barRight);

			slider=new SpriteObject(device,sliderTex);
			slider.Origin=new Vector2(sliderTex.Width/2,sliderTex.Height/2);
			slider.Y=height/2;
			Add(slider);

			//Hook own events to prevent NREs
			Change+=new EventHandler(onChange);
			Press+=new EventHandler(onPress);
		}

		public override float Height {
			get { return sliderHeight; }
		}

		public override float Width {
			get { return sliderWidth; }
		}

		//Distance to move the slider per keypress
		public float Rate {
			get { return rate; }
			set { rate=value; }
		}

		public float Value {
			get { return percent; }
			set {
				value=FMath.Clamp(value,0,1);
				percent=value;
				slider.X=sliderWidth*value;
				Change(this,new EventArgs());
			}
		}

		#region MenuElement Members

		public override void Input(Controls control) {
			if (control==Controls.Left) Value-=rate;
			else if (control==Controls.Right) Value+=rate;
			else if (control==Controls.A) Press(this,new EventArgs());
		}

		public override void Select() {
			slider.Frame=barLeft.Frame=barMiddle.Frame=barRight.Frame=1;
		}

		public override void Deselect() {
			slider.Frame=barLeft.Frame=barMiddle.Frame=barRight.Frame=0;
		}

		#endregion

		#region own event handlers
		private void onChange(object sender, EventArgs e) {
			;//nop
		}

		private void onPress(object sender, EventArgs e) {
			;//nop
		}
		#endregion
	}
}
