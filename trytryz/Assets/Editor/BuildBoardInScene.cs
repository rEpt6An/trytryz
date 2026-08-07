using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool: instantiate BoardPanel prefab into the scene.
/// Menu: Trytryz -> Build Board In Scene
/// </summary>
public class BuildBoardInScene
{
    const string BOARD_PREFAB_PATH = "Prefabs/BoardPanel";

    [MenuItem("Trytryz/Build Board In Scene")]
    static void Build()
    {
        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            Debug.Log("[BuildBoard] Created Canvas.");
        }

        // Load BoardPanel prefab
        GameObject boardPrefab = Resources.Load<GameObject>(BOARD_PREFAB_PATH);
        if (boardPrefab == null)
        {
            Debug.LogError("[BuildBoard] BoardPanel prefab not found at Resources/" + BOARD_PREFAB_PATH);
            return;
        }

        // Remove old board if exists
        var oldPanel = canvas.transform.Find("BoardPanel");
        if (oldPanel != null)
            Undo.DestroyObjectImmediate(oldPanel.gameObject);

        // Instantiate the prefab
        var boardGO = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab, canvas.transform);
        boardGO.name = "BoardPanel";
        Undo.RegisterCreatedObjectUndo(boardGO, "Create BoardPanel");

        // Find or add BoardSetup
        var setup = canvas.GetComponent<BoardSetup>();
        if (setup == null)
            Undo.AddComponent<BoardSetup>(canvas.gameObject);

        EnsureGameManager();

        Debug.Log("[BuildBoard] BoardPanel instantiated from prefab.");
        Selection.activeGameObject = boardGO;
    }

    static void EnsureGameManager()
    {
        var gm = Object.FindObjectOfType<BoardController>();
        if (gm == null)
        {
            var gmGO = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(gmGO, "Create GameManager");
            Undo.AddComponent<BoardController>(gmGO);
            Undo.AddComponent<BattleController>(gmGO);
            Undo.AddComponent<GameManager>(gmGO);
            Debug.Log("[BuildBoard] Created GameManager.");
        }
        else
        {
            var gmGO = gm.gameObject;
            if (gmGO.GetComponent<BattleController>() == null)
                Undo.AddComponent<BattleController>(gmGO);
            if (gmGO.GetComponent<GameManager>() == null)
                Undo.AddComponent<GameManager>(gmGO);
        }

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) return;

        if (canvas.GetComponent<HeroPicker>() == null)
            Undo.AddComponent<HeroPicker>(canvas.gameObject);
        if (canvas.GetComponent<DebugGamePanel>() == null)
            Undo.AddComponent<DebugGamePanel>(canvas.gameObject);
        if (canvas.GetComponent<BattleUI>() == null)
            Undo.AddComponent<BattleUI>(canvas.gameObject);
    }
}