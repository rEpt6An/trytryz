using UnityEngine;

/// <summary>
/// Drop exported .json into Resources or reference as TextAsset, then load at runtime.
/// </summary>
public static class GameTableLoader
{
    public static ItemTable LoadItems(TextAsset asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.text))
            return null;
        return JsonUtility.FromJson<ItemTable>(asset.text);
    }

    public static ItemTable LoadItemsFromResources(string pathWithoutExtension)
    {
        var ta = Resources.Load<TextAsset>(pathWithoutExtension);
        return LoadItems(ta);
    }
}
