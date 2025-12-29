using System;
using System.IO;

namespace StudioCore.Application;

public static class ProjectAssetPath
{
    public static string GetAssetsFolder()
        => Path.Join(AppContext.BaseDirectory, "Assets");

    public static string GetFileDictionariesFolder()
        => Path.Join(AppContext.BaseDirectory, "Assets", "File Dictionaries");

    public static string GetFileDictionaryRemovalFolder()
       => Path.Join(AppContext.BaseDirectory, "Assets", "File Dictionary Removals");

    public static string GetArchiveListsFolder()
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Lists");

    public static string GetArchiveDictionariesFolder()
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Dictionaries");

    public static string GetArchiveKeysFolder()
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Keys");



    public static string GetFileDictionariesFolder(ProjectEntry projectEntry)
       => Path.Join(AppContext.BaseDirectory, "Assets", "File Dictionaries", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry));

    public static string GetFileDictionaryRemovalFolder(ProjectEntry projectEntry)
       => Path.Join(AppContext.BaseDirectory, "Assets", "File Dictionary Removals", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry));

    public static string GetArchiveListsFolder(ProjectEntry projectEntry)
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Lists", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry));

    public static string GetArchiveDictionariesFolder(ProjectEntry projectEntry)
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Dictionaries", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry));

    public static string GetArchiveKeysFolder(ProjectEntry projectEntry)
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Keys", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry));



    public static string GetFileDictionaryPath(ProjectEntry projectEntry)
        => Path.Join(AppContext.BaseDirectory, "Assets", "File Dictionaries", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry), "File-Dictionary.json");

    public static string GetFileDictionaryPath(ProjectEntry projectEntry, string name)
        => Path.Join(AppContext.BaseDirectory, "Assets", "File Dictionaries", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry), $"{name}.json");

    public static string GetFileDictionaryRemovalPath(ProjectEntry projectEntry)
        => Path.Join(AppContext.BaseDirectory, "Assets", "File Dictionary Removals", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry), "File-Dictionary-Removal.json");

    public static string GetFileDictionaryRemovalPath(ProjectEntry projectEntry, string name)
        => Path.Join(AppContext.BaseDirectory, "Assets", "File Dictionary Removals", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry), $"{name}.json");

    public static string GetArchiveListPath(ProjectEntry projectEntry)
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Lists", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry), "Archive-List.json");

    public static string GetArchiveDictionaryPath(ProjectEntry projectEntry, string name)
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Dictionaries", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry), $"{name}.txt");

    public static string GetArchiveKeyPath(ProjectEntry projectEntry, string name)
        => Path.Join(AppContext.BaseDirectory, "Assets", "Archive Keys", ProjectUtils.GetGameDirectory(projectEntry), ProjectUtils.GetPlatformDirectory(projectEntry), $"{name}.pem");
}