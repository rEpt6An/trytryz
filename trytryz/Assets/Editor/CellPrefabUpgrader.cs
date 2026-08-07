using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool: upgrade Cell_of_Board prefab to include HP bar and ATK bar.
/// Menu: Trytryz -> Upgrade Cell Prefab (Add Progress Bars)
/// </summary>
public class CellPrefabUpgrader
{
    const string PREFAB_PATH = "Assets/Resources/Prefabs/Cell_of_Board.prefab";

    [MenuItem("Trytryz/Upgrade Cell Prefab (Add Progress Bars)")]
    static void Upgrade()
    {
        // Load prefab contents for editing
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (prefabRoot == null)
        {
            Debug.LogError("[CellUpgrade] Prefab not found at: " + PREFAB_PATH);
            return;
        }

        // Check if already upgraded
        var existingBars = prefabRoot.transform.Find("Bars");
        if (existingBars != null)
        {
            Debug.Log("[CellUpgrade] Bars already exist, checking references...");
        }

        var cellSlot = prefabRoot.GetComponent<CellSlot>();
        if (cellSlot == null)
        {
            Debug.LogError("[CellUpgrade] CellSlot component not found on prefab root!");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // Remove old Bars if exists
        if (existingBars != null)
            Object.DestroyImmediate(existingBars.gameObject);

        // === Create Bars Root ===
        var barsRoot = CreateChild(prefabRoot.transform, "Bars");
        var barsRt = barsRoot.GetComponent<RectTransform>();
        barsRt.anchorMin = new Vector2(0, 0);
        barsRt.anchorMax = new Vector2(1, 0.12f);
        barsRt.offsetMin = Vector2.zero;
        barsRt.offsetMax = Vector2.zero;

        // === HP Bar Background ===
        var hpBg = CreateChild(barsRoot.transform, "HPBarBg");
        var hpBgRt = hpBg.GetComponent<RectTransform>();
        hpBgRt.anchorMin = new Vector2(0, 0.55f);
        hpBgRt.anchorMax = new Vector2(1, 1);
        hpBgRt.offsetMin = new Vector2(4, 1);
        hpBgRt.offsetMax = new Vector2(-4, -1);
        var hpBgImg = hpBg.AddComponent<Image>();
        hpBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // === HP Bar Fill ===
        var hpFill = CreateChild(hpBg.transform, "HPBarFill");
        var hpFillRt = hpFill.GetComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.offsetMin = Vector2.zero;
        hpFillRt.offsetMax = Vector2.zero;
        hpFillRt.pivot = new Vector2(0, 0.5f);
        var hpFillImg = hpFill.AddComponent<Image>();
        hpFillImg.type = Image.Type.Filled;
        hpFillImg.fillMethod = Image.FillMethod.Horizontal;
        hpFillImg.fillOrigin = 0;
        hpFillImg.fillAmount = 1f;
        hpFillImg.color = new Color(0.2f, 0.8f, 0.2f);

        // === ATK Bar Background ===
        var atkBg = CreateChild(barsRoot.transform, "ATKBarBg");
        var atkBgRt = atkBg.GetComponent<RectTransform>();
        atkBgRt.anchorMin = new Vector2(0, 0);
        atkBgRt.anchorMax = new Vector2(1, 0.45f);
        atkBgRt.offsetMin = new Vector2(4, 1);
        atkBgRt.offsetMax = new Vector2(-4, -1);
        var atkBgImg = atkBg.AddComponent<Image>();
        atkBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // === ATK Bar Fill ===
        var atkFill = CreateChild(atkBg.transform, "ATKBarFill");
        var atkFillRt = atkFill.GetComponent<RectTransform>();
        atkFillRt.anchorMin = Vector2.zero;
        atkFillRt.anchorMax = Vector2.one;
        atkFillRt.offsetMin = Vector2.zero;
        atkFillRt.offsetMax = Vector2.zero;
        atkFillRt.pivot = new Vector2(0, 0.5f);
        var atkFillImg = atkFill.AddComponent<Image>();
        atkFillImg.type = Image.Type.Filled;
        atkFillImg.fillMethod = Image.FillMethod.Horizontal;
        atkFillImg.fillOrigin = 0;
        atkFillImg.fillAmount = 1f;
        atkFillImg.color = new Color(0.2f, 0.5f, 1f);

        // === Wire up CellSlot references ===
        cellSlot.hpBarFill = hpFillImg;
        cellSlot.atkBarFill = atkFillImg;
        cellSlot.barsRoot = barsRoot;

        EditorUtility.SetDirty(cellSlot);

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[CellUpgrade] Cell_of_Board prefab upgraded with HP + ATK progress bars!");
        AssetDatabase.Refresh();
    }

    static GameObject CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return go;
    }
}