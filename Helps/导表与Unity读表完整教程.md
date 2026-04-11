# 导表零基础教程（一步一步，尽量通俗）

这份教程只讲本工程 **`trytryz`** 里已经接好的那一套。你照着序号做就行。

---

## 先搞懂：表格数据是怎么进游戏的？

可以把它想成 **三条线**：

1. **Excel**（你在 `Assets/Design` 里改数字、加列）→ 这是「母本」。
2. **点一下 Unity 里的导表按钮**（或自己运行命令）→ 生成 **`items.json`**。
3. **游戏开始** → 代码用 `Resources` 读到 `items.json`，填进 C# 里的 **`ItemRow`**，再在画面上画出来。

**重要：**  
你在 Excel 里加了 `speed`，也导出了 JSON，但**画面上**每一行字是程序员写死的模板。  
如果模板里没写 `speed`，那 **JSON 里其实已经有 speed 了，只是界面没把它画出来**。  
所以要 **改显示用的代码**（本工程里是 `LoadItemsOnStart.cs`），或改你自己的 UI。

---

## 你每天真正要做的（记住这 3 步）

### 第 1 步：改 Excel

1. 用 Excel 打开：`trytryz` 工程里的  
   **`Assets` → `Design` → `items.xlsx`**
2. 看 **`Sheet1`** 这一页。
3. 第一行是英文列名（例如 `id`、`name`、`hp`、`atk`、`speed`），**不要拼错**，要和下面说的 C# 里一致。
4. 从第 3 行开始写数据（你现在的第 2 行是空的，没关系）。
5. **保存**（Ctrl+S）。

### 第 2 步：导出成 JSON（推荐：用 Unity 里的按钮）

1. 打开 **Unity**，打开本工程。
2. 看顶部菜单栏，点：**`Trytryz`** → **`导表工具`**。
3. 在弹出窗口里点：**「① 导出道具表…」** 那个大按钮。
4. 若弹出「导表完成」，就成功了。Unity 会自动刷新文件。

**如果你从没装过 Python：**  
先在 Windows 里安装 Python（安装时勾选 **加入 PATH**），再在 CMD 里执行一次（整行复制，路径按你电脑上的仓库位置改对）：

```text
python -m pip install -r "d:\_UNITY_Projects\trytryz\tools\excel_export\requirements.txt"
```

装好后，再回到 Unity 点导表按钮。

**CMD 里 `cd` 的小坑（可选读）：**  
若你在 **C 盘**打开 CMD，要进 **D 盘**文件夹，请用：

```text
cd /d d:\_UNITY_Projects\trytryz
```

### 第 3 步：进游戏看效果

1. 打开任意带 **摄像机** 的场景（默认 `SampleScene` 就可以）。
2. 在 **Hierarchy** 里找一个物体，上面挂了 **`LoadItemsOnStart`** 组件；没有就自己建一个空物体，把脚本拖上去。
3. 点 **播放 ▶**。
4. 看 **Game** 窗口（不是 Scene 窗口）左上角：会有一块半透明黑底，上面写着每一行道具，里面应包含 **`speed=`**。

---

## 我加了一列（比如 speed），要改哪些地方？

按顺序检查下面 **4 件事**（缺哪步，哪步就不生效）：

| 顺序 | 要做什么 | 本工程对应文件 |
|------|----------|----------------|
| ① | Excel **第一行**写上英文列名，例如 `speed` | `Assets/Design/items.xlsx` |
| ② | C# 里 **`ItemRow`** 增加 `public float speed;`（或 `int`，和你数据一致） | `Assets/Scripts/Data/ItemRow.cs` |
| ③ | 点 **Trytryz → 导表工具** 导出，让 `items.json` 里有 `speed` | 自动生成到 `Resources/Tables/items.json` |
| ④ | **显示**的地方把 `speed` 写进字符串（或绑到你的 UI 上） | `LoadItemsOnStart.cs` 的 `OnGUI` 里那一行 |

以后你做正式 UI（血条、背包），也是：**数据在 `ItemRow` 里，你自己决定在哪个界面显示哪些字段**。

---

## JSON 在游戏里是怎么用的？（你以后写玩法会用到）

### 1. 文件放哪？

- 放在 **`Assets/Resources/Tables/`** 下面。
- 例如：`items.json`。

### 2. 代码里怎么读？

路径规则：**从 `Resources` 文件夹算起，不要写 `Resources` 这几个字，不要写 `.json`。**

```text
Resources.Load<TextAsset>("Tables/items")
```

### 3. 怎么变成 C# 对象？

本工程已经写好例子：

- `GameTableLoader.LoadItemsFromResources("Tables/items")`  
  会返回 **`ItemTable`**，里面 **`list`** 就是多行 **`ItemRow`**。

你在 **别的脚本**里可以这么用（伪代码思路）：

```csharp
var table = GameTableLoader.LoadItemsFromResources("Tables/items");
if (table?.list == null) return;
foreach (var item in table.list)
{
    // 用 item.id、item.speed 驱动移动、动画、伤害等
}
```

**规则：** Excel 第一行的英文列名 = `ItemRow` 里的 `public` 字段名，**一模一样**。

---

## 我想新建「另一张表」（例如怪物表）——按下面做

不用一次全记住，**照着复制改名**即可。

### 步骤 A：做 Excel

1. 在 **`Assets/Design`** 里复制 `items.xlsx`，改名为 **`monsters.xlsx`**（名字随便，好记即可）。
2. 打开它，在 **`Sheet1`**（或你改个名，但要记住）第一行写怪物字段，例如：`id`、`name`、`hp`。
3. 下面写数据，保存。

### 步骤 B：写两个 C# 类（照 `ItemRow` / `ItemTable` 抄）

1. 复制 **`ItemRow.cs`**，改名为 **`MonsterRow.cs`**，类名改成 **`MonsterRow`**，字段改成你的列（和 Excel 首行一致）。
2. 复制 **`ItemTable.cs`**，改名为 **`MonsterTable.cs`**，里面的数组类型改成 **`MonsterRow[]`**，类名改成 **`MonsterTable`**。

### 步骤 C：导出 JSON

在 CMD 里（或让 AI 帮你写一条），核心是：

- 输入文件：`...Design\monsters.xlsx`
- `--sheets` 后面写你的工作表名（默认 `Sheet1`）
- `--json-basename monsters` → 得到 **`monsters.json`**
- `-o` 指向：`...Assets\Resources\Tables`

示例（路径按你本机改）：

```text
python "d:\_UNITY_Projects\trytryz\tools\excel_export\export_xlsx.py" "d:\_UNITY_Projects\trytryz\trytryz\Assets\Design\monsters.xlsx" --format json --unity-json --sheets Sheet1 --json-basename monsters -o "d:\_UNITY_Projects\trytryz\trytryz\Assets\Resources\Tables"
```

### 步骤 D：在游戏里读

```text
Resources.Load<TextAsset>("Tables/monsters")
```

配合：

```csharp
JsonUtility.FromJson<MonsterTable>(text);
```

### 步骤 E：让 Unity 里也能「一键导出」这张表

打开 **`Assets/Editor/ExportGameTablesWindow.cs`**，照着 **`ExportItems`** 再复制一个方法，改：

- xlsx 文件名
- `--sheets` 工作表名
- `--json-basename` 输出名

然后在窗口里加第二个按钮。若你暂时不会改，可以每次都把上面的 **python 那一行**复制到 CMD 里运行，效果一样。

---

## 独立作者更省事的用法（已经给你做好的）

- **Excel** 仍然最好用：公式、筛选、多人传文件都方便，不必再造一个「表格软件」。
- **Unity 菜单 `Trytryz → 导表工具`**：代替记命令行，这是最适合你一个人的「可视化入口」。
- 若以后表很多，可以按上面的 **步骤 E** 一个表加一个按钮；再大一点的团队才会上数据库、服务器配表，你现阶段不用急。

---

## 出问题先查这几条

1. **Game 窗口看不见字**：确认挂了 **`LoadItemsOnStart`**，且场景里有 **摄像机**。
2. **改了 Excel 游戏里没变**：有没有点 **导表工具**？有没有 **`items.json` 在 Resources/Tables**？
3. **Console 报错 Json**：字段名和 JSON 里是否一致？`ItemRow` 是否 `public` 且带 `[Serializable]`？
4. **新列不显示**：多半是 **没改界面上的那一行字**（见上文「加了一列要改哪些地方」）。

---

## 给 AI 助手看的说明（你可忽略）

工程里有一份 **`tools/skill/SKILL.md`**，里面是路径、命令、命名约定。你让 Cursor 帮你改导表相关代码时，可以说：「按仓库里 `tools/skill/SKILL.md` 来」。

---

*文档路径：`d:\_UNITY_Projects\trytryz\Helps\导表与Unity读表完整教程.md`*
