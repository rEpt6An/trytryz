using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public static BoardController Instance { get; private set; }

    public const int GridSize = 3;
    int[,] _board = new int[GridSize, GridSize];

    public int MaxPopulation { get; set; } = 5;
    public int CurrentPopulation { get; private set; } = 0;
    public bool GodMode { get; set; } = false;

    Dictionary<int, HeroRow> _heroDict = new Dictionary<int, HeroRow>();
    public IReadOnlyDictionary<int, HeroRow> HeroDict { get { return _heroDict; } }

    public event System.Action<int, int, int> OnHeroPlaced;
    public event System.Action<int, int, int> OnHeroRemoved;
    public event System.Action OnBoardChanged;
    public event System.Action<int, int> OnPopulationChanged;

    CellSlot[,] _cells = new CellSlot[GridSize, GridSize];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadHeroCache();
    }

    void LoadHeroCache()
    {
        _heroDict.Clear();
        var table = GameTableLoader.LoadHeroesFromResources("Tables/heroes");
        if (table == null || table.list == null) return;
        foreach (var h in table.list)
        {
            if (!_heroDict.ContainsKey(h.id))
                _heroDict[h.id] = h;
        }
        Debug.Log("[BoardController] Loaded " + _heroDict.Count + " heroes from cache.");
    }

    public void RegisterCell(int x, int y, CellSlot cell)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return;
        _cells[x, y] = cell;
    }

    public CellSlot GetCell(int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return null;
        return _cells[x, y];
    }

    public int GetHeroAt(int x, int y) { return _board[x, y]; }

    public HeroRow GetHeroData(int heroId)
    {
        _heroDict.TryGetValue(heroId, out var row);
        return row;
    }

    public bool IsCellEmpty(int x, int y) { return _board[x, y] == 0; }

    public bool CanPlaceHero(int heroId)
    {
        if (GodMode) return true;
        var hero = GetHeroData(heroId);
        if (hero == null) return false;
        return CurrentPopulation + hero.cost <= MaxPopulation;
    }

    public int GetHeroCost(int heroId)
    {
        var hero = GetHeroData(heroId);
        return hero != null ? hero.cost : 999;
    }

    public List<HeroRow> GetAllHeroes()
    {
        return new List<HeroRow>(_heroDict.Values);
    }

    public bool PlaceHero(int x, int y, int heroId)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return false;
        if (_board[x, y] != 0) return false;
        if (!GodMode && !CanPlaceHero(heroId)) return false;

        int cost = GetHeroCost(heroId);
        _board[x, y] = heroId;
        CurrentPopulation += cost;

        if (OnHeroPlaced != null) OnHeroPlaced(x, y, heroId);
        if (OnBoardChanged != null) OnBoardChanged();
        if (OnPopulationChanged != null) OnPopulationChanged(CurrentPopulation, MaxPopulation);

        return true;
    }

    public bool RemoveHero(int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return false;
        int heroId = _board[x, y];
        if (heroId == 0) return false;

        int cost = GetHeroCost(heroId);
        _board[x, y] = 0;
        CurrentPopulation -= cost;

        if (OnHeroRemoved != null) OnHeroRemoved(x, y, heroId);
        if (OnBoardChanged != null) OnBoardChanged();
        if (OnPopulationChanged != null) OnPopulationChanged(CurrentPopulation, MaxPopulation);

        return true;
    }

    public void ClearBoard()
    {
        for (int x = 0; x < GridSize; x++)
            for (int y = 0; y < GridSize; y++)
                RemoveHero(x, y);
    }

    public void FillBoardWithRandom()
    {
        ClearBoard();
        var heroes = GetAllHeroes();
        if (heroes.Count == 0) return;

        // Build shuffled positions for truly random placement
        int totalCells = GridSize * GridSize;
        int[] posX = new int[totalCells];
        int[] posY = new int[totalCells];
        int idx = 0;
        for (int x = 0; x < GridSize; x++)
            for (int y = 0; y < GridSize; y++)
            {
                posX[idx] = x;
                posY[idx] = y;
                idx++;
            }

        // Fisher-Yates shuffle
        for (int i = totalCells - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tx = posX[i]; posX[i] = posX[j]; posX[j] = tx;
            int ty = posY[i]; posY[i] = posY[j]; posY[j] = ty;
        }

        // Fill shuffled positions until population limit
        int placed = 0;
        for (int i = 0; i < totalCells; i++)
        {
            if (placed >= MaxPopulation && !GodMode) break;
            var hero = heroes[UnityEngine.Random.Range(0, heroes.Count)];
            if (PlaceHero(posX[i], posY[i], hero.id))
                placed += hero.cost;
        }
    }

    // Allow external systems (Debug panel) to force a UI refresh
    public void RefreshDisplay()
    {
        if (OnBoardChanged != null) OnBoardChanged();
        if (OnPopulationChanged != null) OnPopulationChanged(CurrentPopulation, MaxPopulation);
    }
}