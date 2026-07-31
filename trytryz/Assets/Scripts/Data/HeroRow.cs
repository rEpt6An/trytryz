using System;

/// <summary>
/// Hero data row — matches heroes.xlsx column names exactly.
/// </summary>
[Serializable]
public class HeroRow
{
    public int id;
    public string name;
    public string level;     // "Lv1", "Lv2", etc.
    public string job;
    public string faction;
    public string race;
    public int cost;         // population cost (1/2/3 population slots)
    public int hp;
    public int atk;
    public int def;
    public float attackRange;
    public float cd;
    public string skillDesc;
    public string trait;
    public string description;
}
