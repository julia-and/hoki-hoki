using System;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	public abstract class MenuElement : SpriteObject {
		public abstract void Select();
		public abstract void Deselect();
		public abstract void Input(Controls control);

		public MenuElement(Device device,SpriteTexture tex) : base(device,tex) {
			; //(nop)
		}
	}
}