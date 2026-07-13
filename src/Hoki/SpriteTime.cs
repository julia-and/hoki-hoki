using System;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// A graphic display of a time string
	/// </summary>
	public class SpriteTime : SpriteObject {
		private const int numChars=6;		//Number of supported characters
		private float totalWidth;			//Width of all characters together
		private static readonly int[]
			numberPositions,	//Positions of digits in a time string
			localPositions;		//Positions in the characters array that those digits map to
		private SpriteObject[] characters;	//Array of characters in the time

		static SpriteTime() {
			numberPositions=new int[] {0,2,3,5,6};
			localPositions=new int[] {0,2,3,4,5};
		}

		public SpriteTime(Device device,SpriteTexture largeTex,SpriteTexture smallTex) : base(device,null) {
			characters=new SpriteObject[numChars];
			float pos=0;
			float yDiff=largeTex.Height-smallTex.Height;
			for (int i=0;i<characters.Length;i++) {
				if (i==1) pos-=4;	//HACK
				characters[i]=new SpriteObject(device,(i<4?largeTex:smallTex));
				characters[i].X=pos;
				if (i>=4) characters[i].Y=yDiff;
				pos+=characters[i].Width;
				Add(characters[i]);
				if (i==1) pos-=5;	//HACK
			}
			totalWidth=pos;
			characters[1].Frame=10;	//The second character is always a colon
		}

		public override float Width {
			get { return totalWidth; }
		}

		public override float Height {
			get { return characters[0].Height; }
		}

		public void setTime(string time) {
			if (time.Length!=numChars+1) return;	//Can't handle time strings that aren't exactly the right length

			//Set the characters' frames
			for (int i=0;i<numberPositions.Length;i++)
				characters[localPositions[i]].Frame=int.Parse(""+time[numberPositions[i]]);
		}
	}
}
