using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HokiEdit {
	/// <summary>
	/// Dear ImGui renderer/input backend for MonoGame. Vendored, based on the canonical
	/// ImGui.NET XNA sample, updated to the AddKeyEvent input API.
	/// </summary>
	public class ImGuiRenderer {
		[StructLayout(LayoutKind.Sequential)]
		private struct ImGuiVertex : IVertexType {
			public Vector2 Position;
			public Vector2 UV;
			public uint Color;

			public static readonly VertexDeclaration Declaration=new VertexDeclaration(
				new VertexElement(0,VertexElementFormat.Vector2,VertexElementUsage.Position,0),
				new VertexElement(8,VertexElementFormat.Vector2,VertexElementUsage.TextureCoordinate,0),
				new VertexElement(16,VertexElementFormat.Color,VertexElementUsage.Color,0));
			VertexDeclaration IVertexType.VertexDeclaration=>Declaration;
		}

		private readonly Game game;
		private readonly GraphicsDevice device;
		private BasicEffect effect;
		private readonly RasterizerState rasterizer=new RasterizerState{ ScissorTestEnable=true, CullMode=CullMode.None };

		private readonly Dictionary<IntPtr,Texture2D> boundTextures=new();
		private int nextTextureId=1;
		private IntPtr fontTextureId;

		private ImGuiVertex[] vertexScratch=new ImGuiVertex[4096];
		private short[] indexScratch=new short[8192];

		private int prevScroll, prevScrollH;
		private KeyboardState prevKeys;
		private MouseState prevMouse;

		public ImGuiRenderer(Game game) {
			this.game=game;
			device=game.GraphicsDevice;

			ImGui.CreateContext();
			var io=ImGui.GetIO();
			io.BackendFlags|=ImGuiBackendFlags.RendererHasVtxOffset;

			rebuildFontAtlas();

			effect=new BasicEffect(device){ TextureEnabled=true, VertexColorEnabled=true, World=Matrix.Identity, View=Matrix.Identity };

			game.Window.TextInput+=(s,e)=>{
				if (e.Character!='\t') ImGui.GetIO().AddInputCharacter(e.Character);
			};
		}

		private unsafe void rebuildFontAtlas() {
			var io=ImGui.GetIO();
			io.Fonts.GetTexDataAsRGBA32(out byte* pixels,out int w,out int h,out int bpp);
			var tex=new Texture2D(device,w,h,false,SurfaceFormat.Color);
			byte[] data=new byte[w*h*bpp];
			Marshal.Copy(new IntPtr(pixels),data,0,data.Length);
			tex.SetData(data);
			fontTextureId=BindTexture(tex);
			io.Fonts.SetTexID(fontTextureId);
			io.Fonts.ClearTexData();
		}

		public IntPtr BindTexture(Texture2D tex) {
			var id=new IntPtr(nextTextureId++);
			boundTextures[id]=tex;
			return id;
		}

		#region input
		private static readonly (Keys xna,ImGuiKey imgui)[] keyMap=buildKeyMap();

		private static (Keys,ImGuiKey)[] buildKeyMap() {
			var map=new List<(Keys,ImGuiKey)>{
				(Keys.Left,ImGuiKey.LeftArrow),(Keys.Right,ImGuiKey.RightArrow),(Keys.Up,ImGuiKey.UpArrow),(Keys.Down,ImGuiKey.DownArrow),
				(Keys.Tab,ImGuiKey.Tab),(Keys.Enter,ImGuiKey.Enter),(Keys.Escape,ImGuiKey.Escape),(Keys.Space,ImGuiKey.Space),
				(Keys.Back,ImGuiKey.Backspace),(Keys.Delete,ImGuiKey.Delete),(Keys.Home,ImGuiKey.Home),(Keys.End,ImGuiKey.End),
				(Keys.PageUp,ImGuiKey.PageUp),(Keys.PageDown,ImGuiKey.PageDown),(Keys.Insert,ImGuiKey.Insert),
				(Keys.LeftControl,ImGuiKey.LeftCtrl),(Keys.RightControl,ImGuiKey.RightCtrl),
				(Keys.LeftShift,ImGuiKey.LeftShift),(Keys.RightShift,ImGuiKey.RightShift),
				(Keys.LeftAlt,ImGuiKey.LeftAlt),(Keys.RightAlt,ImGuiKey.RightAlt),
				(Keys.LeftWindows,ImGuiKey.LeftSuper),(Keys.RightWindows,ImGuiKey.RightSuper),
				(Keys.OemComma,ImGuiKey.Comma),(Keys.OemPeriod,ImGuiKey.Period)
			};
			//All letters and digits (both enums are contiguous)
			for (int i=0;i<26;i++) map.Add((Keys.A+i,ImGuiKey.A+i));
			for (int i=0;i<10;i++) map.Add((Keys.D0+i,ImGuiKey._0+i));
			return map.ToArray();
		}

		public void Update(GameTime time) {
			var io=ImGui.GetIO();
			io.DisplaySize=new System.Numerics.Vector2(device.PresentationParameters.BackBufferWidth,device.PresentationParameters.BackBufferHeight);
			io.DisplayFramebufferScale=System.Numerics.Vector2.One;
			io.DeltaTime=(float)time.ElapsedGameTime.TotalSeconds;

			var mouse=Mouse.GetState();
			var keys=Keyboard.GetState();

			io.AddMousePosEvent(mouse.X,mouse.Y);
			if (mouse.LeftButton!=prevMouse.LeftButton) io.AddMouseButtonEvent(0,mouse.LeftButton==ButtonState.Pressed);
			if (mouse.RightButton!=prevMouse.RightButton) io.AddMouseButtonEvent(1,mouse.RightButton==ButtonState.Pressed);
			if (mouse.MiddleButton!=prevMouse.MiddleButton) io.AddMouseButtonEvent(2,mouse.MiddleButton==ButtonState.Pressed);
			if (mouse.ScrollWheelValue!=prevScroll||mouse.HorizontalScrollWheelValue!=prevScrollH) {
				//Horizontal negated to match the canonical SDL2 imgui backend
				io.AddMouseWheelEvent(-(mouse.HorizontalScrollWheelValue-prevScrollH)/120f,(mouse.ScrollWheelValue-prevScroll)/120f);
				prevScroll=mouse.ScrollWheelValue;
				prevScrollH=mouse.HorizontalScrollWheelValue;
			}

			foreach (var (xna,imgui) in keyMap) {
				bool now=keys.IsKeyDown(xna), was=prevKeys.IsKeyDown(xna);
				if (now!=was) io.AddKeyEvent(imgui,now);
			}
			io.AddKeyEvent(ImGuiKey.ModCtrl,keys.IsKeyDown(Keys.LeftControl)||keys.IsKeyDown(Keys.RightControl));
			io.AddKeyEvent(ImGuiKey.ModShift,keys.IsKeyDown(Keys.LeftShift)||keys.IsKeyDown(Keys.RightShift));
			io.AddKeyEvent(ImGuiKey.ModAlt,keys.IsKeyDown(Keys.LeftAlt)||keys.IsKeyDown(Keys.RightAlt));
			io.AddKeyEvent(ImGuiKey.ModSuper,keys.IsKeyDown(Keys.LeftWindows)||keys.IsKeyDown(Keys.RightWindows));

			prevMouse=mouse;
			prevKeys=keys;

			ImGui.NewFrame();
		}
		#endregion

		#region render
		public void Render() {
			ImGui.Render();
			var drawData=ImGui.GetDrawData();

			var pp=device.PresentationParameters;
			effect.Projection=Matrix.CreateOrthographicOffCenter(0,pp.BackBufferWidth,pp.BackBufferHeight,0,-1,1);

			device.BlendState=BlendState.NonPremultiplied;
			device.DepthStencilState=DepthStencilState.None;
			device.RasterizerState=rasterizer;
			device.SamplerStates[0]=SamplerState.LinearClamp;

			Rectangle prevScissor=device.ScissorRectangle;

			for (int n=0;n<drawData.CmdListsCount;n++) {
				var cmdList=drawData.CmdLists[n];

				int vtxCount=cmdList.VtxBuffer.Size;
				int idxCount=cmdList.IdxBuffer.Size;
				if (vertexScratch.Length<vtxCount) vertexScratch=new ImGuiVertex[vtxCount*2];
				if (indexScratch.Length<idxCount) indexScratch=new short[idxCount*2];

				unsafe {
					fixed (ImGuiVertex* dst=vertexScratch)
						Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data,dst,vertexScratch.Length*sizeof(ImGuiVertex),vtxCount*sizeof(ImGuiVertex));
					fixed (short* dst=indexScratch)
						Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data,dst,indexScratch.Length*sizeof(short),idxCount*sizeof(short));
				}

				for (int c=0;c<cmdList.CmdBuffer.Size;c++) {
					var cmd=cmdList.CmdBuffer[c];
					if (cmd.ElemCount==0) continue;

					device.ScissorRectangle=new Rectangle(
						(int)cmd.ClipRect.X,(int)cmd.ClipRect.Y,
						(int)(cmd.ClipRect.Z-cmd.ClipRect.X),(int)(cmd.ClipRect.W-cmd.ClipRect.Y));

					effect.Texture=boundTextures.TryGetValue(cmd.TextureId,out var tex)?tex:boundTextures[fontTextureId];
					effect.CurrentTechnique.Passes[0].Apply();

					device.DrawUserIndexedPrimitives(
						PrimitiveType.TriangleList,
						vertexScratch,(int)cmd.VtxOffset,vtxCount,
						indexScratch,(int)cmd.IdxOffset,(int)cmd.ElemCount/3);
				}
			}

			device.ScissorRectangle=prevScissor;
		}
		#endregion
	}
}
