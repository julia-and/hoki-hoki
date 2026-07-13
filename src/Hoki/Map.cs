using System;
using System.IO;
using System.Collections;
using SpriteUtilities;
using FloatMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// A game map. It is derived from SpriteObject and can draw itself, and it also handles
	/// physics and collision detection. Also provides static methods for loading maps
	/// </summary>
	public class Map : SpriteObject,Updateable {
		private const string
			defaultTheme="dark.txt";	//Theme to use by default

		//Colors for preview renderer
		private static readonly Microsoft.Xna.Framework.Color
			lineColor,
			fgColor,
			startColor,
			healColor,
			endColor;

		public const int
			MapScale=8,			//Map's unit size
			WallWidth=8,		//Width of a wall
			PadSize=196,		//Side of a square pad
			PreviewSize=128;	//Size of a map preview tex
		private const int
			previewMargin=128;	//Margin (in real space, not preview space) around the map

		private string
			musicFile; //File with bg music sound data
		
		private Song
			song;		//Background music

		private Vector2
			startPad,
			bgCenter;

		private static readonly short[] wallIndexPattern;

		//Lists
		private ArrayList
			walls,			//All walls (TODO: check if this is still necessary)
			pads,			//All pads
			mines,			//All live mines
			segments,		//ObjectSegments
			sprites,		//Decorative sprites
			update;			//Stuff to propogate updates to
		//Graphics
		private SpritePolygon foreground;		//foreground (area inside the walls)
		private TiledSpriteObject background;	//Background (outside the walls)
		private SpriteTexture
			foregroundTex,
			backgroundTex,
			wallTex,
			explosionTex,
			sparkTex,
			springTex,
			launcherTex,
			mineTex,
			startTex,
			healTex,
			endTex;
		//Layers
		private SpriteObject
			bgLayer,		//Background outside the map
			spriteLayer,	//Sprites
			fgLayer,		//Foreground inside the map
			padLayer,		//Pads
			mineLayer,		//Moving mines
			objectLayer,	//Springs, launchers, etc.
			particleLayer;	//Particle effects
		private AASpriteObject
			wallLayer;		//Walls
		private string
			ghost;			//Tutor ghost for this map
		private bool
			hasStartPad;
		private float
			invParallax;	//one minus parallax rate

		#region getset
		public Song Song {
			get { return song; }
		}

		/// <summary>
		/// Coordinates of the map's startpad
		/// </summary>
		public Vector2 StartPad {
			get { return startPad; }
		}

		public bool HasStartPad {
			get { return hasStartPad; }
		}

		public ArrayList Segments {
			get { return segments; }
		}

		public ArrayList Mines {
			get { return mines; }
		}

		public string Ghost {
			get { return ghost; }
		}
		#endregion

		#region construct/destruct
		static Map() {
            lineColor=ColorX.FromArgb(62,126,208);
			fgColor=ColorX.FromArgb(36,67,102);
			startColor=ColorX.FromArgb(108,185,244);
			healColor=ColorX.FromArgb(245,27,42);
			endColor=ColorX.FromArgb(255,156,0);

			wallIndexPattern=new short[] {0,1,2,1,2,3};
		}

		public Map(Device device) : base(device,null) {
			//Construct lists
			walls=new ArrayList();
			pads=new ArrayList();
			mines=new ArrayList();
			update=new ArrayList();
			sprites=new ArrayList();
			segments=new ArrayList();

			//Construct layers
			bgLayer=new SpriteObject(device,null);
			spriteLayer=new SpriteObject(device,null);
			fgLayer=new SpriteObject(device,null);
			padLayer=new SpriteObject(device,null);
			mineLayer=new SpriteObject(device,null);
			objectLayer=new SpriteObject(device,null);
			wallLayer=new AASpriteObject(device,null);
			particleLayer=new SpriteObject(device,null);

			//Add them
			Add(bgLayer);
			Add(spriteLayer);
			Add(fgLayer);
			Add(padLayer);
			Add(mineLayer);
			Add(objectLayer);
			Add(wallLayer);
			Add(particleLayer);
		}

		#endregion

		public bool Antialias {
			get { return wallLayer.AntiAlias; }
			set { wallLayer.AntiAlias=value; }
		}

		#region loader
		/// <summary>
		/// Load a map from a file
		/// </summary>
		public static Map FromFile(string path,Device device,Game game) {
			return FromString(File.OpenText(path).ReadToEnd(),device,game);
		}

		public static Map FromStream(Stream map,Device device,Game game) {
			StreamReader sr=new StreamReader(map);
			return FromString(sr.ReadToEnd(),device,game);
		}

		/// <summary>
		/// Helper method for FromFile and FromStream, parses the map code and returns the Map
		/// </summary>
		public static Map FromString(string map,Device device,Game game) {
			//TODO: CLEAN THIS SHIT UP
			Map outMap;						//Map to output
			Node[] nodes=null;				//Nodes for line and poly creation later
			Vector2[] vertices=null;		//Equivalent to nodes, but in Vector2 form for use by a SpritePolygon
			short[] indices=null;			//Index list for forground

			ArrayList
				edges=new ArrayList(),			//List of edges to be converted to walls
				launchers=new ArrayList(),		//List of mine launchers
				spriteTextures=new ArrayList(),	//List of textures for decorative sprites
				spriteRates=new ArrayList();	//List of animation rates, parallel to spriteTextures

			Node from,to;					//Line segment (wall) endpoints

			int 
				polyCount=0,				//Number of polygons
				currentNode=0,				//When defining nodes, the current node ID being handled
				currentIndex=0,				//When defining polygons, the next index's position in the array
				catcherIndex=0;				//Index of the catcher being handled (link to launcher)

			string[] data=null;				//Data about a single line broken into segments
			MapModes mode;					//Determines how map data should be handled
			Vector2 maxPos=new Vector2();	//Highest coordinates in each dimension (essentially the width and height of the map)

			//Construct the map
			outMap=new Map(device);

			//Split the map into lines to process one by one
			string[] lines=map.Split('\n','\r');

			//TEMPHACKHACKHACK: load some texes

			string path="Hoki.textures.game.";
			SizeTexture tex=game.loadSizeTexture(path+"sprites.png");

			outMap.springTex=game.loadTexture(tex,path+"spring.dat");
			outMap.launcherTex=game.loadTexture(tex,path+"launcher.dat");
			outMap.mineTex=game.loadTexture(tex,path+"mine.dat");
			outMap.startTex=game.loadTexture(tex,path+"startpad.dat");
			outMap.healTex=game.loadTexture(tex,path+"healpad.dat");
			outMap.endTex=game.loadTexture(tex,path+"finishpad.dat");
			outMap.explosionTex=game.loadTexture(game.loadSizeTexture(path+"particles.explosion.png"),path+"particles.explosion.dat");
			outMap.sparkTex=game.loadTexture(game.loadSizeTexture(path+"particles.spark.png"),path+"particles.spark.dat");

			//Read each line
			mode=MapModes.Meta;	//Need to get data about the map first
			foreach (string line in lines) {
				if (line.Length==0) continue;	//Empty line

				//See if the mode needs to be changed
				if (line[0]=='>') {
					switch (line) {
						case ">NODES":
							currentNode=0;
							mode=MapModes.Nodes;
							break;
						case ">LINES":
							mode=MapModes.Walls;
							break;
						case ">TRIANGLES":
							mode=MapModes.Polygons;
							break;
						case ">PADS":
							mode=MapModes.Pads;
							break;
						case ">SPRINGS":
							mode=MapModes.Springs;
							break;
						case ">LAUNCHERS":
							mode=MapModes.Launchers;
							break;
						case ">CATCHERS":
							mode=MapModes.Catchers;
							break;
						case ">SPRITES":
							mode=MapModes.Sprites;
							break;
					}
				} else {
					switch (mode) {
						case MapModes.Meta:		//Obtain information about the map
							data=line.Split(' ');
							switch (data[0]) {
								case "#NODECOUNT":
									int nodeCount=int.Parse(data[1]);	//This line defines the number of nodes there will be
									nodes=new Node[nodeCount];
									vertices=new Vector2[nodeCount];
									break;
								case "#POLYCOUNT":
									polyCount=int.Parse(data[1])*3;
									if (polyCount>0) indices=new short[polyCount];
									break;
								case "#THEME":
									//Load the theme from file
									string[] theme=new StreamReader(game.getStream("Hoki.data.themes."+data[1])).ReadToEnd().Split('\n');
									string themePath=null;
									SizeTexture spriteTex=null;
									foreach (string themeLineRaw in theme) {
										string themeLine=themeLineRaw.Split('\r')[0];
										string[] themeData=themeLine.Split(' ');
										if (themeLine[0]=='#') {
											switch (themeData[0]) {
												case "#DIR":
													//Load basic textures
													themePath="Hoki.textures.game.themes."+themeData[1]+".";
													outMap.wallTex=game.loadTexture(game.loadSizeTexture(themePath+"wall.png"),themePath+"wall.dat");
													outMap.foregroundTex=game.loadTexture(game.loadSizeTexture(themePath+"fg.png"),themePath+"fg.dat");
													outMap.backgroundTex=game.loadTexture(game.loadSizeTexture(themePath+"bg.png"),themePath+"bg.dat");
													spriteTex=game.loadSizeTexture(themePath+"sprites.png");
													break;
												case "#MUS":
													//Get the music file
													outMap.musicFile=themeData[1];
													break;
												case "#PLX":
													outMap.invParallax=1-float.Parse(themeData[1]);
													break;
											}
										} else {
											spriteTextures.Add(game.loadTexture(spriteTex,themePath+themeData[0]));
											spriteRates.Add(float.Parse(themeData[1]));
										}
									}

									//Construct the song
									if (outMap.musicFile!=null) {
										outMap.song=new SimpleSong(Game.MusicDir+outMap.musicFile);
									}

									break;
								case "#GHOST":
									outMap.ghost=data[1];
									break;
								//TODO: Add processing for other kinds of metadata?
							}

							break;
						case MapModes.Nodes:	//Record node positions
							data=line.Split(',');
							int x=int.Parse(data[0])*MapScale,y=int.Parse(data[1])*MapScale;
							vertices[currentNode]=new Vector2(x,y);
							nodes[currentNode++]=new Node(x,y);

							//Check if this node has a higher coordinate than we've seen yet
							maxPos.X=Math.Max(maxPos.X,x);
							maxPos.Y=Math.Max(maxPos.Y,y);

							break;
						case MapModes.Walls:	//Create walls and determine how many walls are at each node
							//Parse the node IDs and get references to them
							data=line.Split(',');
							from=nodes[int.Parse(data[0])];
							to=nodes[int.Parse(data[1])];

							//Make an edge to temporarily represent the wall in terms of nodes
							Edge e=new Edge(from,to);

							//Tell the two nodes about the connections
							from.AddEdge(e);
							to.AddEdge(e);

							//Add the edge to the list for wall construction later
							edges.Add(e);

							break;
						case MapModes.Polygons:
							data=line.Split(',');
							for (int i=0;i<3;i++) indices[currentIndex++]=short.Parse(data[i]);
							break;
						case MapModes.Pads:
							data=line.Split(',');
							int padX=int.Parse(data[0])*MapScale-MapScale/2,padY=int.Parse(data[1])*MapScale-MapScale/2;
							PadType type=(PadType)int.Parse(data[2]);
							if (type==PadType.Start) {
								outMap.startPad=new Vector2(padX,padY);	//Start pad
								outMap.hasStartPad=true;
							}
							
							//Add the pad visual (TODO: in the future, this texture should be loaded from a theme)
							SpriteTexture useTex=null;
							switch (type) {
								case PadType.Start:
									useTex=outMap.startTex;
									break;
								case PadType.Heal:
									useTex=outMap.healTex;
									break;
								case PadType.End:
									useTex=outMap.endTex;
									break;
							}
							Pad p=new Pad(device,useTex,type);
							p.X=padX;
							p.Y=padY;
							outMap.padLayer.Add(p);
							outMap.pads.Add(p);

							break;
						case MapModes.Springs:
							data=line.Split(',');

							//TODO: tex from theme
							Spring s=new Spring(device,outMap.springTex,int.Parse(data[0])*MapScale,int.Parse(data[1])*MapScale,int.Parse(data[2]));
							s.DrawFilter=TextureFilter.Point;
							outMap.objectLayer.Add(s);
							outMap.update.Add(s);
							outMap.segments.Add(new ObjectSegment(s.Segment,s));

							break;
						case MapModes.Launchers:
							data=line.Split(',');
							Launcher l=new Launcher(device,outMap.launcherTex,outMap.mineTex,int.Parse(data[3]),int.Parse(data[4]),outMap);
							launchers.Add(l);
							l.Turns=int.Parse(data[2]);									//Rotate
							l.X=int.Parse(data[0])*MapScale-((l.Turns+1)%2)*MapScale/2;	//Position
							l.Y=int.Parse(data[1])*MapScale-((l.Turns+1)%2)*MapScale/2;

							//Setup the launcher's segment
							l.UpdateSeg();

							outMap.objectLayer.Add(l);
							outMap.update.Add(l);
							outMap.segments.Add(new ObjectSegment(l.Segment,l));
							break;
						case MapModes.Catchers:
							data=line.Split(',');
							Catcher c=new Catcher(device,outMap.launcherTex);
							c.Turns=int.Parse(data[2]);									//Rotate
							c.X=int.Parse(data[0])*MapScale-((c.Turns+1)%2)*MapScale/2;	//Position
							c.Y=int.Parse(data[1])*MapScale-((c.Turns+1)%2)*MapScale/2;
							((Launcher)launchers[catcherIndex++]).Catcher=c;			//Link to launcher

							//Setup the catcher's segment
							c.UpdateSeg();
							
							//Add to layers
							outMap.objectLayer.Add(c);								//Draw it
							outMap.segments.Add(new ObjectSegment(c.Segment,c));	//Collision detect it
                            break;
						case MapModes.Sprites:
							data=line.Split(',');

							int index=int.Parse(data[2]);
							MapSprite m=new MapSprite(device,(SpriteTexture)spriteTextures[index],outMap,new Vector2(int.Parse(data[0])*4-4f,int.Parse(data[1])*4-4f),float.Parse(data[3]),(float)spriteRates[index]);
							outMap.spriteLayer.Add(m);
							outMap.sprites.Add(m);
							outMap.update.Add(m);

							break;
					}
				}
			}

			//Weld the walls together
			foreach (Node n in nodes) {
				int numEdges=n.NumEdges(); //Get the number of edges incident to this node

				//Sharpen pendant nodes and continue
				if (numEdges==1) {
					//Get the node's only incident edge
					IEnumerator edge=n.GetEdges();
					edge.MoveNext();
					Edge e=(Edge)edge.Current;

					Node f;	//Other node on the edge
					int p;	//Index of the point to modify
					if (e.From==n) {
						f=e.To;
						p=0;
					} else {
						f=e.From;
						p=1;
					}

					float ang=FMath.Angle(new Vector2(f.X,f.Y),new Vector2(n.X,n.Y));
					e.Points[p].X-=FMath.Cos(ang)*Map.WallWidth;
					e.Points[p].Y-=FMath.Sin(ang)*Map.WallWidth;

					continue;
				}

				//If there are 3 or more edges, create a SpriteTriFan to fill the gap between them
				SpriteTriFan center;
				Vector2[] centerVerts=null;
				Vector2[] centerCoords=null;
				if (numEdges>2) {
					//Make arrays for all the vertices and the tex coords at them
					centerVerts=new Vector2[numEdges+2];
					centerCoords=new Vector2[numEdges+2];

					//Create the vertex for the center of the fan
					centerVerts[0]=new Vector2(n.X,n.Y);
					centerCoords[0]=new Vector2(0.5f,0);
				}

				//Get the edges in a list ordered by angle
				n.OrderEdges();
				IEnumerator edgeList=n.GetEdges();

				Edge first=null,last=null,current=null;
				int centerIndex=1;		//Index of the vertex in center fan
				int centerSide=0;		//Side of alternating texture coordinate in center fan
				Vector2 intersection;	//Point where walls intersect
				while (edgeList.MoveNext()) {
					//Get the next edge to operate on
					current=(Edge)edgeList.Current;

					if (last==null)	first=current;					//First iteration, come back to this edge at the end
					else {
						intersection=adjustEdges(current,last,n);	//Adjust a pair of edges to each other
						if (numEdges>2) {
							centerVerts[centerIndex]=new Vector2(intersection.X,intersection.Y);
							centerCoords[centerIndex++]=new Vector2((centerSide++)%2,1);
						}
					}

					//Store a reference to the current edge
					last=current;
				}

				if (numEdges>1) {
					//Adjust the last edge to the first
					intersection=adjustEdges(first,last,n);
					if (numEdges>2) {
						//Add the result of the last adjustment to the center
						centerVerts[centerIndex]=new Vector2(intersection.X,intersection.Y);
						centerCoords[centerIndex++]=new Vector2((centerSide++)%2,1);
						
						//Complete the center
						centerVerts[centerIndex]=centerVerts[1]; //Loop around
						centerCoords[centerIndex++]=new Vector2((centerSide++)%2,1);
					}
				}

				//Construct the center
				if (numEdges>2) {
					center=new SpriteTriFan(device,outMap.wallTex,centerVerts,centerCoords);
					outMap.wallLayer.Add(center);
				}
			}

			//Now that the edges are modified, make walls from them
			PositionColoredTextured[] wallVerts=new PositionColoredTextured[edges.Count*4];	//This is inefficient because some edges share points
			short[] wallIndices=new short[edges.Count*6];
			
			for (int i=0;i<edges.Count;i++) {
				Edge e=(Edge)edges[i];


				//Copy the edge's vertices
				int u=0,v=0; //Texture coordinates
				for (int n=0;n<4;n++) {
					if (n==2) u=1;
					if (n%2==1) v=1; else v=0;

					wallVerts[i*4+n]=new PositionColoredTextured(e.Points[n].X,e.Points[n].Y,1.0f,Microsoft.Xna.Framework.Color.White,u,v);
				}

				//Add the indices
				for (int n=0;n<wallIndexPattern.Length;n++)
					wallIndices[i*wallIndexPattern.Length+n]=(short)(i*4+wallIndexPattern[n]);
			}

			
			if (edges.Count>0) {
				//Build a SpritePolygon to represent the walls
				SpritePolygon wallPoly=new SpritePolygon(device,outMap.wallTex);
				wallPoly.Build(wallVerts,wallIndices);
				outMap.wallLayer.Add(wallPoly);

				//Add all segments to a list for collision detection
				foreach (Edge e in edges) {
					outMap.segments.Add(new ObjectSegment(new Segment(e.Points[0],e.Points[1],null),outMap));
					outMap.segments.Add(new ObjectSegment(new Segment(e.Points[2],e.Points[3],null),outMap));
				}
			}

			//Add the background graphic
			outMap.background=new TiledSpriteObject(device,outMap.backgroundTex,maxPos.X+game.ClientSize.X,maxPos.Y+game.ClientSize.Y);
			outMap.bgCenter=new Vector2(-game.ClientSize.X/2,-game.ClientSize.Y/2);
			outMap.bgLayer.Add(outMap.background);

			//Add the foreground graphic
			if (polyCount>0) {
				outMap.foreground=new SpritePolygon(device,outMap.foregroundTex,vertices,indices);
				outMap.fgLayer.Add(outMap.foreground);
			}

			return outMap;
		}

		/// <summary>
		/// Welds two walls together at a node, and returns their point of intersection
		/// </summary>
		/// <param name="current">Wall of lesser angle</param>
		/// <param name="last">Wall of greater angle</param>
		/// <param name="n">Connecting node</param>
		private static Vector2 adjustEdges(Edge current,Edge last,Node n) {
			int cIndex,lIndex;
			if (current.To==n) cIndex=3; else cIndex=0;
			if (last.To==n) lIndex=1; else lIndex=2;

			float cAngle=current.angleFrom(n);
			float lAngle=last.angleFrom(n);
			float angle=(lAngle+cAngle)/2;
			float length=WallWidth/2/FMath.Cos(cAngle-FMath.PI/2-angle);

			//Find new points
			Vector2 intersection=new Vector2(n.X+FMath.Cos(angle)*length,n.Y+FMath.Sin(angle)*length);

			//Modify the edges
			current.Points[cIndex]=intersection;
			last.Points[lIndex]=intersection;

			//Return the intersection
			return intersection;
		}
		#endregion

		#region preview
		/// <summary>
		/// Generates a preview image of a map
		/// </summary>
		/// <param name="map">The map string</param>
		/// <param name="blankTex">Plain white SpriteTexture</param>
		/// <param name="device">Direct3D device</param>
		/// <param name="game">Game window</param>
		public static Texture2D RenderPreview(string map,SpriteTexture blankTex,Device device,Game game) {
			Node[] nodes=null;				//Nodes for line and poly creation later
			Vector2[] vertices=null;		//Equivalent to nodes, but in Vector2 form for use by a SpritePolygon
			short[]
				indices=null,				//Index list for foreground
				wallIndices=null;			//Index list for walls
			ArrayList
				edges=new ArrayList(),		//List of edges to be converted to walls
				pads=new ArrayList();		//List of pad graphics
			Node from,to;					//Line segment (wall) endpoints
			int 
				polyCount=0,				//Number of polygons
				currentNode=0,				//When defining nodes, the current node ID being handled
				currentIndex=0,				//When defining polygons, the next index's position in the array
				currentWall=0;				//Number of walls read so far
			string[] data=null;				//Data about a single line broken into segments
			MapModes mode;					//Determines how map data should be handled
			Vector2 maxPos=new Vector2();	//Highest coordinates in each dimension (essentially the width and height of the map)

			//Construct layers to hold all of the graphics
			SpriteObject
				padLayer=new SpriteObject(device,null),
				lineLayer=new SpriteObject(device,null),
				renderLayer=new SpriteObject(device,null);

			//Split the map into lines to process one by one
			string[] lines=map.Split('\n','\r');

			//Read each line
			mode=MapModes.Meta;	//Need to get data about the map first
			foreach (string line in lines) {
				if (line.Length==0) continue;	//Empty line

				//See if the mode needs to be changed
				if (line[0]=='>') {
					switch (line) {
						case ">NODES":
							currentNode=0;
							mode=MapModes.Nodes;
							break;
						case ">LINES":
							mode=MapModes.Walls;
							break;
						case ">TRIANGLES":
							mode=MapModes.Polygons;
							break;
						case ">PADS":
							mode=MapModes.Pads;
							break;
						case ">SPRINGS":
							mode=MapModes.Springs;
							break;
						case ">LAUNCHERS":
							mode=MapModes.Launchers;
							break;
						case ">CATCHERS":
							mode=MapModes.Catchers;
							break;
					}
				} else {
					switch (mode) {
						case MapModes.Meta:		//Obtain information about the map
							data=line.Split(' ');
							switch (data[0]) {
								case "#NODECOUNT":
									int nodeCount=int.Parse(data[1]);	//This line defines the number of nodes there will be
									nodes=new Node[nodeCount];
									vertices=new Vector2[nodeCount];
									break;
								case "#WALLCOUNT":
									wallIndices=new short[int.Parse(data[1])*2];
									break;
								case "#POLYCOUNT":
									polyCount=int.Parse(data[1])*3;
									if (polyCount>0) indices=new short[polyCount];
									break;
							}
							break;
						case MapModes.Nodes:	//Record node positions
							data=line.Split(',');
							int x=int.Parse(data[0])*MapScale,y=int.Parse(data[1])*MapScale;
							vertices[currentNode]=new Vector2(x,y);
							nodes[currentNode++]=new Node(x,y);

							//Check if this node has a higher coordinate than we've seen yet
							maxPos.X=Math.Max(maxPos.X,x);
							maxPos.Y=Math.Max(maxPos.Y,y);

							break;
						case MapModes.Walls:	//Create walls and determine how many walls are at each node
							//Parse the node IDs and get references to them
							data=line.Split(',');
							short fromIndex=short.Parse(data[0]),toIndex=short.Parse(data[1]);
							from=nodes[fromIndex];
							to=nodes[toIndex];

							//Add the lines' endpoints to the index list
							wallIndices[currentWall*2]=fromIndex;
							wallIndices[currentWall*2+1]=toIndex;
							currentWall++;

							break;
						case MapModes.Polygons:
							data=line.Split(',');
							for (int i=0;i<3;i++) indices[currentIndex++]=short.Parse(data[i]);
							break;
						case MapModes.Pads:
							data=line.Split(',');
							int padX=int.Parse(data[0])*MapScale-MapScale/2,padY=int.Parse(data[1])*MapScale-MapScale/2;
							PadType type=(PadType)int.Parse(data[2]);
							
							//Determine the pad's color
							Microsoft.Xna.Framework.Color color=Microsoft.Xna.Framework.Color.White;
							switch (type) {
								case PadType.Start:
									color=startColor;
									break;
								case PadType.Heal:
									color=healColor;
									break;
								case PadType.End:
									color=endColor;
									break;
							}

							SpriteLine pad=new SpriteLine(device);
							pad.X=padX;
							pad.Y=padY+PadSize/2;
							pad.Length=PadSize;
							pad.Thickness=PadSize;
							pad.Tint=color;

							pads.Add(pad);
							padLayer.Add(pad);

							break;
					}
				}
			}

			//Form a margin
			float maxCoord=Math.Max(maxPos.X,maxPos.Y);
			Vector2 marginSpace=new Vector2(maxCoord-maxPos.X+128,maxCoord-maxPos.Y+128);
			maxPos.X+=previewMargin;
			maxPos.Y+=previewMargin;

			TransformedObject foreground;
			if (polyCount>0) {
				foreground=new SpritePolygon(device,blankTex,vertices,indices);
				foreground.Tint=fgColor;
			} else foreground=new SpriteObject(device,null);	//This isn't really efficient, but this way there'll be no NREs

			//Determine the ratios and scale the layers
			float maxDimension=Math.Max(maxPos.X,maxPos.Y);
			float ratio=PreviewSize/maxDimension;
			lineLayer.XScale=lineLayer.YScale=ratio;
			padLayer.XScale=padLayer.YScale=ratio;
			foreground.XScale=ratio*game.ClientSize.X/PreviewSize;	//I don't understand why it has to be scaled
			foreground.YScale=ratio*game.ClientSize.Y/PreviewSize;	//to the game's window size, it just does
			foreach (SpriteLine pad in pads) pad.Thickness*=ratio;		//This makes equally little sense to me

			//Add everything to the render layer and position it
			renderLayer.Add(foreground);
			renderLayer.Add(padLayer);
			renderLayer.Add(lineLayer);
			lineLayer.X=marginSpace.X*ratio/2;
			lineLayer.Y=marginSpace.Y*ratio/2;
			padLayer.X=lineLayer.X;
			padLayer.Y=lineLayer.Y;
			foreground.X=lineLayer.X*game.ClientSize.X/PreviewSize;
			foreground.Y=lineLayer.Y*game.ClientSize.Y/PreviewSize;

			//Prepare a render target for rendering (replaces D3DX RenderToSurface)
			RenderTarget2D renderTex=new RenderTarget2D(device,PreviewSize,PreviewSize,false,SurfaceFormat.Color,DepthFormat.None,0,RenderTargetUsage.PreserveContents);

			//Render the scene to tex. The game's 640x480 projection compresses into the square
			//target exactly like the old PreviewSize viewport did (hence the odd scale factors above).
			device.SetRenderTarget(renderTex);
			device.Clear(new Microsoft.Xna.Framework.Color(20,20,20));

			//Perform the drawing operations
			renderLayer.Draw();

			//HACK, SORT OF: draw the walls in the line layer's coordinate space
			PositionColored[] wallVerts=new PositionColored[vertices.Length];
			for (int i=0;i<vertices.Length;i++)
				wallVerts[i]=new PositionColored(vertices[i].X,vertices[i].Y,1,lineColor);

			Renderer.DrawLineList(lineLayer.AbsoluteTransform(),wallVerts,wallIndices);

			//Back to the screen
			device.SetRenderTarget(null);

			//Return the tex
			return renderTex;
		}
		#endregion

		#region scrolling
		/// <summary>
		/// Scrolls by v (if v.X is positive, it will move left) TODO: see if this is still necessary)
		/// </summary>
		public void Scroll(Vector2 v) {
			X-=v.X;
			Y-=v.Y;
			
			updateParallax();
		}

		/// <summary>
		/// Scrolls the provided position to 0,0 in the parent space
		/// </summary>
		public void ScrollTo(Vector2 position) {
			X=-position.X;
			Y=-position.Y;
			
			updateParallax();
		}

		private void updateParallax() {
			//Adjust the background and sprites for the parallax
			background.Position=bgCenter-invParallax*Position;
			foreach (MapSprite m in sprites) m.Scroll();
		}
		#endregion

		#region collision detection
		/// <summary>
		/// Determines whether the heli is on a pad, and returns it (or null)
		/// </summary>
		/// <param name="h">The heli to perform the test on</param>
		public Pad OnPad(Vector2 point) {
			foreach (Pad p in pads) if (p.Collides(point)) return p;
			return null; //None were found
		}

		/// <summary>
		/// Determines whether a segment collides with any of the walls on the map. If it collides with
		/// more than one, the closest one to the provided segment's midpoint will be returned.
		/// </summary>
		/// <param name="s">The segment to test</param>
		/// <param name="intersection">The point of intersection, if any</param>
		/// <param name="seg">The wall intersected, if any</param>
		/// <param name="spring">Whether the intersection was with a spring</param>
		public bool Collision(Segment s,out Vector2 intersection, out ObjectSegment mapObject) {
			bool intersected=false;				//Whether there has been a collision
			float lowDistance=float.MaxValue;	//Distance from midpoint of the closest collision so far

			//Make a copy of the segment
			Segment local=ToLocal(s);

			//Assign to out vars
			intersection=Vector2.Zero;
			mapObject=null;

			//Iterate through the walls and test for collision
			Vector2 v;
			Vector2 midpoint=local.Midpoint;
			foreach (ObjectSegment obj in segments) {
				if (Segment.Intersection(local,obj.Segment,out v)) { //TODO: Test both of the wall's major sides
					float dist=FMath.Distance(midpoint,v);
					if (dist<lowDistance) {
						//Store new low distance, and that an intersection has been found
						lowDistance=dist;
						intersected=true;

						//Store the location of the intersection and the wall that hit it
						intersection=v;
						mapObject=obj;
					}
				}
			}

			if (intersected) return true;	//There was a collision
			else {							//No collision
				return false;
			}
		}

		/// <summary>
		/// Transforms a Segment to local coordinate space
		/// </summary>
		public Segment ToLocal(Segment s) {
			Segment local=(Segment)s.Clone();
			local.Translate(new Vector2(-X,-Y));
			return local;
		}

		/// <summary>
		/// Transforms a Segment to local coordinate space
		/// </summary>
		public SegPair ToLocal(SegPair s) {
			SegPair local=(SegPair)s.Clone();
			local.Translate(new Vector2(-X,-Y));
			return local;
		}
		#endregion

		#region mine management
		public void AddMine(Mine m) {
			mines.Add(m);
			mineLayer.Add(m);
			update.Add(m);
			m.Die+=new EventHandler(onMineDie);
			m.Explode+=new EventHandler(onMineExplode);
		}

		private void onMineDie(object sender, EventArgs e) {
			Mine m=(Mine)sender;

			mines.Remove(m);
			mineLayer.Remove(m);
			update.Remove(m);

			m.Explode-=new EventHandler(onMineExplode);
			m.Die-=new EventHandler(onMineDie);
		}

		private void onMineExplode(object sender, EventArgs e) {
			Mine mine=(Mine)sender;

			Explode(mine.Position);

			onMineDie(mine,new EventArgs());
		}

		public void Explode(Vector2 position) {
			Explode(position,false);
		}

		public void Explode(Vector2 position,bool spark) {
			Explosion ex=new Explosion(device,spark?sparkTex:explosionTex,10,spark);
			ex.Position=position;
			ex.Die+=new EventHandler(onExplosionDie);

			particleLayer.Add(ex);
			update.Add(ex);
		}

		#endregion
		
		#region particle effects

		private void onExplosionDie(object sender, EventArgs e) {
			Explosion ex=(Explosion)sender;
			ex.Die-=new EventHandler(onExplosionDie);
			particleLayer.Remove(ex);
			update.Remove(ex);
		}
		#endregion

		#region inner types
		/// <summary>
		/// Different states the map parsing method can be in. These determine how it interprets the information in the file.
		/// </summary>
		private enum MapModes {
			Meta,
			Nodes,
			Walls,
			Polygons,
			Pads,
			Springs,
			Launchers,
			Catchers,
			Sprites
		}

		#endregion

		#region Updateable Members
		public void Update(float elapsedTime) {
			//Update children
			for (int i=0;i<update.Count;i++) ((Updateable)update[i]).Update(elapsedTime);
		}
		#endregion
	}
}
