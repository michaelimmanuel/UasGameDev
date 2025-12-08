using UnityEditor;
using UnityEngine;

namespace VehiclePhysics.Editor
{
    public static class TireCurveEditorUtility
    {
        [MenuItem("Vehicle Physics/Tires/Reset Selected TireCurveData to Defaults", priority = 1000)]
        public static void ResetSelectedTireCurves()
        {
            var objs = Selection.objects;
            if (objs == null || objs.Length == 0)
            {
                EditorUtility.DisplayDialog("Reset Tire Curves", "Select one or more TireCurveData assets in the Project view.", "OK");
                return;
            }

            int updated = 0;
            foreach (var obj in objs)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                var data = AssetDatabase.LoadAssetAtPath<VehiclePhysics.TireCurveData>(path);
                if (data == null) continue;

                Undo.RecordObject(data, "Reset Tire Curves to Defaults");
                data.muXFront = VehiclePhysics.TireCurveData.CreateDefaultMuXFront();
                data.muXRear  = VehiclePhysics.TireCurveData.CreateDefaultMuXRear();
                data.muYFront = VehiclePhysics.TireCurveData.CreateDefaultMuYFront();
                data.muYRear  = VehiclePhysics.TireCurveData.CreateDefaultMuYRear();

                EditorUtility.SetDirty(data);
                updated++;
            }

            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Reset Tire Curves", $"Updated {updated} TireCurveData asset(s).", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Reset Tire Curves", "No TireCurveData assets selected.", "OK");
            }
        }

        [MenuItem("Vehicle Physics/Tires/Create TireCurveData (Defaults)", priority = 1001)]
        public static void CreateDefaultTireCurveData()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create TireCurveData", "TireCurveData", "asset", "Choose a location for the new TireCurveData asset.");
            if (string.IsNullOrEmpty(path)) return;

            var data = ScriptableObject.CreateInstance<VehiclePhysics.TireCurveData>();
            data.muXFront = VehiclePhysics.TireCurveData.CreateDefaultMuXFront();
            data.muXRear  = VehiclePhysics.TireCurveData.CreateDefaultMuXRear();
            data.muYFront = VehiclePhysics.TireCurveData.CreateDefaultMuYFront();
            data.muYRear  = VehiclePhysics.TireCurveData.CreateDefaultMuYRear();

            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;
        }
    }
}
