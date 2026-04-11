using System;

/// <summary>
/// Example row: match Excel column names (first row) after export.
/// Add [Serializable] and public fields for JsonUtility.
/// </summary>
[Serializable]
public class ItemRow
{
    public int id;
    public string name;
    public int hp;
    public float atk;
    public float speed;
}
