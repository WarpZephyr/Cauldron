using System.Collections.Generic;

namespace StudioCore.Application;

/// <summary>
/// A dictionary of files.
/// </summary>
public class FileDictionary
{
    /// <summary>
    /// The entries describing files in this dictionary.
    /// </summary>
    public List<FileDictionaryEntry> Entries { get; set; }
}

/// <summary>
/// A file dictionary entry describing information about a file.
/// </summary>
public class FileDictionaryEntry
{
    /// <summary>
    /// The archive this entry belongs to.
    /// </summary>
    public string Archive { get; set; }

    /// <summary>
    /// The relative path for this entry
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// The folder path for this entry (excludes the filename and extension)
    /// </summary>
    public string Folder { get; set; }

    /// <summary>
    /// The file name for this entry (excludes extension)
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// The extension for this entry (ignoring .dcx)
    /// </summary>
    public string Extension { get; set; }
}