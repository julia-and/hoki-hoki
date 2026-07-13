using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using SpriteUtilities;
using FloatMath;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// Reverses the direction of the heli when hit
	/// </summary>
	public class Spring : SpriteObject,Updateable {
		private const float frameLength=0.05f;	//Time in seconds to stay on a frame while animating

		private Segment surface;	//Segment to represent the surface in map space
		private Vector2 forward;	//Direction the surface is facing
		private float frameTime;	//if >0, Time until switching frames

		public static SoundEffect Sound;
		private static int currentSound;

		private float soundWait;
		private const float soundDelay=0.07f;

		public Vector2 Forward {
			get { return forward; }
			set { forward=value; }
		}
		
		public Segment Segment {
			get { return surface; }
		}

		public Spring(Device device,SpriteTexture tex,float x,float y,int turns) : base(device,tex) {
			//Put the origin at the root of the spring
			origin.X=tex.Width/2;
			origin.Y=tex.Height;

			//Rotate and get a vector representing an forward direction (the direction the surface faces)
			Rotation=turns*FMath.PI/2;	//Each turn is 45°
			forward=new Vector2(FMath.Cos(Rotation-FMath.PI/2),FMath.Sin(Rotation-FMath.PI/2));

			//Position	
			X=x-4;	//1. Unangled springs move -MapScale/2,-MapScale/2
			Y=y-4;	//2. Account for wall size and move outwards

			//Make a segment representative of the surface
			Vector2 surfaceCenter=forward;
			surfaceCenter = Vector2.Multiply(surfaceCenter, tex.Height);
			surfaceCenter = Vector2.Add(surfaceCenter, position);
			Vector2 slope=new Vector2(forward.Y,forward.X);
			slope = Vector2.Multiply(slope, tex.Width/2);
			surface=new Segment(Vector2.Subtract(surfaceCenter,slope),Vector2.Add(surfaceCenter,slope),null);
		}

		public void Hit() {
			Frame=1;
			frameTime=frameLength;
			if (Game.FXOn && soundWait<0) {
				soundWait=soundDelay;
				Sound.Play(Game.FXVolume,0,0); // PlayFlags.Default
			}
		}

		#region Updateable Members

		public void Update(float elapsedTime) {
			soundWait-=elapsedTime;

			bool aboveZero=frameTime>0;	//determine whether frameTime was >0 so if it's less we know to switch
			frameTime-=elapsedTime;
			if (frameTime<0 && aboveZero) {
				if (Frame==tex.Frames-1) Frame=0;	//End of bounce, stop animating
				else {
					Frame++;
					frameTime=frameLength;			//Continue animating
				}
			}
		}

		#endregion
	}
}