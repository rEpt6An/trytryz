using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 敌人阵容可视化编辑器 —— 在 Unity 里直接查看和编辑敌方 3×3 布阵。
/// 菜单：Trytryz → 敌人阵容编辑器
/// </summary>
public class EnemyFormationEditorWindow : EditorWindow
{
    // ── 数据缓存 ────────────────────────────────
    List<FormationGroup> _formations = new List<FormationGroup>();
    Dictionary<int, HeroRow> _heroDict = new Dictionary<int, HeroRow>();
    int _selectedFormationIdx = -1;
    Vector2 _listScroll, _gridScroll;
    string[] _heroNames;
    int[] _heroIds;

    // ── 编辑缓存 ────────────────────────────────
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

    // ── 窗口入口 ────────────────────────────────
    [MenuItem("Trytryz/敌人阵容编辑器")]
    static void Open() => GetWindow<EnemyFormationEditorWindow>(false, "敌人阵容编辑器");

    void OnEnable()
    {
        LoadHeroDict();
        LoadFormationsFromJson();
    }

    void OnGUI()
    {
        if (_heroDict.Count == 0)
        {
            EditorGUILayout.HelpBox("未加载到英雄数据。请确认 heroes.json 已导出到 Resources/Tables/。", MessageType.Warning);
            if (GUILayout.Button("重新加载英雄数据")) LoadHeroDict();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // ── 左侧：阵型列表 ─────────────────────────
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        DrawFormationList();
        EditorGUILayout.EndVertical();

        GUILayout.Space(8);

        // ── 右侧：3×3 网格 + 编辑 ──────────────────
        EditorGUILayout.BeginVertical();
        if (_selectedFormationIdx >= 0 && _selectedFormationIdx < _formations.Count)
        {
            DrawGridEditor();
        }
        else
        {
            EditorGUILayout.HelpBox("← 选择一个阵型开始编辑，或点下方按钮新建。", MessageType.Info);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    // ── 加载 ─────────────────────────────────────
    void LoadHeroDict()
    {
        _heroDict.Clear();
        var ta = Resources.Load<TextAsset>("Tables/heroes");
        if (ta == null) return;

        var table = JsonUtility.FromJson<HeroTable>(ta.text);
        if (table?.list == null) return;

        foreach (var h in table.list)
        {
            if (!_heroDict.ContainsKey(h.id))
                _heroDict[h.id] = h;
        }

        _heroIds = _heroDict.Keys.OrderBy(k => k).ToArray();
        _heroNames = _heroIds.Select(id =>
        {
            var h = _heroDict[id];
            return $"[{id}] {h.name} ({h.job})";
        }).ToArray();
    }

    void LoadFormationsFromJson()
    {
        _formations.Clear();
        var ta = Resources.Load<TextAsset>("Tables/enemy_formations");
        if (ta == null) return;

        var table = JsonUtility.FromJson<EnemyFormationTable>(ta.text);
        if (table?.list == null) return;

        var groups = new Dictionary<int, FormationGroup>();
        foreach (var row in table.list)
        {
            if (!groups.ContainsKey(row.formationId))
            {
                groups[row.formationId] = new FormationGroup
                {
                    formationId = row.formationId,
                    formationName = row.formationName,
                    roundType = row.roundType,
                };
            }
            groups[row.formationId].rows.Add(row);
        }

        _formations = groups.Values.OrderBy(g => g.formationId).ToList();

        if (_formations.Count > 0 && _selectedFormationIdx < 0)
            SelectFormation(0);
    }

    void SelectFormation(int idx)
    {
        _selectedFormationIdx = idx;
        if (idx < 0 || idx >= _formations.Count) return;

        var g = _formations[idx];
        _editGrid = new int[3, 3];
        _editName = g.formationName;
        _editRoundType = g.roundType;
        _editFormationId = g.formationId;
        _hasUnsavedChanges = false;

        foreach (var row in g.rows)
        {
            if (row.gridX >= 0 && row.gridX < 3 && row.gridY >= 0 && row.gridY < 3)
                _editGrid[row.gridX, row.gridY] = row.heroId;
        }
    }

    // ── 阵型列表 ──────────────────────────────────
    void DrawFormationList()
    {
        GUILayout.Label("阵型列表", EditorStyles.boldLabel);
        GUILayout.Space(4);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        for (int i = 0; i < _formations.Count; i++)
        {
            var f = _formations[i];
            var style = (i == _selectedFormationIdx) ? EditorStyles.toolbarButton : GUI.skin.button;
            string label = $"[{f.formationId}] {f.formationName}";
            if (GUILayout.Button(label, style, GUILayout.Height(28)))
                SelectFormation(i);
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(8);

        if (GUILayout.Button("➕ 新建阵型", GUILayout.Height(30)))
            CreateNewFormation();

        GUILayout.Space(4);

        if (GUILayout.Button("🔄 重新加载 JSON", GUILayout.Height(28)))
        {
            LoadHeroDict();
            LoadFormationsFromJson();
        }

        EditorGUILayout.HelpBox("编辑后请点「保存到 JSON」。\n定稿后再用 Trytryz→导表工具 从 Excel 重新导出。", MessageType.Info);
    }

    void CreateNewFormation()
    {
        int newId = _formations.Count > 0 ? _formations.Max(f => f.formationId) + 1 : 1;
        var g = new FormationGroup
        {
            formationId = newId,
            formationName = "新阵型",
            roundType = "pvp",
        };
        _formations.Add(g);
        SelectFormation(_formations.Count - 1);
        _hasUnsavedChanges = true;
    }

    // ── 3×3 网格编辑器 ───────────────────────────
    void DrawGridEditor()
    {
        // 阵型信息
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("阵型名称:", GUILayout.Width(60));
        var newName = EditorGUILayout.TextField(_editName);
        if (newName != _editName) { _editName = newName; _hasUnsavedChanges = true; }

        GUILayout.Space(12);
        EditorGUILayout.LabelField("类型:", GUILayout.Width(35));
        int rtIdx = Array.IndexOf(_roundTypes, _editRoundType);
        if (rtIdx < 0) rtIdx = 0;
        int newRt = EditorGUILayout.Popup(rtIdx, _roundTypes);
        if (newRt != rtIdx) { _editRoundType = _roundTypes[newRt]; _hasUnsavedChanges = true; }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        // 3x3 网格
        float cellSize = 100f;
        float gap = 4f;
        float gridW = cellSize * 3 + gap * 4;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginVertical(GUILayout.Width(gridW));
        for (int row = 0; row < 3; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < 3; col++)
            {
                DrawCell(col, row, cellSize);
                if (col < 2) GUILayout.Space(gap);
            }
            EditorGUILayout.EndHorizontal();
            if (row < 2) GUILayout.Space(gap);
        }
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(12);

        // 操作按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = _hasUnsavedChanges ? Color.yellow : Color.white;
        if (GUILayout.Button("💾 保存到 JSON", GUILayout.Height(32), GUILayout.Width(150)))
            SaveToJson();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("🗑 删除此阵型", GUILayout.Height(32), GUILayout.Width(120)))
            DeleteCurrentFormation();
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (_hasUnsavedChanges)
            EditorGUILayout.HelpBox("⚠ 有未保存的修改。", MessageType.Warning);
    }

    void DrawCell(int col, int row, float size)
    {
        int heroId = _editGrid[col, row];

        Color bgColor;
        if (heroId == 0)
            bgColor = new Color(0.25f, 0.25f, 0.25f, 0.6f);
        else if (_heroDict.TryGetValue(heroId, out var hr))
            bgColor = GetHeroColor(hr.cost);
        else
            bgColor = Color.gray;

        var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        if (GUI.Button(rect, "", GUI.skin.button))
        {
            ShowHeroPicker(col, row);
        }

        // Draw text inside cell
        if (heroId == 0)
        {
            var labelRect = new Rect(rect.x, rect.y + rect.height / 2 - 10, rect.width, 20);
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            style.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUI.Label(labelRect, $"[{col},{row}]\n(空)", style);
        }
        else if (_heroDict.TryGetValue(heroId, out var h))
        {
            // Hero name
            var nameRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 18);
            var nameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            GUI.Label(nameRect, h.name, nameStyle);

            // Hero id
            var idRect = new Rect(rect.x + 4, rect.y + 22, rect.width - 8, 14);
            var idStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10 };
            idStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(idRect, $"ID:{h.id}", idStyle);

            // Stats
            var statRect = new Rect(rect.x + 4, rect.y + 38, rect.width - 8, 14);
            var statStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9 };
            statStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(statRect, $"HP:{h.hp} ATK:{h.atk} Cost:{h.cost}", statStyle);

            // Job tag
            var jobRect = new Rect(rect.x + 4, rect.y + 52, rect.width - 8, 14);
            var jobStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9 };
            jobStyle.normal.textColor = new Color(0.5f, 1f, 0.5f);
            GUI.Label(jobRect, $"{h.job} | {h.faction}", jobStyle);
        }

        GUI.backgroundColor = oldColor;

        // Right click to clear
        if (heroId != 0 && Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
        {
            Event.current.Use();
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("清除此格"), false, () => { _editGrid[col, row] = 0; _hasUnsavedChanges = true; });
            menu.ShowAsContext();
        }
    }

    Color GetHeroColor(int cost)
    {
        switch (cost)
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
        menu.AddItem(new GUIContent("清除"), false, () => { _editGrid[col, row] = 0; _hasUnsavedChanges = true; });
        menu.AddSeparator("");

        for (int i = 0; i < _heroIds.Length; i++)
        {
            int hid = _heroIds[i];
            var h = _heroDict[hid];
            string label = "[人口" + h.cost + "] " + h.name + " (" + h.job.Replace("/", "+") + " | HP:" + h.hp + " ATK:" + h.atk + ")";
            int c = col, r = row;
            menu.AddItem(new GUIContent(label), false, () => { _editGrid[c, r] = hid; _hasUnsavedChanges = true; });
        }
        menu.ShowAsContext();
    }

    // ── 保存 / 删除 ──────────────────────────────
    void SaveToJson()
    {
        // Build EnemyFormationRow list from all formations
        var allRows = new List<EnemyFormationRow>();
        int rowId = 1;

        // Update current editing formation data first
        if (_selectedFormationIdx >= 0 && _selectedFormationIdx < _formations.Count)
        {
            var g = _formations[_selectedFormationIdx];
            g.formationName = _editName;
            g.roundType = _editRoundType;

            g.rows.Clear();
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    int hid = _editGrid[x, y];
                    if (hid == 0) continue; // skip empty cells
                    g.rows.Add(new EnemyFormationRow
                    {
                        id = 0, // will be reassigned
                        formationId = g.formationId,
                        formationName = g.formationName,
                        roundType = g.roundType,
                        gridX = x,
                        gridY = y,
                        heroId = hid,
                    });
                }
            }
        }

        // Serialize all formations
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

        Debug.Log($"[EnemyFormationEditor] 已保存 {allRows.Count} 条布阵数据到 enemy_formations.json");
        EditorUtility.DisplayDialog("保存成功", $"已保存 {_formations.Count} 个阵型，共 {allRows.Count} 个英雄。", "确定");
    }

    void DeleteCurrentFormation()
    {
        if (_selectedFormationIdx < 0 || _selectedFormationIdx >= _formations.Count) return;
        var g = _formations[_selectedFormationIdx];
        bool confirm = EditorUtility.DisplayDialog("确认删除", $"确定要删除阵型 [{g.formationId}] {g.formationName} 吗？", "删除", "取消");
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