using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using NVec2=System.Numerics.Vector2;

namespace HokiEdit {
	public enum Tool { Select, Node, Wall, Tri, Pad, Spring, Launcher, Pencil, Decal }

	public enum Kind { Node, Wall, Tri, Pad, Spring, Launcher, Decal }

	public partial class EditorGame {
		private Tool tool=Tool.Select;
		private readonly HashSet<(Kind K,int I)> selection=new();

		//Pending tool state
		private int wallFromNode=-1;			//Wall tool: first node picked
		private readonly List<int> triPicks=new();	//Tri tool: nodes picked so far
		private int pendingPadType=1;			//Pad tool: 0=Start 1=End 2=Heal
		private int pendingSpringTurns=0;
		private int pendingLauncherTurns=0;
		private int launcherSender=-1;			//Launcher tool: index of placed sender awaiting catcher
		private int pendingDecalIndex=0;
		private float pendingDecalDepth=1f;
		private int pencilLastNode=-1;

		//Drag state
		private bool draggingSel, boxSelecting;
		private NVec2 boxStartScreen;
		private NVec2 dragRemainder;	//Sub-unit drag distance carried between frames

		private const float PickDist=8f;	//Screen px hit radius

		#region tool switching
		private void switchTool(Tool t) {
			tool=t;
			wallFromNode=-1;
			triPicks.Clear();
			launcherSender=-1;
			pencilLastNode=-1;
			draggingSel=boxSelecting=false;
		}
		#endregion

		#region hit testing (world px coordinates)
		private NVec2 nodeWorld(int i)=>new NVec2(doc.Nodes[i].X*Unit,doc.Nodes[i].Y*Unit);

		private int hitNode(NVec2 w) {
			float best=PickDist/zoom; int bi=-1;
			for (int i=0;i<doc.Nodes.Count;i++) {
				float d=(nodeWorld(i)-w).Length();
				if (d<best) { best=d; bi=i; }
			}
			return bi;
		}

		private int hitWallSeg(NVec2 w) {
			float best=PickDist/zoom; int bi=-1;
			for (int i=0;i<doc.Walls.Count;i++) {
				float d=distToSegment(w,nodeWorld(doc.Walls[i].A),nodeWorld(doc.Walls[i].B));
				if (d<best) { best=d; bi=i; }
			}
			return bi;
		}

		private int hitTriangle(NVec2 w) {
			for (int i=doc.Tris.Count-1;i>=0;i--)
				if (pointInTri(w,nodeWorld(doc.Tris[i].A),nodeWorld(doc.Tris[i].B),nodeWorld(doc.Tris[i].C)))
					return i;
			return -1;
		}

		private int hitPadRect(NVec2 w) {
			for (int i=doc.Pads.Count-1;i>=0;i--) {
				var p=doc.Pads[i];
				if (w.X>=p.X*Unit-4 && w.X<=p.X*Unit-4+196 && w.Y>=p.Y*Unit-4 && w.Y<=p.Y*Unit-4+196)
					return i;
			}
			return -1;
		}

		private int hitSpringAt(NVec2 w) {
			float best=12f/zoom+8; int bi=-1;
			for (int i=0;i<doc.Springs.Count;i++) {
				float d=(new NVec2(doc.Springs[i].X*Unit,doc.Springs[i].Y*Unit)-w).Length();
				if (d<best) { best=d; bi=i; }
			}
			return bi;
		}

		private (int idx,bool sender) hitLauncherAt(NVec2 w) {
			float best=12f/zoom+8;
			(int,bool) res=(-1,false);
			for (int i=0;i<doc.Launchers.Count;i++) {
				var l=doc.Launchers[i];
				float ds=(new NVec2(l.SX*Unit,l.SY*Unit)-w).Length();
				float dc=(new NVec2(l.CX*Unit,l.CY*Unit)-w).Length();
				if (ds<best) { best=ds; res=(i,true); }
				if (dc<best) { best=dc; res=(i,false); }
			}
			return res;
		}

		private int hitDecalAt(NVec2 w) {
			float best=10f/zoom+6; int bi=-1;
			for (int i=0;i<doc.Decals.Count;i++) {
				float d=(new NVec2(doc.Decals[i].X*4,doc.Decals[i].Y*4)-w).Length();
				if (d<best) { best=d; bi=i; }
			}
			return bi;
		}

		/// <summary>Topmost hit in the original editor's priority order.</summary>
		private (Kind K,int I)? hitAny(NVec2 w) {
			int i;
			if ((i=hitNode(w))>=0) return (Kind.Node,i);
			if ((i=hitWallSeg(w))>=0) return (Kind.Wall,i);
			if ((i=hitSpringAt(w))>=0) return (Kind.Spring,i);
			var l=hitLauncherAt(w); if (l.idx>=0) return (Kind.Launcher,l.idx);
			if ((i=hitDecalAt(w))>=0) return (Kind.Decal,i);
			if ((i=hitPadRect(w))>=0) return (Kind.Pad,i);
			if ((i=hitTriangle(w))>=0) return (Kind.Tri,i);
			return null;
		}

		private static float distToSegment(NVec2 p,NVec2 a,NVec2 b) {
			NVec2 ab=b-a;
			float len2=ab.LengthSquared();
			if (len2<1e-6f) return (p-a).Length();
			float t=Math.Clamp(NVec2.Dot(p-a,ab)/len2,0,1);
			return (p-(a+ab*t)).Length();
		}

		private static float cross(NVec2 o,NVec2 a,NVec2 b)=>(a.X-o.X)*(b.Y-o.Y)-(a.Y-o.Y)*(b.X-o.X);

		private static bool pointInTri(NVec2 p,NVec2 a,NVec2 b,NVec2 c) {
			float d1=cross(p,a,b), d2=cross(p,b,c), d3=cross(p,c,a);
			bool neg=(d1<0)||(d2<0)||(d3<0), pos=(d1>0)||(d2>0)||(d3>0);
			return !(neg&&pos);
		}
		#endregion

		#region mutations
		private void snapshot() { undoStack.Push(doc); dirty=true; }

		private int addNode(int ux,int uy) {
			//Reuse an existing node on the same grid point (original silently returned null here)
			for (int i=0;i<doc.Nodes.Count;i++)
				if (doc.Nodes[i].X==ux && doc.Nodes[i].Y==uy) return i;
			doc.Nodes.Add(new Node{X=ux,Y=uy});
			return doc.Nodes.Count-1;
		}

		private void addWall(int a,int b) {
			if (a==b) return;
			if (doc.Walls.Any(w=>(w.A==a&&w.B==b)||(w.A==b&&w.B==a))) return;
			doc.Walls.Add(new Wall{A=a,B=b});
		}

		private void deleteNode(int idx) {
			//Cascade: walls and tris touching the node go with it; indices above shift down
			doc.Walls.RemoveAll(w=>w.A==idx||w.B==idx);
			doc.Tris.RemoveAll(t=>t.A==idx||t.B==idx||t.C==idx);
			doc.Nodes.RemoveAt(idx);
			foreach (var w in doc.Walls) { if (w.A>idx) w.A--; if (w.B>idx) w.B--; }
			foreach (var t in doc.Tris) { if (t.A>idx) t.A--; if (t.B>idx) t.B--; if (t.C>idx) t.C--; }
		}

		private void deleteElement((Kind K,int I) e) {
			switch (e.K) {
				case Kind.Node: deleteNode(e.I); break;
				case Kind.Wall: doc.Walls.RemoveAt(e.I); break;
				case Kind.Tri: doc.Tris.RemoveAt(e.I); break;
				case Kind.Pad: doc.Pads.RemoveAt(e.I); break;
				case Kind.Spring: doc.Springs.RemoveAt(e.I); break;
				case Kind.Launcher: doc.Launchers.RemoveAt(e.I); break;	//Sender+catcher are one pair
				case Kind.Decal: doc.Decals.RemoveAt(e.I); break;
			}
		}

		private void deleteSelection() {
			if (selection.Count==0) return;
			snapshot();
			//Delete per kind, highest index first, nodes last (node removal shifts wall/tri indices)
			foreach (var kind in new[]{Kind.Wall,Kind.Tri,Kind.Pad,Kind.Spring,Kind.Launcher,Kind.Decal,Kind.Node})
				foreach (var e in selection.Where(s=>s.K==kind).OrderByDescending(s=>s.I))
					deleteElement(e);
			selection.Clear();
			resetPending();
		}

		private void resetPending() {
			wallFromNode=-1;
			triPicks.Clear();
			launcherSender=-1;
			pencilLastNode=-1;
		}

		private void moveSelection(int dx,int dy) {
			if (selection.Count==0 || (dx==0&&dy==0)) return;
			//Nodes to move: selected nodes plus endpoints of selected walls/tris
			var nodeSet=new HashSet<int>();
			foreach (var e in selection) {
				switch (e.K) {
					case Kind.Node: nodeSet.Add(e.I); break;
					case Kind.Wall: nodeSet.Add(doc.Walls[e.I].A); nodeSet.Add(doc.Walls[e.I].B); break;
					case Kind.Tri: nodeSet.Add(doc.Tris[e.I].A); nodeSet.Add(doc.Tris[e.I].B); nodeSet.Add(doc.Tris[e.I].C); break;
					case Kind.Pad: doc.Pads[e.I].X+=dx; doc.Pads[e.I].Y+=dy; break;
					case Kind.Spring: doc.Springs[e.I].X+=dx; doc.Springs[e.I].Y+=dy; break;
					case Kind.Launcher:
						doc.Launchers[e.I].SX+=dx; doc.Launchers[e.I].SY+=dy;
						doc.Launchers[e.I].CX+=dx; doc.Launchers[e.I].CY+=dy;
						break;
					case Kind.Decal: doc.Decals[e.I].X+=dx*2; doc.Decals[e.I].Y+=dy*2; break;	//4px units: 2 per map unit
				}
			}
			foreach (int n in nodeSet) { doc.Nodes[n].X+=dx; doc.Nodes[n].Y+=dy; }
		}

		private void flipSelection(bool horizontal) {
			var nodeSet=new HashSet<int>();
			foreach (var e in selection)
				switch (e.K) {
					case Kind.Node: nodeSet.Add(e.I); break;
					case Kind.Wall: nodeSet.Add(doc.Walls[e.I].A); nodeSet.Add(doc.Walls[e.I].B); break;
					case Kind.Tri: nodeSet.Add(doc.Tris[e.I].A); nodeSet.Add(doc.Tris[e.I].B); nodeSet.Add(doc.Tris[e.I].C); break;
				}
			if (nodeSet.Count<2) return;
			snapshot();
			if (horizontal) {
				int min=nodeSet.Min(n=>doc.Nodes[n].X), max=nodeSet.Max(n=>doc.Nodes[n].X);
				foreach (int n in nodeSet) doc.Nodes[n].X=min+max-doc.Nodes[n].X;
			} else {
				int min=nodeSet.Min(n=>doc.Nodes[n].Y), max=nodeSet.Max(n=>doc.Nodes[n].Y);
				foreach (int n in nodeSet) doc.Nodes[n].Y=min+max-doc.Nodes[n].Y;
			}
		}
		#endregion

		#region copy/paste
		private string clipboard;

		/// <summary>
		/// Serializes only the selection, renumbering nodes densely (the original wrote
		/// stale node ids and a wrong node count here).
		/// </summary>
		private void copySelection() {
			if (selection.Count==0) return;
			var sub=new MapDocument{ Author=doc.Author, Theme=doc.Theme, Ghost=doc.Ghost };
			var nodeMap=new Dictionary<int,int>();
			int mapNode(int old) {
				if (!nodeMap.TryGetValue(old,out int nw)) {
					nw=sub.Nodes.Count;
					sub.Nodes.Add(new Node{X=doc.Nodes[old].X,Y=doc.Nodes[old].Y});
					nodeMap[old]=nw;
				}
				return nw;
			}
			foreach (var e in selection.OrderBy(s=>s.I)) {
				switch (e.K) {
					case Kind.Node: mapNode(e.I); break;
					case Kind.Wall: sub.Walls.Add(new Wall{A=mapNode(doc.Walls[e.I].A),B=mapNode(doc.Walls[e.I].B)}); break;
					case Kind.Tri: sub.Tris.Add(new Tri{A=mapNode(doc.Tris[e.I].A),B=mapNode(doc.Tris[e.I].B),C=mapNode(doc.Tris[e.I].C)}); break;
					case Kind.Pad: var p=doc.Pads[e.I]; sub.Pads.Add(new Pad{X=p.X,Y=p.Y,Type=p.Type}); break;
					case Kind.Spring: var s=doc.Springs[e.I]; sub.Springs.Add(new Spring{X=s.X,Y=s.Y,Turns=s.Turns}); break;
					case Kind.Launcher: var l=doc.Launchers[e.I]; sub.Launchers.Add(new LauncherPair{SX=l.SX,SY=l.SY,STurns=l.STurns,FreqPct=l.FreqPct,OffPct=l.OffPct,CX=l.CX,CY=l.CY,CTurns=l.CTurns}); break;
					case Kind.Decal: var d=doc.Decals[e.I]; sub.Decals.Add(new Decal{X=d.X,Y=d.Y,Index=d.Index,Depth=d.Depth}); break;
				}
			}
			clipboard=sub.Serialize();
		}

		private void pasteClipboard(NVec2 worldPos) {
			if (clipboard==null) return;
			var sub=MapDocument.Parse(clipboard,out _);
			if (sub.Nodes.Count+sub.Pads.Count+sub.Springs.Count+sub.Launchers.Count+sub.Decals.Count==0) return;
			snapshot();

			//Offset so pasted content's top-left lands at the cursor's grid point
			int minX=int.MaxValue,minY=int.MaxValue;
			foreach (var n in sub.Nodes) { minX=Math.Min(minX,n.X); minY=Math.Min(minY,n.Y); }
			foreach (var p in sub.Pads) { minX=Math.Min(minX,p.X); minY=Math.Min(minY,p.Y); }
			foreach (var s in sub.Springs) { minX=Math.Min(minX,s.X); minY=Math.Min(minY,s.Y); }
			foreach (var l in sub.Launchers) { minX=Math.Min(minX,l.SX); minY=Math.Min(minY,l.SY); }
			foreach (var d in sub.Decals) { minX=Math.Min(minX,d.X/2); minY=Math.Min(minY,d.Y/2); }
			if (minX==int.MaxValue) { minX=0; minY=0; }
			int dx=(int)MathF.Round(worldPos.X/Unit)-minX, dy=(int)MathF.Round(worldPos.Y/Unit)-minY;

			selection.Clear();
			int nodeBase=doc.Nodes.Count;
			foreach (var n in sub.Nodes) { doc.Nodes.Add(new Node{X=n.X+dx,Y=n.Y+dy}); selection.Add((Kind.Node,doc.Nodes.Count-1)); }
			foreach (var w in sub.Walls) { doc.Walls.Add(new Wall{A=w.A+nodeBase,B=w.B+nodeBase}); selection.Add((Kind.Wall,doc.Walls.Count-1)); }
			foreach (var t in sub.Tris) { doc.Tris.Add(new Tri{A=t.A+nodeBase,B=t.B+nodeBase,C=t.C+nodeBase}); selection.Add((Kind.Tri,doc.Tris.Count-1)); }
			foreach (var p in sub.Pads) { doc.Pads.Add(new Pad{X=p.X+dx,Y=p.Y+dy,Type=p.Type}); selection.Add((Kind.Pad,doc.Pads.Count-1)); }
			foreach (var s in sub.Springs) { doc.Springs.Add(new Spring{X=s.X+dx,Y=s.Y+dy,Turns=s.Turns}); selection.Add((Kind.Spring,doc.Springs.Count-1)); }
			foreach (var l in sub.Launchers) { doc.Launchers.Add(new LauncherPair{SX=l.SX+dx,SY=l.SY+dy,STurns=l.STurns,FreqPct=l.FreqPct,OffPct=l.OffPct,CX=l.CX+dx,CY=l.CY+dy,CTurns=l.CTurns}); selection.Add((Kind.Launcher,doc.Launchers.Count-1)); }
			foreach (var d in sub.Decals) { doc.Decals.Add(new Decal{X=d.X+dx*2,Y=d.Y+dy*2,Index=d.Index,Depth=d.Depth}); selection.Add((Kind.Decal,doc.Decals.Count-1)); }
		}
		#endregion

		#region input
		private void toolInput() {
			var io=ImGui.GetIO();
			if (screenshotPath!=null) return;

			//Tool hotkeys (original scheme) + edit chords
			if (!io.WantCaptureKeyboard) {
				if (!io.KeyCtrl) {
					if (ImGui.IsKeyPressed(ImGuiKey.Z,false)) switchTool(Tool.Select);
					if (ImGui.IsKeyPressed(ImGuiKey.X,false)) switchTool(Tool.Node);
					if (ImGui.IsKeyPressed(ImGuiKey.C,false)) switchTool(Tool.Wall);
					if (ImGui.IsKeyPressed(ImGuiKey.V,false)) switchTool(Tool.Tri);
					if (ImGui.IsKeyPressed(ImGuiKey.B,false)) switchTool(Tool.Pad);
					if (ImGui.IsKeyPressed(ImGuiKey.N,false)) switchTool(Tool.Spring);
					if (ImGui.IsKeyPressed(ImGuiKey.M,false)) switchTool(Tool.Launcher);
					if (ImGui.IsKeyPressed(ImGuiKey.Comma,false)) switchTool(Tool.Pencil);
					if (ImGui.IsKeyPressed(ImGuiKey.Period,false)) switchTool(Tool.Decal);
					if (ImGui.IsKeyPressed(ImGuiKey.Delete,false)||ImGui.IsKeyPressed(ImGuiKey.Backspace,false)) deleteSelection();

					//WASD nudge selection: 1 unit, Shift=6 units (original 8px/48px)
					int step=io.KeyShift?6:1;
					int nx=0,ny=0;
					if (ImGui.IsKeyPressed(ImGuiKey.A)) nx-=step;
					if (ImGui.IsKeyPressed(ImGuiKey.D)) nx+=step;
					if (ImGui.IsKeyPressed(ImGuiKey.W)) ny-=step;
					if (ImGui.IsKeyPressed(ImGuiKey.S)) ny+=step;
					if ((nx!=0||ny!=0)&&selection.Count>0) { snapshot(); moveSelection(nx,ny); }
				} else {
					if (ImGui.IsKeyPressed(ImGuiKey.C,false)) copySelection();
					if (ImGui.IsKeyPressed(ImGuiKey.V,false)) pasteClipboard(screenToWorld(io.MousePos));
				}
			}

			if (io.WantCaptureMouse) return;

			NVec2 world=screenToWorld(io.MousePos);
			int ux=(int)MathF.Round(world.X/Unit), uy=(int)MathF.Round(world.Y/Unit);	//Snapped map units
			bool click=ImGui.IsMouseClicked(ImGuiMouseButton.Left);
			bool rclick=ImGui.IsMouseClicked(ImGuiMouseButton.Right);
			bool release=ImGui.IsMouseReleased(ImGuiMouseButton.Left);
			bool down=ImGui.IsMouseDown(ImGuiMouseButton.Left);

			switch (tool) {
				case Tool.Select: selectInput(world,click,rclick,release,down,io); break;

				case Tool.Node:
					if (click) { snapshot(); addNode(ux,uy); }
					if (rclick) { int n=hitNode(world); if (n>=0) { snapshot(); deleteNode(n); selection.Clear(); } }
					break;

				case Tool.Wall:
					if (click) {
						int n=hitNode(world);
						if (n<0) { snapshot(); n=addNode(ux,uy); }	//Clicking empty space plants a node (small QoL over original)
						else if (wallFromNode>=0&&n!=wallFromNode) snapshot();
						if (wallFromNode>=0&&n!=wallFromNode) {
							addWall(wallFromNode,n);
							wallFromNode=io.KeyShift?n:-1;	//Shift = chain
						} else wallFromNode=n;
					}
					if (rclick) wallFromNode=-1;
					break;

				case Tool.Tri:
					if (click) {
						int n=hitNode(world);
						if (n>=0&&!triPicks.Contains(n)) {
							triPicks.Add(n);
							if (triPicks.Count==3) {
								snapshot();
								doc.Tris.Add(new Tri{A=triPicks[0],B=triPicks[1],C=triPicks[2]});
								if (io.KeyShift) triPicks.RemoveAt(0);	//Shift = fan from last two
								else triPicks.Clear();
							}
						}
					}
					if (rclick) triPicks.Clear();
					break;

				case Tool.Pad:
					if (click) { snapshot(); doc.Pads.Add(new Pad{X=ux,Y=uy,Type=pendingPadType}); }
					if (rclick) {
						int p=hitPadRect(world);
						if (p>=0) { snapshot(); doc.Pads[p].Type=(doc.Pads[p].Type+1)%3; }
						else pendingPadType=(pendingPadType+1)%3;
					}
					break;

				case Tool.Spring:
					if (click) { snapshot(); doc.Springs.Add(new Spring{X=ux,Y=uy,Turns=pendingSpringTurns}); }
					if (rclick) {
						int s=hitSpringAt(world);
						if (s>=0) { snapshot(); doc.Springs[s].Turns=(doc.Springs[s].Turns+2)%8; }
						else pendingSpringTurns=(pendingSpringTurns+2)%8;	//90 deg = 2 eighth-turns
					}
					break;

				case Tool.Launcher:
					if (click) {
						if (launcherSender<0) {
							snapshot();
							doc.Launchers.Add(new LauncherPair{SX=ux,SY=uy,STurns=pendingLauncherTurns,FreqPct=50,OffPct=0,CX=ux,CY=uy,CTurns=pendingLauncherTurns});
							launcherSender=doc.Launchers.Count-1;
						} else {
							var l=doc.Launchers[launcherSender];
							l.CX=ux; l.CY=uy;
							launcherSender=-1;
						}
					}
					if (rclick) {
						if (launcherSender>=0) doc.Launchers[launcherSender].STurns=(doc.Launchers[launcherSender].STurns+1)%8;
						else pendingLauncherTurns=(pendingLauncherTurns+1)%8;
					}
					break;

				case Tool.Pencil:
					if (down) {
						int n=hitNode(world);
						if (pencilLastNode<0) {
							if (click) { snapshot(); pencilLastNode=n>=0?n:addNode(ux,uy); }
						} else {
							//Drop a node roughly every 6 units of travel and link
							var last=nodeWorld(pencilLastNode);
							if ((world-last).Length()>=6*Unit) {
								int nn=addNode(ux,uy);
								addWall(pencilLastNode,nn);
								pencilLastNode=nn;
							}
						}
					}
					if (release) pencilLastNode=-1;
					break;

				case Tool.Decal:
					if (click) {
						snapshot();
						doc.Decals.Add(new Decal{X=(int)MathF.Round(world.X/4),Y=(int)MathF.Round(world.Y/4),Index=pendingDecalIndex,Depth=pendingDecalDepth});
					}
					if (rclick) { int d=hitDecalAt(world); if (d>=0) { snapshot(); doc.Decals.RemoveAt(d); selection.Clear(); } }
					//Wheel changes index, shift+wheel depth (like the original)
					if (io.MouseWheel!=0&&io.KeyShift) pendingDecalDepth=Math.Clamp(pendingDecalDepth+0.05f*MathF.Sign(io.MouseWheel),0,1);
					else if (io.MouseWheel!=0&&io.KeyAlt) pendingDecalIndex=Math.Max(0,pendingDecalIndex+(int)MathF.Sign(io.MouseWheel));
					break;
			}
		}

		private void selectInput(NVec2 world,bool click,bool rclick,bool release,bool down,ImGuiIOPtr io) {
			if (click) {
				var hit=hitAny(world);
				if (hit!=null) {
					if (io.KeyShift) {
						if (!selection.Remove(hit.Value)) selection.Add(hit.Value);
					} else if (!selection.Contains(hit.Value)) {
						selection.Clear();
						selection.Add(hit.Value);
					}
					draggingSel=true;
					dragRemainder=NVec2.Zero;
				} else {
					if (!io.KeyShift) selection.Clear();
					boxSelecting=true;
					boxStartScreen=io.MousePos;
				}
			}

			if (draggingSel&&down&&ImGui.IsMouseDragging(ImGuiMouseButton.Left,2)) {
				//Accumulate world-space movement; apply whole map units
				dragRemainder+=io.MouseDelta/zoom/Unit;
				int dx=(int)dragRemainder.X, dy=(int)dragRemainder.Y;
				if (dx!=0||dy!=0) {
					if (!dragSnapshotTaken) { undoStack.Push(doc); dirty=true; dragSnapshotTaken=true; }
					moveSelection(dx,dy);
					dragRemainder-=new NVec2(dx,dy);
				}
			}

			if (release) {
				if (boxSelecting) {
					var a=screenToWorld(new NVec2(MathF.Min(boxStartScreen.X,io.MousePos.X),MathF.Min(boxStartScreen.Y,io.MousePos.Y)));
					var b=screenToWorld(new NVec2(MathF.Max(boxStartScreen.X,io.MousePos.X),MathF.Max(boxStartScreen.Y,io.MousePos.Y)));
					bool inBox(NVec2 p)=>p.X>=a.X&&p.X<=b.X&&p.Y>=a.Y&&p.Y<=b.Y;
					for (int i=0;i<doc.Nodes.Count;i++) if (inBox(nodeWorld(i))) selection.Add((Kind.Node,i));
					for (int i=0;i<doc.Walls.Count;i++) if (inBox(nodeWorld(doc.Walls[i].A))&&inBox(nodeWorld(doc.Walls[i].B))) selection.Add((Kind.Wall,i));
					for (int i=0;i<doc.Tris.Count;i++) if (inBox(nodeWorld(doc.Tris[i].A))&&inBox(nodeWorld(doc.Tris[i].B))&&inBox(nodeWorld(doc.Tris[i].C))) selection.Add((Kind.Tri,i));
					for (int i=0;i<doc.Pads.Count;i++) if (inBox(new NVec2(doc.Pads[i].X*Unit,doc.Pads[i].Y*Unit))) selection.Add((Kind.Pad,i));
					for (int i=0;i<doc.Springs.Count;i++) if (inBox(new NVec2(doc.Springs[i].X*Unit,doc.Springs[i].Y*Unit))) selection.Add((Kind.Spring,i));
					for (int i=0;i<doc.Launchers.Count;i++) if (inBox(new NVec2(doc.Launchers[i].SX*Unit,doc.Launchers[i].SY*Unit))) selection.Add((Kind.Launcher,i));
					for (int i=0;i<doc.Decals.Count;i++) if (inBox(new NVec2(doc.Decals[i].X*4,doc.Decals[i].Y*4))) selection.Add((Kind.Decal,i));
				}
				draggingSel=false;
				boxSelecting=false;
				dragSnapshotTaken=false;
			}

			if (rclick) {
				var hit=hitAny(world);
				if (hit!=null) {
					if (selection.Contains(hit.Value)) deleteSelection();
					else { snapshot(); deleteElement(hit.Value); selection.Clear(); }
				}
			}
		}

		private bool dragSnapshotTaken;
		#endregion

		#region overlays
		/// <summary>Selection highlights, pending-tool previews, toolbar window.</summary>
		private void drawOverlays() {
			var dl=ImGui.GetBackgroundDrawList();
			var io=ImGui.GetIO();
			uint selCol=rgba(255,120,0);
			NVec2 world=screenToWorld(io.MousePos);

			//Selection highlights
			foreach (var e in selection) {
				switch (e.K) {
					case Kind.Node: { if (e.I>=doc.Nodes.Count) break; var c=worldToScreen(doc.Nodes[e.I].X*Unit,doc.Nodes[e.I].Y*Unit); float r=MathF.Max(4,5*zoom); dl.AddRect(new NVec2(c.X-r,c.Y-r),new NVec2(c.X+r,c.Y+r),selCol,0,ImDrawFlags.None,2); break; }
					case Kind.Wall: { if (e.I>=doc.Walls.Count) break; dl.AddLine(worldToScreen(doc.Nodes[doc.Walls[e.I].A].X*Unit,doc.Nodes[doc.Walls[e.I].A].Y*Unit),worldToScreen(doc.Nodes[doc.Walls[e.I].B].X*Unit,doc.Nodes[doc.Walls[e.I].B].Y*Unit),selCol,MathF.Max(2,4*zoom)); break; }
					case Kind.Tri: { if (e.I>=doc.Tris.Count) break; var t=doc.Tris[e.I]; dl.AddTriangle(worldToScreen(doc.Nodes[t.A].X*Unit,doc.Nodes[t.A].Y*Unit),worldToScreen(doc.Nodes[t.B].X*Unit,doc.Nodes[t.B].Y*Unit),worldToScreen(doc.Nodes[t.C].X*Unit,doc.Nodes[t.C].Y*Unit),selCol,2); break; }
					case Kind.Pad: { if (e.I>=doc.Pads.Count) break; var p=doc.Pads[e.I]; dl.AddRect(worldToScreen(p.X*Unit-4,p.Y*Unit-4),worldToScreen(p.X*Unit-4+196,p.Y*Unit-4+196),selCol,0,ImDrawFlags.None,3); break; }
					case Kind.Spring: { if (e.I>=doc.Springs.Count) break; var s=doc.Springs[e.I]; var c=worldToScreen(s.X*Unit,s.Y*Unit); dl.AddCircle(c,10*zoom,selCol,0,2); break; }
					case Kind.Launcher: { if (e.I>=doc.Launchers.Count) break; var l=doc.Launchers[e.I]; dl.AddCircle(worldToScreen(l.SX*Unit,l.SY*Unit),11*zoom,selCol,0,2); dl.AddCircle(worldToScreen(l.CX*Unit,l.CY*Unit),8*zoom,selCol,0,2); break; }
					case Kind.Decal: { if (e.I>=doc.Decals.Count) break; var d=doc.Decals[e.I]; dl.AddCircle(worldToScreen(d.X*4,d.Y*4),9*zoom,selCol,4,2); break; }
				}
			}

			//Box select rubber band
			if (boxSelecting) dl.AddRect(boxStartScreen,io.MousePos,rgba(0,120,255),0,ImDrawFlags.None,1.5f);

			//Pending previews
			if (!io.WantCaptureMouse) {
				uint prev=rgba(0,120,255,160);
				if (tool==Tool.Wall&&wallFromNode>=0&&wallFromNode<doc.Nodes.Count)
					dl.AddLine(worldToScreen(doc.Nodes[wallFromNode].X*Unit,doc.Nodes[wallFromNode].Y*Unit),io.MousePos,prev,2);
				if (tool==Tool.Tri)
					foreach (int n in triPicks.Where(n=>n<doc.Nodes.Count))
						dl.AddCircle(worldToScreen(doc.Nodes[n].X*Unit,doc.Nodes[n].Y*Unit),7*zoom,prev,0,2);
				if (tool==Tool.Pad) {
					int ux=(int)MathF.Round(world.X/Unit), uy=(int)MathF.Round(world.Y/Unit);
					dl.AddRect(worldToScreen(ux*Unit-4,uy*Unit-4),worldToScreen(ux*Unit-4+196,uy*Unit-4+196),prev,0,ImDrawFlags.None,1.5f);
				}
				if (tool==Tool.Launcher&&launcherSender>=0&&launcherSender<doc.Launchers.Count)
					dl.AddLine(worldToScreen(doc.Launchers[launcherSender].SX*Unit,doc.Launchers[launcherSender].SY*Unit),io.MousePos,prev,2);
			}

			//Toolbar
			ImGui.SetNextWindowPos(new NVec2(8,32),ImGuiCond.FirstUseEver);
			ImGui.Begin("Tools",ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoCollapse);
			toolButton("Select (Z)",Tool.Select);
			toolButton("Node (X)",Tool.Node);
			toolButton("Wall (C)",Tool.Wall);
			toolButton("Triangle (V)",Tool.Tri);
			toolButton("Pad (B)",Tool.Pad);
			toolButton("Spring (N)",Tool.Spring);
			toolButton("Launcher (M)",Tool.Launcher);
			toolButton("Pencil (,)",Tool.Pencil);
			toolButton("Decal (.)",Tool.Decal);
			ImGui.Separator();
			switch (tool) {
				case Tool.Pad: ImGui.TextDisabled($"type: {pendingPadType switch{0=>"START",2=>"HEAL",_=>"END"}}\nright-click cycles"); break;
				case Tool.Spring: ImGui.TextDisabled($"turns: {pendingSpringTurns}\nright-click rotates"); break;
				case Tool.Launcher: ImGui.TextDisabled($"turns: {pendingLauncherTurns}\nclick sender, then catcher\nright-click rotates"); break;
				case Tool.Decal: ImGui.TextDisabled($"index {pendingDecalIndex} depth {pendingDecalDepth:0.00}\nalt+wheel index\nshift+wheel depth"); break;
				case Tool.Wall: ImGui.TextDisabled("click two nodes\nshift chains"); break;
				case Tool.Tri: ImGui.TextDisabled("click three nodes\nshift fans"); break;
				case Tool.Select: ImGui.TextDisabled($"{selection.Count} selected\nshift multi, drag moves\nWASD nudges, Del deletes"); break;
			}
			//Launcher timing editors for selected launchers
			foreach (var e in selection.Where(s=>s.K==Kind.Launcher).Take(1)) {
				if (e.I>=doc.Launchers.Count) break;
				ImGui.Separator();
				var l=doc.Launchers[e.I];
				int f=l.FreqPct,o=l.OffPct;
				if (ImGui.SliderInt("freq %",ref f,0,100)) { l.FreqPct=f; dirty=true; }
				if (ImGui.SliderInt("offset %",ref o,0,100)) { l.OffPct=o; dirty=true; }
			}
			ImGui.End();
		}

		private void toolButton(string label,Tool t) {
			bool active=tool==t;
			if (active) ImGui.PushStyleColor(ImGuiCol.Button,rgba(0,100,200));
			if (ImGui.Button(label,new NVec2(110,0))) switchTool(t);
			if (active) ImGui.PopStyleColor();
		}
		#endregion
	}
}
