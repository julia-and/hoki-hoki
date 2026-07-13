using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities {

	/// <summary>
	/// SpriteObject derivative that draws text
	/// </summary>
	public class SpriteText : SpriteObject {
		protected SpriteFontBase font;		//Font to draw with
		protected Rectangle rect;			//Clipping dimensions for the text
		protected string text;				//text to draw
		protected FontDrawFlags format;		//Format for drawing

		#region getset
		/// <summary>
		/// Text to draw
		/// </summary>
		public string Text {
			get { return text; }
			set { text=value; }
		}

		/// <summary>
		/// Font used for drawing
		/// </summary>
		public SpriteFontBase DrawFont {
			get { return font; }
			set { font=value; }
		}

		/// <summary>
		/// Format to pass to DrawText
		/// </summary>
		public FontDrawFlags Format {
			get { return format; }
			set { format=value; }
		}

		/// <summary>
		/// Clipping width
		/// </summary>
		public override float Width {
			get { return (float)rect.Width; }
			set { rect.Width=(int)value; }
		}

		/// <summary>
		/// Clipping height
		/// </summary>
		public override float Height {
			get { return (float)rect.Height; }
			set { rect.Height=(int)value; }
		}

		#endregion

		/// <param name="width">Clipping width</param>
		/// <param name="height">Clipping height</param>
		public SpriteText(GraphicsDevice device,SpriteFontBase font,int width,int height) : base(device,null) {
			//Store the font
			this.font=font;

			//Create a clipping rectangle
			rect=new Rectangle(0,0,width,height);

			//Set defaults
			text="";
			format=0;
			Tint=Color.Black;
		}

		public Rectangle Area() {
			Vector2 size=font.MeasureString(text??"");
			return new Rectangle(0,0,(int)Math.Ceiling(size.X),(int)Math.Ceiling(size.Y));
		}

		/// <summary>
		/// Word-wraps text to fit the clipping width
		/// </summary>
		private string wrap(string s) {
			string[] words=s.Split(' ');
			string result="",line="";
			foreach (string word in words) {
				string test=line.Length==0?word:line+" "+word;
				if (font.MeasureString(test).X>rect.Width && line.Length>0) {
					result+=line+"\n";
					line=word;
				} else line=test;
			}
			return result+line;
		}

		/// <summary>
		/// Transform the batch and draw the text to the screen
		/// </summary>
		/// <param name="trans">Absolute transformation matrix</param>
		protected override void deviceDraw(Matrix trans) {
			if (string.IsNullOrEmpty(text)) return;

			string drawn=(format&FontDrawFlags.WordBreak)!=0?wrap(text):text;

			//Alignment within the clipping rect
			Vector2 pos=Vector2.Zero;
			if ((format&(FontDrawFlags.Center|FontDrawFlags.Right|FontDrawFlags.VerticalCenter))!=0) {
				Vector2 size=font.MeasureString(drawn);
				if ((format&FontDrawFlags.Center)!=0)			pos.X=(rect.Width-size.X)/2;
				else if ((format&FontDrawFlags.Right)!=0)		pos.X=rect.Width-size.X;
				if ((format&FontDrawFlags.VerticalCenter)!=0)	pos.Y=(rect.Height-size.Y)/2;
			}

			Renderer.Batch.Begin(SpriteSortMode.Deferred,BlendState.NonPremultiplied,null,null,null,null,trans);
			font.DrawText(Renderer.Batch,drawn,pos,ColorX.FromArgb((int)alpha,color));
			Renderer.Batch.End();
		}
	}
}
