namespace StudioCore.Editors.Viewport;

public class ViewportShortcuts
{
    public Viewport Parent;
    public Cauldron BaseEditor;

    public ViewportShortcuts(Cauldron baseEditor, Viewport parent)
    {
        this.BaseEditor = baseEditor;
        Parent = parent;
    }

    public void Update()
    {

    }
}

