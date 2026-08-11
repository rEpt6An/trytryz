using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗测试面板（F2）：选择敌方阵容 → 预览 → 开始战斗 → 查看日志 → 返回布阵。
/// </summary>
public class BattleUI : MonoBehaviour
{
    bool _showPanel = false;
    Rect _windowRect = new Rect(50, 50, 500, 620);
    Vector2 _logScroll;
    string _resultText = "";
    int _selectedFormationId = 1;
    string[] _formationNames;
    int[] _formationIds;

    void Start()
    {
        LoadFormationList();
        if (BattleController.Instance != null)
            BattleController.Instance.OnBattleEnd += OnBattleFinished;
    }

    void Update() { if (Input.GetKeyDown(KeyCode.F2)) _showPanel = !_showPanel; }

    void OnGUI()
    {
        if (!_showPanel) return;
        _windowRect = GUILayout.Window(998, _windowRect, DrawWindow, "Battle UI (F2)");
    }

    void DrawWindow(int id)
    {
        var bc = BattleController.Instance;
        if (bc == null) { GUILayout.Label("BattleController not found."); GUI.DragWindow(); return; }

        GUILayout.Label("-- Select Enemy Formation --");
        if (_formationNames != null && _formationNames.Length > 0)
        {
            int sel = _selectedFormationId > 0 ? System.Array.IndexOf(_formationIds, _selectedFormationId) : 0;
            if (sel < 0) sel = 0;
            sel = GUILayout.SelectionGrid(sel, _formationNames, 1);
            if (sel >= 0 && sel < _formationIds.Length)
            {
                int newFid = _formationIds[sel];
                if (newFid != _selectedFormationId)
                {
                    _selectedFormationId = newFid;
                    PreviewEnemyFormation(_selectedFormationId);
                }
            }
        }
        else { GUILayout.Label("No formations found."); _selectedFormationId = 1; }
        GUILayout.Space(4);

        bool can = bc.State == BattleController.BattleState.Idle || bc.State == BattleController.BattleState.Finished;
        GUI.enabled = can;
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("Start Battle!", GUILayout.Height(36)))
        {
            _resultText = "";
            PreviewEnemyFormation(_selectedFormationId); // 确保敌方随从实体已上棋盘
            EnterAllBattleMode();
            bc.StartBattleWithFormation(_selectedFormationId);
        }
        GUI.enabled = true; GUI.backgroundColor = Color.white;
        GUILayout.Space(6);

        GUILayout.Label("State: " + bc.State);
        if (bc.State == BattleController.BattleState.Running) GUILayout.Label("Actions: " + bc.ActionCount);
        if (bc.Winner >= 0 && bc.State == BattleController.BattleState.Finished)
            GUILayout.Label("Winner: " + (bc.Winner == 0 ? "PLAYER" : bc.Winner == 1 ? "ENEMY" : "DRAW"));

        if (!string.IsNullOrEmpty(_resultText))
        {
            GUILayout.Space(4);
            var rs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            rs.normal.textColor = bc.Winner == 0 ? Color.green : bc.Winner == 1 ? Color.red : Color.yellow;
            GUILayout.Label(_resultText, rs);
        }
        GUILayout.Space(4);

        GUILayout.Label("-- Battle Log (" + bc.BattleLog.Count + " lines) --");
        _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(200));
        foreach (var line in bc.BattleLog)
        {
            var s = GUI.skin.label;
            if (line.Contains("DEAD")) { s = new GUIStyle(GUI.skin.label); s.normal.textColor = Color.red; }
            else if (line.Contains("CRIT")) { s = new GUIStyle(GUI.skin.label); s.normal.textColor = Color.yellow; }
            GUILayout.Label(line, s);
        }
        GUILayout.EndScrollView();
        GUILayout.Space(6);

        GUILayout.Label("-- Player Survivors --");
        foreach (var h in bc.PlayerHeroes) if (h.isAlive) GUILayout.Label("  " + h.heroName + "  HP:" + h.currentHp + "/" + h.maxHp + " S:" + h.shield);
        GUILayout.Label("-- Enemy Survivors --");
        foreach (var h in bc.EnemyHeroes) if (h.isAlive) GUILayout.Label("  " + h.heroName + "  HP:" + h.currentHp + "/" + h.maxHp + " S:" + h.shield);

        if (bc.State == BattleController.BattleState.Finished)
        {
            GUILayout.Space(8);
            if (GUILayout.Button("Return (Exit Battle Mode)", GUILayout.Height(30))) ExitAllBattleMode();
        }
        GUI.DragWindow();
    }

    void OnBattleFinished(int winner, int actions)
    {
        _resultText = winner == 0 ? "VICTORY!" : winner == 1 ? "DEFEAT!" : "DRAW!";
        _resultText += " (" + actions + " actions)";
    }

    /// <summary>在敌方棋盘上实例化所选阵容（左右镜像）。</summary>
    void PreviewEnemyFormation(int fid)
    {
        var bc = BoardController.Instance;
        if (bc == null) return;
        bc.SetupEnemyFormation(fid);
    }

    void EnterAllBattleMode()
    {
        var bc = BoardController.Instance;
        if (bc == null) return;

        // 直接从棋盘数组取实体，避免依赖 CellSlot.Occupant（帧刷新）导致刚生成的随从漏掉
        foreach (var cell in bc.PlayerCells)
        {
            var e = bc.GetFollowerEntityAt(cell);
            if (e != null) e.EnterBattleMode(0, cell.gridX - 1, cell.gridY - 1);
        }
        foreach (var cell in bc.EnemyCells)
        {
            var e = bc.GetFollowerEntityAt(cell);
            if (e != null) e.EnterBattleMode(1, cell.gridX - 1, cell.gridY - 1);
        }
    }

    void ExitAllBattleMode()
    {
        var bc = BoardController.Instance;
        if (bc == null) return;

        foreach (var cell in bc.PlayerCells)
        {
            var e = bc.GetFollowerEntityAt(cell);
            if (e != null) e.ExitBattleMode();
        }
        foreach (var cell in bc.EnemyCells)
        {
            var e = bc.GetFollowerEntityAt(cell);
            if (e != null) e.ExitBattleMode();
        }
    }

    void LoadFormationList()
    {
        var table = GameTableLoader.LoadEnemyFormationsFromResources("Tables/enemy_formations");
        if (table == null || table.list == null) return;
        var idSet = new HashSet<int>(); var nm = new Dictionary<int, string>();
        foreach (var r in table.list) { if (!idSet.Contains(r.formationId)) { idSet.Add(r.formationId); nm[r.formationId] = r.formationName; } }
        _formationIds = new int[idSet.Count]; _formationNames = new string[idSet.Count]; int i = 0;
        foreach (var kv in nm) { _formationIds[i] = kv.Key; _formationNames[i] = "[" + kv.Key + "] " + kv.Value; i++; }
    }
}
