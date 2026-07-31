/// <summary>
/// Runtime battle state for a single hero on the battlefield.
/// </summary>
public class BattleHero
{
    public int heroId;
    public string heroName;
    public int team;        // 0 = player, 1 = enemy
    public int gridX;
    public int gridY;
    public int maxHp;
    public int currentHp;
    public int atk;
    public float cd;        // lower = acts first
    public bool isAlive;

    public bool IsDead { get { return !isAlive || currentHp <= 0; } }

    public BattleHero Clone()
    {
        return new BattleHero
        {
            heroId = this.heroId,
            heroName = this.heroName,
            team = this.team,
            gridX = this.gridX,
            gridY = this.gridY,
            maxHp = this.maxHp,
            currentHp = this.currentHp,
            atk = this.atk,
            cd = this.cd,
            isAlive = this.isAlive
        };
    }
}