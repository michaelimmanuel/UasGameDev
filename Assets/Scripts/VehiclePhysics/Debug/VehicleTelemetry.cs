using UnityEngine;

namespace VehiclePhysics
{
    public class VehicleTelemetry : MonoBehaviour
    {
        public PowertrainSystem powertrain;
        public BrakeSystem brakes;
        public TireForcesApplier tireForces;

        void Reset()
        {
            if (powertrain == null) powertrain = GetComponentInParent<PowertrainSystem>();
            if (brakes == null) brakes = GetComponentInParent<BrakeSystem>();
            if (tireForces == null) tireForces = GetComponentInParent<TireForcesApplier>();
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(500, 10, 280, 160), GUI.skin.box);
            GUILayout.Label("Powertrain Telemetry");
            if (powertrain != null)
            {
                GUILayout.Label($"Throttle={powertrain.throttle:F2} Gear={powertrain.currentGear}");
                GUILayout.Label($"RPM={powertrain.engineRpm:F0} Torque={powertrain.engineTorque:F0} EngBrake={powertrain.engineBrakeTorque:F0}");
            }
            if (brakes != null)
            {
                GUILayout.Label($"BrakeInput={brakes.brakeInput:F2}");
            }
            if (tireForces != null && tireForces.powertrain != null && tireForces.powertrain.wheelFxRequested != null)
            {
                var arr = tireForces.powertrain.wheelFxRequested;
                string fx = "Fx_req:";
                for (int i = 0; i < arr.Length; i++) fx += $" [{i}]={arr[i]:F0}";
                GUILayout.Label(fx);
            }
            GUILayout.EndArea();
        }
    }
}
