using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗控制器：我方/敌方所有随从同时开始读秒，
/// 每个随从经过自身 cd（秒）后攻击目标，循环往复直到一方全部阵亡。
/// 使用 Time.deltaTime 驱动，保证 CD 严格按秒走。
/// </summary>
public class BattleController : MonoBehaviour
{
    public static BattleController Instance { get; private set; }

    public enum BattleState { Idle, Running, Finished }
    public BattleState State { get; private set; } = BattleState.Idle;

    public int Winner { get; private set; } = -1;
    public int ActionCount { get; private set; }
    public float BattleSpeed { get; set; } = 1f;

    List<BattleHero> _playerHeroes = new List<BattleHero>();
    List<BattleHero> _enemyHeroes = new List<BattleHero>();
    List<string> _battleLog = new List<string>();
    Dictionary<int, BattleHero> _heroByPos = new Dictionary<int, BattleHero>();
    Dictionary<int, FollowerEntity> _entityByKey = new Dictionary<int, FollowerEntity>();

    public List<BattleHero> PlayerHeroes { get { return _playerHeroes; } }
    public List<BattleHero> EnemyHeroes { get { return _enemyHeroes; } }
    public List<string> BattleLog { get { return _battleLog; } }

    public event System.Action<int, int> OnBattleEnd;

    float _battleTime;
    const float MAX_BATTLE_TIME = 300f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (State != BattleState.Running) return;

        float dt = Time.deltaTime * BattleSpeed;
        _battleTime += dt;
        if (_battleTime > MAX_BATTLE_TIME)
        {
            Log("Timeout. Draw.");
            EndBattle(-1);
            return;
        }

        Tick(dt);

        int ap = CountAlive(_playerHeroes);
        int ae = CountAlive(_enemyHeroes);
        if (ap == 0 && ae == 0) EndBattle(-1);
        else if (ap == 0) EndBattle(1);
        else if (ae == 0) EndBattle(0);
    }

    public BattleHero GetBattleHeroAt(int team, int gridX0, int gridY0)
    {
        int key = team * 100 + gridX0 * 10 + gridY0;
        _heroByPos.TryGetValue(key, out var bh);
        return bh;
    }

    /// <summary>CD 进度：0 = 刚开始读秒（全暗），1 = CD 完成（全亮）。</summary>
    public float GetCdProgressAt(int team, int gridX0, int gridY0)
    {
        var bh = GetBattleHeroAt(team, gridX0, gridY0);
        if (bh == null || bh.cd <= 0f) return 1f;
        return Mathf.Clamp01(1f - bh.cdTimer / bh.cd);
    }

    /// <summary>读取我方随从实体（含永久/局内成长）与敌方阵容，构建战斗快照。</summary>
    public void InitBattle(int[,] enemyGrid)
    {
        _battleLog.Clear();
        _playerHeroes.Clear();
        _enemyHeroes.Clear();
        _heroByPos.Clear();
        _entityByKey.Clear();
        Winner = -1;
        ActionCount = 0;
        _battleTime = 0f;
        State = BattleState.Idle;

        var bc = BoardController.Instance;
        if (bc == null) return;

        int start = BoardController.GridStartIndex;
        int end = start + BoardController.GridSize;

        // 我方：从随从实体取 原始 + 永久成长 + 局内成长 后的总属性
        for (int x = start; x < end; x++)
        {
            for (int y = start; y < end; y++)
            {
                var entity = bc.GetFollowerAt(x, y);
                if (entity == null || entity.FollowerId == 0) continue;
                var data = entity.Data;
                if (data == null) continue;

                int gx = x - start, gy = y - start;
                var bh = new BattleHero
                {
                    heroId = entity.FollowerId,
                    heroName = data.name,
                    team = 0,
                    gridX = gx,
                    gridY = gy,
                    maxHp = entity.CurrentMaxHp,
                    currentHp = entity.CurrentHp,
                    atk = entity.TotalAtk,
                    magicAtk = entity.TotalMagicAtk,
                    shield = entity.TotalShield,
                    crit = entity.TotalCrit,
                    hit = entity.TotalHit,
                    dodge = entity.TotalDodge,
                    cd = Mathf.Max(0.1f, entity.BaseCd),
                    targetMode = data.target,
                    cdTimer = Mathf.Max(0.1f, entity.BaseCd),
                    isAlive = true
                };
                _playerHeroes.Add(bh);
                _heroByPos[0 * 100 + gx * 10 + gy] = bh;
                _entityByKey[0 * 100 + gx * 10 + gy] = entity;
            }
        }

        // 敌方：按阵容表（已左右镜像后的坐标）生成，属性为原始属性
        for (int x0 = 0; x0 < BoardController.GridSize; x0++)
        {
            for (int y0 = 0; y0 < BoardController.GridSize; y0++)
            {
                int heroId = enemyGrid[x0, y0];
                if (heroId == 0) continue;
                var data = bc.GetFollowerData(heroId);
                if (data == null) continue;

                int mgx = (BoardController.GridSize - 1) - x0; // 左右镜像
                var bh = new BattleHero
                {
                    heroId = heroId,
                    heroName = data.name,
                    team = 1,
                    gridX = mgx,
                    gridY = y0,
                    maxHp = data.hp,
                    currentHp = data.hp,
                    atk = data.atk,
                    magicAtk = data.magicAtk,
                    shield = data.shield,
                    crit = data.crit,
                    hit = data.hit,
                    dodge = data.dodge,
                    cd = Mathf.Max(0.1f, data.cd),
                    targetMode = data.target,
                    cdTimer = Mathf.Max(0.1f, data.cd),
                    isAlive = true
                };
                _enemyHeroes.Add(bh);
                _heroByPos[1 * 100 + mgx * 10 + y0] = bh;

                // 敌方格子上显示的随从实体（用于攻击震动）
                var entity = bc.GetFollowerAt(mgx + 1, y0 + 1, true);
                if (entity != null && entity.FollowerId == heroId)
                    _entityByKey[1 * 100 + mgx * 10 + y0] = entity;
            }
        }

        Debug.Log("[BattleController] Init: player=" + _playerHeroes.Count + " enemy=" + _enemyHeroes.Count);
    }

    public void RunBattle()
    {
        if (State == BattleState.Running) return;
        if (_playerHeroes.Count == 0) { Log("No player followers! Auto-lose."); EndBattle(1); return; }
        if (_enemyHeroes.Count == 0) { Log("No enemy followers! Auto-win."); EndBattle(0); return; }
        State = BattleState.Running;
    }

    void Tick(float dt)
    {
        var all = new List<BattleHero>();
        foreach (var h in _playerHeroes) if (h.isAlive) all.Add(h);
        foreach (var h in _enemyHeroes) if (h.isAlive) all.Add(h);

        foreach (var h in all)
        {
            h.cdTimer -= dt;
            if (h.cdTimer <= 0f)
            {
                h.cdTimer = h.cd; // 攻击后重新开始读秒
                PerformAttack(h);
            }
        }
    }

    void PerformAttack(BattleHero attacker)
    {
        var target = FindTarget(attacker);
        if (target == null) return;

        int effHit = Mathf.Clamp(attacker.hit - target.dodge, 0, 100);
        bool missed = Random.Range(0, 100) >= effHit;
        bool crit = !missed && Random.Range(0, 100) < attacker.crit;
        float cm = crit ? 1.5f : 1f;

        int totalDmg = 0;
        string msg;
        if (missed)
        {
            msg = "[" + attacker.heroName + "] missed [" + target.heroName + "]!";
        }
        else
        {
            // 物理伤害：先打护盾后扣血
            int phys = Mathf.RoundToInt(attacker.atk * cm);
            if (target.shield > 0)
            {
                int absorbed = Mathf.Min(target.shield, phys);
                target.shield -= absorbed;
                phys -= absorbed;
            }
            target.currentHp -= phys;
            totalDmg += phys;

            // 魔法攻击：无视护盾直接造成血量伤害
            int magic = Mathf.RoundToInt(attacker.magicAtk * cm);
            if (magic > 0)
            {
                target.currentHp -= magic;
                totalDmg += magic;
            }

            if (target.currentHp <= 0)
            {
                target.currentHp = 0;
                target.shield = 0;
                target.isAlive = false;
                msg = (crit ? "***CRIT*** " : "") + "[" + attacker.heroName + "] -> [" + target.heroName + "]"
                    + " DMG:" + totalDmg + " HP:" + target.currentHp + "/" + target.maxHp + " [DEAD]";
            }
            else
            {
                msg = (crit ? "***CRIT*** " : "") + "[" + attacker.heroName + "] -> [" + target.heroName + "]"
                    + " DMG:" + totalDmg + " HP:" + target.currentHp + "/" + target.maxHp + " S:" + target.shield;
            }
        }
        ActionCount++;
        Log(msg);

        // 攻击动效：随从方块朝攻击目标方向震动（敌我棋盘相邻，x 方向带 1 格间隔）
        var atkEntity = GetEntity(attacker);
        if (atkEntity != null)
        {
            Vector2 dir = new Vector2(PhysicalDx(attacker, target), target.gridY - attacker.gridY);
            atkEntity.PlayAttackShake(dir);
        }
    }

    FollowerEntity GetEntity(BattleHero hero)
    {
        int key = hero.team * 100 + hero.gridX * 10 + hero.gridY;
        _entityByKey.TryGetValue(key, out var e);
        return e;
    }

    /// <summary>
    /// 攻击目标：
    ///   最近 —— 到每个敌方位置向量绝对值最小的目标，相等则随机；
    ///   直线 —— 优先同一行方向上最近，该方向无敌人后转为最近；
    ///   随机 —— 随机敌方随从。
    /// </summary>
    BattleHero FindTarget(BattleHero attacker)
    {
        var enemies = attacker.team == 0 ? _enemyHeroes : _playerHeroes;
        var alive = new List<BattleHero>();
        foreach (var e in enemies) if (e.isAlive) alive.Add(e);
        if (alive.Count == 0) return null;

        string mode = attacker.targetMode ?? "";

        if (mode.Contains("随机"))
            return alive[Random.Range(0, alive.Count)];

        if (mode.Contains("直线"))
        {
            var sameColumn = new List<BattleHero>();
            foreach (var e in alive)
                if (e.gridX == attacker.gridX) sameColumn.Add(e);
            if (sameColumn.Count > 0)
            {
                sameColumn.Sort((a, b) => Mathf.Abs(a.gridY - attacker.gridY).CompareTo(Mathf.Abs(b.gridY - attacker.gridY)));
                return sameColumn[0];
            }
        }

        // 最近（默认）：我方棋盘在左、敌方棋盘在右（敌方左右镜像，显示列 3 靠玩家侧），
        // 距离 = 双方物理位置的欧氏距离（含两棋盘之间的 1 格间隔），相等时随机
        BattleHero best = null;
        float bestDist = float.MaxValue;
        foreach (var e in alive)
        {
            float dx = PhysicalDx(attacker, e);
            float dy = attacker.gridY - e.gridY;
            float d = dx * dx + dy * dy;
            if (d < bestDist - 0.0001f)
            {
                bestDist = d;
                best = e;
            }
            else if (Mathf.Abs(d - bestDist) < 0.0001f && Random.value < 0.5f)
            {
                best = e;
            }
        }
        return best;
    }

    /// <summary>
    /// 从攻击者指向目标在 x 方向的真实屏幕分量（正 = 目标在右）。
    /// 我方棋盘在左：我方列 gx 位于 x = gx + 1；
    /// 敌方棋盘在右且左右镜像：敌方显示列 mgx 位于 x = 2*GridSize - mgx
    /// （即敌方显示列 3 在最左、紧邻我方列 3，距离 1 格）。
    /// </summary>
    float PhysicalDx(BattleHero attacker, BattleHero target)
    {
        int gs = BoardController.GridSize;
        if (attacker.team == 0)
            return 2 * gs - 1 - attacker.gridX - target.gridX;
        return attacker.gridX + target.gridX - (2 * gs - 1);
    }

    int CountAlive(List<BattleHero> list)
    {
        int c = 0;
        foreach (var h in list) if (h.isAlive) c++;
        return c;
    }

    void Log(string msg) { _battleLog.Add(msg); }

    void EndBattle(int winner)
    {
        State = BattleState.Finished;
        Winner = winner;

        // 战斗结束：我方所有随从属性回到 原始属性 + 永久成长（局内成长清除）
        var bc = BoardController.Instance;
        if (bc != null)
        {
            int s = BoardController.GridStartIndex, e = s + BoardController.GridSize;
            for (int x = s; x < e; x++)
                for (int y = s; y < e; y++)
                {
                    var entity = bc.GetFollowerAt(x, y);
                    if (entity != null && !entity.IsEnemy)
                        entity.ResetBattleGrowth();
                }
        }

        if (OnBattleEnd != null) OnBattleEnd(winner, ActionCount);
    }

    public void StartBattleWithFormation(int formationId)
    {
        var table = GameTableLoader.LoadEnemyFormationsFromResources("Tables/enemy_formations");
        if (table == null) { Debug.LogError("[BattleController] No formations."); return; }
        InitBattle(GameTableLoader.BuildFormationGrid(table, formationId));
        RunBattle();
    }
}
