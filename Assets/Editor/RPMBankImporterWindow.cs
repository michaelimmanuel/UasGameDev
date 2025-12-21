using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class RPMBankImporterWindow : EditorWindow
{
    SimpleRPMBankPlayer targetPlayer;
    DefaultAsset folderAsset;

    [MenuItem("Tools/RPM Bank Importer")]
    static void ShowWindow()
    {
        var win = GetWindow<RPMBankImporterWindow>("RPM Bank Importer");
        win.minSize = new Vector2(420, 160);
    }

    void OnGUI()
    {
        GUILayout.Label("Populate SimpleRPMBankPlayer samples from audio filenames", EditorStyles.boldLabel);

        targetPlayer = (SimpleRPMBankPlayer)EditorGUILayout.ObjectField("Target Player", targetPlayer, typeof(SimpleRPMBankPlayer), true);
        folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Folder (Assets)", folderAsset, typeof(DefaultAsset), false);

        if (GUILayout.Button("Scan Folder and Assign"))
        {
            if (targetPlayer == null) { EditorUtility.DisplayDialog("Error", "Assign a target SimpleRPMBankPlayer first.", "OK"); return; }
            if (folderAsset == null) { EditorUtility.DisplayDialog("Error", "Assign a folder under Assets to scan.", "OK"); return; }
            string path = AssetDatabase.GetAssetPath(folderAsset);
            if (string.IsNullOrEmpty(path)) { EditorUtility.DisplayDialog("Error", "Invalid folder.", "OK"); return; }
            ScanFolderAndAssign(path);
        }

        if (GUILayout.Button("Scan Selected AudioClips and Assign"))
        {
            if (targetPlayer == null) { EditorUtility.DisplayDialog("Error", "Assign a target SimpleRPMBankPlayer first.", "OK"); return; }
            ScanSelectionAndAssign();
        }

        GUILayout.Space(8);
        EditorGUILayout.HelpBox("Filenames should contain RPM values, e.g. 'Engine_1000RPM.wav' or '1000.wav'. The importer will parse the first 3-5 digit number found (prefers numbers followed by 'rpm').", MessageType.Info);
    }

    void ScanFolderAndAssign(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
        var found = new List<(int rpm, AudioClip clip)>();
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            if (clip == null) continue;
            if (TryParseRPMFromName(clip.name, out int rpm)) found.Add((rpm, clip));
        }
        AssignToPlayer(found);
    }

    void ScanSelectionAndAssign()
    {
        var objs = Selection.objects;
        var found = new List<(int rpm, AudioClip clip)>();
        foreach (var o in objs)
        {
            if (o is AudioClip ac)
            {
                if (TryParseRPMFromName(ac.name, out int rpm)) found.Add((rpm, ac));
            }
            else
            {
                string p = AssetDatabase.GetAssetPath(o);
                if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
                {
                    string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { p });
                    foreach (var g in guids)
                    {
                        string pp = AssetDatabase.GUIDToAssetPath(g);
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(pp);
                        if (clip == null) continue;
                        if (TryParseRPMFromName(clip.name, out int rpm)) found.Add((rpm, clip));
                    }
                }
            }
        }
        AssignToPlayer(found);
    }

    bool TryParseRPMFromName(string name, out int rpm)
    {
        rpm = 0;
        // prefer patterns like 1000RPM or 1000_rpm
        var m = Regex.Match(name, @"(?i)(\d{3,5})\s*_?rpm");
        if (m.Success && int.TryParse(m.Groups[1].Value, out rpm)) return true;
        // fallback: first 3-5 digit number
        m = Regex.Match(name, @"(\d{3,5})");
        if (m.Success && int.TryParse(m.Groups[1].Value, out rpm)) return true;
        return false;
    }

    void AssignToPlayer(List<(int rpm, AudioClip clip)> found)
    {
        if (found == null || found.Count == 0)
        {
            EditorUtility.DisplayDialog("No clips found", "No audio clips with RPM patterns were found.", "OK");
            return;
        }

        // group by rpm (pick first clip per rpm) and sort
        var groups = found.GroupBy(f => f.rpm).Select(g => (rpm: g.Key, clip: g.First().clip)).OrderBy(x => x.rpm).ToArray();

        var entries = new SimpleRPMBankPlayer.SampleEntry[groups.Length];
        for (int i = 0; i < groups.Length; i++) entries[i] = new SimpleRPMBankPlayer.SampleEntry { rpm = groups[i].rpm, clip = groups[i].clip };

        Undo.RecordObject(targetPlayer, "Populate RPM samples");
        targetPlayer.samples = entries;
        EditorUtility.SetDirty(targetPlayer);
        Debug.Log($"Assigned {entries.Length} RPM samples to {targetPlayer.name}");
    }
}
