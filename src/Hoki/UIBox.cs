using System;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// A graphical box
	/// </summary>
	public class UIBox : SpriteObject {
		private int
			boxWidth,
			boxHeight;

		private SpriteObject 
			tlc,
			trc,
			blc,
			brc,
			top,
			left,
			right,
			bottom,
			middle;

		public UIBox(Device device,UIBoxTex tex,int width,int height) : base(device,null) {
			//Store the size
			boxWidth=width;
			boxHeight=height;

			//Make objects
			tlc=new SpriteObject(device,tex.Tlc);
			trc=new SpriteObject(device,tex.Trc);
			blc=new SpriteObject(device,tex.Blc);
			brc=new SpriteObject(device,tex.Brc);
			top=new SpriteObject(device,tex.Top);
			left=new SpriteObject(device,tex.Left);
			right=new SpriteObject(device,tex.Right);
			bottom=new SpriteObject(device,tex.Bottom);
			middle=new SpriteObject(device,tex.Middle);

			//Position them
			top.X=tlc.Width;
			top.Width=width-tlc.Width-trc.Width;

			trc.X=top.X+top.Width;

			left.Y=top.Height;
			left.Height=height-trc.Height-brc.Height;

			blc.Y=left.Y+left.Height;

			right.X=trc.X;
			right.Y=left.Y;
			right.Height=left.Height;

			bottom.X=top.X;
			bottom.Y=blc.Y;
			bottom.Width=top.Width;

			brc.X=trc.X;
			brc.Y=blc.Y;

			middle.X=tlc.Width;
			middle.Y=tlc.Height;
			middle.Width=top.Width;
			middle.Height=left.Height;

			//Add to draw list
			Add(tlc);
			Add(trc);
			Add(blc);
			Add(brc);
			Add(top);
			Add(left);
			Add(right);
			Add(bottom);
			Add(middle);
		}

		public override float Width {
			get { return boxWidth; }
		}

		public override float Height {
			get { return boxHeight; }
		}
	}
}
