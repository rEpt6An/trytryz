using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EnemyFormationEditorWindow : EditorWindow
{
    List<FormationGroup> _formations = new List<FormationGroup>();
    Dictionary<int, HeroRow> _heroDict = new Dictionary<int, HeroRow>();
    int _selectedFormationIdx = -1;
    Vector2 _listScroll, _gridScroll;
    string[] _heroNames;
    int[] _heroIds;

    int[,] _editGrid = new int[3, 3];
    string _editName = "";
    string _editRoundType = "pvp";
    int _editFormationId = 0;

    readonly string[] _roundTypes = { "pvp", "pve" };
    bool _hasUnsavedChanges = false;

    class FormationGroup
    {
        public int formationId;
        public string formationName;
        public string roundType;
        public List<EnemyFormationRow> rows = new List<EnemyFormationRow>();
    }

    [MenuItem("Trytryz/Enemy Formation Editor")]
    static void Open() => GetWindow<EnemyFormationEditorWindow>(false, "Enemy Formation Editor");

    void OnEnable()
    {
        LoadHeroDict();
        LoadFormationsFromJson();
    }

    void OnGUI()
    {
        if (_heroDict.Count == 0)
        {
            EditorGUILayout.HelpBox("No hero data loaded. Make sure heroes.json is exported to Resources/Tables/.", MessageType.Warning);
            if (GUILayout.Button("Reload Hero Data")) LoadHeroDict();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        DrawFormationList();
        EditorGUILayout.EndVertical();

        GUILayout.Space(8);

        EditorGUILayout.BeginVertical();
        if (_selectedFormationIdx >= 0 && _selectedFormationIdx < _formations.Count)
            DrawGridEditor();
        else
            EditorGUILayout.HelpBox("Select a formation or create a new one.", MessageType.Info);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    void LoadHeroDict()
    {
        _heroDict.Clear();
        var ta = Resources.Load<TextAsset>("Tables/heroes");
        if (ta == null) return;

        var table = JsonUtility.FromJson<HeroTable>(ta.text);
        if (table == null || table.list == null) return;

        foreach (var h in table.list)
            if (!_heroDict.ContainsKey(h.id))
                _heroDict[h.id] = h;

        _heroIds = _heroDict.Keys.OrderBy(k => k).ToArray();
        _heroNames = _heroIds.Select(id =>
        {
            var h = _heroDict[id];
            return "[" + id + "] " + h.name + " (" + h.job + ")";
        }).ToArray();
    }

    void LoadFormationsFromJson()
    {
        _formations.Clear();
        var ta = Resources.Load<TextAsset>("Tables/enemy_formations");
        if (ta == null) return;

        var table = JsonUtility.FromJson<EnemyFormationTable>(ta.text);
        if (table == null || table.list == null) return;

        var groups = new Dictionary<int, FormationGroup>();
        foreach (var row in table.list)
        {
            if (!groups.ContainsKey(row.formationId))
            {
                groups[row.formationId] = new FormationGroup
                {
                    formationId = row.formationId,
                    formationName = row.formationName,
                    roundType = row.roundType
                };
            }
            groups[row.formationId].rows.Add(row);
        }

        _formations = groups.Values.OrderBy(g => g.formationId).ToList();
        if (_formations.Count > 0)
            SelectFormation(0);
    }

    void DrawFormationList()
    {
        EditorGUILayout.LabelField("Formations", EditorStyles.boldLabel);
        GUILayout.Space(4);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

        for (int i = 0; i < _formations.Count; i++)
        {
            var g = _formations[i];
            bool selected = i == _selectedFormationIdx;

            Color oldBg = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = new Color(0.3f, 0.5f, 0.3f);

            if (GUILayout.Button("[" + g.formationId + "] " + g.formationName + " (" + g.roundType + ")"))
                SelectFormation(i);

            GUI.backgroundColor = oldBg;
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(8);

        if (GUILayout.Button("New Formation"))
            CreateNewFormation();

        if (_selectedFormationIdx >= 0)
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete Formation"))
                DeleteCurrentFormation();
            GUI.backgroundColor = Color.white;
        }
    }

    void SelectFormation(int idx)
    {
        if (idx < 0 || idx >= _formations.Count) return;

        if (_hasUnsavedChanges && _selectedFormationIdx >= 0)
        {
            bool save = EditorUtility.DisplayDialog("Unsaved Changes",
                "Save changes to current formation?", "Save", "Discard");
            if (save) SaveCurrentToGrid();
        }

        _selectedFormationIdx = idx;
        var g = _formations[idx];
        _editName = g.formationName;
        _editRoundType = g.roundType;
        _editFormationId = g.formationId;

        // Load grid
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                _editGrid[x, y] = 0;

        foreach (var row in g.rows)
        {
            if (row.gridX >= 0 && row.gridX < 3 && row.gridY >= 0 && row.gridY < 3)
                _editGrid[row.gridX, row.gridY] = row.heroId;
        }
    }

    void CreateNewFormation()
    {
        if (_hasUnsavedChanges && _selectedFormationIdx >= 0)
        {
            bool save = EditorUtility.DisplayDialog("Unsaved Changes",
                "Save changes to current formation?", "Save", "Discard");
            if (save) SaveCurrentToGrid();
        }

        int newId = _formations.Count > 0 ? _formations.Max(f => f.formationId) + 1 : 1;
        var g = new FormationGroup
        {
            formationId = newId,
            formationName = "New Formation",
            roundType = "pvp",
            rows = new List<EnemyFormationRow>()
        };

        _formations.Add(g);
        _hasUnsavedChanges = true;
        SelectFormation(_formations.Count - 1);
    }

    void SaveCurrentToGrid()
    {
        if (_selectedFormationIdx < 0 || _selectedFormationIdx >= _formations.Count) return;
        var g = _formations[_selectedFormationIdx];
        g.formationName = _editName;
        g.roundType = _editRoundType;
        g.formationId = _editFormationId;
        g.rows.Clear();
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            {
                int hid = _editGrid[x, y];
                if (hid != 0)
                    g.rows.Add(new EnemyFormationRow
                    {
                        formationId = g.formationId,
                        formationName = g.formationName,
                        roundType = g.roundType,
                        gridX = x,
                        gridY = y,
                        heroId = hid
                    });
            }
    }

    void DrawGridEditor()
    {
        EditorGUILayout.BeginHorizontal();

        // Formation info
        EditorGUILayout.BeginVertical(GUILayout.Width(180));
        EditorGUILayout.LabelField("Formation ID:", _editFormationId.ToString());
        _editName = EditorGUILayout.TextField("Name:", _editName);
        int typeIdx = System.Array.IndexOf(_roundTypes, _editRoundType);
        if (typeIdx < 0) typeIdx = 0;
        typeIdx = EditorGUILayout.Popup("Type:", typeIdx, _roundTypes);
        _editRoundType = _roundTypes[typeIdx];

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Click grid cell to add hero.");
        EditorGUILayout.LabelField("Right-click to clear.");

        GUILayout.Space(12);

        if (GUILayout.Button("Save to JSON", GUILayout.Height(30)))
            SaveToJson();
        EditorGUILayout.EndVertical();

        GUILayout.Space(8);

        // 3x3 Grid
        EditorGUILayout.BeginVertical();
        float cellSize = 80f;
        float gap = 4f;

        for (int row = 0; row < 3; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < 3; col++)
            {
                DrawGridCell(col, row, cellSize);
                if (col < 2) GUILayout.Space(gap);
            }
            EditorGUILayout.EndHorizontal();
            if (row < 2) GUILayout.Space(gap);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    void DrawGridCell(int col, int row, float size)
    {
        int heroId = _editGrid[col, row];
        Color oldColor = GUI.backgroundColor;

        if (heroId == 0)
        {
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
        }
        else
        {
            var h = _heroDict.ContainsKey(heroId) ? _heroDict[heroId] : null;
            GUI.backgroundColor = h != null ? GetHeroColor(h.pop) : new Color(0.3f, 0.3f, 0.3f);
        }

        var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

        if (GUI.Button(rect, ""))
        {
            SaveCurrentToGrid();
            ShowHeroPicker(col, row);
        }

        // Draw hero info if occupied
        if (heroId != 0 && _heroDict.ContainsKey(heroId))
        {
            var h = _heroDict[heroId];

            var nameRect = new Rect(rect.x + 2, rect.y + 4, rect.width - 4, 22);
            var nameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            nameStyle.normal.textColor = Color.white;
            GUI.Label(nameRect, h.name, nameStyle);

            var statRect = new Rect(rect.x + 2, rect.y + 28, rect.width - 4, 18);
            var statStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            statStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            GUI.Label(statRect, "HP:" + h.hp + " ATK:" + h.atk + " CD:" + h.cd, statStyle);

            var jobRect = new Rect(rect.x + 2, rect.y + size - 18, rect.width - 4, 14);
            var jobStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9 };
            jobStyle.normal.textColor = new Color(0.5f, 1f, 0.5f);
            GUI.Label(jobRect, h.job + " | " + h.faction, jobStyle);
        }

        GUI.backgroundColor = oldColor;

        if (heroId != 0 && Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
        {
            Event.current.Use();
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Clear Cell"), false, () => { _editGrid[col, row] = 0; _hasUnsavedChanges = true; });
            menu.ShowAsContext();
        }
    }

    Color GetHeroColor(int pop)
    {
        switch (pop)
        {
            case 1: return new Color(0.3f, 0.5f, 0.3f, 0.8f);
            case 2: return new Color(0.3f, 0.3f, 0.7f, 0.8f);
            case 3: return new Color(0.7f, 0.3f, 0.7f, 0.8f);
            default: return new Color(0.8f, 0.5f, 0.2f, 0.8f);
        }
    }

    void ShowHeroPicker(int col, int row)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Clear"), false, () => { _editGrid[col, row] = 0; _hasUnsavedChanges = true; });
        menu.AddSeparator("");

        for (int i = 0; i < _heroIds.Length; i++)
        {
            int hid = _heroIds[i];
            var h = _heroDict[hid];
            string label = "[Pop" + h.pop + "] " + h.name + " (" + h.job + " | HP:" + h.hp + " ATK:" + h.atk + ")";
            int c = col, r = row;
            menu.AddItem(new GUIContent(label), false, () => { _editGrid[c, r] = hid; _hasUnsavedChanges = true; });
        }
        menu.ShowAsContext();
    }

    void SaveToJson()
    {
        SaveCurrentToGrid();

        var allRows = new List<EnemyFormationRow>();
        int rowId = 1;

        foreach (var g in _formations)
        {
            foreach (var r in g.rows)
            {
                r.id = rowId++;
                r.formationName = g.formationName;
                r.roundType = g.roundType;
                r.formationId = g.formationId;
                allRows.Add(r);
            }
        }

        var table = new EnemyFormationTable { list = allRows.ToArray() };
        string json = JsonUtility.ToJson(table, true);
        string path = Path.Combine(Application.dataPath, "Resources", "Tables", "enemy_formations.json");
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);

        AssetDatabase.Refresh();
        _hasUnsavedChanges = false;
        LoadFormationsFromJson();

        Debug.Log("[EnemyFormationEditor] Saved " + allRows.Count + " formation entries.");
        EditorUtility.DisplayDialog("Save Success", "Saved " + _formations.Count + " formations, " + allRows.Count + " heroes.", "OK");
    }

    void DeleteCurrentFormation()
    {
        if (_selectedFormationIdx < 0 || _selectedFormationIdx >= _formations.Count) return;
        var g = _formations[_selectedFormationIdx];
        bool confirm = EditorUtility.DisplayDialog("Confirm Delete",
            "Delete formation [" + g.formationId + "] " + g.formationName + "?", "Delete", "Cancel");
        if (!confirm) return;

        _formations.RemoveAt(_selectedFormationIdx);
        _hasUnsavedChanges = true;

        if (_formations.Count > 0)
            SelectFormation(Mathf.Min(_selectedFormationIdx, _formations.Count - 1));
        else
            _selectedFormationIdx = -1;

        SaveToJson();
    }
}