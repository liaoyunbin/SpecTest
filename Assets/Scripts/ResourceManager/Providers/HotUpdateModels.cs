using System;
using System.Collections.Generic;

[Serializable]
public class VersionInfo
{
    public string version;
    public string downloadUrl;
}

[Serializable]
public class FileEntry
{
    public string name;
    public string md5;
    public long size;
}

[Serializable]
public class FileList
{
    public List<FileEntry> files = new List<FileEntry>();
}
