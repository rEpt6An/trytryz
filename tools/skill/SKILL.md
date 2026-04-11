# trytryz 导表与表格相关（给 AI 用）

在协助用户处理 **本仓库** 的 Excel / JSON / Unity 读表时，优先阅读本文件。

## 仓库结构

- Unity 工程根目录：`trytryz/trytryz/`（其下有 `Assets`、`ProjectSettings`）。
- 仓库根目录：`trytryz/`（与 Unity 工程文件夹 **同级**），内含 `tools/`。
- Excel 源表（策划编辑）：`Assets/Design/items.xlsx`（工作表名当前为 **`Sheet1`**）。
- 运行时 JSON：`Assets/Resources/Tables/items.json`（由脚本生成，**勿手改为主流程**）。
- Python 导表脚本：`tools/excel_export/export_xlsx.py`。
- Unity 一键导表：`Assets/Editor/ExportGameTablesWindow.cs`，菜单 **Trytryz → 导表工具**。

## 从 Unity 解析路径的规则

- `Application.dataPath` → `.../Assets`
- Unity 工程目录 → `Directory.GetParent(Application.dataPath)`
- 仓库根目录 → **再上一级** `Directory.GetParent(unityProjectDir)`
- 脚本路径 → `repoRoot/tools/excel_export/export_xlsx.py`

## 推荐导出命令（道具表）

```text
python "…/tools/excel_export/export_xlsx.py" "…/Assets/Design/items.xlsx" --format json --unity-json --sheets Sheet1 --json-basename items -o "…/Assets/Resources/Tables"
```

- **不要**对「第 2 行是空行」的表滥用 `--skip-row2`；仅当第 2 行是**纯说明行**时使用。
- `--json-basename items`：在只导出一个 sheet 时固定输出 `items.json`。

## Unity JsonUtility 约定

- JSON 根须为对象；导出使用 `--unity-json` → `{"list":[...]}`。
- 行类型：`[Serializable]` + `public` 字段，字段名与 Excel **首行英文列名**一致。
- 包装类：`public ItemRow[] list;`（类名如 `ItemTable`）。
- 加载：`Resources.Load<TextAsset>("Tables/items")`，路径**无** `.json` 后缀。
- **新增列**：必须更新 C# 行类型字段；若用 `LoadItemsOnStart` 的 OnGUI 字符串，也要更新显示，否则局内「看不见」新字段（数据可能已加载）。

## 新建一张表的清单

1. `Assets/Design/` 新建或复制 `xxx.xlsx`，第一张表命名一致（或记下 sheet 名）。
2. 首行英文列名；自第 3 行起数据（或第 2 行说明则导出时 `--skip-row2`）。
3. 新建 `XxxRow.cs`、`XxxTable.cs`（`list` 数组）。
4. 新建加载逻辑或扩展现有 Loader。
5. JSON 放 `Resources/Tables/`，`Resources.Load("Tables/文件名不含后缀")`。
6. 在 **导表工具** 中增加对应按钮（或复制 `ExportGameTablesWindow` 中的进程调用并改参数）。

## 人类可读教程

- `Helps/导表与Unity读表完整教程.md`（零基础分步）。
