/// <summary>
/// 单个随从在战斗中的运行时状态（战斗快照）。
/// </summary>
public class BattleHero
{
    public int heroId;
    public string heroName;
    public int team;            // 0 = 我方, 1 = 敌方
    public int gridX;           // 0 基战斗棋盘坐标
    public int gridY;

    // 战斗属性
    public int maxHp;
    public int currentHp;
    public int atk;
    public int magicAtk;
    public int shield;
    public int crit;            // 暴击率 %
    public int hit;             // 命中率 %
    public int dodge;           // 闪避率 %
    public float cd;            // 攻击间隔（秒）

    // 攻击目标模式："最近" / "直线" / "随机"
    public string targetMode;

    // 运行时状态
    public float cdTimer;       // 下一次攻击剩余秒数
    public bool isAlive;

    public bool IsDead { get { return !isAlive || currentHp <= 0; } }
}
