using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 棋盘格：只负责格子的背景 / 高亮 / 坐标提示 / 点击交互。
/// 随从实体（FollowerEntity）作为子物体挂在格子上，由 BoardController 管理。
/// 每帧自动刷新，放置/拖拽/战斗期间 UI 始终即时更新。
/// </summary>
public class CellSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int gridX;
    public int gridY;
    public bool isEnemy;

    public Image background;
    public Image highlightBorder;
    public TextMeshProUGUI infoText;

    /// <summary>当前格子上的随从实体（子物体）。</summary>
    public FollowerEntity Occupant { get; private set; }

    bool _hovered;

    Color _defaultBg = new Color(0.18f, 0.18f, 0.22f, 0.9f);
    Color _hoverBg = new Color(0.28f, 0.28f, 0.35f, 0.95f);
    Color _occupiedBg = new Color(0.16f, 0.26f, 0.16f, 0.9f);
    Color _enemyBg = new Color(0.24f, 0.13f, 0.13f, 0.9f);

    void Start()
    {
        if (BoardController.Instance != null)
            BoardController.Instance.RegisterCell(this);
    }

    void OnEnable()
    {
        if (BoardController.Instance != null)
            BoardController.Instance.RegisterCell(this);
    }

    void OnDisable()
    {
        if (BoardController.Instance != null)
            BoardController.Instance.UnregisterCell(this);
    }

    void Update()
    {
        Refresh();
        // 战斗中每帧实时刷新；战斗外仅在数据变化时刷新（不干扰玩家拖拽血条 Slider）
        if (Occupant != null && (Occupant.InBattle || Occupant.VisualsDirty)) Occupant.RefreshVisuals();
    }

    /// <summary>每帧刷新：占用状态 / 高亮 / 坐标提示 / 背景色。</summary>
    public void Refresh()
    {
        Occupant = GetComponentInChildren<FollowerEntity>();
        bool occupied = Occupant != null && Occupant.FollowerId != 0;

        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(_hovered);

        if (infoText != null)
        {
            infoText.gameObject.SetActive(!occupied && !_hovered);
            if (!occupied) infoText.text = "[" + gridX + "," + gridY + "]";
        }

        if (background != null)
        {
            if (occupied) background.color = isEnemy ? _enemyBg : _occupiedBg;
            else if (_hovered) background.color = _hoverBg;
            else background.color = _defaultBg;
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (isEnemy) return;
        var bc = BattleController.Instance;
        if (bc != null && bc.State == BattleController.BattleState.Running) return;

        if (e.button == PointerEventData.InputButton.Right)
        {
            if (BoardController.Instance != null)
                BoardController.Instance.RemoveFollower(gridX, gridY);
        }
        else if (e.button == PointerEventData.InputButton.Left)
        {
            Refresh();
            if (Occupant == null && HeroPicker.Instance != null)
                HeroPicker.Instance.OpenForCell(gridX, gridY);
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        _hovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData e)
    {
        _hovered = false;
        Refresh();
    }
}
