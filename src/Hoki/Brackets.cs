using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;
using FloatMath;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// Summary description for Brackets.
	/// </summary>
	public class Brackets : SpriteObject,Updateable {
		private SpriteObject targetObject;	//Object to mark
		private SpriteObject[] corners;		//The four corners
		private Vector2[] targets;			//Targets for each corner
		private Vector2[] nullTargets;		//Locations to go to when there is no target
		private Vector2 cornerSize;			//Size of the corner texture

		private const float rate=6;			//Rate of approach towards target (*diff/sec)
		private const float hover=4;		//Distance to hover away from target

		/// <param name="tex">Texture for one corner of the brackets</param>
		public Brackets(Device device,SpriteTexture tex,float screenWidth,float screenHeight) : base(device,null) {
			//Note the corner's size
			cornerSize=new Vector2(tex.Width,tex.Height);

			//Create the corners array
			corners=new SpriteObject[4];

			//Create the null targets
			targets=nullTargets=getTargets(new Vector2(),new Vector2(screenWidth,screenHeight));

			//Create the corners
			Vector2 origin=new Vector2(tex.Width,tex.Height);	//Origin for each object
			for (int i=0;i<corners.Length;i++) {
				//Create the corner and orient it
				corners[i]=new SpriteObject(device,tex);
				corners[i].Origin=origin;
				corners[i].Rotation=i*FMath.PI/2;
				
				//Position it outside the screen
				corners[i].X=targets[i].X;
				corners[i].Y=targets[i].Y;

				//Add the corner to the childlist
				Add(corners[i]);
			}
		}

		public void SetTarget(SpriteObject o) {
			targetObject=o;
		}

		private Vector2[] getTargets(Vector2 topLeft,Vector2 bottomRight) {
			Vector2[] targets=new Vector2[4];
			for (int i=0;i<4;i++) {
				targets[i]=new Vector2();
				
				if (i<2) targets[i].Y=topLeft.Y-hover;
				else targets[i].Y=bottomRight.Y+hover;

				if (i==0 || i==3) targets[i].X=topLeft.X-hover;
				else  targets[i].X=bottomRight.X+hover;
			}

			return targets;
		}

		#region Updateable Members
		public void Update(float elapsedTime) {
			//Update targets
			if (targetObject==null) targets=nullTargets;
			else {
				//Find the corners
				Vector2 topLeft=new Vector2(-targetObject.Origin.X,-targetObject.Origin.Y);
				targetObject.LocalToGlobal(ref topLeft);
				Vector2 bottomRight=Vector2.Add(topLeft,new Vector2(targetObject.AbsoluteWidth,targetObject.AbsoluteHeight));

				//Make the target array
				targets=getTargets(topLeft,bottomRight);
			}
			
			//Move the corners towards the target
			for (int i=0;i<4;i++) {
				corners[i].X+=(targets[i].X-corners[i].X)*rate*elapsedTime;
				corners[i].Y+=(targets[i].Y-corners[i].Y)*rate*elapsedTime;
			}
		}
		#endregion
	}
}
