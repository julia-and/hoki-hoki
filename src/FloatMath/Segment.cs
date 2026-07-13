using Microsoft.Xna.Framework;
using System;

namespace FloatMath {
	/// <summary>
	/// A line segment with methods for collision detection
	/// </summary>
	public class Segment : ICloneable {
		#region vars
		private Vector2
			p1,p2;	//Two points defining the segment
		#endregion

		#region getset

		public Vector2 P1 {
			get { return p1; }
		}
		
		public Vector2 P2 {
			get { return p2; }
		}

		public float Width {
			get { return p2.X-p1.X; }
		}

		public float Height {
			get { return p2.Y-p1.Y; }
		}

		public float Length {
			get { return FMath.Distance(P1,P2); }
			set {
				Vector2 dir=Vector2.Subtract(p2,p1);
				dir = Vector2.Multiply(dir, value/Length);
				//dir.Scale(value/Length);
				p2.X=p1.X+dir.X;
				p2.Y=p1.Y+dir.Y;
			}
		}
		
		public Vector2 Normal {
			get { 
				float l=Length;
				Vector2 norm=new Vector2(-Height/l,-Width/l);
				if (norm.Y<0) {
					norm.X*=-1;
					norm.Y*=-1;
				}
				return norm;
			}
		}

		public Vector2 Size {
			get { return P2-P1; }
		}

		public Vector2 Midpoint {
			get { return new Vector2((p1.X+p2.X)/2.0f,(p1.Y+p2.Y)/2.0f); }
		}

		public float Slope {
			get { return (p2.Y-p1.Y)/(p2.X-p1.X); }
		}

		public float YIntercept {
			get { return p1.Y-Slope*p1.X; }
		}

		public float Angle {
			get { return FMath.Atan2(p2.Y-p1.Y,p2.X-p1.X); }
		}

		public Rect BoundingBox {
			get { return new Rect(p1,p2); }
		}

		#endregion

		#region constructor
		/// <summary>
		/// Defines a segment by a position and a segment vector
		/// </summary>
		/// <param name="position">Position of one endpoint</param>
		/// <param name="size">Vector describing the segment to the other</param>
		public Segment(Vector2 position,Vector2 size) {
			p1=position;
			p2=Vector2.Add(position,size);
		}

		/// <summary>
		/// Defines a segment by its endpoints
		/// </summary>
		public Segment(Vector2 p1,Vector2 p2,object dummy) {
			this.p1=p1;
			this.p2=p2;
		}

		public Segment() {
			p1=new Vector2();
			p2=new Vector2();
		}
		#endregion

		#region transformations
		
		/// <summary>
		/// Translate the segment across the coordinate plane
		/// </summary>
		public void Translate(Vector2 shift) {
			//Move the points
			p1.X+=shift.X;
			p1.Y+=shift.Y;
			p2.X+=shift.X;
			p2.Y+=shift.Y;
		}

		/// <summary>
		/// Rotate the segment about an arbitrary point
		/// </summary>
		public void Rotate(Vector2 center,float radians) {
			FMath.RotatePoint(ref p1,center,radians);
			FMath.RotatePoint(ref p2,center,radians);
		}
		#endregion

		#region math

		/// <summary>
		/// Gets the segment's y-value at given x. Does not account for the segment's endpoints;
		/// treats it as an infinite line.
		/// </summary>
		public float Y(float x) {
			return Slope*x+YIntercept;
		}

		/// <summary>
		/// Gets the segment's x-value at a given y. Does not account for the segment's endpoints;
		/// treats it as an infinite line
		/// </summary>
		public float X(float y) {
			return (y-YIntercept)/Slope;
		}

		/// <summary>
		/// Returns whether a given coordinate x has a corresponding y on this segment
		/// </summary>
		public bool InDomain(float x) {
			return (x>=Math.Min(p1.X,p2.X) && x<=Math.Max(p1.X,p2.X));
		}

		/// <summary>
		/// Returns whether a given coordinate y has a corresponding x on this segment
		/// </summary>
		public bool InRange(float y) {
			return (y>=Math.Min(p1.Y,p2.Y) && y<=Math.Max(p1.Y,p2.Y));
		}

		public static bool Intersection(Segment a,Segment b,out Vector2 point) {
			//Assign to point
			point=new Vector2();

			//Avoid div by 0
			float ma=a.Slope,mb=b.Slope;
			if (ma==mb) return false;	//Parallel, no intersection; however, does not account for colinearity

			//Handle the infinite slope cases
			if (float.IsInfinity(ma)) return VerticalIntersection(b,a,out point);
			if (float.IsInfinity(mb)) return VerticalIntersection(a,b,out point);

			//Get intersection
			float x=(b.YIntercept-a.YIntercept)/(ma-mb);
			point=new Vector2(x,a.Y(x));

			//Return whether the point is on the segments
			return (a.InDomain(x) && b.InDomain(x));
		}

		/// <summary>
		/// Helper for Intersection. Handles the case where one of the segments is vertical.
		/// </summary>
		private static bool VerticalIntersection(Segment seg,Segment vertical,out Vector2 point) {
			point=new Vector2();
			float x=vertical.P1.X,y=seg.Y(vertical.P1.X);
			point.X=x;
			point.Y=y;
			return (seg.InDomain(x) && vertical.InRange(y));
		}

		public int Side(Vector2 point) {
			return point.Y>Y(point.X)?1:-1;
		}

		#endregion

		#region ICloneable members

		public object Clone() {
			return new Segment(p1,Size);
		}

		#endregion
	}
}