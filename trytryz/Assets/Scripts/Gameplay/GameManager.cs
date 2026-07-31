using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main game loop controller.
/// Manages day/round cycle, events, battles, HP and crowns.
/// Press F3 to toggle game UI.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Game State ──────────────────────────────
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

    // ── Event Data ──────────────────────────────
    enum EventType { Shop, FreeHero, Rest }
    EventType _currentEvent;
    List<HeroRow> _shopHeroes = new List<HeroRow>();
    int _pickedShopIndex = -1;

    // ── UI ───────────────────────────────────────
    bool _showUI = true;
    Rect _uiRect = new Rect(10, 10, 340, 520);
    string _message = "";
    string _battleResultText = "";
    int _battleResultWinner = -1;

    // ── Battle ───────────────────────────────────
    int _pendingFormationId = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _message = "Day " + Day + " begins! Place your heroes.";
        Debug.Log("[GameManager] Game started. Day=" + Day + " Round=" + Round);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
            _showUI = !_showUI;

        // Auto-advance after battle
        if (CurrentState == State.BattleOver && Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceFromBattle();
        }
    }

    void OnGUI()
    {
        if (!_showUI) return;

        _uiRect = GUILayout.Window(996, _uiRect, DrawGameUI, "Game [F3=hide]");
    }

    void DrawGameUI(int id)
    {
        // ── Status Bar ──────────────────────────
        GUILayout.Label("Day " + Day + "  |  Round " + Round + " / " + ROUNDS_PER_DAY);
        GUILayout.Label("HP: " + Hp + " / " + MAX_HP + "    Crowns: " + Crowns + " / " + WIN_CROWNS);
        GUILayout.Label("State: " + CurrentState);
        GUILayout.Space(6);

        if (!string.IsNullOrEmpty(_message))
        {
            GUILayout.Label(_message);
            GUILayout.Space(4);
        }

        // ── Per-state UI ────────────────────────
        switch (CurrentState)
        {
            case State.Placement:
                DrawPlacementUI();
                break;
            case State.Event:
                DrawEventUI();
                break;
            case State.BattlePrep:
                DrawBattlePrepUI();
                break;
            case State.Battle:
                DrawBattleUI();
                break;
            case State.BattleOver:
                DrawBattleOverUI();
                break;
            case State.GameOver:
                DrawGameOverUI();
                break;
        }

        GUI.DragWindow();
    }

    // ══════════════════════════════════════════════
    //  PLACEMENT
    // ══════════════════════════════════════════════
    void DrawPlacementUI()
    {
        GUILayout.Label("Place heroes on the board, then:");
        GUILayout.Space(4);

        if (GUILayout.Button("Next Round", GUILayout.Height(36)))
        {
            StartRound();
        }
    }

    void StartRound()
    {
        if (IsBattleRound(Round))
        {
            CurrentState = State.BattlePrep;
            _message = "Round " + Round + ": Battle incoming! Adjust your board.";
            return;
        }

        // Random event
        RollEvent();
        CurrentState = State.Event;
    }

    // ══════════════════════════════════════════════
    //  EVENTS
    // ══════════════════════════════════════════════
    void RollEvent()
    {
        float roll = Random.value;
        if (roll < 0.5f)
            _currentEvent = EventType.Shop;
        else if (roll < 0.85f)
            _currentEvent = EventType.FreeHero;
        else
            _currentEvent = EventType.Rest;

        _shopHeroes.Clear();
        _pickedShopIndex = -1;

        if (_currentEvent == EventType.Shop)
        {
            var allHeroes = BoardController.Instance != null
                ? BoardController.Instance.GetAllHeroes()
                : new List<HeroRow>();

            if (allHeroes.Count >= 3)
            {
                // Pick 3 random heroes for the shop
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
            else
            {
                _shopHeroes = allHeroes;
            }

            _message = "Shop! Pick a hero:";
        }
        else if (_currentEvent == EventType.FreeHero)
        {
            GiveFreeHero();
        }
        else
        {
            _message = "Nothing happens. Rest.";
        }
    }

    void GiveFreeHero()
    {
        var bc = BoardController.Instance;
        if (bc == null) return;

        var heroes = bc.GetAllHeroes();
        if (heroes.Count == 0) return;

        var hero = heroes[Random.Range(0, heroes.Count)];

        // Find an empty cell
        bool placed = false;
        for (int x = 0; x < 3 && !placed; x++)
        {
            for (int y = 0; y < 3 && !placed; y++)
            {
                if (bc.IsCellEmpty(x, y) && bc.CanPlaceHero(hero.id))
                {
                    bc.PlaceHero(x, y, hero.id);
                    var cell = bc.GetCell(x, y);
                    if (cell != null)
                    {
                        var hob = cell.GetComponent<HeroOnBoard>();
                        if (hob != null) hob.Init(hero.id, x, y);
                    }
                    placed = true;
                }
            }
        }

        _message = placed
            ? "Free Hero: " + hero.name + " joined!"
            : "Free Hero: " + hero.name + " but no space/pop!";
    }

    void DrawEventUI()
    {
        GUILayout.Label("Event: " + _currentEvent);
        GUILayout.Space(4);

        switch (_currentEvent)
        {
            case EventType.Shop:
                DrawShop();
                break;
            case EventType.FreeHero:
            case EventType.Rest:
                if (GUILayout.Button("Continue", GUILayout.Height(36)))
                    AdvanceRound();
                break;
        }
    }

    void DrawShop()
    {
        for (int i = 0; i < _shopHeroes.Count; i++)
        {
            var hero = _shopHeroes[i];
            bool canAfford = BoardController.Instance != null
                && BoardController.Instance.CanPlaceHero(hero.id);

            GUI.enabled = canAfford;
            GUI.backgroundColor = canAfford
                ? new Color(0.2f, 0.4f, 0.2f)
                : new Color(0.4f, 0.15f, 0.15f);

            string label = "[" + hero.cost + "] " + hero.name + "  " + hero.job
                + "  HP:" + hero.hp + "  ATK:" + hero.atk;

            if (GUILayout.Button(label, GUILayout.Height(32)))
            {
                _pickedShopIndex = i;
                PlaceShopHero(hero);
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }

        GUILayout.Space(8);

        if (GUILayout.Button("Skip (buy nothing)", GUILayout.Height(30)))
        {
            _message = "Skipped shop.";
            AdvanceRound();
        }
    }

    void PlaceShopHero(HeroRow hero)
    {
        var bc = BoardController.Instance;
        if (bc == null) return;

        // Find first empty cell
        bool placed = false;
        for (int x = 0; x < 3 && !placed; x++)
        {
            for (int y = 0; y < 3 && !placed; y++)
            {
                if (bc.IsCellEmpty(x, y) && bc.CanPlaceHero(hero.id))
                {
                    bc.PlaceHero(x, y, hero.id);
                    var cell = bc.GetCell(x, y);
                    if (cell != null)
                    {
                        var hob = cell.GetComponent<HeroOnBoard>();
                        if (hob != null) hob.Init(hero.id, x, y);
                    }
                    placed = true;
                }
            }
        }

        _message = placed
            ? "Bought: " + hero.name + "!"
            : "Can't place " + hero.name + " (no space/population)";
    }

    // ══════════════════════════════════════════════
    //  BATTLE
    // ══════════════════════════════════════════════
    bool IsBattleRound(int r)
    {
        return r == PVE_ROUND || r == PVP_ROUND;
    }

    void DrawBattlePrepUI()
    {
        string battleType = Round == PVE_ROUND ? "PvE (Jungle)" : "PvP (Arena)";
        GUILayout.Label("BATTLE PREP: " + battleType);
        GUILayout.Label("Adjust your board, then:");

        if (GUILayout.Button("Start Battle!", GUILayout.Height(36)))
        {
            StartBattle();
        }
    }

    void StartBattle()
    {
        var bc = BattleController.Instance;
        if (bc == null)
        {
            _message = "BattleController not found!";
            return;
        }

        int formationId;
        if (Round == PVE_ROUND)
        {
            formationId = 1; // PvE formation
            _message = "Round " + Round + ": Fighting jungle monsters...";
        }
        else
        {
            formationId = 2; // PvP formation
            _message = "Round " + Round + ": Fighting arena opponent...";
        }

        _pendingFormationId = formationId;
        bc.InitBattle(GameTableLoader.BuildFormationGrid(
            GameTableLoader.LoadEnemyFormationsFromResources("Tables/enemy_formations"),
            formationId));
        bc.RunBattle();

        _battleResultWinner = bc.Winner;
        _battleResultText = bc.Winner == 0
            ? "VICTORY! (" + bc.RoundCount + " rounds)"
            : bc.Winner == 1
                ? "DEFEAT! (" + bc.RoundCount + " rounds)"
                : "DRAW! (" + bc.RoundCount + " rounds)";

        CurrentState = State.BattleOver;
    }

    void DrawBattleUI()
    {
        GUILayout.Label("Battle in progress...");
    }

    void DrawBattleOverUI()
    {
        GUILayout.Label(_battleResultText);

        if (_battleResultWinner == 0)
        {
            GUILayout.Label("You win!");
        }
        else if (_battleResultWinner == 1)
        {
            GUILayout.Label("You lose!");
        }
        else
        {
            GUILayout.Label("Draw!");
        }

        GUILayout.Space(8);
        GUILayout.Label("[Space] to continue");
    }

    void AdvanceFromBattle()
    {
        // Apply battle consequences
        if (_battleResultWinner == 0)
        {
            // Win
            if (Round == PVP_ROUND)
            {
                Crowns++;
                _message = "Crown +1! (" + Crowns + "/" + WIN_CROWNS + ")";
            }
            else
            {
                _message = "PvE victory! No penalty if lost anyway.";
            }
        }
        else if (_battleResultWinner == 1)
        {
            // Lose
            if (Round == PVP_ROUND)
            {
                int aliveEnemies = CountAliveEnemies();
                Hp -= Mathf.Max(1, aliveEnemies);
                _message = "Lost PvP! -" + Mathf.Max(1, aliveEnemies) + " HP. (" + Hp + "/" + MAX_HP + ")";
            }
            else
            {
                _message = "PvE defeat. No penalty.";
            }
        }
        else
        {
            _message = "Draw. No effect.";
        }

        // Check game over
        if (Hp <= 0)
        {
            Hp = 0;
            _message = "HP depleted! GAME OVER.";
            CurrentState = State.GameOver;
            return;
        }
        if (Crowns >= WIN_CROWNS)
        {
            _message = "10 Crowns! YOU WIN!";
            CurrentState = State.GameOver;
            return;
        }

        AdvanceRound();
    }

    int CountAliveEnemies()
    {
        var bc = BattleController.Instance;
        if (bc == null) return 5;
        int count = 0;
        foreach (var h in bc.EnemyHeroes)
            if (h.isAlive) count++;
        return count;
    }

    // ══════════════════════════════════════════════
    //  ROUND / DAY ADVANCE
    // ══════════════════════════════════════════════
    void AdvanceRound()
    {
        Round++;

        if (Round > ROUNDS_PER_DAY)
        {
            Round = 1;
            Day++;
            _message = "Day " + Day + " begins!";
        }

        CurrentState = State.Placement;
        Debug.Log("[GameManager] Day=" + Day + " Round=" + Round);
    }

    // ══════════════════════════════════════════════
    //  GAME OVER
    // ══════════════════════════════════════════════
    void DrawGameOverUI()
    {
        bool won = Crowns >= WIN_CROWNS;
        GUI.backgroundColor = won ? Color.green : Color.red;
        GUILayout.Label(won ? "YOU WIN!" : "GAME OVER", new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        });
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);
        GUILayout.Label("Final: Day " + Day + "  Round " + Round);
        GUILayout.Label("HP: " + Hp + "  Crowns: " + Crowns);

        GUILayout.Space(12);
        if (GUILayout.Button("Restart Game", GUILayout.Height(40)))
        {
            RestartGame();
        }
    }

    void RestartGame()
    {
        Day = 1;
        Round = 1;
        Hp = MAX_HP;
        Crowns = 0;
        _message = "Day 1 begins! Place your heroes.";
        CurrentState = State.Placement;
        _battleResultText = "";

        var bc = BoardController.Instance;
        if (bc != null) bc.ClearBoard();
        if (bc != null) bc.MaxPopulation = 5;
    }
}