using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Battle UI — shows log and results during/after battle.
/// Press F2 to toggle visibility.
/// </summary>
public class BattleUI : MonoBehaviour
{
    bool _showPanel = false;
    Rect _windowRect = new Rect(50, 50, 500, 600);
    Vector2 _logScroll;
    string _resultText = "";

    int _selectedFormationId = 1;
    string[] _formationNames;
    int[] _formationIds;

    void Start()
    {
        LoadFormationList();

        if (BattleController.Instance != null)
        {
            BattleController.Instance.OnBattleEnd += OnBattleFinished;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
            _showPanel = !_showPanel;
    }

    void OnGUI()
    {
        if (!_showPanel) return;

        _windowRect = GUILayout.Window(998, _windowRect, DrawWindow, "Battle UI (F2)");
    }

    void DrawWindow(int id)
    {
        var bc = BattleController.Instance;
        if (bc == null)
        {
            GUILayout.Label("BattleController not found.");
            GUI.DragWindow();
            return;
        }

        // Formation selector
        GUILayout.Label("-- Select Enemy Formation --");
        GUILayout.BeginHorizontal();
        if (_formationNames != null && _formationNames.Length > 0)
        {
            _selectedFormationId = GUILayout.SelectionGrid(
                _selectedFormationId > 0 ? System.Array.IndexOf(_formationIds, _selectedFormationId) : 0,
                _formationNames, 1);

            if (_selectedFormationId >= 0 && _selectedFormationId < _formationIds.Length)
                _selectedFormationId = _formationIds[_selectedFormationId];
            else
                _selectedFormationId = _formationIds[0];
        }
        else
        {
            GUILayout.Label("No formations found.");
            _selectedFormationId = 1;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // Start battle button
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        bool canBattle = bc.State == BattleController.BattleState.Idle
                      || bc.State == BattleController.BattleState.Finished;
        GUI.enabled = canBattle;
        if (GUILayout.Button("Start Battle!", GUILayout.Height(36)))
        {
            _resultText = "";
            bc.StartBattleWithFormation(_selectedFormationId);
        }
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);

        // Status
        GUILayout.Label("State: " + bc.State);
        GUILayout.Label("Round: " + bc.RoundCount);
        if (bc.Winner >= 0)
        {
            GUILayout.Label("Winner: " + (bc.Winner == 0 ? "PLAYER" : "ENEMY"));
        }

        // Result text
        if (!string.IsNullOrEmpty(_resultText))
        {
            GUILayout.Space(4);
            var resultStyle = new GUIStyle(GUI.skin.label);
            resultStyle.fontSize = 18;
            resultStyle.fontStyle = FontStyle.Bold;
            resultStyle.normal.textColor = bc.Winner == 0 ? Color.green : Color.red;
            GUILayout.Label(_resultText, resultStyle);
        }

        GUILayout.Space(4);

        // Battle log
        GUILayout.Label("-- Battle Log (" + bc.BattleLog.Count + " lines) --");
        _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(250));
        foreach (var line in bc.BattleLog)
        {
            var style = GUI.skin.label;
            if (line.Contains("[DEAD]"))
            {
                style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.red;
            }
            GUILayout.Label(line, style);
        }
        GUILayout.EndScrollView();

        GUILayout.Space(6);

        // Survivors
        GUILayout.Label("-- Player Survivors --");
        foreach (var h in bc.PlayerHeroes)
        {
            if (h.isAlive)
                GUILayout.Label("  " + h.heroName + "  HP:" + h.currentHp + "/" + h.maxHp);
        }

        GUILayout.Label("-- Enemy Survivors --");
        foreach (var h in bc.EnemyHeroes)
        {
            if (h.isAlive)
                GUILayout.Label("  " + h.heroName + "  HP:" + h.currentHp + "/" + h.maxHp);
        }

        GUI.DragWindow();
    }

    void OnBattleFinished(int winner, int roundCount)
    {
        if (winner == 0)
            _resultText = "VICTORY! (" + roundCount + " rounds)";
        else if (winner == 1)
            _resultText = "DEFEAT! (" + roundCount + " rounds)";
        else
            _resultText = "DRAW! (" + roundCount + " rounds)";
    }

    void LoadFormationList()
    {
        var table = GameTableLoader.LoadEnemyFormationsFromResources("Tables/enemy_formations");
        if (table == null || table.list == null) return;

        var idSet = new HashSet<int>();
        var nameMap = new Dictionary<int, string>();
        foreach (var row in table.list)
        {
            if (!idSet.Contains(row.formationId))
            {
                idSet.Add(row.formationId);
                nameMap[row.formationId] = row.formationName;
            }
        }

        _formationIds = new int[idSet.Count];
        _formationNames = new string[idSet.Count];
        int i = 0;
        foreach (var kv in nameMap)
        {
            _formationIds[i] = kv.Key;
            _formationNames[i] = "[" + kv.Key + "] " + kv.Value;
            i++;
        }
    }
}