using UnityEngine;
using UnityEditor;

namespace VehiclePhysics.Editor
{
    [CustomEditor(typeof(EngineTorqueCurve))]
    public class EngineTorqueCurveEditor : UnityEditor.Editor
    {
        SerializedProperty fullTorqueProp;
        SerializedProperty idleRpmProp;
        SerializedProperty redlineRpmProp;
        SerializedProperty torqueScaleProp;
        SerializedProperty engineBrakeFractionProp;
        SerializedProperty engineBrakeMinRpmProp;

        void OnEnable()
        {
            fullTorqueProp = serializedObject.FindProperty("fullThrottleTorque");
            idleRpmProp = serializedObject.FindProperty("idleRpm");
            redlineRpmProp = serializedObject.FindProperty("redlineRpm");
            torqueScaleProp = serializedObject.FindProperty("torqueScale");
            engineBrakeFractionProp = serializedObject.FindProperty("engineBrakeFraction");
            engineBrakeMinRpmProp = serializedObject.FindProperty("engineBrakeMinRpm");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Engine Torque Curve", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fullTorqueProp);
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(idleRpmProp);
            EditorGUILayout.PropertyField(redlineRpmProp);
            EditorGUILayout.PropertyField(torqueScaleProp);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Engine Braking", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(engineBrakeFractionProp);
            EditorGUILayout.PropertyField(engineBrakeMinRpmProp);

            EditorGUILayout.Space(10);
            DrawPresetButtons();
            EditorGUILayout.Space(6);
            DrawModificationButtons();
            EditorGUILayout.Space(6);
            DrawAnalysis();
            EditorGUILayout.Space(6);
            DrawExport();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawPresetButtons()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply E30 M3 Stock"))
                {
                    ApplyCurve(EngineTorqueCurve.CreateE30M3S14B23StockCurve());
                }
                if (GUILayout.Button("Apply E30 M3 Evo2"))
                {
                    ApplyCurve(EngineTorqueCurve.CreateE30M3S14B23Evo2Curve());
                }
            }
        }

        void DrawModificationButtons()
        {
            EditorGUILayout.LabelField("Adjustments", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scale x1.05"))
                {
                    ScaleTorque(1.05f);
                }
                if (GUILayout.Button("Scale x0.95"))
                {
                    ScaleTorque(0.95f);
                }
                if (GUILayout.Button("Boost Low <3000 +5%"))
                {
                    BoostLowRpm(3000f, 0.05f);
                }
                if (GUILayout.Button("Smooth (1 pass)"))
                {
                    SmoothCurve(1);
                }
            }
        }

        void DrawAnalysis()
        {
            var curve = GetCurve();
            if (curve == null || curve.length == 0) return;
            float peakTorque = 0f; float peakTorqueRpm = 0f;
            float peakPowerHp = 0f; float peakPowerRpm = 0f;
            // Sample in 250 rpm increments
            var engineData = (EngineTorqueCurve)target;
            float start = engineData.idleRpm; float end = engineData.redlineRpm;
            for (float rpm = start; rpm <= end; rpm += 250f)
            {
                float tq = curve.Evaluate(rpm) * engineData.torqueScale;
                float hp = tq * rpm / 7127f; // mechanical hp approximation
                if (tq > peakTorque) { peakTorque = tq; peakTorqueRpm = rpm; }
                if (hp > peakPowerHp) { peakPowerHp = hp; peakPowerRpm = rpm; }
            }
            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Peak Torque: {peakTorque:F1} Nm @ {peakTorqueRpm:F0} rpm\n" +
                $"Peak Power: {peakPowerHp:F1} hp @ {peakPowerRpm:F0} rpm\n" +
                $"TorqueScale: {engineData.torqueScale:F2}", MessageType.Info);
        }

        void DrawExport()
        {
            if (GUILayout.Button("Export Curve CSV"))
            {
                string path = EditorUtility.SaveFilePanel("Save Torque Curve CSV", Application.dataPath, "TorqueCurve", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    ExportCsv(path);
                }
            }
        }

        AnimationCurve GetCurve() => fullTorqueProp.animationCurveValue;

        void ApplyCurve(AnimationCurve preset)
        {
            Undo.RecordObject(target, "Apply Torque Preset");
            fullTorqueProp.animationCurveValue = preset;
            EditorUtility.SetDirty(target);
        }

        void ScaleTorque(float scale)
        {
            var c = GetCurve();
            if (c == null) return;
            Undo.RecordObject(target, "Scale Torque Curve");
            for (int i = 0; i < c.length; i++)
            {
                var k = c.keys[i];
                k.value *= scale;
                c.MoveKey(i, k);
            }
            fullTorqueProp.animationCurveValue = c;
            EditorUtility.SetDirty(target);
        }

        void BoostLowRpm(float threshold, float boostFraction)
        {
            var c = GetCurve();
            if (c == null) return;
            Undo.RecordObject(target, "Boost Low RPM Torque");
            for (int i = 0; i < c.length; i++)
            {
                var k = c.keys[i];
                if (k.time < threshold) { k.value *= (1f + boostFraction); c.MoveKey(i, k); }
            }
            fullTorqueProp.animationCurveValue = c;
            EditorUtility.SetDirty(target);
        }

        void SmoothCurve(int passes)
        {
            var c = GetCurve();
            if (c == null || c.length < 3) return;
            Undo.RecordObject(target, "Smooth Torque Curve");
            for (int p = 0; p < passes; p++)
            {
                for (int i = 1; i < c.length - 1; i++)
                {
                    var prev = c.keys[i - 1];
                    var curr = c.keys[i];
                    var next = c.keys[i + 1];
                    float smoothed = (prev.value + curr.value + next.value) / 3f;
                    curr.value = smoothed;
                    c.MoveKey(i, curr);
                }
            }
            fullTorqueProp.animationCurveValue = c;
            EditorUtility.SetDirty(target);
        }

        void ExportCsv(string path)
        {
            var c = GetCurve();
            if (c == null) return;
            var engineData = (EngineTorqueCurve)target;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("RPM,Torque(Nm),Power(HP)");
            for (float rpm = engineData.idleRpm; rpm <= engineData.redlineRpm; rpm += 250f)
            {
                float tq = c.Evaluate(rpm) * engineData.torqueScale;
                float hp = tq * rpm / 7127f;
                sb.AppendLine($"{rpm:F0},{tq:F2},{hp:F2}");
            }
            System.IO.File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
        }
    }
}
