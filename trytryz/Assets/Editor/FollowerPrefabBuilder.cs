using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一键生成随从预制体：
///   1. 升级 Cell_of_Board 预制体（移除旧 HeroOnBoard 缺失组件、隐藏格子自身多余的条/遮罩）。
///   2. 基于 Cell_of_Board 复制生成 Follower 预制体，挂载 FollowerEntity + FollowerDragHandler，
///      包含：立绘、名字、属性、加厚血条（当前/最大）、血条上方攻击倒计时、CD 明暗遮罩。
/// 菜单：Trytryz > Build Follower Prefab (from Cell_of_Board)
/// </summary>
public class FollowerPrefabBuilder
{
    const string CELL_PATH = "Assets/Resources/Prefabs/Cell_of_Board.prefab";
    const string FOLLOWER_PATH = "Assets/Resources/Prefabs/Follower.prefab";

    [MenuItem("Trytryz/Build Follower Prefab (from Cell_of_Board)")]
    public static void Build()
    {
        UpgradeCellPrefab();
        BuildFollowerPrefab();
        AssetDatabase.Refresh();
        Debug.Log("[FollowerPrefabBuilder] Done. Follower.prefab created from Cell_of_Board.prefab.");
    }

    // ── 1. 升级格子预制体 ──
    static void UpgradeCellPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CELL_PATH);
        if (root == null) { Debug.LogError("[FollowerPrefabBuilder] Cell_of_Board not found."); return; }

        root.name = "Cell_of_Board";

        // 删除缺失脚本（旧 HeroOnBoard）与旧的 CellSlot 之外的冗余
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

        // 格子自身只保留：背景 / 高亮 / 坐标文字；条、倒计时、遮罩、卡片由 Follower 提供
        SetActive(root.transform, "CDCountdown", false);
        SetActive(root.transform, "Bars", false);
        SetActive(root.transform, "CDOverlay", false);
        SetActive(root.transform, "HeroCard", false);
        SetActive(root.transform, "InfoText", true);
        SetActive(root.transform, "Highlight", true);

        // 重新整理 CellSlot 引用（新字段）
        var slot = root.GetComponent<CellSlot>();
        if (slot != null)
        {
            slot.background = root.GetComponent<Image>();
            slot.highlightBorder = FindImage(root.transform, "Highlight");
            slot.infoText = FindText(root.transform, "InfoText");
        }

        PrefabUtility.SaveAsPrefabAsset(root, CELL_PATH);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[FollowerPrefabBuilder] Cell_of_Board upgraded.");
    }

    // ── 2. 从 Cell_of_Board 生成 Follower 预制体 ──
    static void BuildFollowerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CELL_PATH);
        if (root == null) { Debug.LogError("[FollowerPrefabBuilder] Cell_of_Board not found."); return; }

        Slider hpSlider = null; // RPG 血条 Slider（先创建组件，最后赋给实体）

        root.name = "Follower";

        // 移除格子专用组件
        var slot = root.GetComponent<CellSlot>();
        if (slot != null) Object.DestroyImmediate(slot);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

        // 移除格子专用元素
        DestroyChild(root.transform, "InfoText");
        DestroyChild(root.transform, "Highlight");
        DestroyChild(root.transform, "CostBadge");

        var heroCard = root.transform.Find("HeroCard");
        if (heroCard != null) DestroyChild(heroCard, "CostText");

        if (heroCard == null)
        {
            heroCard = new GameObject("HeroCard", typeof(RectTransform)).transform;
            heroCard.SetParent(root.transform, false);
        }

        // 激活随从表现元素
        var bars = root.transform.Find("Bars");
        var cd = root.transform.Find("CDCountdown");
        var overlay = root.transform.Find("CDOverlay");
        if (bars != null) bars.gameObject.SetActive(true);
        if (cd != null) cd.gameObject.SetActive(true);
        if (overlay != null) overlay.gameObject.SetActive(true);
        heroCard.gameObject.SetActive(true);

        // 立绘：放在 HeroCard 最底层（先于名字/属性渲染）
        var photoGO = new GameObject("Photo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        photoGO.transform.SetParent(heroCard, false);
        photoGO.transform.SetAsFirstSibling();
        var prt = photoGO.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(6, 6); prt.offsetMax = new Vector2(-6, -6);
        var pimg = photoGO.GetComponent<Image>();
        pimg.color = new Color(0.32f, 0.32f, 0.38f, 1f);
        pimg.raycastTarget = false;
        pimg.preserveAspect = true;

        // 名字（顶部）与属性（中部偏下）
        var nameRT = FindChild(heroCard, "Name");
        if (nameRT != null)
        {
            nameRT.anchorMin = new Vector2(0, 0.78f);
            nameRT.anchorMax = new Vector2(1, 0.97f);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var t = nameRT.GetComponent<TextMeshProUGUI>();
            if (t != null) { t.fontSize = 16; t.fontStyle = FontStyles.Bold; }
        }
        var statsRT = FindChild(heroCard, "Stats");
        if (statsRT != null)
        {
            statsRT.anchorMin = new Vector2(0, 0.52f);
            statsRT.anchorMax = new Vector2(1, 0.74f);
            statsRT.offsetMin = Vector2.zero;
            statsRT.offsetMax = Vector2.zero;
            var t = statsRT.GetComponent<TextMeshProUGUI>();
            if (t != null) t.fontSize = 12;
        }

        // 血条加厚 2 倍（0~20% 高度），内嵌 当前/最大 文字
        if (bars != null)
        {
            var brt = bars.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0.20f);
            brt.offsetMin = new Vector2(4, 2);
            brt.offsetMax = new Vector2(-4, -2);
            var hpText = FindText(bars, "HPText");
            if (hpText != null) hpText.fontSize = 14;
            var hpFill = FindImage(bars, "HPBarFill");
            if (hpFill != null)
            {
                hpFill.type = Image.Type.Filled;
                hpFill.fillMethod = Image.FillMethod.Horizontal;
                hpFill.fillOrigin = 0;
                hpFill.fillAmount = 1f;
                hpFill.raycastTarget = false;
            }
            // RPG 风格血条：在 HPBarBg 上挂 Slider，绿条占比 = 血量百分比
            var hpBarBg = FindChild(bars, "HPBarBg");
            if (hpBarBg != null && hpFill != null)
            {
                var slider = hpBarBg.GetComponent<Slider>();
                if (slider == null) slider = hpBarBg.gameObject.AddComponent<Slider>();
                slider.fillRect = hpFill.rectTransform;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 1f;
                slider.interactable = false;
                slider.transition = Selectable.Transition.None;
                hpSlider = slider;
            }
        }

        // 血条上方：下一次攻击剩余时间
        if (cd != null)
        {
            var crt = cd.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0.20f);
            crt.anchorMax = new Vector2(1, 0.33f);
            crt.offsetMin = new Vector2(4, 0);
            crt.offsetMax = new Vector2(-4, 0);
            var t = cd.GetComponent<TextMeshProUGUI>();
            if (t != null) t.fontSize = 16;
        }

        // CD 明暗遮罩：纵向填充，从下往上消退（暗→亮）
        if (overlay != null)
        {
            var oimg = overlay.GetComponent<Image>();
            oimg.type = Image.Type.Filled;
            oimg.fillMethod = Image.FillMethod.Vertical;
            oimg.fillOrigin = 0;
            oimg.fillAmount = 0f;
            oimg.raycastTarget = false;
            var ort = overlay.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        }

        // 渲染顺序：HeroCard(立绘+文字) → CDOverlay(遮罩) → CDCountdown(倒计时) → Bars(血条)
        heroCard.SetAsFirstSibling();
        if (overlay != null) overlay.SetAsLastSibling();
        if (cd != null) cd.SetAsLastSibling();
        if (bars != null) bars.SetAsLastSibling();

        // 挂载随从实体脚本
        var entity = root.AddComponent<FollowerEntity>();
        entity.background = root.GetComponent<Image>();
        entity.photoImage = pimg;
        entity.hpSlider = hpSlider;
        entity.nameText = FindText(heroCard, "Name");
        entity.statsText = FindText(heroCard, "Stats");
        entity.hpBarFill = FindImage(bars, "HPBarFill");
        entity.hpBarText = FindText(bars, "HPText");
        entity.cdOverlay = overlay != null ? overlay.GetComponent<Image>() : null;
        entity.cdCountdownText = cd != null ? cd.GetComponent<TextMeshProUGUI>() : null;

        if (root.GetComponent<CanvasGroup>() == null)
            root.AddComponent<CanvasGroup>();
        entity.canvasGroup = root.GetComponent<CanvasGroup>();
        root.AddComponent<FollowerDragHandler>();

        PrefabUtility.SaveAsPrefabAsset(root, FOLLOWER_PATH);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[FollowerPrefabBuilder] Follower prefab saved: " + FOLLOWER_PATH);
    }

    // ── 工具 ──
    static void SetActive(Transform root, string childName, bool active)
    {
        var t = root.Find(childName);
        if (t != null) t.gameObject.SetActive(active);
    }

    static void DestroyChild(Transform root, string childName)
    {
        var t = FindChild(root, childName);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static RectTransform FindChild(Transform root, string childName)
    {
        var t = root.Find(childName);
        if (t != null) return t as RectTransform;
        foreach (Transform child in root)
        {
            var found = FindChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    static Image FindImage(Transform root, string childName)
    {
        var t = FindChild(root, childName);
        return t != null ? t.GetComponent<Image>() : null;
    }

    static TextMeshProUGUI FindText(Transform root, string childName)
    {
        var t = FindChild(root, childName);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }
}
