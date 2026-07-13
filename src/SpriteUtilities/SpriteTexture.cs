using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities {
	/// <summary>
	/// A wrapper for textures that allows dimensions and frames to be specified
	/// </summary>
	public class SpriteTexture {
		private Texture2D tex;
		private int
			width,		//Size of one frame
			height,
			frames,		//Frames in the texture
			rowSize;	//Frames on one row
		private Vector2
			sizeRatio,	//Ratios of the dimensions of one frame to the dimensions of the source image
			coordOffset,//Offset from 0,0 of the texture's region on the image in tex coords
			frameBuffer;//Space in texture coordinates between frames

		#region getset

		/// <summary>
		/// The texture for the corresponding frame
		/// </summary>
		public Texture2D Tex {
			get { return tex; }
		}

		/// <summary>
		/// The width of one frame (readonly)
		/// </summary>
		public int Width {
			get { return width; }
		}

		/// <summary>
		/// The height of each frame (readonly)
		/// </summary>
		public int Height {
			get { return height; }
		}

		/// <summary>
		/// The number of frames (readonly)
		/// </summary>
		public int Frames {
			get { return frames; }
		}

		#endregion

		/// <summary>
		/// Creates a sprite texture from embedded resources
		/// </summary>
		/// <param name="device">The device that will be used to render this texture</param>
		/// <param name="texture">The loaded texture</param>
		/// <param name="data">Contents of a data file. Format must be: x,y,width,height[,frames,framesPerRow[,frameBufferX,frameBufferY]], where the bracketed material is optional. If null, the texture's native dimensions will be used.</param>
		public SpriteTexture(GraphicsDevice device,Texture2D texture,string data,int texWidth,int texHeight) {
			//Set defaults
			int frameBufferX=0;	//Temporarily hold frame buffer values
			int frameBufferY=0;
			int x=0,y=0;		//Temporarily hold texel position
			frames=rowSize=1;	//Default to one frame

			if (data==null) {
				width=texWidth;
				height=texHeight;
			} else {
				//Read the data
				String[] dimensions=data.Split(',');	//Split the file into an array of dimensions

				//Ensure that there are enough values in the data
				if (dimensions.Length<4) throw new InvalidDataException("The data for a SpriteTexture must contain at least four values: x,y,width,height");

				x=int.Parse(dimensions[0]);			//Origin, width, and height, MUST be defined in the file
				y=int.Parse(dimensions[1]);			//x and y are locals, will be used in calculating instance vars later
				width=int.Parse(dimensions[2]);
				height=int.Parse(dimensions[3]);
				if (dimensions.Length>=6) {				//If a frame count is declared, use it
					frames=int.Parse(dimensions[4]);
					rowSize=int.Parse(dimensions[5]);
					if (dimensions.Length>=8) {			//If a frame buffer is declared, use it
						frameBufferX=int.Parse(dimensions[6]);
						frameBufferY=int.Parse(dimensions[7]);
					}
				}
			}

			//Get the texture
			this.tex=texture;

			//Find the size ratio between a frame and the image
			sizeRatio.X=(float)(width+frameBufferX)/texWidth;
			sizeRatio.Y=(float)(height+frameBufferY)/texHeight;

			//Find the offset of the texture region on the image
			coordOffset.X=(float)x/texWidth;
			coordOffset.Y=(float)y/texHeight;
			frameBuffer.X=(float)frameBufferX/texWidth;
			frameBuffer.Y=(float)frameBufferY/texHeight;
		}

		public Vector4 TexCoords(int frame) {
			int col=frame%rowSize;
			int row=(frame-col)/rowSize;
			return new Vector4(coordOffset.X+(sizeRatio.X+frameBuffer.X)*col,coordOffset.Y+(sizeRatio.Y+frameBuffer.Y)*row,coordOffset.X+frameBuffer.X*col+sizeRatio.X*(col+1),coordOffset.Y+frameBuffer.Y*row+sizeRatio.Y*(row+1));
		}
	}
}
