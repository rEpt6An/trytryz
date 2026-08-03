using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelBuilder
{
    [MenuItem("Trytryz/Build All Panels (HUD/Event/Shop/Warehouse/Battle)")]
    static void BuildAll()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            Undo.RegisterCreatedObjectUndo(cgo, "Create Canvas");
        }

        BuildHUD(canvas);
        BuildEventPanel(canvas);
        BuildShopPanel(canvas);
        BuildWarehousePanel(canvas);
        BuildBattlePanel(canvas);

        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[UIPanelBuilder] All panels built. Check Hierarchy under Canvas.");
    }

    // ============================================
    //  HUD
    // ============================================
    static void BuildHUD(Canvas canvas)
    {
        var hud = GetOrCreateChild(canvas.transform, "HUD");
        var rt = hud.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // Commander (top-right)
        var cmd = GetOrCreateChild(hud.transform, "CommanderSection");
        SetAnchor(cmd.GetComponent<RectTransform>(), 1, 1, 1, 1);
        cmd.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
        cmd.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30, -30);
        cmd.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 130);

        var frame = GetOrCreateChild(cmd.transform, "PortraitFrame");
        SetAnchorStretch(frame.GetComponent<RectTransform>(), 0, 0.35f, 1, 1);
        frame.GetOrAddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);

        var portrait = GetOrCreateChild(frame.transform, "Portrait");
        Stretch(portrait.GetComponent<RectTransform>(), 4);
        portrait.GetOrAddComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f, 1f);

        var hpGO = GetOrCreateChild(cmd.transform, "HPText");
        SetAnchorStretch(hpGO.GetComponent<RectTransform>(), 0, 0, 1, 0.3f);
        var hpTxt = hpGO.GetOrAddComponent<TextMeshProUGUI>();
        hpTxt.text = "15"; hpTxt.font = GetTMPFont(); hpTxt.fontSize = 28;
        hpTxt.fontStyle = FontStyles.Bold; hpTxt.alignment = TextAlignmentOptions.Center;
        hpTxt.color = new Color(1f, 0.3f, 0.3f);

        // Gold (right of commander)
        var gold = GetOrCreateChild(hud.transform, "GoldSection");
        SetAnchor(gold.GetComponent<RectTransform>(), 1, 1, 1, 1);
        gold.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        gold.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30 + 100 + 10, -30);
        gold.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 130);

        var gIcon = GetOrCreateChild(gold.transform, "GoldIcon");
        SetAnchorStretch(gIcon.GetComponent<RectTransform>(), 0, 0.55f, 0.3f, 0.9f);
        gIcon.GetOrAddComponent<Image>().color = new Color(1f, 0.85f, 0.2f);

        var gTxtGO = GetOrCreateChild(gold.transform, "GoldText");
        SetAnchorStretch(gTxtGO.GetComponent<RectTransform>(), 0.35f, 0.55f, 1, 0.9f);
        var gTxt = gTxtGO.GetOrAddComponent<TextMeshProUGUI>();
        gTxt.text = "0"; gTxt.font = GetTMPFont(); gTxt.fontSize = 32;
        gTxt.fontStyle = FontStyles.Bold; gTxt.alignment = TextAlignmentOptions.Left;
        gTxt.color = new Color(1f, 0.85f, 0.2f);

        var incGO = GetOrCreateChild(gold.transform, "IncomeText");
        SetAnchorStretch(incGO.GetComponent<RectTransform>(), 0, 0.15f, 1, 0.5f);
        var incTxt = incGO.GetOrAddComponent<TextMeshProUGUI>();
        incTxt.text = "+0 / turn"; incTxt.font = GetTMPFont(); incTxt.fontSize = 16;
        incTxt.alignment = TextAlignmentOptions.Center; incTxt.color = new Color(0.6f, 1f, 0.6f);

        // Day/Round (top-center)
        var dayGO = GetOrCreateChild(hud.transform, "DayRoundText");
        var drt = dayGO.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.5f, 1); drt.anchorMax = new Vector2(0.5f, 1);
        drt.pivot = new Vector2(0.5f, 1); drt.anchoredPosition = new Vector2(0, -20);
        drt.sizeDelta = new Vector2(300, 40);
        var dayTxt = dayGO.GetOrAddComponent<TextMeshProUGUI>();
        dayTxt.text = "Day 1  Round 1"; dayTxt.font = GetTMPFont();
        dayTxt.fontSize = 22; dayTxt.alignment = TextAlignmentOptions.Center; dayTxt.color = Color.white;

        // Warehouse Button (bottom-right)
        var whBtn = GetOrCreateChild(hud.transform, "WarehouseButton");
        var wbrt = whBtn.GetComponent<RectTransform>();
        wbrt.anchorMin = new Vector2(1, 0); wbrt.anchorMax = new Vector2(1, 0);
        wbrt.pivot = new Vector2(1, 0); wbrt.anchoredPosition = new Vector2(-30, 30);
        wbrt.sizeDelta = new Vector2(120, 50);
        whBtn.GetOrAddComponent<Image>().color = new Color(0.2f, 0.2f, 0.4f, 1f);
        whBtn.GetOrAddComponent<Button>();
        var whLabel = GetOrCreateChild(whBtn.transform, "Label");
        Stretch(whLabel.GetComponent<RectTransform>(), 0);
        var whlTxt = whLabel.GetOrAddComponent<TextMeshProUGUI>();
        whlTxt.text = "Warehouse"; whlTxt.font = GetTMPFont(); whlTxt.fontSize = 16;
        whlTxt.alignment = TextAlignmentOptions.Center; whlTxt.color = Color.white;
    }

    // ============================================
    //  EVENT PANEL
    // ============================================
    static void BuildEventPanel(Canvas canvas)
    {
        var panel = GetOrCreateChild(canvas.transform, "EventPanel");
        SetAnchorStretch(panel.GetComponent<RectTransform>(), 0.05f, 0.2f, 0.35f, 0.8f);
        panel.GetOrAddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        panel.SetActive(false);

        var title = GetOrCreateChild(panel.transform, "Title");
        SetAnchorStretch(title.GetComponent<RectTransform>(), 0, 0.88f, 1, 1);
        var tTxt = title.GetOrAddComponent<TextMeshProUGUI>();
        tTxt.text = "Event"; tTxt.font = GetTMPFont(); tTxt.fontSize = 24;
        tTxt.fontStyle = FontStyles.Bold; tTxt.alignment = TextAlignmentOptions.Center; tTxt.color = Color.white;

        for (int i = 0; i < 3; i++)
        {
            var opt = GetOrCreateChild(panel.transform, "Option" + (i + 1));
            float top = 0.88f - (i + 1) * 0.28f;
            SetAnchorStretch(opt.GetComponent<RectTransform>(), 0.05f, top - 0.25f, 0.95f, top);
            opt.GetOrAddComponent<Image>().color = new Color(0.15f, 0.25f, 0.15f, 1f);
            opt.GetOrAddComponent<Button>();
            var label = GetOrCreateChild(opt.transform, "Label");
            Stretch(label.GetComponent<RectTransform>(), 8);
            var lTxt = label.GetOrAddComponent<TextMeshProUGUI>();
            lTxt.text = "Option " + (i + 1); lTxt.font = GetTMPFont(); lTxt.fontSize = 18;
            lTxt.alignment = TextAlignmentOptions.Center; lTxt.color = Color.white;
        }

        var skip = GetOrCreateChild(panel.transform, "SkipButton");
        SetAnchorStretch(skip.GetComponent<RectTransform>(), 0.3f, 0, 0.7f, 0.06f);
        skip.GetOrAddComponent<Image>().color = new Color(0.3f, 0.15f, 0.15f);
        skip.GetOrAddComponent<Button>();
        var sl = GetOrCreateChild(skip.transform, "Label");
        Stretch(sl.GetComponent<RectTransform>(), 0);
        var slTxt = sl.GetOrAddComponent<TextMeshProUGUI>();
        slTxt.text = "Skip"; slTxt.font = GetTMPFont(); slTxt.fontSize = 16;
        slTxt.alignment = TextAlignmentOptions.Center; slTxt.color = Color.white;
    }

    // ============================================
    //  SHOP PANEL
    // ============================================
    static void BuildShopPanel(Canvas canvas)
    {
        var panel = GetOrCreateChild(canvas.transform, "ShopPanel");
        SetAnchorStretch(panel.GetComponent<RectTransform>(), 0.25f, 0.2f, 0.75f, 0.8f);
        panel.GetOrAddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        panel.SetActive(false);

        var title = GetOrCreateChild(panel.transform, "Title");
        SetAnchorStretch(title.GetComponent<RectTransform>(), 0, 0.92f, 1, 1);
        var tTxt = title.GetOrAddComponent<TextMeshProUGUI>();
        tTxt.text = "Shop"; tTxt.font = GetTMPFont(); tTxt.fontSize = 24;
        tTxt.fontStyle = FontStyles.Bold; tTxt.alignment = TextAlignmentOptions.Center; tTxt.color = Color.white;

        var refresh = GetOrCreateChild(panel.transform, "RefreshButton");
        SetAnchorStretch(refresh.GetComponent<RectTransform>(), 0.02f, 0.86f, 0.18f, 0.95f);
        refresh.GetOrAddComponent<Image>().color = new Color(0.3f, 0.3f, 0.5f);
        refresh.GetOrAddComponent<Button>();
        var rl = GetOrCreateChild(refresh.transform, "Label");
        Stretch(rl.GetComponent<RectTransform>(), 0);
        var rlTxt = rl.GetOrAddComponent<TextMeshProUGUI>();
        rlTxt.text = "Refresh"; rlTxt.font = GetTMPFont(); rlTxt.fontSize = 16;
        rlTxt.alignment = TextAlignmentOptions.Center; rlTxt.color = Color.white;

        for (int i = 0; i < 4; i++)
        {
            int col = i % 2; int row = i / 2;
            var slot = GetOrCreateChild(panel.transform, "Slot" + (i + 1));
            float x0 = 0.05f + col * 0.47f;
            float x1 = x0 + 0.43f;
            float y0 = 0.04f + (1 - row) * 0.4f;
            float y1 = y0 + 0.38f;
            SetAnchorStretch(slot.GetComponent<RectTransform>(), x0, y0, x1, y1);
            slot.GetOrAddComponent<Image>().color = new Color(0.15f, 0.2f, 0.3f, 1f);
            slot.GetOrAddComponent<Button>();
            var label = GetOrCreateChild(slot.transform, "Label");
            Stretch(label.GetComponent<RectTransform>(), 8);
            var lTxt = label.GetOrAddComponent<TextMeshProUGUI>();
            lTxt.text = "Slot " + (i + 1); lTxt.font = GetTMPFont(); lTxt.fontSize = 18;
            lTxt.alignment = TextAlignmentOptions.Center; lTxt.color = Color.white;
        }

        var close = GetOrCreateChild(panel.transform, "CloseButton");
        SetAnchorStretch(close.GetComponent<RectTransform>(), 0.82f, 0.86f, 0.98f, 0.95f);
        close.GetOrAddComponent<Image>().color = new Color(0.5f, 0.15f, 0.15f);
        close.GetOrAddComponent<Button>();
        var cl = GetOrCreateChild(close.transform, "Label");
        Stretch(cl.GetComponent<RectTransform>(), 0);
        var clTxt = cl.GetOrAddComponent<TextMeshProUGUI>();
        clTxt.text = "X"; clTxt.font = GetTMPFont(); clTxt.fontSize = 16;
        clTxt.fontStyle = FontStyles.Bold; clTxt.alignment = TextAlignmentOptions.Center; clTxt.color = Color.white;
    }

    // ============================================
    //  WAREHOUSE PANEL
    // ============================================
    static void BuildWarehousePanel(Canvas canvas)
    {
        var panel = GetOrCreateChild(canvas.transform, "WarehousePanel");
        SetAnchorStretch(panel.GetComponent<RectTransform>(), 0.55f, 0.25f, 0.95f, 0.75f);
        panel.GetOrAddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.95f);
        panel.SetActive(false);

        var title = GetOrCreateChild(panel.transform, "Title");
        SetAnchorStretch(title.GetComponent<RectTransform>(), 0, 0.92f, 1, 1);
        var tTxt = title.GetOrAddComponent<TextMeshProUGUI>();
        tTxt.text = "Warehouse"; tTxt.font = GetTMPFont(); tTxt.fontSize = 20;
        tTxt.fontStyle = FontStyles.Bold; tTxt.alignment = TextAlignmentOptions.Center; tTxt.color = Color.white;

        var board = GetOrCreateChild(panel.transform, "WH_Board");
        SetAnchorStretch(board.GetComponent<RectTransform>(), 0.05f, 0.05f, 0.95f, 0.88f);
        board.GetOrAddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 1f);

        float cs = 100f; float gap = 6f;
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var cell = GetOrCreateChild(board.transform, "WH_Cell_" + x + "_" + y);
                var crt = cell.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.sizeDelta = new Vector2(cs, cs);
                crt.anchoredPosition = new Vector2((x - 1) * (cs + gap), (1 - y) * (cs + gap));
                cell.GetOrAddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);
                var label = GetOrCreateChild(cell.transform, "Label");
                Stretch(label.GetComponent<RectTransform>(), 4);
                var lTxt = label.GetOrAddComponent<TextMeshProUGUI>();
                lTxt.text = "[" + x + "," + y + "]"; lTxt.font = GetTMPFont(); lTxt.fontSize = 12;
                lTxt.alignment = TextAlignmentOptions.Center; lTxt.color = new Color(0.5f, 0.5f, 0.6f);
            }
        }

        var close = GetOrCreateChild(panel.transform, "CloseButton");
        SetAnchorStretch(close.GetComponent<RectTransform>(), 0.82f, 0.93f, 0.98f, 1);
        close.GetOrAddComponent<Image>().color = new Color(0.5f, 0.15f, 0.15f);
        close.GetOrAddComponent<Button>();
        var cl = GetOrCreateChild(close.transform, "Label");
        Stretch(cl.GetComponent<RectTransform>(), 0);
        var clTxt = cl.GetOrAddComponent<TextMeshProUGUI>();
        clTxt.text = "X"; clTxt.font = GetTMPFont(); clTxt.fontSize = 16;
        clTxt.fontStyle = FontStyles.Bold; clTxt.alignment = TextAlignmentOptions.Center; clTxt.color = Color.white;
    }

    // ============================================
    //  BATTLE PANEL
    // ============================================
    static void BuildBattlePanel(Canvas canvas)
    {
        var panel = GetOrCreateChild(canvas.transform, "BattlePanel");
        SetAnchorStretch(panel.GetComponent<RectTransform>(), 0, 0, 1, 1);
        panel.GetOrAddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        panel.SetActive(false);

        var eBoard = GetOrCreateChild(panel.transform, "EnemyBoard");
        var ebrt = eBoard.GetComponent<RectTransform>();
        ebrt.anchorMin = new Vector2(0.55f, 0.5f); ebrt.anchorMax = new Vector2(0.55f, 0.5f);
        ebrt.pivot = new Vector2(0.5f, 0.5f); ebrt.sizeDelta = new Vector2(520, 520);
        eBoard.GetOrAddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 1f);

        float cs = 150f; float gap = 10f;
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var cell = GetOrCreateChild(eBoard.transform, "E_Cell_" + x + "_" + y);
                var crt = cell.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.sizeDelta = new Vector2(cs, cs);
                crt.anchoredPosition = new Vector2((x - 1) * (cs + gap), (1 - y) * (cs + gap));
                cell.GetOrAddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);
                var label = GetOrCreateChild(cell.transform, "Label");
                Stretch(label.GetComponent<RectTransform>(), 4);
                var lTxt = label.GetOrAddComponent<TextMeshProUGUI>();
                lTxt.text = ""; lTxt.font = GetTMPFont(); lTxt.fontSize = 12;
                lTxt.alignment = TextAlignmentOptions.Center; lTxt.color = new Color(0.5f, 0.5f, 0.6f);
            }
        }

        var eCmd = GetOrCreateChild(panel.transform, "EnemyCommander");
        var ecrt = eCmd.GetComponent<RectTransform>();
        ecrt.anchorMin = new Vector2(0.82f, 0.74f); ecrt.anchorMax = new Vector2(0.82f, 0.74f);
        ecrt.sizeDelta = new Vector2(80, 100);
        eCmd.GetOrAddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var epImg = GetOrCreateChild(eCmd.transform, "Portrait");
        Stretch(epImg.GetComponent<RectTransform>(), 4);
        epImg.GetOrAddComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f, 1f);
        var ehpGO = GetOrCreateChild(eCmd.transform, "HPText");
        SetAnchorStretch(ehpGO.GetComponent<RectTransform>(), 0, 0, 1, 0.25f);
        var ehpTxt = ehpGO.GetOrAddComponent<TextMeshProUGUI>();
        ehpTxt.text = "?"; ehpTxt.font = GetTMPFont(); ehpTxt.fontSize = 18;
        ehpTxt.alignment = TextAlignmentOptions.Center; ehpTxt.color = Color.red;

        var result = GetOrCreateChild(panel.transform, "BattleResult");
        SetAnchorStretch(result.GetComponent<RectTransform>(), 0.35f, 0.45f, 0.65f, 0.55f);
        var resTxt = result.GetOrAddComponent<TextMeshProUGUI>();
        resTxt.text = ""; resTxt.font = GetTMPFont(); resTxt.fontSize = 36;
        resTxt.fontStyle = FontStyles.Bold; resTxt.alignment = TextAlignmentOptions.Center; resTxt.color = Color.white;

        var cont = GetOrCreateChild(panel.transform, "ContinueButton");
        SetAnchorStretch(cont.GetComponent<RectTransform>(), 0.4f, 0.2f, 0.6f, 0.28f);
        cont.GetOrAddComponent<Image>().color = new Color(0.2f, 0.4f, 0.2f);
        cont.GetOrAddComponent<Button>();
        var clb = GetOrCreateChild(cont.transform, "Label");
        Stretch(clb.GetComponent<RectTransform>(), 0);
        var clbTxt = clb.GetOrAddComponent<TextMeshProUGUI>();
        clbTxt.text = "Continue"; clbTxt.font = GetTMPFont(); clbTxt.fontSize = 20;
        clbTxt.alignment = TextAlignmentOptions.Center; clbTxt.color = Color.white;
    }

    // ============================================
    //  HELPERS
    // ============================================
    static GameObject GetOrCreateChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    static void SetAnchor(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
    }

    static void SetAnchorStretch(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Stretch(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    static TMP_FontAsset GetTMPFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/KaiTi SDF");
        if (font != null) return font;
        font = TMP_Settings.defaultFontAsset;
        if (font != null) return font;
        Debug.LogWarning("[UIPanelBuilder] KaiTi SDF not found and no TMP default. Text will be invisible.");
        return null;
    }
}

static class UIHelperExt
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }
}