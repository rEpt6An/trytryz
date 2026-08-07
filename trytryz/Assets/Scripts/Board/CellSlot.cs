using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CellSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int gridX;
    public int gridY;

    public Image background;
    public Image highlightBorder;
    public TextMeshProUGUI infoText;
    public GameObject heroCardRoot;
    public TextMeshProUGUI heroNameText;
    public TextMeshProUGUI heroStatsText;
    public Image heroCostBadge;
    public TextMeshProUGUI heroCostText;

    [Header("Progress Bars")]
    public Image hpBarFill;
    public Image atkBarFill;
    public GameObject barsRoot;

    Color _defaultBgColor = new Color(0.18f, 0.18f, 0.22f, 0.9f);
    Color _hoverBgColor = new Color(0.28f, 0.28f, 0.35f, 0.95f);
    Color _occupiedBgColor = new Color(0.15f, 0.25f, 0.15f, 0.9f);

    Color[] _costColors = {
        new Color(0.3f, 0.5f, 0.3f),
        new Color(0.3f, 0.35f, 0.7f),
        new Color(0.7f, 0.35f, 0.7f),
        new Color(0.8f, 0.55f, 0.2f),
    };

    HeroOnBoard _heroOnBoard;

    void Start()
    {
        _heroOnBoard = GetComponent<HeroOnBoard>();

        if (BoardController.Instance != null)
            BoardController.Instance.RegisterCell(gridX, gridY, this);

        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(false);

        if (barsRoot != null)
            barsRoot.SetActive(false);

        UpdateDisplay();
    }

    void OnEnable()
    {
        if (BoardController.Instance != null)
            BoardController.Instance.OnBoardChanged += UpdateDisplay;
    }

    void OnDisable()
    {
        if (BoardController.Instance != null)
            BoardController.Instance.OnBoardChanged -= UpdateDisplay;
    }

    void Update()
    {
        UpdateProgressBars();
    }

    public void UpdateDisplay()
    {
        if (BoardController.Instance == null) return;

        int heroId = BoardController.Instance.GetHeroAt(gridX, gridY);

        if (heroId == 0)
        {
            background.color = _defaultBgColor;
            if (infoText != null)
                infoText.text = "[" + gridX + "," + gridY + "]";
            if (heroCardRoot != null)
                heroCardRoot.SetActive(false);
            if (barsRoot != null)
                barsRoot.SetActive(false);
        }
        else
        {
            var hero = BoardController.Instance.GetHeroData(heroId);
            if (hero != null)
            {
                background.color = _occupiedBgColor;
                if (infoText != null) infoText.text = "";
                if (heroCardRoot != null) heroCardRoot.SetActive(true);
                if (heroNameText != null) heroNameText.text = hero.name;
                if (heroStatsText != null) heroStatsText.text = "HP:" + hero.hp + "  ATK:" + hero.atk;
                if (heroCostBadge != null)
                {
                    int idx = Mathf.Clamp(hero.cost - 1, 0, _costColors.Length - 1);
                    heroCostBadge.color = _costColors[idx];
                }
                if (heroCostText != null) heroCostText.text = hero.cost.ToString();
                if (barsRoot != null)
                    barsRoot.SetActive(true);
            }
        }
    }

    void UpdateProgressBars()
    {
        if (_heroOnBoard == null) return;
        if (_heroOnBoard.HeroId == 0) return;

        // HP bar
        if (hpBarFill != null && _heroOnBoard.MaxHp > 0)
        {
            float hpRatio = Mathf.Clamp01((float)_heroOnBoard.CurrentHp / _heroOnBoard.MaxHp);
            hpBarFill.fillAmount = hpRatio;
            if (hpRatio > 0.6f)
                hpBarFill.color = new Color(0.2f, 0.8f, 0.2f);
            else if (hpRatio > 0.3f)
                hpBarFill.color = new Color(1f, 0.85f, 0.2f);
            else
                hpBarFill.color = new Color(1f, 0.25f, 0.25f);
        }

        // ATK bar (placeholder - will be driven by battle system later)
        if (atkBarFill != null)
        {
            // For now, show full (battle system will control this)
            atkBarFill.fillAmount = 1f;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BoardController.Instance == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (BoardController.Instance.GetHeroAt(gridX, gridY) != 0)
                BoardController.Instance.RemoveHero(gridX, gridY);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (BoardController.Instance.IsCellEmpty(gridX, gridY))
            {
                if (HeroPicker.Instance != null)
                    HeroPicker.Instance.OpenForCell(gridX, gridY);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(true);

        if (BoardController.Instance != null && BoardController.Instance.IsCellEmpty(gridX, gridY))
            background.color = _hoverBgColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(false);
        UpdateDisplay();
    }
}