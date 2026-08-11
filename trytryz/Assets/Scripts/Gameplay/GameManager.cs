using UnityEngine;

/// <summary>
/// 游戏主流程占位（F3 旧版 OnGUI 界面已按要求删除，后续再重写）。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Day { get; private set; } = 1;
    public int Round { get; private set; } = 1;
    public int Hp { get; private set; } = 15;
    public int Crowns { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
}
