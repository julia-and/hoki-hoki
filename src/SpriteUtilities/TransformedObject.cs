using System;
using System.Collections;
using FloatMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities;


/* NOTE: It is highly likely that the GlobalToLocal function isn't
	 * completely working right now, particularly in cases where the
	 * TransformedObject has an origin other than 0,0 and its parent
	 * is scaled. This needs to be looked into ASAP. */

/// <summary>
/// An object on a 2D plane with various transformations applied to it.
/// </summary>
public abstract class TransformedObject
{
    #region vars
    protected TransformedObject parent; //Parent SpriteObject (or null if none exists)
    protected GraphicsDevice device;    //Store the device for later use
    protected ArrayList children;   //List of child TransformedObjects

    protected Vector2
        position,   //Coordinates relative to parent
        scale,      //Local scale
        origin;     //Point to scale and rotate about
    protected float
        rotation,   //Rotation relative to parent
        localAlpha, //Local opacity 0-255
        alpha;      //Absolute opacity 0-255
    protected bool
        visible,    //If false, calls to Draw() will be skipped
        bufferNeedsUpdate,  //If true, will reset the buffer data before drawing
        colorNeedsUpdate;   //If true, will reset the vertices' color before drawing
    protected Color
        localColor, //Local tint
        color;      //Absolute tint

    #region const
    public const float MaxAlpha = 255; //Highest allowed value for alpha
    #endregion

    #endregion

    #region getset

    /// <summary>
    /// Local X-coordinate
    /// </summary>
    virtual public float X
    {
        get { return position.X; }
        set { position.X = value; }
    }

    /// <summary>
    /// Local Y-coordinate
    /// </summary>
    virtual public float Y
    {
        get { return position.Y; }
        set { position.Y = value; }
    }

    virtual public Vector2 Position
    {
        get { return new Vector2(X, Y); }
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    /// <summary>
    /// Horizontal scale as a decimal percentage
    /// </summary>
    virtual public float XScale
    {
        get { return scale.X; }
        set { scale.X = value; }
    }

    /// <summary>
    /// Vertical scale as a decimal percentage
    /// </summary>
    virtual public float YScale
    {
        get { return scale.Y; }
        set { scale.Y = value; }
    }

    /// <summary>
    /// Rotation in radians
    /// </summary>
    virtual public float Rotation
    {
        get { return rotation; }
        set { rotation = value; }
    }

    /// <summary>
    /// Whether the object and its children are drawn
    /// </summary>
    virtual public bool Visible
    {
        get { return visible; }
        set { visible = value; }
    }

    virtual public TransformedObject Parent
    {
        get { return parent; }
        set { this.parent = value; }
    }

    /// <summary>
    /// Point about which the sprite is rotated, in terms of displacement from the top-left corner
    /// </summary>
    virtual public Vector2 Origin
    {
        get { return origin; }
        set { origin = value; }
    }

    /// <summary>
    /// Colors the sprite (a value of Color.White leaves it unchanged)
    /// </summary>
    virtual public Color Tint
    {
        get { return color; }
        set
        {
            localColor = value;
            colorNeedsUpdate = true;
        }
    }

    /// <summary>
    /// Opacity (0.0f to 255.0f)
    /// </summary>
    virtual public float Alpha
    {
        get { return localAlpha; }
        set
        {
            localAlpha = FMath.Clamp(value, 0, MaxAlpha);
            colorNeedsUpdate = true;
        }
    }

    virtual public float AbsoluteAlpha
    {
        get { return alpha; }
    }

    /// <summary>
    /// Position to draw at.
    /// </summary>
    virtual protected Vector2 coords
    {
        get { return position; }
    }

    #endregion

    #region constructor
    public TransformedObject(GraphicsDevice device)
    {
        //Store the device
        this.device = device;

        //Construct an ArrayList to hold child sprites
        children = new ArrayList();
        parent = null; //By default the TransformedObject has no parent

        //Set default values
        position = new Vector2();
        origin = new Vector2();
        scale = new Vector2(1.0f, 1.0f);
        alpha = localAlpha = 255;
        color = localColor = Color.White;
        rotation = 0;
        visible = true;
    }
    #endregion

    #region childlist
    /// <summary>
    /// Add a child SpriteObject to the end of the list
    /// </summary>
    virtual public void Add(TransformedObject child)
    {
        Add(child, children.Count);
    }

    /// <summary>
    /// Add a child SpriteObject to a particular part of the list
    /// </summary>
    virtual public void Add(TransformedObject child, int index)
    {
        if (child == this) throw new InvalidOperationException("A SpriteObject cannot be its own parent.");
        if (child.parent != null) throw new InvalidOperationException("The specified TransformedObject already has a parent.");
        children.Insert(index, child);
        child.parent = this;
    }

    /// <summary>
    /// Remove a child SpriteObject.
    /// </summary>
    /// <param name="child"></param>
    virtual public void Remove(TransformedObject child)
    {
        if (children.Contains(child))
        {
            children.Remove(child);
            child.parent = null;
        }
        else throw new InvalidOperationException("The specified SpriteObject is not of child of this SpriteObject");
    }

    virtual public void Clear()
    {
        foreach (TransformedObject child in children) child.parent = null;
        children.Clear();
    }

    /// <summary>
    /// Returns the count of child SpriteObjects
    /// </summary>
    virtual public int Count()
    {
        return children.Count;
    }

    /// <summary>
    /// See if this SpriteObject contains a given child
    /// </summary>
    virtual public bool Contains(TransformedObject child)
    {
        return children.Contains(child);
    }

    #endregion

    #region transformations
    public Matrix OriginTransform()
    {
        return MatrixUtil.Transformation2D(Vector2.Zero, 0, new Vector2(1, 1), Vector2.Zero, 0, Vector2.Multiply(origin, -1));
    }

    public Matrix AbsoluteTransform()
    {
        if (parent == null) return LocalTransform(); //If there is no parent, the absolute Matrix is the same as the local one
        else return Matrix.Multiply(parent.AbsoluteTransform(), LocalTransform()); //Return own transform times parent's
    }

    protected Matrix LocalTransform()
    {
        return MatrixUtil.Transformation2D(origin, 0.0f, scale, Vector2.Zero, rotation, coords);
    }

    /// <summary>
    /// Converts a point in screen coordinates to local coordinate space in place
    /// </summary>
    /// <param name="globalPoint">A point in screen coordinates</param>
    public void GlobalToLocal(ref Vector2 globalPoint)
    {
        //Make a stack to hold all ancestor objects in order (this, parent, grandparent, etc.)
        Stack ancestors = new Stack();

        //Fill the stack
        TransformedObject current = this;
        while (current != null)
        {
            ancestors.Push(current);
            current = current.parent;
        }

        //Work through the stack
        while (ancestors.Count > 0)
        {
            current = (TransformedObject)ancestors.Pop();
            globalPoint = Vector2.Transform(globalPoint, Matrix.Invert(current.LocalTransform()));
        }
    }

    /// <summary>
    /// Converts a point in local coordinate space to screen coordinates in place
    /// </summary>
    public void LocalToGlobal(ref Vector2 localPoint)
    {
        TransformedObject current = this;
        while (current != null)
        {
            localPoint = Vector2.Transform(localPoint, current.LocalTransform());
            current = current.parent;
        }
    }

    /// <summary>
    /// Converts a point in local coordinate space to the parent's coordinate space in place
    /// </summary>
    public void LocalToParent(ref Vector2 localPoint)
    {
        localPoint = Vector2.Transform(localPoint, LocalTransform());
    }

    /// <summary>
    /// Converts a point in the parent's coordinate space to local coordinate space in place
    /// </summary>
    public void ParentToLocal(ref Vector2 parentPoint)
    {
        parentPoint = Vector2.Transform(parentPoint, Matrix.Invert(LocalTransform()));
    }

    /// <summary>
    /// Converts a point in local coordinate space to another object's coordinate space
    /// </summary>
    /// <param name="localPoint">A point in local coordinate space</param>
    /// <param name="targetSpace">The TransformedObject whose coordinate space the point should be converted to</param>
    public void LocalToLocal(ref Vector2 localPoint, TransformedObject targetSpace)
    {
        LocalToGlobal(ref localPoint);
        targetSpace.GlobalToLocal(ref localPoint);
    }

    /// <summary>
    /// Finds the origin shift for this SpriteObject instance
    /// </summary>
    private Vector2 OriginShift()
    {
        return new Vector2(origin.X * scale.X, origin.Y * scale.Y);
    }

    /// <summary>
    /// Makes sure that
    /// </summary>
    virtual protected void checkUpdates()
    {
        if (colorNeedsUpdate)
        {
            //Blend alpha and tint
            float parentAlpha;
            if (parent == null) parentAlpha = MaxAlpha; else parentAlpha = parent.AbsoluteAlpha;
            alpha = localAlpha * parentAlpha / MaxAlpha;
            color = localColor; //Need to add color blending
        }
    }
    #endregion

    #region drawing
    /// <summary>
    /// Draw the SpriteObject and all of its children
    /// </summary>
    virtual public void Draw()
    {
        if (!visible) return;
        Draw(Matrix.Identity, Vector2.Zero);
    }

    /// <summary>
    /// Draws the SpriteObject using passed transformation values
    /// </summary>
    /// <param name="parentMatrix">The parent SpriteObject's absolute transformation</param>
    /// <param name="parentShift">The parent SpriteObject's origin shift</param>
    virtual public void Draw(Matrix parentMatrix, Vector2 parentShift)
    {
        if (!visible) return;

        //Make sure that the combined Tint/Alpha has been calculated and that the buffer contains up-to-date vertices
        checkUpdates();

        //Don't use AbsoluteTransform(), this way is faster since the whole hierarchy is traversed anyway
        Matrix trans = LocalTransform() * parentMatrix; //This matrix is only the object's transformations, and can be passed to children
        Matrix texTrans = OriginTransform() * trans;    //This matrix is effectively for the drawn object's coordinate space, and can be used for drawing

        //Have the device draw the quad
        deviceDraw(texTrans);

        //Draw all children, and instruct them to update their colors if they must inherit a change from this object
        foreach (TransformedObject child in children)
        {
            drawChild(child, trans);
        }

        //By the end of this method, any updates that were necessary have been handled
        colorNeedsUpdate = false;
    }

    /// <summary>
    /// Draws the object on the device. Draw() essentially just traverses the SpriteObject hierarchy and calculates
    /// transformation matrices along the way, but the code that handles actually getting the Sprite onto the screen
    /// is contained in this method. This way, it is easier for derived classes to replace the drawing functionality.
    /// </summary>
    /// <param name="trans">Absolute transformation matrix</param>
    protected abstract void deviceDraw(Matrix trans);

    /// <summary>
    /// Calls each child's draw method.
    /// </summary>
    /// <param name="child"></param>
    virtual protected void drawChild(TransformedObject child, Matrix trans)
    {
        if (colorNeedsUpdate) child.colorNeedsUpdate = true;    //Local color updated, so children must re-blend
        child.Draw(trans, origin);                          //Call the child's draw method
    }

    #endregion
}
