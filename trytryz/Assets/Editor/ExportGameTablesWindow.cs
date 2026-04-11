using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 独立制作向：在 Unity 里一键调用本仓库的 Python 导表脚本，无需记命令行。
/// 菜单：Trytryz → 导表工具
/// </summary>
public class ExportGameTablesWindow : EditorWindow
{
    string _lastLog = "";

    [MenuItem("Trytryz/导表工具")]
    static void Open() => GetWindow<ExportGameTablesWindow>(true, "导表工具", true);

    void OnGUI()
    {
        GUILayout.Label("从 Excel 生成 JSON（放进 Resources，游戏才能读到）", EditorStyles.boldLabel);
        GUILayout.Space(6);

        if (GUILayout.Button("① 导出「道具表」items.xlsx → items.json", GUILayout.Height(36)))
            ExportItems();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "第一次使用前请安装 Python，并在 CMD 里执行一次：\n"
                + "python -m pip install -r \"…\\tools\\excel_export\\requirements.txt\"\n"
                + "（完整路径见 Helps 文件夹里的教程）",
            MessageType.Info);

        if (!string.IsNullOrEmpty(_lastLog))
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("上次输出：", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(_lastLog, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(120));
        }
    }

    void ExportItems()
    {
        if (!TryResolvePaths(out var script, out var xlsx, out var outDir, out var error))
        {
            _lastLog = error;
            EditorUtility.DisplayDialog("导表失败", error, "确定");
            return;
        }

        string args =
            $"\"{script}\" \"{xlsx}\" --format json --unity-json --sheets Sheet1 --json-basename items -o \"{outDir}\"";

        if (!RunPython(args, out var output, out var err))
        {
            _lastLog = output + "\n" + err;
            EditorUtility.DisplayDialog(
                "导表失败",
                "请确认已安装 Python 且已 pip install openpyxl。\n\n" + _lastLog,
                "确定");
            return;
        }

        _lastLog = string.IsNullOrWhiteSpace(output) ? "(无标准输出)" : output;
        if (!string.IsNullOrWhiteSpace(err))
            _lastLog += "\n" + err;

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("导表完成", "已生成 items.json，资源已刷新。", "确定");
    }

    static bool TryResolvePaths(out string script, out string xlsx, out string outDir, out string error)
    {
        script = xlsx = outDir = null;
        error = null;

        string assets = Application.dataPath.Replace("\\", "/");
        string unityProj = Path.GetDirectoryName(assets);
        if (string.IsNullOrEmpty(unityProj))
        {
            error = "无法解析 Unity 工程路径。";
            return false;
        }

        string repoRoot = Path.GetDirectoryName(unityProj);
        if (string.IsNullOrEmpty(repoRoot))
        {
            error = "无法解析仓库根目录（上一级文件夹）。";
            return false;
        }

        script = Path.Combine(repoRoot, "tools", "excel_export", "export_xlsx.py");
        xlsx = Path.Combine(assets, "Design", "items.xlsx");
        outDir = Path.Combine(assets, "Resources", "Tables");

        if (!File.Exists(script))
        {
            error = "找不到导表脚本：\n" + script;
            return false;
        }

        if (!File.Exists(xlsx))
        {
            error = "找不到 Excel：\n" + xlsx + "\n\n请在 Assets/Design 下放置 items.xlsx。";
            return false;
        }

        error = null;
        return true;
    }

    static bool RunPython(string arguments, out string stdout, out string stderr)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        try
        {
            using var p = Process.Start(psi);
            if (p == null)
            {
                stdout = "";
                stderr = "无法启动进程。";
                return false;
            }

            stdout = p.StandardOutput.ReadToEnd();
            stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(60000);
            return p.ExitCode == 0;
        }
        catch (Exception ex)
        {
            stdout = "";
            stderr = ex.Message;
            return false;
        }
    }
}
