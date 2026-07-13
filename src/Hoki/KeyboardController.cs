using System;
using System.Collections;
using Microsoft.Xna.Framework.Input;

namespace Hoki {
	/// <summary>
	/// Listens for key presses and maps them to game controls
	/// </summary>
	public class KeyboardController : Controller {
		private static readonly Keys[] defaultScheme;
		private const int numControls=7;

		private Keys[] controlMap;
		private bool[] states;

		public event ControlEventHandler ControlDown;
		public event ControlEventHandler ControlUp;

		static KeyboardController() {
			defaultScheme=new Keys[numControls];
			defaultScheme[(int)Controls.Down]=Keys.Down;
			defaultScheme[(int)Controls.Up]=Keys.Up;
			defaultScheme[(int)Controls.Left]=Keys.Left;
			defaultScheme[(int)Controls.Right]=Keys.Right;
			defaultScheme[(int)Controls.A]=Keys.Z;
			defaultScheme[(int)Controls.B]=Keys.X;
			defaultScheme[(int)Controls.Start]=Keys.Enter;
		}
		
		public KeyboardController(Game game) {
			//Capture events
			game.AnyKeyDown+=new KeyEventHandler(OnKeyDown);
			game.AnyKeyUp+=new KeyEventHandler(OnKeyUp);

			//Create a control array
			controlMap=new Keys[numControls];
			defaultScheme.CopyTo(controlMap,0);	//Set all keys to defaults

			//Create a state array
			states=new bool[numControls];	//They're automatically all set false

			//Make sure that the events have something to invoke
			ControlDown+=new ControlEventHandler(onControlDown);
			ControlUp+=new ControlEventHandler(onControlUp);
		}

		public void Unhook(Game game) {
			game.AnyKeyDown-=new KeyEventHandler(OnKeyDown);
			game.AnyKeyUp-=new KeyEventHandler(OnKeyUp);
		}

		public bool Down(Controls control) {
			return states[(int)control];
		}

		public void SetKey(Controls control,Keys k) {
			controlMap[(int)control]=k;
		}

		public Keys GetKey(Controls control) {
			return controlMap[(int)control];
		}

		/// <summary>
		/// Programmatically presses a control (down+up), as if its key were tapped
		/// </summary>
		public void Press(Controls control) {
			ControlDown(this,new ControlEventArgs(control));
			ControlUp(this,new ControlEventArgs(control));
		}

		private void OnKeyDown(object sender, KeyEventArgs e) {
			for(int i=0;i<numControls;i++) 
				if (e.KeyCode==controlMap[i]) {
					ControlDown(this,new ControlEventArgs((Controls)i));
					states[i]=true;
				}
		}

		private void OnKeyUp(object sender, KeyEventArgs e) {
			for(int i=0;i<numControls;i++) 
				if (e.KeyCode==controlMap[i]) {
					ControlUp(this,new ControlEventArgs((Controls)i));
					states[i]=false;
				}
		}

		private void onControlDown(object sender, ControlEventArgs e) {
			; //nop
		}

		private void onControlUp(object sender, ControlEventArgs e) {
			; //nop
		}
	}

	public delegate void ControlEventHandler(object sender,ControlEventArgs e);

	public class ControlEventArgs : EventArgs {
		private Controls c;
		public Controls Control {
			get { return c; }
		}
		public ControlEventArgs(Controls c) {
			this.c=c;
		}
	}
}
