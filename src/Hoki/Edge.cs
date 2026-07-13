using System;
using FloatMath;
using Microsoft.Xna.Framework;

namespace Hoki {
	/// <summary>
	/// Represent a connection between two nodes in a Map during construction, as well
	/// as the four points that should be used to make a wall from it
	/// </summary>
	public class Edge : IComparable {
		private Node from,to;
		private float angle;
		private Segment seg;

		public Vector2[] Points;

		public Node From {
			get { return from; }
		}

		public Node To {
			get { return to; }
		}

		public float Angle {
			get { return angle; }
			set { angle=value; }
		}

		public Segment Seg {
			get { return seg; }
		}

		public Edge(Node from,Node to) {
			//Store node references
			this.from=from;
			this.to=to;

			//Find the angle between the nodes
			angle=FMath.Atan2(from.Y-to.Y,from.X-to.X);

			//Make four points to represent the four graphic corners of the wall. By default, in rectangular position
			Points=new Vector2[4];
			Node local=null;
			int dir=-1;
			for (int i=0;i<4;i++) {
				if (i%2==0) local=from; else local=to;
				if (i>1) dir=1;
				Points[i]=new Vector2(local.X+FMath.Cos(angle+FMath.PI/2)*Map.WallWidth/2*dir,local.Y+FMath.Sin(angle+FMath.PI/2)*Map.WallWidth/2*dir);
			}

			//Get the wall's mathematical segment
			seg=new Segment(new Vector2(from.X,from.Y),new Vector2(to.X,to.Y),null);
		}

		public float angleFrom(Node n) {
			if (n==To) {
				if (angle<0) return angle+FMath.PI;
				return angle-FMath.PI;
			}
			return angle;
		}

		public int CompareTo(object obj) {
			float diff=Angle-((Edge)obj).Angle;
			if (diff>0)			return 1;
			else if (diff<0)	return -1;
			else				return 0;
		}
	}
}
