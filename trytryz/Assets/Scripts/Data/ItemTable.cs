using System;

/// <summary>
/// Wrapper for JSON exported with --unity-json (root object is { "list": [...] }).
/// </summary>
[Serializable]
public class ItemTable
{
    public ItemRow[] list;
}
