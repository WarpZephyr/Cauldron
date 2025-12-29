using Andre.IO.VFS;
using Microsoft.Extensions.Logging;
using SoulsFormats;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StudioCore.Application;

public class ProjectUtils
{
    public static string GetGameDirectory(ProjectEntry curProject)
    {
        return GetGameDirectory(curProject.ProjectType);
    }

    public static string GetGameDirectory(ProjectType curProjectType)
    {
        switch (curProjectType)
        {
            case ProjectType.Undefined:
                return "NONE";
            case ProjectType.ACFA:
                return "ACFA";
            case ProjectType.ACV:
                return "ACV";
            case ProjectType.ACVD:
                return "ACVD";
            default:
                throw new Exception("Game type not set");
        }
    }

    public static string GetPlatformDirectory(ProjectEntry curProject)
    {
        return GetPlatformDirectory(curProject.ProjectPlatform);
    }

    public static string GetPlatformDirectory(ProjectPlatform curProjectPlatform)
    {
        switch (curProjectPlatform)
        {
            case ProjectPlatform.Undefined:
                return "NONE";
            case ProjectPlatform.PC:
                return "DES";
            case ProjectPlatform.PS3:
                return "PS3";
            case ProjectPlatform.Xbox360:
                return "Xbox360";
            default:
                throw new Exception("Platform type not set");
        }
    }

    public static ProjectPlatform[] GetSupportedPlatforms(ProjectType curProjectType)
    {
        switch (curProjectType)
        {
            case ProjectType.Undefined:
                return [];
            case ProjectType.ACFA:
            case ProjectType.ACV:
            case ProjectType.ACVD:
                return [ProjectPlatform.PS3, ProjectPlatform.Xbox360];
            default:
                throw new Exception("Platform type not set");
        }
    }

    public static void DeleteProject(Cauldron editor, ProjectEntry curProject)
    {
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Delete the project file
        var filename = Path.Join(localAppDataPath, "Cauldron", "Projects", $"{curProject.ProjectGUID}.json");
        if (File.Exists(filename))
        {
            File.Delete(filename);
        }

        // Unload the project editor stuff
        editor.ProjectManager.SelectedProject = null;
        editor.ProjectManager.Projects.Remove(curProject);
    }

    public static string GetBaseFolder()
    {
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Join(localAppDataPath, "Cauldron");
    }

    public static string GetConfigurationFolder()
    {
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Join(localAppDataPath, "Cauldron", "Configuration");
    }
    public static string GetThemeFolder()
    {
        return Path.Join(AppContext.BaseDirectory, "Assets", "Themes");
    }

    public static string GetProjectsFolder()
    {
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Join(localAppDataPath, "Cauldron", "Projects");
    }

    public static string GetLocalProjectFolder(ProjectEntry project)
    {
        return Path.Join(project.ProjectPath, ".cauldron", "Project");
    }

    public static List<string> GetLooseParamsInDir(VirtualFileSystem fs, string dir)
    {
        List<string> looseParams = new();

        string paramDir = Path.Combine(dir, "Param");
        looseParams.AddRange(fs.GetFileNamesWithExtensions(paramDir, ".param"));

        return looseParams;
    }

    /// <summary>
    /// Build a FileDictionary from a source path
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static FileDictionary BuildFromSource(string sourcePath, FileDictionary existingDict, ProjectType type)
    {
        var fileDict = new FileDictionary();
        fileDict.Entries = new();

        if (!Directory.Exists(sourcePath))
        {
            TaskLogs.AddLog($"[Cauldron] Source path not found: {sourcePath}", LogLevel.Error, LogPriority.High);
            return fileDict;
        }

        var allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);

        string archiveName = new DirectoryInfo(sourcePath).Name;

        // Use HashSet for fast lookup of existing relative paths (normalized to forward slashes)
        var existingPaths = new HashSet<string>(
            existingDict.Entries.Select(e => e.Path.Replace('\\', '/')),
            StringComparer.OrdinalIgnoreCase);

        // Filter Witchy unpacked directories
        Dictionary<string, bool> confirmedManifestDict = new();
        var groupedByDir = allFiles.GroupBy(f => Path.GetDirectoryName(f)).ToList();
        groupedByDir = groupedByDir.Where(g =>
        {
            if (g.Key == null) return false;
            var dir = g.Key;
            while (dir != null)
            {
                if (confirmedManifestDict.TryGetValue(dir, out var result) && result) return false;
                var manifestFiles = Directory.GetFiles(dir, "_witchy-*.xml");
                confirmedManifestDict.TryAdd(dir, manifestFiles.Any());
                if (manifestFiles.Any()) return false;
                dir = Path.GetDirectoryName(dir);
            }
            return true;
        }).ToList();

        allFiles = groupedByDir.SelectMany(g => g).ToArray();

        foreach (var filePath in allFiles)
        {
            string relativePath = @$"/{Path.GetRelativePath(sourcePath, filePath).Replace('\\', '/')}";

            // Skip if already present
            if (existingPaths.Contains(relativePath))
                continue;

            string folder = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? "";
            string fileName = Path.GetFileNameWithoutExtension(filePath.Replace('\\', Path.DirectorySeparatorChar));
            string extension = Path.GetExtension(filePath.Replace('\\', Path.DirectorySeparatorChar))?.TrimStart('.').ToLower();

            // Special handling: if file ends with .dcx, strip both extensions (e.g., .bnd.dcx → .bnd)
            if (extension == "dcx")
            {
                string noDcx = Path.GetFileNameWithoutExtension(fileName);
                string prevExt = Path.GetExtension(fileName)?.TrimStart('.').ToLower();
                if (!string.IsNullOrEmpty(prevExt))
                {
                    fileName = Path.GetFileNameWithoutExtension(fileName);
                    extension = prevExt;
                }
                else
                {
                    extension = "dcx"; // fallback if no prior extension
                }
            }

            if (type == ProjectType.ER)
            {
                if (folder.ToLower().StartsWith("/menu/deploy")) continue; // Gideon folders
                if (extension == "matbinbnd") continue; // There are no custom MATBINBNDs
            }

            fileDict.Entries.Add(new FileDictionaryEntry
            {
                Archive = archiveName,
                Path = relativePath,
                Folder = folder,
                Filename = fileName,
                Extension = extension
            });
        }

        return fileDict;
    }

    public static FileDictionary MergeFileDictionaries(FileDictionary first, FileDictionary second)
    {
        var combined = new FileDictionary();
        combined.Entries = new();

        // Normalize and track unique paths
        var seenPaths = new HashSet<string>(
            first.Entries
                 .Select(e => NormalizePath(e.Path))
                 .Where(p => p != null),
            StringComparer.OrdinalIgnoreCase);

        combined.Entries.AddRange(first.Entries);

        foreach (var entry in second.Entries)
        {
            var normalizedPath = NormalizePath(entry.Path);
            if (normalizedPath != null && !seenPaths.Contains(normalizedPath))
            {
                combined.Entries.Add(entry);
                seenPaths.Add(normalizedPath);
            }
        }

        combined.Entries = combined.Entries.OrderBy(e => e.Filename).ToList();
        return combined;
    }

    public static FileDictionary MergeFileDictionaries(FileDictionary first, List<FileDictionary> secondaries)
    {
        var combined = new FileDictionary();
        combined.Entries = new();

        // Normalize and track unique paths
        var seenPaths = new HashSet<string>(
            first.Entries
                 .Select(e => NormalizePath(e.Path))
                 .Where(p => p != null),
            StringComparer.OrdinalIgnoreCase);

        combined.Entries.AddRange(first.Entries);

        foreach (var dict in secondaries)
        {
            foreach (var entry in dict.Entries)
            {
                var normalizedPath = NormalizePath(entry.Path);
                if (normalizedPath != null && !seenPaths.Contains(normalizedPath))
                {
                    combined.Entries.Add(entry);
                    seenPaths.Add(normalizedPath);
                }
            }
        }

        return combined;
    }

    public static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : path.Trim().Replace('\\', '/'); // normalize separators and trim
    }

    public static void RemoveMissingFiles(VirtualFileSystem fs, FileDictionary dict)
    {
        for (int i = dict.Entries.Count - 1; i >= 0; i--)
        {
            var entry = dict.Entries[i];
            if (!fs.FileExists(entry.Path))
            {
                dict.Entries.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Gets the VirtualFileSystem we should use for writes.
    /// If no suitable VFS is found, throws InvalidOperationException.
    /// </summary>
    /// <returns></returns>
    public static VirtualFileSystem GetFilesystemForWrite(ProjectEntry curProject)
    {
        if (curProject.ProjectFS is not EmptyVirtualFileSystem)
            return curProject.ProjectFS;

        if (curProject.VanillaRealFS is not EmptyVirtualFileSystem)
            return curProject.VanillaRealFS;

        throw new InvalidOperationException("No suitable VFS was found for writes");
    }

    public static void CreateBackupFolder(ProjectEntry curProject)
    {
        if(CFG.Current.BackupProcessType is ProjectBackupBehaviorType.Complete)
        {
            var folderPath = Path.Combine(curProject.ProjectPath, ".backup");

            if(!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }
    }

    public static void WriteWithBackup<T>(ProjectEntry curProject, string assetPath, T item,
        params object[] writeparms) where T : SoulsFile<T>, new()
    {
        WriteWithBackup(curProject, curProject.FS, curProject.ProjectFS, assetPath, item,
            curProject.ProjectType, writeparms);
    }

    public static void WriteWithBackup<T>(ProjectEntry curProject, VirtualFileSystem vanillaFs, VirtualFileSystem toFs, string assetPath,
        T item, ProjectType gameType = ProjectType.Undefined, params object[] writeparms) where T : SoulsFile<T>, new()
    {
        try
        {
            // Make a backup of the original file if a mod path doesn't exist
            if (toFs != curProject.ProjectFS && !toFs.FileExists($"{assetPath}.bak") && toFs.FileExists(assetPath))
            {
                if (CFG.Current.EnableBackupSaves)
                {
                    toFs.Copy(assetPath, $"{assetPath}.bak");
                }
            }

            if (gameType == ProjectType.DS3 && item is BND4 bndDS3)
            {
                toFs.WriteFile(assetPath + ".temp", SFUtil.EncryptDS3Regulation(bndDS3));
            }
            else if (gameType == ProjectType.ER && item is BND4 bndER)
            {
                toFs.WriteFile(assetPath + ".temp", SFUtil.EncryptERRegulation(bndER));
            }
            else if (gameType == ProjectType.NR && item is BND4 bndNR)
            {
                toFs.WriteFile(assetPath + ".temp", SFUtil.EncryptNightreignRegulation(bndNR));
            }
            else if (gameType == ProjectType.AC6 && item is BND4 bndAC6)
            {
                toFs.WriteFile(assetPath + ".temp", SFUtil.EncryptAC6Regulation(bndAC6));
            }
            else if (item is BXF3 or BXF4)
            {
                var bhdPath = $@"{(string)writeparms[0]}";
                if (item is BXF3 bxf3)
                {
                    bxf3.Write(out var bhd, out var bdt);
                    toFs.WriteFile(bhdPath + ".temp", bhd);
                    toFs.WriteFile(assetPath + ".temp", bdt);

                    // Ugly but until I rethink the binder API we need to dispose it before touching the existing files
                    bxf3.Dispose();
                }
                else if (item is BXF4 bxf4)
                {
                    bxf4.Write(out var bhd, out var bdt);
                    toFs.WriteFile(bhdPath + ".temp", bhd);
                    toFs.WriteFile(assetPath + ".temp", bdt);

                    // Ugly but until I rethink the binder API we need to dispose it before touching the existing files
                    bxf4.Dispose();
                }

                if (CFG.Current.EnableBackupSaves)
                {
                    if (toFs.FileExists(bhdPath))
                    {
                        toFs.Copy(bhdPath, bhdPath + ".prev");
                    }
                }

                toFs.Move(bhdPath + ".temp", bhdPath);

                return;
            }
            else
            {
                toFs.WriteFile(assetPath + ".temp", item.Write());
            }

            // Ugly but until I rethink the binder API we need to dispose it before touching the existing files
            if (item is IDisposable d)
            {
                d.Dispose();
            }

            if (CFG.Current.EnableBackupSaves)
            {
                if (toFs.FileExists(assetPath))
                {
                    toFs.Copy(assetPath, assetPath + ".prev");
                }
            }
            toFs.Move(assetPath + ".temp", assetPath);
        }
        catch (Exception e)
        {
            TaskLogs.AddLog($"[{curProject.ProjectName}] Failed to save: {Path.GetFileName(assetPath)} - {e}");
        }
    }

    /// <summary>
    /// These are checks for the editor initializations so they don't appear for project types that don't support them.
    /// </summary>

    public static bool SupportsMapEditor(ProjectType curType)
    {
        return true;
    }

    public static bool SupportsModelEditor(ProjectType curType)
    {
        if (curType is ProjectType.ACFA)
        {
            return false;
        }

        return true;
    }

    public static bool SupportsTextEditor(ProjectType curType)
    {
        return true;
    }

    public static bool SupportsParamEditor(ProjectType curType)
    {
        return true;
    }

    public static bool SupportsGraphicsParamEditor(ProjectType curType)
    {
        if (curType is ProjectType.ACFA
            or ProjectType.ACV 
            or ProjectType.ACVD)
        {
            return false;
        }

        return true;
    }

    public static bool SupportsMaterialEditor(ProjectType curType)
    {
        return true;
    }

    public static bool SupportsTextureViewer(ProjectType curType)
    {
        return true;
    }

    public static bool SupportsFileBrowser(ProjectType curType)
    {
        return true;
    }
}