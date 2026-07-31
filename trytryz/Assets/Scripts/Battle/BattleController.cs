using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto-battle controller. Simulates 3x3 board combat.
/// Player board from BoardController, enemy from EnemyFormationTable.
/// </summary>
public class BattleController : MonoBehaviour
{
    public static BattleController Instance { get; private set; }

    public enum BattleState { Idle, Running, Finished }
    public BattleState State { get; private set; } = BattleState.Idle;

    List<BattleHero> _playerHeroes = new List<BattleHero>();
    List<BattleHero> _enemyHeroes = new List<BattleHero>();

    List<string> _battleLog = new List<string>();
    public List<string> BattleLog { get { return _battleLog; } }

    int _roundCount = 0;
    int _winner = -1; // 0=player, 1=enemy, -1=draw

    public int RoundCount { get { return _roundCount; } }
    public int Winner { get { return _winner; } }
    public List<BattleHero> PlayerHeroes { get { return _playerHeroes; } }
    public List<BattleHero> EnemyHeroes { get { return _enemyHeroes; } }

    public event System.Action OnBattleStart;
    public event System.Action<string> OnBattleTick;
    public event System.Action<int, int> OnBattleEnd; // winner, roundCount

    const int MAX_ROUNDS = 50;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Init battle from player board and enemy formation grid.
    /// </summary>
    public void InitBattle(int[,] enemyGrid)
    {
        _battleLog.Clear();
        _playerHeroes.Clear();
        _enemyHeroes.Clear();
        _roundCount = 0;
        _winner = -1;
        State = BattleState.Idle;

        var bc = BoardController.Instance;
        if (bc == null) return;

        // Build player heroes from board
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                int heroId = bc.GetHeroAt(x, y);
                if (heroId == 0) continue;

                var data = bc.GetHeroData(heroId);
                if (data == null) continue;

                _playerHeroes.Add(new BattleHero
                {
                    heroId = heroId,
                    heroName = data.name,
                    team = 0,
                    gridX = x,
                    gridY = y,
                    maxHp = data.hp,
                    currentHp = data.hp,
                    atk = data.atk,
                    cd = data.cd,
                    isAlive = true
                });
            }
        }

        // Build enemy heroes from formation grid
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                int heroId = enemyGrid[x, y];
                if (heroId == 0) continue;

                var data = bc.GetHeroData(heroId);
                if (data == null) continue;

                _enemyHeroes.Add(new BattleHero
                {
                    heroId = heroId,
                    heroName = data.name,
                    team = 1,
                    gridX = x,
                    gridY = y,
                    maxHp = data.hp,
                    currentHp = data.hp,
                    atk = data.atk,
                    cd = data.cd,
                    isAlive = true
                });
            }
        }

        Debug.Log("[BattleController] Init: player=" + _playerHeroes.Count + " enemy=" + _enemyHeroes.Count);
    }

    /// <summary>
    /// Run the full battle simulation and return result.
    /// </summary>
    public void RunBattle()
    {
        if (_playerHeroes.Count == 0)
        {
            Log("Player has no heroes! Auto-lose.");
            EndBattle(1);
            return;
        }
        if (_enemyHeroes.Count == 0)
        {
            Log("Enemy has no heroes! Auto-win.");
            EndBattle(0);
            return;
        }

        State = BattleState.Running;
        if (OnBattleStart != null) OnBattleStart();

        while (State == BattleState.Running && _roundCount < MAX_ROUNDS)
        {
            _roundCount++;
            ProcessRound();

            // Check win conditions
            int alivePlayer = CountAlive(_playerHeroes);
            int aliveEnemy = CountAlive(_enemyHeroes);

            if (alivePlayer == 0 && aliveEnemy == 0)
            {
                Log("Round " + _roundCount + ": Both sides wiped! Draw.");
                EndBattle(-1);
                break;
            }
            else if (alivePlayer == 0)
            {
                Log("Round " + _roundCount + ": Player wiped out! Enemy wins.");
                EndBattle(1);
                break;
            }
            else if (aliveEnemy == 0)
            {
                Log("Round " + _roundCount + ": Enemy wiped out! Player wins.");
                EndBattle(0);
                break;
            }
        }

        if (State == BattleState.Running)
        {
            Log("Battle reached max rounds (" + MAX_ROUNDS + "). Draw.");
            EndBattle(-1);
        }
    }

    void ProcessRound()
    {
        // Collect all alive heroes and sort by cd (lower first)
        var allHeroes = new List<BattleHero>();
        foreach (var h in _playerHeroes)
            if (h.isAlive) allHeroes.Add(h);
        foreach (var h in _enemyHeroes)
            if (h.isAlive) allHeroes.Add(h);

        allHeroes.Sort((a, b) => a.cd.CompareTo(b.cd));

        foreach (var attacker in allHeroes)
        {
            if (attacker.IsDead) continue;
            if (State != BattleState.Running) return;

            var target = FindTarget(attacker);
            if (target == null) continue;

            // Deal damage
            int damage = Mathf.Max(1, attacker.atk); // min 1 damage
            target.currentHp -= damage;

            string msg = "[" + attacker.heroName + "](team" + attacker.team + ")"
                + " -> [" + target.heroName + "](team" + target.team + ")"
                + "  DMG:" + damage + "  HP:" + target.currentHp + "/" + target.maxHp;

            if (target.currentHp <= 0)
            {
                target.isAlive = false;
                target.currentHp = 0;
                msg += "  [DEAD]";
            }

            Log(msg);
            if (OnBattleTick != null) OnBattleTick(msg);
        }
    }

    /// <summary>
    /// Find nearest enemy target for attacker.
    /// Priority: same col, closest row (lower y = front), then adjacent cols.
    /// </summary>
    BattleHero FindTarget(BattleHero attacker)
    {
        var enemies = attacker.team == 0 ? _enemyHeroes : _playerHeroes;
        BattleHero bestTarget = null;
        int bestScore = int.MaxValue;

        foreach (var enemy in enemies)
        {
            if (!enemy.isAlive) continue;

            // Score: prefer same column, then closer row (front line)
            int colDist = Mathf.Abs(attacker.gridX - enemy.gridX);
            int rowPriority = enemy.gridY; // lower y = front row = higher priority

            int score = rowPriority * 10 + colDist;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    int CountAlive(List<BattleHero> heroes)
    {
        int count = 0;
        foreach (var h in heroes)
            if (h.isAlive) count++;
        return count;
    }

    void Log(string msg)
    {
        _battleLog.Add(msg);
    }

    void EndBattle(int winner)
    {
        State = BattleState.Finished;
        _winner = winner;
        int alivePlayer = CountAlive(_playerHeroes);
        int aliveEnemy = CountAlive(_enemyHeroes);
        Debug.Log("[BattleController] Battle ended. Winner=" + winner + " playerAlive=" + alivePlayer + " enemyAlive=" + aliveEnemy);

        if (OnBattleEnd != null) OnBattleEnd(winner, _roundCount);
    }

    /// <summary>
    /// Convenience: load enemy formation by ID and start battle.
    /// </summary>
    public void StartBattleWithFormation(int formationId)
    {
        var table = GameTableLoader.LoadEnemyFormationsFromResources("Tables/enemy_formations");
        if (table == null)
        {
            Debug.LogError("[BattleController] Failed to load enemy formations.");
            return;
        }

        int[,] enemyGrid = GameTableLoader.BuildFormationGrid(table, formationId);
        InitBattle(enemyGrid);
        RunBattle();
    }
}