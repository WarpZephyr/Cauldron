using Hexa.NET.ImGui;
using StudioCore.Application;
using StudioCore.Renderer;
using System.Collections.Generic;

namespace StudioCore.Editors.TextureViewer;

public class TexContentView
{
    public TextureViewerScreen Editor;
    public ProjectEntry Project;

    public TexContentView(TextureViewerScreen editor, ProjectEntry project)
    {
        Editor = editor;
        Project = project;
    }

    public void Display()
    {
        ImGui.Begin("Textures##TextureList");
        Editor.Selection.SwitchWindowContext(TextureViewerContext.TextureList);

        Editor.Filters.DisplayTextureFilterSearch();

        ImGui.BeginChild("TextureList");
        Editor.Selection.SwitchWindowContext(TextureViewerContext.TextureList);

        if (Editor.Selection.SelectedTpf != null)
        {
            int index = 0;

            var nameCounts = new Dictionary<string, int>();
            foreach (var entry in Editor.Selection.SelectedTpf.Textures)
            {
                if (Editor.Filters.IsTextureFilterMatch(entry.Name))
                {
                    var displayName = entry.Name;
                    if (!nameCounts.TryAdd(displayName, 1))
                    {
                        int nameCount = nameCounts[displayName];
                        displayName = $"{displayName} ({nameCount})";

                        nameCount++;
                        nameCounts[displayName] = nameCount;
                    }

                    var isSelected = false;
                    if (Editor.Selection.SelectedTextureKey == displayName)
                    {
                        isSelected = true;
                    }

                    // Texture row
                    if (ImGui.Selectable($@"{displayName}##Tex{index}", isSelected))
                    {
                        Editor.Selection.SelectTextureEntry(displayName, entry);
                        TargetIndex = index;
                        LoadTexture = true;
                    }

                    // Arrow Selection
                    if (ImGui.IsItemHovered() && Editor.Selection.SelectTexture)
                    {
                        Editor.Selection.SelectTexture = false;
                        Editor.Selection.SelectTextureEntry(displayName, entry);
                        TargetIndex = index;
                        LoadTexture = true;
                    }
                    if (ImGui.IsItemFocused() && (InputTracker.GetKey(Veldrid.Key.Up) || InputTracker.GetKey(Veldrid.Key.Down)))
                    {
                        Editor.Selection.SelectTexture = true;
                    }
                }

                index++;
            }
        }

        ImGui.EndChild();

        ImGui.End();
    }


    private int TargetIndex = -1;
    public bool LoadTexture = false;

    public void Update()
    {
        if (!Cauldron.LowRequirementsMode)
        {
            if (LoadTexture)
            {
                if (TargetIndex != -1)
                {
                    Editor.Selection.ViewerTextureResource = new TextureResource(Editor.Selection.SelectedTpf, TargetIndex);
                    Editor.Selection.ViewerTextureResource._LoadTexture(AccessLevel.AccessFull);
                }

                LoadTexture = false;
            }
        }
    }
}
