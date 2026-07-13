using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using SpriteUtilities;
using FloatMath;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	public class Heli : AASpriteObject, Updateable {
		#region vars
		private const bool
			invincible=false;	//DEBUG

		public const int
			FullHealth=3,
			relaxations=10;	//Times to relax constraints per frame

		private const float
			rSpeed=1.4f,	//Rotation speed, rad/sec
			tSpeed=170,		//Translation speed, px/sec
			aBoost=1.4f,	//A-button speed coefficient
			bBoost=1.6f,	//B-button speed coefficient
			bounce=300,		//Force with which particles bounce on collision
			maxForce=750,	//Maximum force that can ever be in effect on a particle
			expPower=1500,	//Magnitude of force from explosions
			recover=10,		//Rate of recovery from forced motion
			finishSpeed=1000,		//Rate of movement to center once finished, px/sec
			finishRotate=10,		//Rate of rotation once finished, rad/sec
			immuneTime=0.5f,		//Time immune to damage after taking a hit
			springImmuneTime=0.5f,	//Time immune to spring turns after touching a spring
			padHealthWait=1;		//Time it takes to get a unit of health from a pad

		private Controller
			controller;		//Input provider
		private Map
			map;			//Map this is located in
		private Vector2
			p1,p2,			//Particles representing endpoints of the stick
			f1,f2,			//Force on each particle
			expForce,		//Force from explosion
			finishPos;		//Position to move to after entering the finish pad
		private int
			length,			//Length of the stick constraint
			health,			//Current health remaining
			rDirection=1;	//Direction of rotation
		private bool
			started,		//Whether the player has moved off the start pad
			finished,		//Whether the player has moved onto the finish pad
			dead,			//Whether the player has died
			perfect;		//True until the player hits something
		private float
			padTime=padHealthWait,	//Time until granted one health from a pad
			immunity=0,				//If >0, player is immune to damage for this many seconds
			springImmunity=0;		//If >0, player is immune to spring turns for this many seconds

		//Events
		public event HitEventHandler Hit;
		public event EventHandler
			Start,
			Finish,
			Die;
		#endregion

		public static SoundEffect HitSound;
		public static SoundEffect HealSound;
		private float soundWait;
		private const float soundDelay=0.07f;

		public Heli(Device device,SpriteTexture tex,Map map) : base(device,tex) {
			//Store a map reference
			this.map=map;

			//Default perfect to true, health to full
			perfect=true;
			health=FullHealth;

			//Store the size of the heli
			length=tex.Width;	//Assumes horizontal orientation

			//Create endpoints and set their initial positions
			p1=new Vector2(-length/2,0);
			p2=new Vector2(length/2,0);

			//Push the origin to the center of the graphic
			origin.X=tex.Width/2;
			origin.Y=tex.Height/2;

			//Hook own events to avoid nullref exceptions
			Hit+=new HitEventHandler(onHit);
			Start+=new EventHandler(onStart);
			Finish+=new EventHandler(onFinish);
			Die+=new EventHandler(onDie);
		}

		#region getset
		public int Length {
			get { return length; }
		}

		public bool Perfect {
			get { return perfect; }
		}
		
		/// <summary>
		/// Object providing input to the Heli
		/// </summary>
		public Controller Controller {
			get { return controller; }
			set { controller=value; }
		}

		/// <summary>
		/// The point in the exact center of the stick
		/// </summary>
		public Vector2 Midpoint {
			get { return Vector2.Multiply(p1+p2,0.5f); }
			set {
				Vector2 difference=value-Midpoint;
				p1+=difference;
				p2+=difference;
			}
		}

		public int Health {
			get { return health; }
			set { health=value; }
		}
		#endregion

		#region Updateable Members

		public void Update(float elapsedTime) {
			soundWait-=elapsedTime;

			if (dead) return;	//Don't do anything if dead.

			if (finished) {
				float finishDist=FMath.Distance(Midpoint,finishPos);
				if (finishDist>0.1f) {
					float moveDist=Math.Min(finishSpeed*elapsedTime,finishDist);
					Vector2 finishMove=Vector2.Multiply(finishPos-Midpoint,moveDist/finishDist);
					p1+=finishMove;
					p2+=finishMove;
					position+=finishMove;
				}
				Rotation+=finishRotate*elapsedTime;
			} else {
				bool hit=false;			//If there is a collision at any time this frame, it will be stored here
				Vector2 lf1,lf2;		//Most recently calculated forces for each point
				lf1=lf2=Vector2.Zero;

				//Count down immunity
				immunity-=elapsedTime;
				springImmunity-=elapsedTime;

				//Get movement from the input
				Vector2 move=new Vector2();
				float speedScale=1;
				if (controller.Down(Controls.Left))		move.X-=tSpeed;		//Directional keys
				if (controller.Down(Controls.Right))	move.X+=tSpeed;
				if (controller.Down(Controls.Up))		move.Y-=tSpeed;
				if (controller.Down(Controls.Down))		move.Y+=tSpeed;
				if (controller.Down(Controls.A))		speedScale*=aBoost;	//Speed booster keys
				if (controller.Down(Controls.B))		speedScale*=bBoost;
				move = Vector2.Multiply(move, speedScale*elapsedTime);	//Scale the move by dt and applied boost

				//Store the particles' current position
				Vector2 op1=p1,op2=p2;
				
				//Cap the force
				if (f1.Length()>maxForce) f1 = Vector2.Multiply(f1, maxForce/f1.Length());
				if (f2.Length()>maxForce) f2 = Vector2.Multiply(f2, maxForce/f2.Length());

				//Move the particles
				p1+=move+Vector2.Multiply(f1+expForce,elapsedTime);
				p2+=move+Vector2.Multiply(f2+expForce,elapsedTime);

				//Scale down the force
				f1 = Vector2.Multiply(f1, 1-recover*elapsedTime);
				f2 = Vector2.Multiply(f2, 1-recover*elapsedTime);
				expForce = Vector2.Multiply(expForce, 1-recover*elapsedTime);

				//Get the midpoint and rotate the particles
				Vector2 mp=Midpoint;
				FMath.RotatePoint(ref p1,mp,rSpeed*rDirection*elapsedTime);
				FMath.RotatePoint(ref p2,mp,rSpeed*rDirection*elapsedTime);

				Vector2 ix;	//Hold points output by Segment.Intersection
				Vector2 hitPos=Vector2.Zero;

				float slope,norm,ang;
				int side;

				//Prepare Segment lists
				ArrayList segments=(ArrayList)map.Segments.Clone(),bounded=new ArrayList();
				for (int r=0;r<5;r++) {
					Vector2[] delta={
						p1-op1,
						p2-op2,
					};

					Segment s=new Segment(p1,p2,null),os=new Segment(op1,op2,null);
					Triangle t1,t2;
					if (Segment.Intersection(s,os,out ix)) {
						t1=new Triangle(p1,ix,op1);
						t2=new Triangle(p2,ix,op2);
					} else {
						t1=new Triangle(op1,p1,op2);
						t2=new Triangle(op2,p1,p2);
					}

					//Apply the stick constraint
					float dist=FMath.Distance(p1,p2);
					Vector2 diff=p1-p2;
					diff = Vector2.Multiply(diff, (length-dist)/dist*0.5f);
					p1+=diff;
					p2-=diff;

					//Get the move segments
					Segment[] moveSegs={
						new Segment(op1,p1,null),
						new Segment(op2,p2,null)
					};

					//Add wall segments that are within bounds
					Rect bound=Rect.GetRect(s.BoundingBox,os.BoundingBox);
					bound.Expand(16);

					for (int i=0;i<segments.Count;i++) {
						ObjectSegment mapSeg=(ObjectSegment)segments[i];

						if (Rect.Overlap(mapSeg.Segment.BoundingBox,bound)) {
							segments.Remove(mapSeg);
							bounded.Add(mapSeg);
							i--;
						}
					}
					
					//Apply the wall constraint
					float ratio;	//Indicate where on the heli a hit occurred
					foreach (ObjectSegment mapSeg in bounded) {	//TODO: GET BOUNDED WORKING
						//Determine whether this object is a spring
						bool spring=mapSeg.Object is Spring;

						//See if either of the heli's endpoints violated a wall
						for (int i=0;i<moveSegs.Length;i++) {
							Segment moveSeg=moveSegs[i];
							if (Segment.Intersection(mapSeg.Segment,moveSeg,out ix)) {
								hitPos=ix;
								side=1;

								slope=mapSeg.Segment.Slope;
								norm=FMath.Atan(-1/slope);

								if (slope==0) {
									norm=FMath.PI/2;
									if (op1.Y<mapSeg.Segment.P1.Y) side=-1;
								} else if (mapSeg.Segment.X(op1.Y)>op1.X || (float.IsInfinity(slope) && op1.X<mapSeg.Segment.P1.X))
									side=-1;

								//Determine force (different if it's a spring)
								Vector2 force;
								if (spring) force=Vector2.Multiply(((Spring)mapSeg.Object).Forward,bounce/4);	//Project out regardless of side
								else force=new Vector2(FMath.Cos(norm)*side*delta[i].Length()*bounce,FMath.Sin(norm)*side*delta[i].Length()*bounce);

								if (i==0) {
									if (!spring) p1=op1;
									if (r==0) lf1=force;
								} else {
									if (!spring) p2=op2;
									if (r==0) lf2=force;
								}

								if (spring)	hitSpring(mapSeg.Segment,((Spring)mapSeg.Object).Forward,ix,(Spring)mapSeg.Object);
								else		hit=true;	//Note that a hit occurred
							}
						}
						Vector2[] ends={mapSeg.Segment.P1,mapSeg.Segment.P2};
						foreach (Vector2 end in ends) {
							//Point-in-triangle test: see if a vector from the map is in the future triangle
							if (t1.PtInTri(end) || t2.PtInTri(end)) {
								//Find the closest point on the line
								ang=mapSeg.Segment.Angle;
								Segment perpendicular=new Segment(end,new Vector2(FMath.Cos(ang),FMath.Sin(ang)));
								Segment.Intersection(perpendicular,os,out ix);

								//Find the position of the intersection on the heli
								ratio=FMath.Clamp(FMath.Distance(p1,ix)/length,0,1);

								if (spring) {
									Vector2 force=Vector2.Multiply(((Spring)mapSeg.Object).Forward,bounce/4);	//Project out regardless of side
									if (r==0) {
										lf1=Vector2.Multiply(force,1-ratio);
										lf2=Vector2.Multiply(force,ratio);
									}

									hitSpring(mapSeg.Segment,((Spring)mapSeg.Object).Forward,ix,(Spring)mapSeg.Object);
								} else {
									if (r==0) {
										lf1=Vector2.Multiply(delta[0],-2*bounce*(1-ratio));
										lf2=Vector2.Multiply(delta[1],-2*bounce*ratio);
									}

									hitPos=ix;
									hit=true;	//Note that a hit occurred
								}

								if (!spring) {
									p1=op1;
									p2=op2;
								}
							}
						}
					}

					//Mine testing
					for (int i=0;i<map.Mines.Count;i++) {
						//Get the mine
						Mine mine=(Mine)map.Mines[i];

						//Get a unit vector of the perpendicular angle
						ang=s.Angle+FMath.PI/2;
						Vector2 unit=new Vector2(FMath.Cos(ang),FMath.Sin(ang));

						Segment perpendicular=new Segment(mine.Position,unit);	//Line perpendicular to the heli through the mine's midpoint
						Segment.Intersection(perpendicular,s,out ix);			//Point of intersection with heli

						Vector2 left,right;	//Left and right ends of the heli
						if (s.P1.X<s.P2.X)	{ left=s.P1; right=s.P2; }	//Figure out which end is which
						else				{ left=s.P2; right=s.P1; }

						//Bound the intersection point within the segment
						if (ix.X<left.X)		ix=left;
						else if (ix.X>right.X)	ix=right;

						//Get the distance to the intersection and test if it's within range
						if (FMath.Distance(ix,mine.Position)<mine.Width/2) {
							//Determine the point to apply force at
							ratio=FMath.Distance(s.P1,ix)/length;

							//Get a unit vector for the direction of force
							ang=FMath.Angle(ix,mine.Position);
							Vector2 forceUnit=new Vector2(FMath.Cos(ang),FMath.Sin(ang));

							//Apply the force
							lf1=Vector2.Multiply(forceUnit,(1-ratio)*maxForce);
							lf2=Vector2.Multiply(forceUnit,ratio*maxForce);
							expForce=Vector2.Multiply(forceUnit,expPower);

							mine.Hit();	//Inform the mine it has been hit
							i--;		//Step back in the list, since a mine was removed
							hit=true;	//Note that a hit occurred
						}
					}
				}

				//If there was a collision...
				if (hit) {
					perfect=false;				//No longer a perfect run

					//Take damage
					bool hurt=false;
					if (immunity<0) {
						hurt=true;
						immunity=immuneTime;
						if (!invincible) health=Math.Max(0,health-1);

						if (health==0) {
							dead=true;
							Die(this,new EventArgs());
						}
					}

					//Inform the game
					Hit(this,new HitEventArgs(hurt,hitPos));	//fire the Hit event
				}

				//Force
				f1+=lf1;
				f2+=lf2;

				//Check for pad collision
				Pad pad=map.OnPad(Midpoint);
				if (pad==null) {
					if (!started) {
						Start(this,new EventArgs());
						started=true;
					}
				} else if (pad.Type==PadType.End) {
					if (!finished) {
						Finish(this,new EventArgs());
						finished=true;
						finishPos=new Vector2(pad.X+pad.Width/2,pad.Y+pad.Height/2);
					}
				} else {
					//On a start or heal pad, slowly regenerate health
					padTime-=elapsedTime;
					//Regenerate if the wait time has passed and nothing has been hit recently
					if (padTime<0 && immunity<0) {
						padTime=padHealthWait;
						if (health<FullHealth) {
							health++;
							Game.PlaySfx(HealSound);
						}
					}
				}

				//Find the effective move and scroll the map
				position=Midpoint;

				//Rotate the graphic
				Rotation=FMath.Angle(p1,p2);
			}
		}

		private void hitSpring(Segment springSeg,Vector2 springNorm,Vector2 ix,Spring spring) {
			if (springImmunity>0) return;

			//Get the normal that points in the same direction as the heli's centerpoint
			if (springSeg.Side(Vector2.Add(ix,springNorm))!=springSeg.Side(Midpoint)) springNorm = Vector2.Multiply(springNorm, -1);
			Vector2 normPoint=Vector2.Add(ix,springNorm);
                                    
			//Get the angle between the normal and the heli
			float normAng=FMath.Angle(ix,normPoint);
			float heliAng=FMath.Angle(ix,Midpoint);

			float heliNormAng;
			if (normAng==FMath.PI) heliNormAng=heliAng;
			else heliNormAng=normAng-heliAng;

			this.rDirection=-FMath.Sgn(heliNormAng);

			//Project out
			p1 = Vector2.Add(p1, springNorm);
			p2 = Vector2.Add(p2, springNorm);

			//Let the Spring know it's been hit
			spring.Hit();
		}

		#endregion

		#region event handlers
		private void onStart(object sender, EventArgs e) {

		}

		private void onFinish(object sender, EventArgs e) {

		}

		private void onDie(object sender, EventArgs e) {

		}

		private void onHit(object sender, HitEventArgs e) {
			if (Game.FXOn && soundWait<0) {
				soundWait=soundDelay;
				Game.PlaySfx(HitSound);
			}
			map.Explode(e.Position,true);
		}
		#endregion
	}

	#region event stuff
	public delegate void HitEventHandler(object sender,HitEventArgs e);

	public class HitEventArgs : EventArgs {
		private bool hurt;
		private Vector2 position;

		public bool Hurt {
			get { return hurt; }
		}

		public Vector2 Position {
			get { return position; }
		}

		public HitEventArgs(bool hurt,Vector2 position) {
			this.hurt=hurt;
			this.position=position;
		}
	}
	#endregion
}