using StudioCore.Application;
using StudioCore.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudioCore.Editors.TextureViewer;

public class TextureData
{
    public Cauldron BaseEditor;
    public ProjectEntry Project;

    public TextureBank PrimaryBank;
    public TextureBank PreviewBank;

    public FileDictionary TextureFiles = new();

    public FileDictionary TexturePackedFiles = new();

    public FileDictionary ShoeboxFiles = new();

    public TextureData(Cauldron baseEditor, ProjectEntry project)
    {
        BaseEditor = baseEditor;
        Project = project;
    }

    public async Task<bool> Setup()
    {
        await Task.Yield();

        SetupTextureDictionaries();
        SetupPackedTextureDictionaries();
        SetupShoeboxDictionaries();

        PrimaryBank = new("Primary", BaseEditor, Project, Project.FS);

        Task<bool> primaryChrBankTask = PrimaryBank.Setup();
        bool primaryChrBankTaskResult = await primaryChrBankTask;

        if (!primaryChrBankTaskResult)
        {
            TaskLogs.AddLog($"[{Project.ProjectName}:Texture Viewer] Failed to setup Primary Texture bank.");
        }

        PreviewBank = new("Preview", BaseEditor, Project, Project.FS);

        Task<bool> previewBankTask = PreviewBank.Setup();
        bool previewBankTaskResult = await previewBankTask;

        if (!previewBankTaskResult)
        {
            TaskLogs.AddLog($"[{Project.ProjectName}:Texture Viewer] Failed to setup Preview Texture bank.");
        }

        return true;
    }

    public void SetupTextureDictionaries()
    {
        var secondaryDicts = new List<FileDictionary>();

        // TPF
        var baseDict = new FileDictionary();
        baseDict.Entries = Project.FileDictionary.Entries
            .Where(e => e.Archive != "sd")
            .Where(e => e.Extension == "tpf")
            .ToList();

        // Object Textures
        var objDict = new FileDictionary();
        objDict.Entries = Project.FileDictionary.Entries
            .Where(e => e.Archive != "sd")
            .Where(e => e.Extension == "objbnd")
            .ToList();

        if (Project.ProjectType == ProjectType.DS2S || Project.ProjectType == ProjectType.DS2)
        {
            objDict.Entries = Project.FileDictionary.Entries
                .Where(e => e.Archive != "sd")
                .Where(e => e.Extension == "bnd" && e.Folder == "/model/obj")
                .ToList();
        }

        if (Project.ProjectType == ProjectType.ACFA)
        {
            objDict.Entries = Project.FileDictionary.Entries
                .Where(e => e.Filename.EndsWith("_t") && e.Extension == "bnd" && e.Folder.StartsWith("/model/obj"))
                .ToList();
        }

        if (Project.ProjectType == ProjectType.ACV || Project.ProjectType == ProjectType.ACVD)
        {
            objDict.Entries = Project.FileDictionary.Entries
                .Where(e => (e.Extension == "tpf") && e.Folder.StartsWith("/model/obj"))
                .ToList();
        }

        secondaryDicts.Add(objDict);

        // Ene Textures
        var eneDict = new FileDictionary();
        eneDict.Entries = Project.FileDictionary.Entries
            .Where(e => e.Archive != "sd")
            .Where(e => e.Extension == "texbnd")
            .ToList();

        if (Project.ProjectType == ProjectType.ACFA)
        {
            eneDict.Entries = Project.FileDictionary.Entries
                .Where(e => e.Filename.EndsWith("_t") && e.Extension == "bnd" && e.Folder.StartsWith("/model/ene"))
                .ToList();
        }

        if (Project.ProjectType == ProjectType.ACV || Project.ProjectType == ProjectType.ACVD)
        {
            eneDict.Entries = Project.FileDictionary.Entries
                .Where(e => (e.Extension == "tpf") && e.Folder.StartsWith("/model/ene"))
                .ToList();
        }

        secondaryDicts.Add(eneDict);

        // Map Textures
        var mapDict = new FileDictionary
        {
            Entries = []
        };

        if (Project.ProjectType == ProjectType.ACFA)
        {
            mapDict.Entries = Project.FileDictionary.Entries
                .Where(e => e.Folder.StartsWith("/model/map") && e.Filename.EndsWith("_t") && e.Extension == "bnd")
                .ToList();
        }

        if (Project.ProjectType == ProjectType.ACV || Project.ProjectType == ProjectType.ACVD)
        {
            mapDict.Entries = Project.FileDictionary.Entries
                .Where(e => e.Folder.StartsWith("/model/map") && e.Filename.EndsWith("_htdcx") && e.Extension == "bnd")
                .ToList();
        }

        secondaryDicts.Add(mapDict);

        // Part Textures
        var partDict = new FileDictionary();
        partDict.Entries = Project.FileDictionary.Entries
            .Where(e => e.Archive != "sd")
            .Where(e => e.Extension == "partsbnd")
            .ToList();

        if (Project.ProjectType == ProjectType.ACFA)
        {
            partDict.Entries = Project.FileDictionary.Entries
                .Where(e => (e.Folder.StartsWith("/model/ac/") || e.Folder.StartsWith("/model/garage")) && e.Filename.EndsWith("_t") && e.Extension == "bnd")
                .ToList();

            secondaryDicts.Add(partDict);
        }
        else if (Project.ProjectType == ProjectType.ACV || Project.ProjectType == ProjectType.ACVD)
        {
            partDict.Entries = Project.FileDictionary.Entries
                .Where(e => e.Folder.StartsWith("/model/ac/") && e.Extension == "tpf")
                .ToList();

            secondaryDicts.Add(partDict);
        }
        else if (Project.ProjectType == ProjectType.DS2S || Project.ProjectType == ProjectType.DS2)
        {
            var commonPartDict = new FileDictionary();
            commonPartDict.Entries = Project.FileDictionary.Entries
                .Where(e => e.Archive != "sd")
                .Where(e => e.Extension == "commonbnd")
                .ToList();

            secondaryDicts.Add(commonPartDict);

            partDict.Entries = Project.FileDictionary.Entries
                .Where(e => e.Archive != "sd")
                .Where(e => e.Extension == "bnd" && e.Folder.Contains("/model/parts"))
                .ToList();

            secondaryDicts.Add(partDict);
        }
        else
        {
            secondaryDicts.Add(partDict);
        }

        // SFX Textures
        var sfxDict = new FileDictionary();
        sfxDict.Entries = Project.FileDictionary.Entries
            .Where(e => e.Archive != "sd")
            .Where(e => e.Extension == "ffxbnd")
            .ToList();

        secondaryDicts.Add(sfxDict);

        // Merge all unique entries from the secondary dicts into the base dict to form the final dictionary
        TextureFiles = ProjectUtils.MergeFileDictionaries(baseDict, secondaryDicts);
    }

    public void SetupPackedTextureDictionaries()
    {
        TexturePackedFiles.Entries = Project.FileDictionary.Entries
            .Where(e => e.Archive != "sd")
            .Where(e => e.Extension == "tpfbhd")
            .ToList();
    }

    public void SetupShoeboxDictionaries()
    {
        ShoeboxFiles.Entries = Project.FileDictionary.Entries
            .Where(e => e.Archive != "sd")
            .Where(e => e.Extension == "sblytbnd")
            .ToList();
    }
}

