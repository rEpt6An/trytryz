using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋盘控制器：管理 3x3 棋盘格与随从实体（FollowerEntity）。
/// 随从实体化 —— 每个随从都是独立的预制体实例，可拖拽换位、记录自己的成长。
/// </summary>
public class BoardController : MonoBehaviour
{
    public static BoardController Instance { get; private set; }

    public const int GridSize = 3;
    public const int GridStartIndex = 1;

    FollowerEntity[,] _playerFollowers = new FollowerEntity[GridSize, GridSize];
    FollowerEntity[,] _enemyFollowers = new FollowerEntity[GridSize, GridSize];
    List<CellSlot> _playerCells = new List<CellSlot>();
    List<CellSlot> _enemyCells = new List<CellSlot>();

    Dictionary<int, HeroRow> _followerDict = new Dictionary<int, HeroRow>();
    public IReadOnlyDictionary<int, HeroRow> FollowerDict { get { return _followerDict; } }

    GameObject _followerPrefab;

    public int MaxPopulation { get; set; } = 5;
    public int CurrentPopulation { get; private set; }
    public bool GodMode { get; set; } = false;

    public event System.Action<int, int, int> OnFollowerPlaced;
    public event System.Action<int, int, int> OnFollowerRemoved;
    public event System.Action OnBoardChanged;
    public event System.Action<int, int> OnPopulationChanged;

    public List<CellSlot> PlayerCells { get { return _playerCells; } }
    public List<CellSlot> EnemyCells { get { return _enemyCells; } }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadFollowerCache();
        _followerPrefab = Resources.Load<GameObject>("Prefabs/Follower");
    }

    void LoadFollowerCache()
    {
        _followerDict.Clear();
        var table = GameTableLoader.LoadHeroesFromResources("Tables/heroes");
        if (table == null || table.list == null) return;
        foreach (var h in table.list)
        {
            if (!_followerDict.ContainsKey(h.id))
                _followerDict[h.id] = h;
        }
        Debug.Log("[BoardController] Loaded " + _followerDict.Count + " followers from table.");
    }

    // ── 格子注册 ──
    public void RegisterCell(CellSlot cell)
    {
        if (cell == null) return;
        if (cell.isEnemy)
        {
            if (!_enemyCells.Contains(cell)) _enemyCells.Add(cell);
        }
        else
        {
            if (!_playerCells.Contains(cell)) _playerCells.Add(cell);
        }
    }

    public void UnregisterCell(CellSlot cell)
    {
        if (cell == null) return;
        _playerCells.Remove(cell);
        _enemyCells.Remove(cell);
    }

    public CellSlot GetCell(int x, int y)
    {
        return GetCell(x, y, false);
    }

    /// <summary>按阵营取格子（敌方格子坐标为镜像后的显示坐标）。</summary>
    public CellSlot GetCell(int x, int y, bool isEnemy)
    {
        var list = isEnemy ? _enemyCells : _playerCells;
        foreach (var c in list) if (c.gridX == x && c.gridY == y) return c;
        return null;
    }

    static int ToIdx(int coord) { return coord - GridStartIndex; }
    static bool InRange(int coord) { return coord >= GridStartIndex && coord < GridStartIndex + GridSize; }

    // ── 数据表 ──
    public HeroRow GetFollowerData(int followerId)
    {
        _followerDict.TryGetValue(followerId, out var row);
        return row;
    }

    public List<HeroRow> GetAllFollowers()
    {
        return new List<HeroRow>(_followerDict.Values);
    }

    public int GetFollowerPop(int followerId)
    {
        var row = GetFollowerData(followerId);
        return row != null ? row.pop : 999;
    }

    // ── 查询 ──
    /// <summary>我方格子上的随从实体（敌方请使用带 isEnemy 的重载）。</summary>
    public FollowerEntity GetFollowerAt(int x, int y)
    {
        return GetFollowerAt(x, y, false);
    }

    /// <summary>按阵营取随从实体（敌方坐标为镜像后的显示坐标）。</summary>
    public FollowerEntity GetFollowerAt(int x, int y, bool isEnemy)
    {
        if (!InRange(x) || !InRange(y)) return null;
        return (isEnemy ? _enemyFollowers : _playerFollowers)[ToIdx(x), ToIdx(y)];
    }

    public FollowerEntity GetFollowerEntityAt(CellSlot cell)
    {
        if (cell == null) return null;
        return GetFollowerAt(cell.gridX, cell.gridY, cell.isEnemy);
    }

    public bool IsCellEmpty(int x, int y)
    {
        return IsCellEmpty(x, y, false);
    }

    public bool IsCellEmpty(int x, int y, bool isEnemy)
    {
        if (!InRange(x) || !InRange(y)) return false;
        return GetFollowerAt(x, y, isEnemy) == null;
    }

    public bool CanPlaceFollower(int followerId)
    {
        if (GodMode) return true;
        var row = GetFollowerData(followerId);
        if (row == null) return false;
        return CurrentPopulation + row.pop <= MaxPopulation;
    }

    // ── 放置 / 移除 ──
    /// <summary>在格子上生成随从实体（实例化 Follower 预制体）。</summary>
    public bool PlaceFollower(int x, int y, int followerId, bool isEnemy = false)
    {
        if (!InRange(x) || !InRange(y)) return false;
        CellSlot cell = GetCell(x, y, isEnemy);
        if (cell == null) return false;
        if (!IsCellEmpty(x, y, isEnemy)) return false;
        if (!isEnemy && !CanPlaceFollower(followerId)) return false;

        if (_followerPrefab == null)
        {
            Debug.LogError("[BoardController] Follower prefab missing! Run menu: Trytryz > Build Follower Prefab (from Cell_of_Board)");
            return false;
        }

        GameObject go = Instantiate(_followerPrefab, cell.transform, false);
        go.name = (isEnemy ? "Enemy_" : "Follower_") + followerId;
        StretchFollower(go);

        var entity = go.GetComponent<FollowerEntity>();
        if (entity == null)
        {
            Debug.LogError("[BoardController] Follower prefab has no FollowerEntity!");
            Destroy(go);
            return false;
        }
        entity.Init(followerId, x, y, isEnemy);
        (isEnemy ? _enemyFollowers : _playerFollowers)[ToIdx(x), ToIdx(y)] = entity;

        if (!isEnemy)
        {
            int pop = GetFollowerPop(followerId);
            CurrentPopulation += pop;
            if (OnPopulationChanged != null) OnPopulationChanged(CurrentPopulation, MaxPopulation);
            if (OnFollowerPlaced != null) OnFollowerPlaced(x, y, followerId);
        }

        if (OnBoardChanged != null) OnBoardChanged();
        return true;
    }

    public bool RemoveFollower(int x, int y)
    {
        return RemoveFollower(x, y, false);
    }

    public bool RemoveFollower(int x, int y, bool isEnemy)
    {
        if (!InRange(x) || !InRange(y)) return false;
        var entity = (isEnemy ? _enemyFollowers : _playerFollowers)[ToIdx(x), ToIdx(y)];
        if (entity == null) return false;

        (isEnemy ? _enemyFollowers : _playerFollowers)[ToIdx(x), ToIdx(y)] = null;
        if (!entity.IsEnemy)
        {
            int pop = GetFollowerPop(entity.FollowerId);
            CurrentPopulation -= pop;
            if (OnPopulationChanged != null) OnPopulationChanged(CurrentPopulation, MaxPopulation);
            if (OnFollowerRemoved != null) OnFollowerRemoved(x, y, entity.FollowerId);
        }
        if (OnBoardChanged != null) OnBoardChanged();
        Destroy(entity.gameObject);
        return true;
    }

    /// <summary>拖拽换位：目标为空则移动，目标有随从则交换。</summary>
    public bool MoveFollower(CellSlot from, CellSlot to)
    {
        if (from == null || to == null || from.isEnemy || to.isEnemy) return false;
        if (from == to) return false;
        var moving = GetFollowerEntityAt(from);
        if (moving == null) return false;

        var swapped = GetFollowerEntityAt(to);
        int fx = from.gridX, fy = from.gridY;
        int tx = to.gridX, ty = to.gridY;

        if (swapped != null)
        {
            _playerFollowers[ToIdx(fx), ToIdx(fy)] = swapped;
            swapped.SetGridPosition(fx, fy);
            ReparentFollower(swapped, from);
        }
        else
        {
            _playerFollowers[ToIdx(fx), ToIdx(fy)] = null;
        }

        _playerFollowers[ToIdx(tx), ToIdx(ty)] = moving;
        moving.SetGridPosition(tx, ty);
        // moving 实体由 FollowerDragHandler 在吸附动画结束后自己挂到目标格

        if (OnBoardChanged != null) OnBoardChanged();
        return true;
    }

    /// <summary>把实体立刻铺到某个格子（用于交换时被换出的那个）。</summary>
    public void ReparentFollower(FollowerEntity entity, CellSlot cell)
    {
        if (entity == null || cell == null) return;
        entity.transform.SetParent(cell.transform, false);
        StretchFollower(entity.gameObject);
        entity.SetGridPosition(cell.gridX, cell.gridY);
        entity.RefreshVisuals();
    }

    void StretchFollower(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localPosition = Vector3.zero;
        rt.localScale = Vector3.one;
    }

    // ── 批量操作 ──
    public void ClearBoard()
    {
        for (int x = GridStartIndex; x < GridStartIndex + GridSize; x++)
            for (int y = GridStartIndex; y < GridStartIndex + GridSize; y++)
                RemoveFollower(x, y);
    }

    public void ClearEnemyBoard()
    {
        var snapshot = new List<FollowerEntity>();
        foreach (var cell in _enemyCells)
        {
            var e = GetFollowerEntityAt(cell);
            if (e != null) snapshot.Add(e);
        }
        foreach (var e in snapshot)
        {
            int x = e.GridX, y = e.GridY;
            if (InRange(x) && InRange(y) && _enemyFollowers[ToIdx(x), ToIdx(y)] == e)
                _enemyFollowers[ToIdx(x), ToIdx(y)] = null;
            Destroy(e.gameObject);
        }
        if (OnBoardChanged != null) OnBoardChanged();
    }

    public void FillBoardWithRandom()
    {
        ClearBoard();
        var heroes = GetAllFollowers();
        if (heroes.Count == 0) return;

        int totalCells = GridSize * GridSize;
        int[] posX = new int[totalCells];
        int[] posY = new int[totalCells];
        int idx = 0;
        for (int x = GridStartIndex; x < GridStartIndex + GridSize; x++)
            for (int y = GridStartIndex; y < GridStartIndex + GridSize; y++)
            { posX[idx] = x; posY[idx] = y; idx++; }

        for (int i = totalCells - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tx = posX[i]; posX[i] = posX[j]; posX[j] = tx;
            int ty = posY[i]; posY[i] = posY[j]; posY[j] = ty;
        }

        int placed = 0;
        for (int i = 0; i < totalCells; i++)
        {
            if (placed >= MaxPopulation && !GodMode) break;
            var hero = heroes[Random.Range(0, heroes.Count)];
            if (PlaceFollower(posX[i], posY[i], hero.id))
                placed += hero.pop;
        }
    }

    /// <summary>根据阵容表在敌方棋盘上生成随从（自动左右镜像显示）。</summary>
    public void SetupEnemyFormation(int formationId)
    {
        ClearEnemyBoard();
        var table = GameTableLoader.LoadEnemyFormationsFromResources("Tables/enemy_formations");
        if (table == null) return;

        int[,] grid = GameTableLoader.BuildFormationGrid(table, formationId);
        for (int x0 = 0; x0 < GridSize; x0++)
        {
            for (int y0 = 0; y0 < GridSize; y0++)
            {
                int heroId = grid[x0, y0];
                if (heroId == 0) continue;
                int mirrorX = GridStartIndex + (GridSize - 1 - x0); // 左右镜像后的 1 基列号
                int gy = y0 + 1;
                PlaceFollower(mirrorX, gy, heroId, true);
            }
        }
    }

    public void RefreshDisplay()
    {
        if (OnBoardChanged != null) OnBoardChanged();
        if (OnPopulationChanged != null) OnPopulationChanged(CurrentPopulation, MaxPopulation);
    }
}
