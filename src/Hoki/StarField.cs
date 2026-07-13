using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;
using FloatMath;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// A parallax starfield that can have other objects in it
	/// </summary>
	public class StarField : SpriteObject, Updateable {
		private const float
			minScale=0.5f,		//Minimum scale of a star
			starZ=0.5f,			//Z-value of the closest (1-scale) stars
			totalMoveTime=0.8f,	//Time to spend on each move
			partRatio=0.5f;		//Decimal percent of the move time spent on the acceleration curve
		
		private float
			width,			//Bounds of the starfield
			height,
			lastStep,		//Length of the last timestep
			moveTime;		//Time into the current move

		private Vector2
			shift,			//Scroll
			velocity,		//Current velocity derived from change in position
			target,			//Position the current move is targeting
			lastCoord,		//Last coordinate set used in the shift
			initVelocity,	//Velocity at the beginning of the current move
			initPosition,	//Position at the beginning of the current move
			topSpeed,		//Top speed in each direction for the current move
			partTime,		//First segment of time for the current move
			distance;		//Distance in each direction for the current move

		private bool
			overShot;		//Whether the target has been overshot and the move needs to be compensated

		private SpriteObject
			objLayer,	//All non-star objects
			starLayer;	//All stars go in this object

		private Star[] stars;		//Array of stars for updates

		public StarField(Device device,SpriteTexture starTex,float width,float height,int numStars) : base(device,null) {
			//Copy the width and height
			this.width=width;
			this.height=height;

			//Prevent premature movement
			moveTime=float.MaxValue;

			velocity=new Vector2();
			lastStep=1;

			//Make an object layer
			objLayer=new SpriteObject(device,null);

			//Make the star layer and array
			starLayer=new SpriteObject(device,null);
			stars=new Star[numStars];

			for (int i=0;i<numStars;i++) {
				//Create a new star
				Star star=new Star(device,starTex,Rand.NextFloat(minScale,1));
				
				//Position it randomly
				star.X=Rand.NextFloat(width);
				star.Y=Rand.NextFloat(height);

				//Add it to the layer and array
				stars[i]=star;
				starLayer.Add(star);
			}

			//Add layers
			Add(starLayer);
			Add(objLayer);
		}


		public void AddObject(SpriteObject obj) {
			objLayer.Add(obj);
		}

		public void MoveTo(Vector2 position) {
			target=position;
			distance=position-shift;				//Get the distance
			initVelocity=Vector2.Multiply(velocity,1/lastStep);	//Store the current velocity TO REVERT TO STABLE (but still not right) VERSION, get rid of the scale on this line
			initPosition=shift;						//Store the current position;

			topSpeed=new Vector2(getTopSpeed(distance.X,initVelocity.X),getTopSpeed(distance.Y,initVelocity.Y));
			partTime=new Vector2(getPartTime(distance.X,initVelocity.X,topSpeed.X),getPartTime(distance.Y,initVelocity.Y,topSpeed.Y));

			moveTime=0;	//Reset the move timer
		}

		public void SetTo(Vector2 position) {
			target=shift=position;
			MoveTo(position);
		}

		private float getTopSpeed(float dist,float initVel) {
			float a=-totalMoveTime*2/3,b=dist*(2-partRatio)-totalMoveTime*initVel/3,c=dist*initVel*(1-partRatio);
			float root=FMath.Sqrt(b*b-4*a*c),denom=2*a;

			return Math.Max(FMath.Abs((-b-root)/denom),FMath.Abs((-b+root)/denom))*FMath.Sgn(dist);
		}

		private float getPartTime(float dist,float initVel,float topSpeed) {
			return partRatio*dist/(initVel+(topSpeed-initVel)*2/3);
		}

		public float StarRotation {
			get { return stars[0].Rotation; }
			set { foreach (Star star in stars) star.Rotation=value; }
		}

		private float coord(float dist,float initSpeed,float topSpeed,float partTime,float time,float currentPos) {
			//Prevent nonmoving objects from NaNing
			if (float.IsNaN(partTime)) return 0;

			if (time<partTime) {
				float tSquared=time*time;

				return initSpeed*time+tSquared/partTime*(topSpeed-initSpeed)*(1-time/3/partTime);
			} else if (time<totalMoveTime) {
				float t=time-partTime,secondTime=totalMoveTime-partTime;

				return dist*partRatio+topSpeed*t*(1-t/secondTime+t*t/3/secondTime/secondTime);
			} else {
				if ((target-shift).Length()<=velocity.Length()/lastStep) return dist;
				else {
					overShot=true;
					return currentPos;
				}
			}
		}

		#region Updateable Members

		public void Update(float elapsedTime) {
			//Record the timestep length
			lastStep=elapsedTime;

			//Find position, velocity
			moveTime+=elapsedTime;
			Vector2 oldShift=shift;
			lastCoord=new Vector2(coord(distance.X,initVelocity.X,topSpeed.X,partTime.X,moveTime,lastCoord.X),coord(distance.Y,initVelocity.Y,topSpeed.Y,partTime.Y,moveTime,lastCoord.Y));
			shift=initPosition+lastCoord;
			velocity=shift-oldShift;

			if (overShot) {
				MoveTo(target);
				overShot=false;
			}

			//Get the unit velocity
			float speed=velocity.Length();

			//Move the stars
			float starScale=1+speed/3;
			foreach (Star star in stars) {
				Vector2 starVelocity=Vector2.Multiply(velocity,-star.Z*starZ);
				star.X+=starVelocity.X;
				star.Y+=starVelocity.Y;
				star.XScale=starScale*star.Z*Star.SizeFactor;

				//Bound/wrap the move
				if (star.X>width)	star.X-=width;
				else if (star.X<0)	star.X+=width;
				if (star.Y>height)	star.Y-=height;
				else if (star.Y<0)	star.Y+=height;
			}

			//Rotate stars (HACK: skipping on 180° turns, but there's got to be a better when to get rid of the jumpiness
			float newRotation=FMath.Atan2(velocity.Y,velocity.X);
			if (Math.Abs(Math.Abs(newRotation%(FMath.PI))-Math.Abs(StarRotation%(FMath.PI)))>0.1f && velocity.Length()!=0) StarRotation=newRotation;	//Rotate the stars

			//Move everything else
			objLayer.Position=-shift;
		}

		#endregion

		private class Star : SpriteObject {
			public const float SizeFactor=0.7f;

			private float z;
			
			public Star(Device device,SpriteTexture tex,float scale) : base(device,tex) {
				//Copy the scale
				z=scale;

				//Center the origin
				origin.X=tex.Width/2;
				origin.Y=tex.Height/2;

				float factor=scale*SizeFactor;
				XScale=YScale=factor;
				Alpha*=factor;
			}

			public float Z {
				get { return z; }
			}
		}
	}
}
