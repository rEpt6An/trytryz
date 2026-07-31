using System;

/// <summary>
/// Enemy formation placement row — one hero per row per formation.
/// Group by formationId at runtime to rebuild the board.
/// </summary>
[Serializable]
public class EnemyFormationRow
{
    public int id;
    public int formationId;
    public string formationName;
    public string roundType;    // pve / pvp
    public int gridX;           // 0-2
    public int gridY;           // 0-2
    public int heroId;          // 0 = empty cell
}
