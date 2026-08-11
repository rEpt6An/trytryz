using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 随从选择器（OnGUI 调试用）：点击空格子打开，把随从放到格子上。
/// </summary>
public class HeroPicker : MonoBehaviour
{
    public static HeroPicker Instance { get; private set; }

    bool _isOpen = false;
    int _targetCellX, _targetCellY;
    Rect _windowRect = new Rect(100, 60, 340, 580);
    Vector2 _scrollPos;
    List<HeroRow> _followers = new List<HeroRow>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() { LoadFollowers(); }

    void LoadFollowers()
    {
        _followers.Clear();
        var bc = BoardController.Instance;
        if (bc == null) return;
        _followers = bc.GetAllFollowers();
        _followers.Sort((a, b) =>
        {
            int c = a.pop.CompareTo(b.pop);
            if (c != 0) return c;
            return a.id.CompareTo(b.id);
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _isOpen) _isOpen = false;
    }

    void OnGUI()
    {
        if (!_isOpen) return;
        _windowRect = GUILayout.Window(997, _windowRect, DrawPickerWindow, "Select Follower [Esc=close]");
    }

    void DrawPickerWindow(int id)
    {
        var bc = BoardController.Instance;
        if (bc != null)
        {
            string p = bc.GodMode ? "Pop: " + bc.CurrentPopulation + "/" + bc.MaxPopulation + "  [GOD]"
                : "Pop: " + bc.CurrentPopulation + "/" + bc.MaxPopulation;
            GUILayout.Label(p);
        }
        GUILayout.Label("Target: [" + _targetCellX + "," + _targetCellY + "]");
        GUILayout.Space(4);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(440));
        foreach (var hero in _followers)
        {
            bool canPlace = bc != null && bc.CanPlaceFollower(hero.id);
            GUI.enabled = canPlace;
            GUI.backgroundColor = canPlace ? new Color(0.2f, 0.35f, 0.2f) : new Color(0.35f, 0.15f, 0.15f);
            string label = "[人口" + hero.pop + "] " + hero.name + " " + hero.job
                + "  HP:" + hero.hp + " ATK:" + hero.atk + " CD:" + hero.cd.ToString("F1");
            if (GUILayout.Button(label, GUILayout.Height(30))) PlaceAndClose(hero);
            GUI.backgroundColor = Color.white; GUI.enabled = true;
        }
        GUILayout.EndScrollView();
        GUILayout.Space(6);
        if (GUILayout.Button("Close", GUILayout.Height(30))) _isOpen = false;
        GUI.DragWindow();
    }

    void PlaceAndClose(HeroRow hero)
    {
        var bc = BoardController.Instance; if (bc == null) return;
        if (bc.PlaceFollower(_targetCellX, _targetCellY, hero.id))
        {
            _isOpen = false;
        }
    }

    public void OpenForCell(int x, int y)
    {
        _targetCellX = x;
        _targetCellY = y;
        if (_followers.Count == 0) LoadFollowers();
        _isOpen = true;
    }

    public void Close() { _isOpen = false; }
}
