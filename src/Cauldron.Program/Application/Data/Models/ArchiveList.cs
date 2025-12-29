using System.Collections.Generic;

namespace StudioCore.Application;

/// <summary>
/// An archive list for finding the split header and data archives present in a game.
/// </summary>
public class ArchiveList
{
    /// <summary>
    /// The entries of this list.
    /// </summary>
    public List<ArchiveListEntry> Entries { get; set; }
}

/// <summary>
/// An archive list entry describing where the header and data files of an archive are.
/// </summary>
public class ArchiveListEntry
{
    /// <summary>
    /// The name of this entry.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Whether or not this entry's archive is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// The header of this entry.
    /// </summary>
    public ArchiveFileEntry Header { get; set; }

    /// <summary>
    /// The data of this entry.
    /// </summary>
    public ArchiveFileEntry Data { get; set; }
}

/// <summary>
/// An archive file entry describing where the archive file is.
/// </summary>
public class ArchiveFileEntry
{
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