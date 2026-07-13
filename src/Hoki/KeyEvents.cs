using System;
using Microsoft.Xna.Framework.Input;

namespace Hoki {
	//Minimal stand-ins for the WinForms key event types the input plumbing was built on.
	//Game.Update polls the keyboard and raises these; KeyboardController consumes them unchanged.

	public delegate void KeyEventHandler(object sender,KeyEventArgs e);

	public class KeyEventArgs : EventArgs {
		public Keys KeyCode;

		public KeyEventArgs(Keys keyCode) {
			KeyCode=keyCode;
		}
	}

	public delegate void KeyPressEventHandler(object sender,KeyPressEventArgs e);

	public class KeyPressEventArgs : EventArgs {
		public char KeyChar;

		public KeyPressEventArgs(char keyChar) {
			KeyChar=keyChar;
		}
	}
}
