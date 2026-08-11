using System;

[Serializable]
public class HeroRow
{
    public int id;
    public string name;
    public string faction;
    public string quality;
    public string job;
    public int pop;          // population cost
    public int hp;
    public int atk;
    public float cd;         // attack cooldown in seconds
    public int shield;
    public int magicAtk;
    public int crit;         // crit chance %
    public int hit;          // hit rate %
    public int dodge;        // dodge rate %
    public string skill;
    public string target;    // "最近" / "直线" / "随机"
    public string character;
    public string photo;     // sprite path
    public string description;
}