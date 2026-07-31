using UnityEngine;
using UnityEngine.UI;

public class BoardSetup : MonoBehaviour
{
    public float cellSize = 160f;
    public float spacing = 10f;
    public Vector2 boardOffset = new Vector2(0, 0);
    public BoardController boardController;

    void Awake()
    {
        if (boardController == null)
            boardController = FindObjectOfType<BoardController>();

        CreateBoard();
    }

    void CreateBoard()
    {
        int gs = BoardController.GridSize;
        float totalW = gs * cellSize + (gs + 1) * spacing;
        float totalH = gs * cellSize + (gs + 1) * spacing;

        var boardPanel = MakeUI("BoardPanel", transform);
        var bprt = boardPanel.GetComponent<RectTransform>();
        bprt.sizeDelta = new Vector2(totalW, totalH);
        bprt.anchoredPosition = boardOffset;

        var bg = boardPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

        for (int x = 0; x < gs; x++)
        {
            for (int y = 0; y < gs; y++)
            {
                CreateCell(boardPanel.transform, x, y, totalW, totalH);
            }
        }
    }

    void CreateCell(Transform parent, int x, int y, float panelW, float panelH)
    {
        float startX = -panelW / 2f + spacing + cellSize / 2f;
        float startY = panelH / 2f - spacing - cellSize / 2f;
        float posX = startX + x * (cellSize + spacing);
        float posY = startY - y * (cellSize + spacing);

        var cellGO = MakeUI("Cell_" + x + "_" + y, parent);
        var crt = cellGO.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(cellSize, cellSize);
        crt.anchoredPosition = new Vector2(posX, posY);

        var bg = cellGO.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);

        // Highlight border
        var hlGO = MakeUI("Highlight", cellGO.transform);
        Stretch(hlGO.GetComponent<RectTransform>());
        var hlImg = hlGO.AddComponent<Image>();
        hlImg.color = new Color(1f, 0.85f, 0.2f, 0.4f);
        hlImg.raycastTarget = false;

        // Info text
        var infoGO = MakeUI("InfoText", cellGO.transform);
        StretchMargin(infoGO.GetComponent<RectTransform>(), 4);
        var infoTxt = infoGO.AddComponent<Text>();
        infoTxt.text = "[" + x + "," + y + "]";
        infoTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoTxt.fontSize = 14;
        infoTxt.alignment = TextAnchor.MiddleCenter;
        infoTxt.color = new Color(0.6f, 0.6f, 0.65f);

        // Hero card root
        var cardRoot = MakeUI("HeroCard", cellGO.transform);
        StretchMargin(cardRoot.GetComponent<RectTransform>(), 6);
        cardRoot.SetActive(false);

        // Name
        var nameGO = MakeUI("Name", cardRoot.transform);
        var nrt = nameGO.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0, 0.55f); nrt.anchorMax = new Vector2(1, 0.95f);
        nrt.offsetMin = Vector2.zero; nrt.offsetMax = Vector2.zero;
        var nameTxt = nameGO.AddComponent<Text>();
        nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameTxt.fontSize = 20;
        nameTxt.fontStyle = FontStyle.Bold;
        nameTxt.alignment = TextAnchor.MiddleCenter;
        nameTxt.color = Color.white;

        // Stats
        var statGO = MakeUI("Stats", cardRoot.transform);
        var srt = statGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 0.15f); srt.anchorMax = new Vector2(1, 0.55f);
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        var statTxt = statGO.AddComponent<Text>();
        statTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statTxt.fontSize = 14;
        statTxt.alignment = TextAnchor.MiddleCenter;
        statTxt.color = new Color(0.85f, 0.85f, 0.85f);

        // Cost badge
        var badgeGO = MakeUI("CostBadge", cardRoot.transform);
        var brt = badgeGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1, 1); brt.anchorMax = new Vector2(1, 1);
        brt.pivot = new Vector2(1, 1);
        brt.sizeDelta = new Vector2(28, 28);
        brt.anchoredPosition = new Vector2(-4, -4);
        var badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.color = new Color(0.3f, 0.5f, 0.3f);

        var costGO = MakeUI("CostText", badgeGO.transform);
        Stretch(costGO.GetComponent<RectTransform>());
        var costTxt = costGO.AddComponent<Text>();
        costTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        costTxt.fontSize = 16;
        costTxt.fontStyle = FontStyle.Bold;
        costTxt.alignment = TextAnchor.MiddleCenter;
        costTxt.color = Color.white;

        // CellSlot component
        var slot = cellGO.AddComponent<CellSlot>();
        slot.gridX = x;
        slot.gridY = y;
        slot.background = bg;
        slot.highlightBorder = hlImg;
        slot.infoText = infoTxt;
        slot.heroCardRoot = cardRoot;
        slot.heroNameText = nameTxt;
        slot.heroStatsText = statTxt;
        slot.heroCostBadge = badgeImg;
        slot.heroCostText = costTxt;

        // HeroOnBoard
        var hob = cellGO.AddComponent<HeroOnBoard>();
        hob.Init(0, x, y);
    }

    GameObject MakeUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return go;
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void StretchMargin(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }
}