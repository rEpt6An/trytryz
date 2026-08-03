using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: one-click build the 3x3 board into the scene hierarchy.
/// Menu: Trytryz -> Build Board In Scene
/// </summary>
public class BuildBoardInScene
{
    [MenuItem("Trytryz/Build Board In Scene")]
    static void Build()
    {
        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            Debug.Log("[BuildBoard] Created Canvas.");
        }

        // Find or create BoardSetup
        var setup = canvas.GetComponent<BoardSetup>();
        if (setup == null)
        {
            setup = Undo.AddComponent<BoardSetup>(canvas.gameObject);
        }

        // Remove old board panel if exists
        var oldPanel = canvas.transform.Find("BoardPanel");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        // Build
        setup.BuildInEditor();

        // Register undo for all created objects
        var boardPanel = canvas.transform.Find("BoardPanel");
        if (boardPanel != null)
        {
            Undo.RegisterCreatedObjectUndo(boardPanel.gameObject, "Build Board");
            foreach (Transform child in boardPanel)
            {
                Undo.RegisterCreatedObjectUndo(child.gameObject, "Build Board Cell");
                foreach (Transform sub in child)
                {
                    Undo.RegisterCreatedObjectUndo(sub.gameObject, "Build Board Sub");
                }
            }
        }

        // Make sure GameManager object exists
        EnsureGameManager();

        Debug.Log("[BuildBoard] Board built! Check Hierarchy: Canvas -> BoardPanel -> Cell_*");
        Selection.activeGameObject = boardPanel != null ? boardPanel.gameObject : canvas.gameObject;
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
            Debug.Log("[BuildBoard] Created GameManager with BoardController + BattleController + GameManager.");
        }
        else
        {
            // Ensure BattleController and GameManager exist
            var gmGO = gm.gameObject;
            if (gmGO.GetComponent<BattleController>() == null)
                Undo.AddComponent<BattleController>(gmGO);
            if (gmGO.GetComponent<GameManager>() == null)
                Undo.AddComponent<GameManager>(gmGO);
        }

        // Ensure HeroPicker exists
        var picker = Object.FindObjectOfType<HeroPicker>();
        if (picker == null)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Undo.AddComponent<HeroPicker>(canvas.gameObject);
                Debug.Log("[BuildBoard] Added HeroPicker to Canvas.");
            }
        }

        // Ensure DebugGamePanel exists
        var debug = Object.FindObjectOfType<DebugGamePanel>();
        if (debug == null)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Undo.AddComponent<DebugGamePanel>(canvas.gameObject);
                Debug.Log("[BuildBoard] Added DebugGamePanel to Canvas.");
            }
        }

        // Ensure BattleUI exists
        var battleUI = Object.FindObjectOfType<BattleUI>();
        if (battleUI == null)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Undo.AddComponent<BattleUI>(canvas.gameObject);
                Debug.Log("[BuildBoard] Added BattleUI to Canvas.");
            }
        }
    }
}