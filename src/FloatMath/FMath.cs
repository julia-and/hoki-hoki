using Microsoft.Xna.Framework;
using System;


namespace FloatMath {
	/// <summary>
	/// Provides a subset of the functionality of the Math class for System.float variables so that repetitive casting in the client code is unnecesary. Also adds new methods not available in Math.
	/// </summary>
	public class FMath {
		public static float Abs(float x) {
			return Math.Abs(x);
		}

		public static float Pow(float x,float y) {
			return (float)Math.Pow((double)x,(double)y);
		}

		public static float Sqrt(float f) {
			return (float)Math.Sqrt((double)f);
		}

		public static float Sin(float theta) {
			return (float) Math.Sin((double)theta);
		}

		public static float Cos(float theta) {
			return (float) Math.Cos((double)theta);
		}

		public static float Tan(float theta) {
			return (float) Math.Tan((double)theta);
		}

		public static float Asin(float theta) {
			return (float) Math.Asin((double)theta);
		}

		public static float Acos(float theta) {
			return (float) Math.Acos((double)theta);
		}

		public static float Atan(float theta) {
			return (float) Math.Atan((double)theta);
		}

		public static float Atan2(float y,float x) {
			return (float) Math.Atan2((double)y,(double)x);
		}

		public static float PI {
			get { return (float)Math.PI; }
		}

		public static float Floor(float f) {
			return (float)Math.Floor(f);
		}

		public static float Ceil(float f) {
			return (float)Math.Ceiling(f);
		}

		public static int Sgn(float f) {
			if (f>0) return 1;
			else if (f<0) return -1;
			else return 0;
		}
		
		/// <summary>
		/// Limits the value of a float to a set range
		/// </summary>
		/// <param name="x">Input value</param>
		/// <param name="min">Minimum output</param>
		/// <param name="max">Maximum output</param>
		/// <returns></returns>
		public static float Clamp (float x,float min,float max) {
			if (x<min) return min;
			if (x>max) return max;
			return x;
		}
		
		/// <summary>
		/// If x is positive or negative infinity, returns float.MaxValue or float.MinValue, respectively. Otherwise returns x.
		public static float MakeFinite(float x) {
			if (float.IsPositiveInfinity(x)) return float.MaxValue;
			if (float.IsNegativeInfinity(x)) return float.MinValue;
			return x;
		}

		/// <summary>
		/// Returns the distance between two points
		/// </summary>
		public static float Distance(Vector2 a,Vector2 b) {
			return FMath.Sqrt(FMath.Pow(a.X-b.X,2)+FMath.Pow(a.Y-b.Y,2));
		}

		public static float Angle(Vector2 a,Vector2 b) {
			return FMath.Atan2((a.Y-b.Y),(a.X-b.X));
		}

		public static float Round(float val) {
			return Round(val,0);
		}

		public static float Round(float val,int places) {
			return (float)Math.Round(val,places);
		}

		/// <summary>
		/// Rotates a point about another point
		/// </summary>
		public static void RotatePoint(ref Vector2 point,Vector2 center,float radians) {
			float distance=FMath.Distance(point,center);
			float angle=FMath.Atan2(point.Y-center.Y,point.X-center.X)+radians;
			point.X=center.X+FMath.Cos(angle)*distance;
			point.Y=center.Y+FMath.Sin(angle)*distance;
		}
		
		public static float CrossProduct(Vector2 a,Vector2 b) {
			return a.X*b.Y-a.Y*b.X;
		}

		public static float VectorAngle(Vector2 a,Vector2 b) {
			return Acos(Vector2.Dot(a,b)/a.Length()/b.Length());
		}
	}
}
