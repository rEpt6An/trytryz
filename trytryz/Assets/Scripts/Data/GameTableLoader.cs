using UnityEngine;

/// <summary>
/// Drop exported .json into Resources or reference as TextAsset, then load at runtime.
/// </summary>
public static class GameTableLoader
{
    // ── Items ──────────────────────────────────────────
    public static ItemTable LoadItems(TextAsset asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.text))
            return null;
        return JsonUtility.FromJson<ItemTable>(StripBom(asset.text));
    }

    public static ItemTable LoadItemsFromResources(string pathWithoutExtension)
    {
        var ta = Resources.Load<TextAsset>(pathWithoutExtension);
        return LoadItems(ta);
    }

    // ── Heroes ──────────────────────────────────────────
    public static HeroTable LoadHeroes(TextAsset asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.text))
            return null;
        return JsonUtility.FromJson<HeroTable>(StripBom(asset.text));
    }

    public static HeroTable LoadHeroesFromResources(string pathWithoutExtension)
    {
        var ta = Resources.Load<TextAsset>(pathWithoutExtension);
        return LoadHeroes(ta);
    }

    // ── Enemy Formations ────────────────────────────────
    public static EnemyFormationTable LoadEnemyFormations(TextAsset asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.text))
            return null;
        return JsonUtility.FromJson<EnemyFormationTable>(StripBom(asset.text));
    }

    /// <summary>去掉 UTF-8 BOM，避免 JsonUtility 解析失败。</summary>
    static string StripBom(string text)
    {
        return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
    }

    public static EnemyFormationTable LoadEnemyFormationsFromResources(string pathWithoutExtension)
    {
        var ta = Resources.Load<TextAsset>(pathWithoutExtension);
        return LoadEnemyFormations(ta);
    }

    /// <summary>
    /// Build a 3x3 hero-id grid from formation rows matching the given formationId.
    /// Returns int[3,3] where 0 = empty, otherwise heroId.
    /// </summary>
    public static int[,] BuildFormationGrid(EnemyFormationTable table, int formationId)
    {
        int[,] grid = new int[3, 3];
        if (table?.list == null) return grid;

        foreach (var row in table.list)
        {
            if (row.formationId != formationId) continue;
            if (row.gridX < 0 || row.gridX > 2 || row.gridY < 0 || row.gridY > 2) continue;
            grid[row.gridX, row.gridY] = row.heroId;
        }
        return grid;
    }
}
