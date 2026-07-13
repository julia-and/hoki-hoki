using System;
using System.Collections;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;

namespace Hoki {
using Device=Microsoft.Xna.Framework.Graphics.GraphicsDevice;
	/// <summary>
	/// A set of UI elements
	/// </summary>
	public class Menu : SpriteObject, Updateable {
		private const float buffer=2;

		private int index;
		private ArrayList
			elements;		//All menu elements
		private Controller
			controller;		//Get input
		private bool
			locked,
			transfer;		//If true, will unlock on next update
		private MenuElement
			cancel;			//Default element ('B' input pushes it)

		public override float Width {
			get {
				float width=0;
				foreach (SpriteObject obj in elements) width=Math.Max(obj.Width,width);
				return width;
			}
		}

		public override float Height {
			get {
				float height=0;
				foreach (SpriteObject obj in elements) height+=obj.Height;
				height+=(elements.Count-1)*2;
				return height;
			}
		}

		public int Elements {
			get { return elements.Count; }
		}

		public Menu(Device device,Controller controller) : base(device,null) {
			elements=new ArrayList();

			this.controller=controller;
			controller.ControlDown+=new ControlEventHandler(onControlDown);
		}

		public void Unhook() {
			controller.ControlDown-=new ControlEventHandler(onControlDown);
		}

		private void onControlDown(object sender, ControlEventArgs e) {
			if (locked || elements.Count==0) return;

			switch (e.Control) {
				case Controls.Up:
					//Scroll up the menu
					deselect(index);
					if (--index<0) index=elements.Count-1;
					select(index);
					break;
				case Controls.Down:
					//Scroll down the menu
					deselect(index);
					index=(index+1)%elements.Count;
					select(index);
					break;
				case Controls.B:
					//Send a Controls.A to the cancel element
					if (cancel!=null) cancel.Input(Controls.A);
					break;
				default:
					//Pass any other input to the selected element
					if (elements.Count!=0) ((MenuElement)elements[index]).Input(e.Control);
					break;
			}
		}

		/// <summary>
		/// Adds an interface element to the menu
		/// </summary>
		public void AddElement(MenuElement e) {
			AddElement(e,false,elements.Count);
		}

		/// <summary>
		/// Adds an interface element to the menu
		/// </summary>
		/// <param name="cancel">If true, this button will be pressed if 'B' is read</param>
		public void AddElement(MenuElement e,bool cancel) {
			AddElement(e,cancel,elements.Count);
		}

		public void AddElement(MenuElement e,bool cancel,int index) {
			elements.Insert(index,e);
			e.Y=(e.Height+buffer)*index;	//Space the buttons

			//Move all the other buttons up
			for (int i=index+1;i<elements.Count;i++) {
				MenuElement cur=(MenuElement)elements[i];
				cur.Y+=cur.Height+buffer;
			}

			if (elements.Count==1 && !locked) e.Select();		//If this is the first element, select it

			if (cancel) this.cancel=e;

			Add(e);
		}

		public void RemoveElement(MenuElement e) {
			int index=elements.IndexOf(e);	//Get the index of the element

			if (index>=0) {	//If the element is in the list
				elements.RemoveAt(index);
				Remove(e);

				for (int i=index;i<elements.Count;i++) {
					MenuElement cur=(MenuElement)elements[i];
					cur.Y-=cur.Height+buffer;
				}
			}

			//Keep index in bounds
			index=Math.Min(index,elements.Count-1);
		}

		public void ClearElements() {
			foreach (MenuElement e in elements) Remove(e);
			elements.Clear();
			index=0;
		}

		public int Index {
			get { return index; }
		}

		public bool Locked {
			get { return locked; }
		}

		public void Lock() {
			locked=true;
			deselect(index);
		}

		public void Unlock() {
			transfer=true;
			select(index);
		}

		public void Select(int index) {
			deselect(this.index);
			select(index);
			this.index=index;
		}

		private void select(int index) {
			if (elements.Count==0) return;
			((MenuElement)elements[index]).Select();
		}

		private void deselect(int index) {
			if (elements.Count==0) return;
			((MenuElement)elements[index]).Deselect();
		}

		#region Updateable Members

		public void Update(float elapsedTime) {
			if (transfer) transfer=locked=false;
		}

		#endregion
	}
}