using System.Collections.Generic;
using UnityEngine;

public class HeroPicker : MonoBehaviour
{
    public static HeroPicker Instance { get; private set; }

    bool _isOpen = false;
    int _targetCellX, _targetCellY;
    Rect _windowRect = new Rect(100, 60, 320, 560);
    Vector2 _scrollPos;

    List<HeroRow> _heroes = new List<HeroRow>();

    void Awake()
    {
        Instance = this;
        Debug.Log("[HeroPicker] OnGUI mode ready.");
    }

    void Start()
    {
        // Auto-load hero list once
        LoadHeroes();
    }

    void LoadHeroes()
    {
        _heroes.Clear();
        var bc = BoardController.Instance;
        if (bc == null) return;
        _heroes = bc.GetAllHeroes();
        _heroes.Sort((a, b) => {
            int c = a.cost.CompareTo(b.cost);
            if (c != 0) return c;
            return a.id.CompareTo(b.id);
        });
        Debug.Log("[HeroPicker] Loaded " + _heroes.Count + " heroes for picker.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _isOpen)
        {
            _isOpen = false;
        }
    }

    void OnGUI()
    {
        if (!_isOpen) return;

        _windowRect = GUILayout.Window(997, _windowRect, DrawPickerWindow, "Select Hero [Esc=close]");
    }

    void DrawPickerWindow(int id)
    {
        var bc = BoardController.Instance;

        // Population info
        if (bc != null)
        {
            string popText = bc.GodMode
                ? "Pop: " + bc.CurrentPopulation + "/" + bc.MaxPopulation + "  [GOD MODE]"
                : "Pop: " + bc.CurrentPopulation + "/" + bc.MaxPopulation;
            GUILayout.Label(popText);
        }

        GUILayout.Label("Target: [" + _targetCellX + "," + _targetCellY + "]");
        GUILayout.Space(4);

        // Scrollable hero list
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(440));

        foreach (var hero in _heroes)
        {
            bool canPlace = bc != null && bc.CanPlaceHero(hero.id);

            GUI.enabled = canPlace;
            GUI.backgroundColor = canPlace
                ? new Color(0.2f, 0.35f, 0.2f)
                : new Color(0.35f, 0.15f, 0.15f);

            string label = "[" + hero.cost + "] " + hero.name + "  " + hero.job
                + "  HP:" + hero.hp + "  ATK:" + hero.atk;

            if (GUILayout.Button(label, GUILayout.Height(28)))
            {
                PlaceAndClose(hero);
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }

        GUILayout.EndScrollView();

        GUILayout.Space(6);

        // Close button
        if (GUILayout.Button("Close", GUILayout.Height(30)))
        {
            _isOpen = false;
        }

        GUI.DragWindow();
    }

    void PlaceAndClose(HeroRow hero)
    {
        var bc = BoardController.Instance;
        if (bc == null) return;

        if (bc.PlaceHero(_targetCellX, _targetCellY, hero.id))
        {
            var cell = bc.GetCell(_targetCellX, _targetCellY);
            if (cell != null)
            {
                var hob = cell.GetComponent<HeroOnBoard>();
                if (hob != null) hob.Init(hero.id, _targetCellX, _targetCellY);
            }
            _isOpen = false;
        }
    }

    public void OpenForCell(int x, int y)
    {
        _targetCellX = x;
        _targetCellY = y;
        if (_heroes.Count == 0) LoadHeroes();
        _isOpen = true;
    }

    public void Close()
    {
        _isOpen = false;
    }
}