using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗棋盘装配：直接实例化 Resources/Prefabs 下的 BoardPanel 与 EnemyBoardPanel 预制体
/// （每个格子是 Cell_of_Board 的嵌套实例，坐标已序列化在面板预制体里）。
/// </summary>
public class BoardSetup : MonoBehaviour
{
    [Header("References")]
    public BoardController boardController;

    const string PLAYER_BOARD_PREFAB = "Prefabs/BoardPanel";
    const string ENEMY_BOARD_PREFAB = "Prefabs/EnemyBoardPanel";

    void Awake()
    {
        if (boardController == null)
            boardController = FindObjectOfType<BoardController>();

        // 清理旧版运行时面板（含场景里遗留的 BoardPanel/EnemyBoardPanel 实例）
        Transform oldP = transform.Find("BoardPanel");
        if (oldP != null) DestroyImmediate(oldP.gameObject);
        Transform oldE = transform.Find("EnemyBoardPanel");
        if (oldE != null) DestroyImmediate(oldE.gameObject);

        InstantiateBoard(PLAYER_BOARD_PREFAB, "BoardPanel", false);
        InstantiateBoard(ENEMY_BOARD_PREFAB, "EnemyBoardPanel", true);
        Debug.Log("[BoardSetup] Boards instantiated from prefabs: BoardPanel + EnemyBoardPanel (mirrored).");
    }

    void InstantiateBoard(string prefabPath, string panelName, bool enemy)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[BoardSetup] Prefab not found: " + prefabPath);
            return;
        }

        GameObject panel = Instantiate(prefab, transform, false);
        panel.name = panelName;

        var slots = panel.GetComponentsInChildren<CellSlot>();
        bool needCoords = false;
        foreach (var slot in slots)
        {
            slot.isEnemy = enemy;
            // 容错：若预制体里未保存坐标，按格子在面板内的物理位置推算
            if (slot.gridX == 0 || slot.gridY == 0) needCoords = true;
        }
        if (needCoords) AssignGridCoords(new List<CellSlot>(slots), enemy);
    }

    /// <summary>
    /// 按物理位置（左上 → 右下）给格子补坐标。
    /// 我方：左 → 右列号 1,2,3；敌方：左右镜像，左 → 右列号 3,2,1（显示列 3 靠玩家侧）。
    /// </summary>
    static void AssignGridCoords(List<CellSlot> slots, bool enemy)
    {
        var list = new List<CellSlot>(slots);
        list.Sort((a, b) =>
        {
            var ra = (RectTransform)a.transform;
            var rb = (RectTransform)b.transform;
            int cy = rb.anchoredPosition.y.CompareTo(ra.anchoredPosition.y); // 从上到下
            if (cy != 0) return cy;
            return ra.anchoredPosition.x.CompareTo(rb.anchoredPosition.x);   // 从左到右
        });
        int gs = BoardController.GridSize;
        for (int i = 0; i < list.Count; i++)
        {
            int col = i % gs;
            int x = enemy
                ? (BoardController.GridStartIndex + (gs - 1 - col))
                : (BoardController.GridStartIndex + col);
            int y = i / gs + BoardController.GridStartIndex;
            list[i].gridX = x;
            list[i].gridY = y;
        }
    }
}
