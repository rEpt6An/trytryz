using UnityEngine;

public class HeroOnBoard : MonoBehaviour
{
    public int HeroId { get; private set; }
    public int GridX { get; private set; }
    public int GridY { get; private set; }

    public int CurrentHp;
    public int CurrentAtk;
    public int MaxHp;
    public int BaseAtk;

    public bool IsDead { get { return CurrentHp <= 0; } }

    public void Init(int heroId, int gridX, int gridY)
    {
        HeroId = heroId;
        GridX = gridX;
        GridY = gridY;

        var heroData = BoardController.Instance != null
            ? BoardController.Instance.GetHeroData(heroId)
            : null;

        if (heroData != null)
        {
            MaxHp = heroData.hp;
            CurrentHp = heroData.hp;
            BaseAtk = heroData.atk;
            CurrentAtk = heroData.atk;
        }
    }
}