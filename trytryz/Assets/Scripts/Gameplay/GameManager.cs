using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum State { Placement, Event, BattlePrep, Battle, BattleOver, GameOver }
    public State CurrentState { get; private set; } = State.Placement;

    public int Day { get; private set; } = 1;
    public int Round { get; private set; } = 1;
    public int Hp { get; private set; } = 15;
    public int Crowns { get; private set; } = 0;

    const int MAX_HP = 15;
    const int WIN_CROWNS = 10;
    const int ROUNDS_PER_DAY = 8;
    const int PVE_ROUND = 4;
    const int PVP_ROUND = 8;

    enum EventType { Shop, FreeHero, Rest }
    EventType _currentEvent;
    List<HeroRow> _shopHeroes = new List<HeroRow>();
    int _pickedShopIndex = -1;

    bool _showUI = true;
    Rect _uiRect = new Rect(10, 10, 340, 520);
    string _message = "";
    string _battleResultText = "";
    int _battleResultWinner = -1;
    int _pendingFormationId = 1;

    static readonly int GRID_START = BoardController.GridStartIndex;
    static readonly int GRID_END = BoardController.GridStartIndex + BoardController.GridSize;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _message = "Day " + Day + " begins! Place your heroes.";
        RefreshGameUI();
        Debug.Log("[GameManager] Game started. Day=" + Day + " Round=" + Round);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
            _showUI = !_showUI;

        if (CurrentState == State.BattleOver && Input.GetKeyDown(KeyCode.Space))
            AdvanceFromBattle();
    }

    // ========== OnGUI Game Panel (F3) ==========
    void OnGUI()
    {
        if (!_showUI) return;
        _uiRect = GUILayout.Window(996, _uiRect, DrawGameUI, "Game [F3=hide]");
    }

    void DrawGameUI(int id)
    {
        GUILayout.Label("Day " + Day + "  |  Round " + Round + " / " + ROUNDS_PER_DAY);
        GUILayout.Label("HP: " + Hp + " / " + MAX_HP + "    Crowns: " + Crowns + " / " + WIN_CROWNS);
        GUILayout.Label("State: " + CurrentState);
        GUILayout.Space(6);

        if (!string.IsNullOrEmpty(_message))
        {
            GUILayout.Label(_message);
            GUILayout.Space(4);
        }

        switch (CurrentState)
        {
            case State.Placement:  DrawPlacementUI(); break;
            case State.Event:      DrawEventUI(); break;
            case State.BattlePrep: DrawBattlePrepUI(); break;
            case State.Battle:     DrawBattleUI(); break;
            case State.BattleOver: DrawBattleOverUI(); break;
            case State.GameOver:   DrawGameOverUI(); break;
        }
        GUI.DragWindow();
    }

    // ========== PLACEMENT ==========
    void DrawPlacementUI()
    {
        GUILayout.Label("Place heroes on the board, then:");
        if (GUILayout.Button("Next Round", GUILayout.Height(36)))
            StartRound();
    }

    void StartRound()
    {
        if (IsBattleRound(Round))
        {
            CurrentState = State.BattlePrep;
            _message = "Round " + Round + ": Battle incoming! Adjust your board.";
            return;
        }
        RollEvent();
        CurrentState = State.Event;
    }

    // ========== EVENTS ==========
    void RollEvent()
    {
        float roll = Random.value;
        if (roll < 0.5f)       _currentEvent = EventType.Shop;
        else if (roll < 0.85f) _currentEvent = EventType.FreeHero;
        else                   _currentEvent = EventType.Rest;

        _shopHeroes.Clear();
        _pickedShopIndex = -1;

        if (_currentEvent == EventType.Shop)
        {
            var allHeroes = BoardController.Instance != null
                ? BoardController.Instance.GetAllHeroes() : new List<HeroRow>();
            if (allHeroes.Count >= 3)
            {
                var shuffled = new List<HeroRow>(allHeroes);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp;
                }
                _shopHeroes.Add(shuffled[0]);
                _shopHeroes.Add(shuffled[1]);
                _shopHeroes.Add(shuffled[2]);
            }
            else _shopHeroes = allHeroes;
            _message = "Shop! Pick a hero:";
        }
        else if (_currentEvent == EventType.FreeHero) { GiveFreeHero(); }
        else { _message = "Nothing happens. Rest."; }
    }

    void GiveFreeHero()
    {
        var bc = BoardController.Instance;
        if (bc == null) return;
        var heroes = bc.GetAllHeroes();
        if (heroes.Count == 0) return;
        var hero = heroes[Random.Range(0, heroes.Count)];
        bool placed = false;
        for (int x = GRID_START; x < GRID_END && !placed; x++)
            for (int y = GRID_START; y < GRID_END && !placed; y++)
                if (bc.IsCellEmpty(x, y) && bc.CanPlaceHero(hero.id))
                {
                    bc.PlaceHero(x, y, hero.id);
                    var cell = bc.GetCell(x, y);
                    if (cell != null) { var hob = cell.GetComponent<HeroOnBoard>(); if (hob != null) hob.Init(hero.id, x, y); }
                    placed = true;
                }
        _message = placed ? "Free hero: " + hero.name + "!" : "Board full, can't receive.";
    }

    void AdvanceRound()
    {
        Round++;
        if (Round > ROUNDS_PER_DAY) { Round = 1; Day++; _message = "Day " + Day + " begins!"; }
        CurrentState = State.Placement;
        RefreshGameUI();
        Debug.Log("[GameManager] Day=" + Day + " Round=" + Round);
    }

    bool IsBattleRound(int r) { return r == PVE_ROUND || r == PVP_ROUND; }

    // ========== EVENTS UI ==========
    void DrawEventUI()
    {
        if (_currentEvent == EventType.Shop)
        {
            GUILayout.Label("Shop - Pick one hero:");
            for (int i = 0; i < _shopHeroes.Count; i++)
            {
                var hero = _shopHeroes[i];
                string label = "[" + hero.cost + "] " + hero.name + "  HP:" + hero.hp + " ATK:" + hero.atk;
                if (GUILayout.Button(label, GUILayout.Height(30)))
                {
                    ChooseShopHero(i);
                }
            }
            if (GUILayout.Button("Skip", GUILayout.Height(30))) { _message = "Skipped shop."; AdvanceRound(); }
        }
        else if (_currentEvent == EventType.FreeHero)
        {
            GUILayout.Label(_message);
            if (GUILayout.Button("Continue", GUILayout.Height(30))) AdvanceRound();
        }
        else
        {
            GUILayout.Label(_message);
            if (GUILayout.Button("Continue", GUILayout.Height(30))) AdvanceRound();
        }
    }

    void ChooseShopHero(int index)
    {
        if (index < 0 || index >= _shopHeroes.Count) return;
        PlaceShopHero(_shopHeroes[index]);
        AdvanceRound();
    }

    void PlaceShopHero(HeroRow hero)
    {
        var bc = BoardController.Instance;
        if (bc == null) return;
        bool placed = false;
        for (int x = GRID_START; x < GRID_END && !placed; x++)
            for (int y = GRID_START; y < GRID_END && !placed; y++)
                if (bc.IsCellEmpty(x, y) && bc.CanPlaceHero(hero.id))
                {
                    bc.PlaceHero(x, y, hero.id);
                    var cell = bc.GetCell(x, y);
                    if (cell != null) { var hob = cell.GetComponent<HeroOnBoard>(); if (hob != null) hob.Init(hero.id, x, y); }
                    placed = true;
                }
        _message = placed ? "Bought: " + hero.name + "!" : "Can't place " + hero.name;
    }

    // ========== BATTLE ==========
    void DrawBattlePrepUI()
    {
        string type = Round == PVE_ROUND ? "PvE (Jungle)" : "PvP (Arena)";
        GUILayout.Label("BATTLE PREP: " + type);
        if (GUILayout.Button("Start Battle!", GUILayout.Height(36))) StartBattle();
    }

    void StartBattle()
    {
        var bc = BattleController.Instance;
        if (bc == null) { _message = "BattleController not found!"; return; }
        int fid = Round == PVE_ROUND ? 1 : 2;
        _message = Round == PVE_ROUND ? "Fighting jungle monsters..." : "Fighting arena opponent...";
        _pendingFormationId = fid;
        var table = GameTableLoader.LoadEnemyFormationsFromResources("Tables/enemy_formations");
        bc.InitBattle(GameTableLoader.BuildFormationGrid(table, fid));
        bc.RunBattle();
        _battleResultWinner = bc.Winner;
        _battleResultText = bc.Winner == 0 ? "VICTORY! (" + bc.RoundCount + " rounds)"
            : bc.Winner == 1 ? "DEFEAT! (" + bc.RoundCount + " rounds)" : "DRAW!";
        CurrentState = State.BattleOver;
        RefreshGameUI();
    }

    void DrawBattleUI() { GUILayout.Label("Battle in progress..."); }

    void DrawBattleOverUI()
    {
        GUILayout.Label(_battleResultText);
        GUILayout.Label(_battleResultWinner == 0 ? "You win!" : _battleResultWinner == 1 ? "You lose!" : "Draw!");
        GUILayout.Label("[Space] to continue");
    }

    void AdvanceFromBattle()
    {
        if (_battleResultWinner == 0)
        {
            if (Round == PVP_ROUND) { Crowns++; _message = "Crown +1! (" + Crowns + "/" + WIN_CROWNS + ")"; }
            else _message = "PvE victory!";
        }
        else if (_battleResultWinner == 1)
        {
            if (Round == PVP_ROUND)
            {
                int alive = CountAliveEnemies();
                Hp -= Mathf.Max(1, alive);
                _message = "Lost PvP! -" + Mathf.Max(1, alive) + " HP. (" + Hp + "/" + MAX_HP + ")";
            }
            else _message = "PvE defeat. No penalty.";
        }
        else _message = "Draw.";

        if (Hp <= 0) { Hp = 0; _message = "HP depleted! GAME OVER."; CurrentState = State.GameOver; return; }
        if (Crowns >= WIN_CROWNS) { _message = "10 Crowns! YOU WIN!"; CurrentState = State.GameOver; return; }

        RefreshGameUI();
        AdvanceRound();
    }

    int CountAliveEnemies()
    {
        var bc = BattleController.Instance;
        if (bc == null) return 5;
        int count = 0;
        foreach (var h in bc.EnemyHeroes) if (h.isAlive) count++;
        return count;
    }

    void DrawGameOverUI()
    {
        bool won = Crowns >= WIN_CROWNS;
        GUI.backgroundColor = won ? Color.green : Color.red;
        GUILayout.Label(won ? "YOU WIN!" : "GAME OVER", new GUIStyle(GUI.skin.label)
        {
            fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
        });
        GUI.backgroundColor = Color.white;
        GUILayout.Space(8);
        GUILayout.Label("Final: Day " + Day + "  Round " + Round);
        GUILayout.Label("HP: " + Hp + "  Crowns: " + Crowns);
        GUILayout.Space(12);
        if (GUILayout.Button("Restart Game", GUILayout.Height(40))) RestartGame();
    }

    void RestartGame()
    {
        Day = 1; Round = 1; Hp = MAX_HP; Crowns = 0;
        _message = "Day 1 begins! Place your heroes.";
        CurrentState = State.Placement;
        _battleResultText = "";
        var bc = BoardController.Instance;
        if (bc != null) bc.ClearBoard();
        if (bc != null) bc.MaxPopulation = 5;
        RefreshGameUI();
    }

    void RefreshGameUI()
    {
        
    }
}