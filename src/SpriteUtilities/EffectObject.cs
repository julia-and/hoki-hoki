using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities {

	/* EffectObject inheritance rules
	 * If an effect has been assigned, it will use that effect regardless of its parent's effect.
	 * If a technique has been assigned, it will be set on the effect before drawing.
	 * If no technique has been assigned, the effect will be used as-is, meaning that the parent's will be inherited.
	 * If no effect has been assigned, the parent's will be used. It is stored in a static variable.
	 * If no effect has been assigned, the parent's FXConstants will be used along with the child's.
	 * If an effect has been assigned, parent FXConstants will not be used. The constant list will be cleared.
	 * Inheriting parent FXConstants means inheriting its parent's and so forth, so long as any were inherited.
	 */

	/// <summary>
	/// A TransformedObject with added support for effect inheritance. It does not
	/// implement drawing with effects, only methods for setting the effect and technique.
	/// </summary>
	public abstract class EffectObject : TransformedObject {
		protected static Effect
			lastEffect;		//Last effect used. Necessary for inheritance - if not overriden by an instance effect, this will be used again.
		protected static ArrayList
			lastConst;		//Last list of constants used. If lastEffect is used, these constants will be passed at draw time. If the effect is changed, the reference is changed to that object's constant list.
		protected ArrayList
			FXConstants,	//FXConstants to use. Parent FXConstants are inherited, up to the point that an effect or technique is changed (so, changing the technique causes constants from higher in the tree not to be inherited)
			allConstants;	//Local FXConstants plus inherited ones. Created in deviceDraw() and set to null at the end of the draw call.
		protected Effect
			effect,			//If not null, overrides parent's effect.
			current;		//The effect to use for drawing. Updated after calling deviceDraw()
		protected string
			technique;		//Technique to use. null is an acceptable value, the effect will be used as-is.

		#region getset
		/// <summary>
		/// The effect to use. If null, and a parent exists, the parent's effect will be used.
		/// </summary>
		public Effect DrawEffect {
			get { return effect; }
			set { effect=value; }
		}

		public Effect InheritedEffect {
			get {
				/* If there is an effect, it will override parent's - return it
				 * If a parent exists and there is no override, use the parent's
				 * Otherwise, return null */
				if (effect!=null) return effect;
				if (parent is EffectObject) return ((EffectObject)parent).InheritedEffect;
				return null;
			}
		}

		/// <summary>
		/// The technique to use. If null, the effect's will be used without setting the technique.
		/// </summary>
		public string Technique {
			get { return technique; }
			set { technique=value; }
		}
		#endregion

		public EffectObject(GraphicsDevice device) : base(device) {
			FXConstants=new ArrayList();
		}

		static EffectObject() {
			lastConst=new ArrayList();
		}

		#region fxc list
		public void AddEffectConstant(string constantName,ConstType constantType) {
			FXConstants.Add(new FXConstant(constantName,constantType));
		}

		public void RemoveEffectConstant(string constantName) {
			foreach (FXConstant c in FXConstants)
				if (c.Name==constantName)
					FXConstants.Remove(c);
		}

		public void RemoveEffectConstant(ConstType constantType) {
			foreach (FXConstant c in FXConstants)
				if (c.Type==constantType)
					FXConstants.Remove(c);
		}
		#endregion

		#region drawing
		public override void Draw(Matrix parentMatrix,Vector2 parentShift) {
			base.Draw(parentMatrix,parentShift);
			allConstants=null;	//Prevent memory leakage
		}

		protected override void deviceDraw(Matrix trans) {
			//Effect inheritance
			current=InheritedEffect;

			//FXConstant inheritance
			allConstants=lastConst;
			if (parent==null) allConstants.Clear();	//If there's no parent, should start with a clean list

			//Effect and technique overrides
			if (effect!=null) {
				current=effect;					//Override effect if not null
				allConstants.Clear();			//If effect is overriden, constants must be cleared
				if (technique!=null)
					effect.CurrentTechnique=effect.Techniques[technique];	//Set technique if one exists
			}

			//Add own constants to the inherited ones
			allConstants.AddRange(FXConstants);

			//If there is an effect to use, then set effect constants
			if (current!=null) foreach (FXConstant fxc in allConstants) sendFXC(fxc,trans);
		}

		protected override void drawChild(TransformedObject child,Matrix trans) {
			//Reset the lastConstant list for this inheritance level
			lastConst=allConstants;
			base.drawChild(child,trans);
		}

		#endregion

		/// <summary>
		/// Sets an effect constant
		/// </summary>
		/// <param name="fxc">Description (name, type of data) of the constant</param>
		protected abstract void sendFXC(FXConstant fxc,Matrix trans);

		/// <summary>
		/// Maps an effect constant name to a SpriteObject variable
		/// </summary>
		protected struct FXConstant {
			/// <summary>
			/// The name of the effect constant
			/// </summary>
			public string Name;
			/// <summary>
			/// The data that should be passed to the constant
			/// </summary>
			public ConstType Type;

			public FXConstant(string Name,ConstType Type) {
				this.Name=Name;
				this.Type=Type;
			}
		}
	}
}

public enum ConstType {
	WorldMatrix,
	Texture,
	/// <summary>
	/// Passes color as a 4-component vector
	/// </summary>
	Color
}
