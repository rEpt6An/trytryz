using UnityEngine;

/// <summary>
/// Loads items from Resources and shows them on screen (OnGUI) plus a short Console summary.
/// Attach to any scene object; ensure the scene has a Camera (Game view shows the overlay).
/// </summary>
public class LoadItemsOnStart : MonoBehaviour
{
    [SerializeField] string resourcesPath = "Tables/items";

    ItemTable _table;
    Texture2D _backdrop;

    void Start()
    {
        _table = GameTableLoader.LoadItemsFromResources(resourcesPath);
        if (_table == null || _table.list == null)
        {
            Debug.LogWarning($"[LoadItemsOnStart] Failed to load '{resourcesPath}'.");
            return;
        }

        Debug.Log($"[LoadItemsOnStart] Loaded {_table.list.Length} row(s) from '{resourcesPath}'.");
    }

    void OnGUI()
    {
        if (_table == null || _table.list == null)
            return;

        const float pad = 12f;
        float y = pad;
        float boxH = 44f + _table.list.Length * 28f;
        var box = new Rect(0f, 0f, Mathf.Min(Screen.width, 720f), boxH);
        if (_backdrop == null)
        {
            _backdrop = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _backdrop.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
            _backdrop.Apply();
        }

        GUI.DrawTexture(box, _backdrop, ScaleMode.StretchToFill);

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = Color.white },
        };
        GUI.Label(new Rect(pad, y, box.width - pad * 2f, 28f), "道具表（Resources / items.json）", labelStyle);
        y += 32f;

        foreach (var row in _table.list)
        {
            var line =
                $"id={row.id}   {row.name}   hp={row.hp}   atk={row.atk}   speed={row.speed}";
            GUI.Label(new Rect(pad, y, box.width - pad * 2f, 28f), line, labelStyle);
            y += 28f;
        }
    }
}
