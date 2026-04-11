# Excel → CSV / JSON 导表工具

## 环境

1. 安装 [Python 3](https://www.python.org/downloads/)，安装时勾选 **Add python.exe to PATH**。
2. 安装依赖（任选一种，推荐第一种，不依赖当前目录）：

```text
python -m pip install -r "d:\_UNITY_Projects\trytryz\tools\excel_export\requirements.txt"
```

若要先 `cd` 到本目录，在 **CMD** 里从 `C:` 切到 `D:` 必须用 **`cd /d`**，否则目录其实没变，`pip` 会在 `C:\Users\你的用户名` 下找 `requirements.txt` 并报错：

```text
cd /d d:\_UNITY_Projects\trytryz\tools\excel_export
python -m pip install -r requirements.txt
```

（PowerShell 里 `cd d:\...` 一般会直接切过去；不确定时可用 `cd` 后执行 `cd` 看当前路径。）

## 表格约定（推荐）

| 第 1 行 | 英文字段名（与 C# 字段一致），如 `id`、`name`、`hp` |
| 第 2 行 | 策划备注 / 类型说明（可选）。导出时加 `--skip-row2` 会跳过 |
| 第 3 行起 | 数据 |

导出为 **Unity `JsonUtility`** 可用的 JSON 时，请使用 `--unity-json`，根结构为 `{"list":[...]}`。

## 常用命令

导出当前目录下 `sample\items.xlsx`（需自备），同时生成 CSV + JSON 到 `export` 子目录：

```text
python export_xlsx.py sample\items.xlsx
```

从 Unity 工程里的设计表导出（本仓库：`trytryz\Assets\Design\items.xlsx`，工作表 `Sheet1`），生成 **`items.json`**：

```text
python export_xlsx.py "..\..\trytryz\Assets\Design\items.xlsx" --format json --unity-json --sheets Sheet1 --json-basename items -o "..\..\trytryz\Assets\Resources\Tables"
```

- `--json-basename items`：在**只导出一个工作表**时，把输出文件名定为 `items.json`（与 `Resources.Load("Tables/items")` 一致）。
- 若第 2 行是说明行而非数据，可再加 `--skip-row2`。

Windows 可在本目录双击 `export_to_unity_resources.bat`（会读取 `Assets\Design\items.xlsx`）。

## Unity 侧

- 将生成的 `Items.json`（或你表名对应的 json）放在 `Assets/Resources/Tables/`。
- 代码里用 `Resources.Load<TextAsset>("Tables/Items")`（**不要**带 `.json` 后缀）。
- 为每张表维护对应的 `[Serializable]` 行类型与 `XXXTable { public XXXRow[] list; }`。
