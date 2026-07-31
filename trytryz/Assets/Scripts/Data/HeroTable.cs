using System;

/// <summary>
/// Wrapper for JSON exported with --unity-json (root object is { "list": [...] }).
/// </summary>
[Serializable]
public class HeroTable
{
    public HeroRow[] list;
}
