using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool: build enemy board by instantiating BoardPanel prefab and flipping horizontally.
/// Menu: Trytryz -> Build Enemy Board
/// </summary>
public class BuildEnemyBoard
{
    const string BOARD_PREFAB_PATH = "Prefabs/BoardPanel";

    [MenuItem("Trytryz/Build Enemy Board")]
    static void Build()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[BuildEnemyBoard] Canvas not found! Run Build Board In Scene first.");
            return;
        }

        GameObject boardPrefab = Resources.Load<GameObject>(BOARD_PREFAB_PATH);
        if (boardPrefab == null)
        {
            Debug.LogError("[BuildEnemyBoard] BoardPanel prefab not found at Resources/" + BOARD_PREFAB_PATH);
            return;
        }

        // Remove old enemy board if exists
        var oldPanel = canvas.transform.Find("EnemyBoardPanel");
        if (oldPanel != null)
            Undo.DestroyObjectImmediate(oldPanel.gameObject);

        // Instantiate BoardPanel prefab
        var enemyGO = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab, canvas.transform);
        enemyGO.name = "EnemyBoardPanel";
        Undo.RegisterCreatedObjectUndo(enemyGO, "Create EnemyBoardPanel");

        // Set enemy-tinted background
        var bg = enemyGO.GetComponent<Image>();
        if (bg != null)
            bg.color = new Color(0.15f, 0.08f, 0.08f, 0.9f);

        // Flip: for each cell, mirror X position, swap gridX (1<->3), rename
        foreach (Transform child in enemyGO.transform)
        {
            var cellName = child.name;
            if (!cellName.StartsWith("Cell_")) continue;

            var crt = child.GetComponent<RectTransform>();
            var slot = child.GetComponent<CellSlot>();

            if (crt != null)
            {
                // Mirror X position
                Vector2 pos = crt.anchoredPosition;
                crt.anchoredPosition = new Vector2(-pos.x, pos.y);
            }

            if (slot != null)
            {
                // Mirror gridX: 1<->3, 2 stays
                int newX = BoardController.GridStartIndex + BoardController.GridSize + BoardController.GridStartIndex - 1 - slot.gridX;
                slot.gridX = newX;

                // Tint background reddish
                if (slot.background != null)
                    slot.background.color = new Color(0.22f, 0.12f, 0.12f, 0.9f);

                // Update infoText to show mirrored coords
                if (slot.infoText != null)
                    slot.infoText.text = "[" + newX + "," + slot.gridY + "]";
            }

            // Rename
            child.name = "E_" + cellName;

            // Disable interaction on enemy cells
            var heroPicker = child.GetComponent<HeroPicker>();
            if (heroPicker != null) Object.DestroyImmediate(heroPicker);

            Undo.RecordObject(child.gameObject, "Flip Enemy Cell");
        }

        Debug.Log("[BuildEnemyBoard] Enemy board built from BoardPanel prefab. Flipped horizontally.");
        Selection.activeGameObject = enemyGO;
    }
}