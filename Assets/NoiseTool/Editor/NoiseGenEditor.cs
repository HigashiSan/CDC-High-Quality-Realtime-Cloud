using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NoiseGenerator))]
public class NoiseGenEditor : Editor
{

    NoiseGenerator noise;
    Editor noiseSettingsEditor;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Update"))
        {
            noise.ManualUpdate();
            EditorApplication.QueuePlayerLoopUpdate();
        }

    }

    void DrawSettingsEditor(Object settings, ref bool foldout, ref Editor editor)
    {
        if (settings != null)
        {
            foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (foldout)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();
                }
                if (check.changed)
                {
                    noise.ActiveNoiseSettingsChanged();
                }
            }
        }
    }

    void OnEnable()
    {
        noise = (NoiseGenerator)target;
    }

}