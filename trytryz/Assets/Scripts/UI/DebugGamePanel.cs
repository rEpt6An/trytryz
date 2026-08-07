using UnityEngine;
using UnityEngine.UI;

public class DebugGamePanel : MonoBehaviour
{
    bool _showPanel = true;
    Rect _panelRect = new Rect(10, 10, 300, 420);
    int _popSliderValue = 5;
    string _statusMsg = "";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            _showPanel = !_showPanel;

        if (BoardController.Instance != null)
            _popSliderValue = BoardController.Instance.MaxPopulation;
    }

    void OnGUI()
    {
        if (!_showPanel) return;
        _panelRect = GUILayout.Window(999, _panelRect, DrawDebugWindow, "Debug Panel (F1)");
    }

    void DrawDebugWindow(int id)
    {
        var bc = BoardController.Instance;
        if (bc == null)
        {
            GUILayout.Label("BoardController not found.");
            GUI.DragWindow();
            return;
        }

        GUILayout.Label("Pop: " + bc.CurrentPopulation + " / " + bc.MaxPopulation);
        GUILayout.Label("God Mode: " + (bc.GodMode ? "ON" : "OFF"));
        GUILayout.Space(6);

        // God mode toggle
        GUI.backgroundColor = bc.GodMode ? Color.green : Color.gray;
        if (GUILayout.Button(bc.GodMode ? "God Mode: ON" : "God Mode: OFF", GUILayout.Height(30)))
        {
            bc.GodMode = !bc.GodMode;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(6);

        // Pop slider
        GUILayout.Label("Max Pop: " + _popSliderValue);
        _popSliderValue = (int)GUILayout.HorizontalSlider(_popSliderValue, 1, 15);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply", GUILayout.Width(100)))
        {
            bc.MaxPopulation = _popSliderValue;
            _statusMsg = "Max pop set to " + _popSliderValue;
            bc.RefreshDisplay();
        }
        GUILayout.Space(10);
        _popSliderValue = bc.MaxPopulation;
        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        // Quick actions
        GUILayout.Label("-- Game --");
        var gm = GameManager.Instance;
        if (gm != null)
        {
            GUILayout.Label("D" + gm.Day + " R" + gm.Round + "  HP:" + gm.Hp + "  Crowns:" + gm.Crowns);
        }
        GUILayout.Space(4);
        GUILayout.Label("-- Quick Actions --");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Board", GUILayout.Height(30)))
        {
            bc.ClearBoard();
            _statusMsg = "Board cleared";
        }
        if (GUILayout.Button("Fill Random", GUILayout.Height(30)))
        {
            bc.FillBoardWithRandom();
            _statusMsg = "Board filled";
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Hero List", GUILayout.Height(30)))
        {
            if (HeroPicker.Instance != null)
                HeroPicker.Instance.OpenForCell(1, 1);
            _statusMsg = "Hero picker opened";
        }
        if (GUILayout.Button("Refresh UI", GUILayout.Height(30)))
        {
            bc.RefreshDisplay();
            _statusMsg = "UI refreshed";
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Battle UI (F2)", GUILayout.Height(30)))
        {
            _statusMsg = "Press F2 to open Battle UI";
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        // Board snapshot (1-based display)
        GUILayout.Label("-- Board Snapshot --");
        int start = BoardController.GridStartIndex;
        int end = start + BoardController.GridSize;
        for (int y = start; y < end; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = start; x < end; x++)
            {
                int heroId = bc.GetHeroAt(x, y);
                string label;
                if (heroId == 0)
                    label = "[ ]";
                else
                {
                    var hero = bc.GetHeroData(heroId);
                    label = hero != null ? hero.name : "#" + heroId;
                }
                GUILayout.Label(label, GUILayout.Width(85));
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(6);

        if (!string.IsNullOrEmpty(_statusMsg))
            GUILayout.Label(_statusMsg);

        GUI.DragWindow();
    }

    void OnEnable()
    {
        if (BoardController.Instance != null)
            _popSliderValue = BoardController.Instance.MaxPopulation;
        else
            _popSliderValue = 5;
    }
}