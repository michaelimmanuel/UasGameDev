using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Comprehensive telemetry HUD for vehicle physics debugging and tuning.
    /// Displays vehicle state, inputs, per-wheel data, and drift information.
    /// Press F1 to toggle visibility.
    /// </summary>
    [AddComponentMenu("Vehicle Physics/Debug/Vehicle Telemetry")]
    public class VehicleTelemetry : MonoBehaviour
    {
        [Header("References")]
        public Rigidbody rb;
        public SuspensionSystem suspension;
        public PowertrainSystem powertrain;
        public BrakeSystem brakes;
        public SlipCalculator slipCalc;
        public SteeringInput steering;
        public TireForcesApplier tireForces;

        [Header("Display Settings")]
        [Tooltip("Show the telemetry HUD")]
        public bool showTelemetry = true;
        [Tooltip("Position of the HUD on screen")]
        public Vector2 hudPosition = new Vector2(10, 10);
        [Tooltip("Width of the HUD panels")]
        public float panelWidth = 300f;
        [Tooltip("Background opacity")]
        [Range(0f, 1f)] public float backgroundAlpha = 0.85f;

        [Header("Toggle Sections")]
        public bool showVehicleState = true;
        public bool showInputs = true;
        public bool showWheelData = true;
        public bool showDriftInfo = true;
        public bool showGripInfo = true;

        // Cached styles
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _goodStyle;
        private Texture2D _bgTexture;
        private Texture2D _barTexture;
        private bool _stylesInitialized;

        // Calculated values
        private float _speedKmh;
        private float _speedMph;
        private float _bodySlipAngle;
        private float _driftAngle;
        private bool _isDrifting;
        private float[] _wheelOmegaRPM;

        void Reset()
        {
            AutoFindReferences();
        }

        void Awake()
        {
            AutoFindReferences();
        }

        void AutoFindReferences()
        {
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (powertrain == null) powertrain = GetComponentInParent<PowertrainSystem>();
            if (brakes == null) brakes = GetComponentInParent<BrakeSystem>();
            if (slipCalc == null) slipCalc = GetComponentInParent<SlipCalculator>();
            if (steering == null) steering = GetComponentInParent<SteeringInput>();
            if (tireForces == null) tireForces = GetComponentInParent<TireForcesApplier>();
        }

        void Update()
        {
            // Toggle telemetry with F1
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showTelemetry = !showTelemetry;
            }

            CalculateValues();
        }

        void CalculateValues()
        {
            if (rb == null) return;

            // Speed
            Vector3 velocity = rb.linearVelocity;
            _speedKmh = velocity.magnitude * 3.6f;
            _speedMph = velocity.magnitude * 2.237f;

            // Body slip angle (angle between velocity and forward direction)
            Vector3 localVel = transform.InverseTransformDirection(velocity);
            if (localVel.magnitude > 0.5f)
            {
                _bodySlipAngle = Mathf.Atan2(localVel.x, localVel.z) * Mathf.Rad2Deg;
            }
            else
            {
                _bodySlipAngle = 0f;
            }

            // Drift detection
            _driftAngle = Mathf.Abs(_bodySlipAngle);
            _isDrifting = _driftAngle > 15f && _speedKmh > 20f;

            // Wheel omega in RPM
            if (suspension != null && suspension.CurrentWheelStates != null)
            {
                int n = suspension.CurrentWheelStates.Length;
                if (_wheelOmegaRPM == null || _wheelOmegaRPM.Length != n)
                    _wheelOmegaRPM = new float[n];

                for (int i = 0; i < n; i++)
                {
                    float omega = suspension.CurrentWheelStates[i].omega;
                    _wheelOmegaRPM[i] = Mathf.Abs(omega) * 60f / (2f * Mathf.PI);
                }
            }
        }

        void InitStyles()
        {
            if (_stylesInitialized) return;

            _bgTexture = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.15f, backgroundAlpha));
            _barTexture = MakeTexture(1, 1, Color.white);

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _bgTexture;
            _boxStyle.padding = new RectOffset(8, 8, 5, 5);

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.fontSize = 13;
            _headerStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);
            _headerStyle.alignment = TextAnchor.MiddleCenter;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 11;
            _labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.fontSize = 11;
            _valueStyle.normal.textColor = new Color(0.5f, 1f, 0.5f);
            _valueStyle.alignment = TextAnchor.MiddleRight;

            _warningStyle = new GUIStyle(GUI.skin.label);
            _warningStyle.fontSize = 11;
            _warningStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
            _warningStyle.fontStyle = FontStyle.Bold;

            _goodStyle = new GUIStyle(GUI.skin.label);
            _goodStyle.fontSize = 11;
            _goodStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);

            _stylesInitialized = true;
        }

        Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        void OnGUI()
        {
            if (!showTelemetry) return;
            InitStyles();

            float x = hudPosition.x;
            float y = hudPosition.y;
            float padding = 5f;

            // Left column
            if (showVehicleState)
            {
                y = DrawVehicleStatePanel(x, y);
                y += padding;
            }

            if (showInputs)
            {
                y = DrawInputsPanel(x, y);
                y += padding;
            }

            if (showDriftInfo)
            {
                y = DrawDriftPanel(x, y);
                y += padding;
            }

            // Right column - Wheel Data
            if (showWheelData)
            {
                DrawWheelDataPanel(x + panelWidth + 10, hudPosition.y);
            }

            // Right column - Grip Info
            if (showGripInfo && slipCalc != null)
            {
                DrawGripPanel(x + panelWidth + 10, hudPosition.y + 240);
            }

            // Help text at bottom
            GUI.Label(new Rect(x, Screen.height - 22, 250, 20), "F1: Toggle Telemetry", _labelStyle);
        }

        float DrawVehicleStatePanel(float x, float y)
        {
            float height = 95f;
            GUILayout.BeginArea(new Rect(x, y, panelWidth, height), _boxStyle);

            GUILayout.Label("══ VEHICLE STATE ══", _headerStyle);
            GUILayout.Space(3);

            // Speed
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:", _labelStyle, GUILayout.Width(70));
            GUILayout.Label($"{_speedKmh:F1} km/h  ({_speedMph:F1} mph)", _valueStyle);
            GUILayout.EndHorizontal();

            // Engine
            if (powertrain != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Engine:", _labelStyle, GUILayout.Width(70));
                GUILayout.Label($"{powertrain.engineRpm:F0} RPM  |  Torque: {powertrain.engineTorque:F0} Nm", _valueStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Gear:", _labelStyle, GUILayout.Width(70));
                int gear = powertrain.currentGear;
                string gearStr = gear == 0 ? "N" : (gear < 0 ? "R" : gear.ToString());
                GUILayout.Label(gearStr, _valueStyle);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
            return y + height;
        }

        float DrawInputsPanel(float x, float y)
        {
            float height = 115f;
            GUILayout.BeginArea(new Rect(x, y, panelWidth, height), _boxStyle);

            GUILayout.Label("══ INPUTS ══", _headerStyle);
            GUILayout.Space(3);

            // Throttle
            if (powertrain != null)
            {
                DrawProgressBar("Throttle", powertrain.throttle, new Color(0.3f, 0.9f, 0.3f));
            }

            // Brake
            if (brakes != null)
            {
                DrawProgressBar("Brake", brakes.brakeInput, new Color(0.9f, 0.3f, 0.3f));
                
                // Handbrake with status
                GUILayout.BeginHorizontal();
                GUILayout.Label("Handbrake:", _labelStyle, GUILayout.Width(70));
                DrawBarOnly(brakes.handbrakeInput, new Color(1f, 0.6f, 0.2f), 120);
                string hbStatus = brakes.handbrakeInput > 0.5f ? "ACTIVE" : $"{brakes.handbrakeInput * 100:F0}%";
                var hbStyle = brakes.handbrakeInput > 0.5f ? _warningStyle : _valueStyle;
                GUILayout.Label(hbStatus, hbStyle, GUILayout.Width(60));
                GUILayout.EndHorizontal();
            }

            // Steering
            if (steering != null && suspension != null && suspension.wheels != null && suspension.wheels.Length > 0)
            {
                float steerAngle = suspension.wheels[0].steerDeg;
                float steerNorm = steerAngle / Mathf.Max(1f, steering.maxSteerDeg);
                DrawSteeringBar("Steering", steerNorm, steerAngle);
            }

            GUILayout.EndArea();
            return y + height;
        }

        float DrawDriftPanel(float x, float y)
        {
            float height = 70f;
            GUILayout.BeginArea(new Rect(x, y, panelWidth, height), _boxStyle);

            GUILayout.Label("══ DRIFT STATUS ══", _headerStyle);
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Body Slip:", _labelStyle, GUILayout.Width(70));
            var slipStyle = _driftAngle > 30f ? _warningStyle : (_driftAngle > 15f ? new GUIStyle(_valueStyle) { normal = { textColor = Color.yellow } } : _valueStyle);
            string dir = _bodySlipAngle > 1f ? "→" : (_bodySlipAngle < -1f ? "←" : "-");
            GUILayout.Label($"{dir} {_bodySlipAngle:F1}°", slipStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Status:", _labelStyle, GUILayout.Width(70));
            if (_isDrifting)
            {
                GUILayout.Label($"★ DRIFTING ({_driftAngle:F0}°) ★", _warningStyle);
            }
            else if (_driftAngle > 8f)
            {
                GUILayout.Label("Sliding", new GUIStyle(_valueStyle) { normal = { textColor = Color.yellow } });
            }
            else
            {
                GUILayout.Label("Stable", _goodStyle);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            return y + height;
        }

        void DrawWheelDataPanel(float x, float y)
        {
            if (suspension == null || suspension.CurrentWheelStates == null) return;

            int n = suspension.CurrentWheelStates.Length;
            float height = 35 + n * 45;
            GUILayout.BeginArea(new Rect(x, y, panelWidth + 80, height), _boxStyle);

            GUILayout.Label("══ WHEEL DATA ══", _headerStyle);
            GUILayout.Space(3);

            string[] wheelNames = { "FL", "FR", "RL", "RR" };
            for (int i = 0; i < n; i++)
            {
                var ws = suspension.CurrentWheelStates[i];
                string name = i < wheelNames.Length ? wheelNames[i] : $"W{i}";

                float slipRatio = (slipCalc != null && slipCalc.slipRatio != null && i < slipCalc.slipRatio.Length)
                    ? slipCalc.slipRatio[i] : 0f;
                float slipAngle = (slipCalc != null && slipCalc.slipAngleDeg != null && i < slipCalc.slipAngleDeg.Length)
                    ? slipCalc.slipAngleDeg[i] : 0f;
                float rpm = (_wheelOmegaRPM != null && i < _wheelOmegaRPM.Length) ? _wheelOmegaRPM[i] : 0f;

                // Status detection
                bool isLocked = slipRatio < -0.5f && _speedKmh > 5f;
                bool isSpinning = slipRatio > 0.35f && _speedKmh > 3f;

                // Line 1: Name + Load + Status
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{name}]", _labelStyle, GUILayout.Width(30));
                GUILayout.Label($"Load: {ws.loadN:F0}N", _labelStyle, GUILayout.Width(80));
                GUILayout.Label($"ω: {rpm:F0} rpm", _labelStyle, GUILayout.Width(85));

                if (isLocked) GUILayout.Label("■ LOCKED", _warningStyle);
                else if (isSpinning) GUILayout.Label("■ SPIN", new GUIStyle(_labelStyle) { normal = { textColor = Color.yellow } });
                else if (ws.grounded) GUILayout.Label("● Grip", _goodStyle);
                else GUILayout.Label("○ Air", _labelStyle);

                GUILayout.EndHorizontal();

                // Line 2: Slip data
                GUILayout.BeginHorizontal();
                GUILayout.Label("", _labelStyle, GUILayout.Width(30));
                
                var sStyle = Mathf.Abs(slipRatio) > 0.35f ? _warningStyle : _valueStyle;
                GUILayout.Label($"Slip: {slipRatio:F3}", sStyle, GUILayout.Width(85));
                
                var aStyle = Mathf.Abs(slipAngle) > 20f ? _warningStyle : _valueStyle;
                GUILayout.Label($"Angle: {slipAngle:F1}°", aStyle, GUILayout.Width(85));

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        void DrawGripPanel(float x, float y)
        {
            if (slipCalc == null || slipCalc.muX == null) return;

            int n = slipCalc.muX.Length;
            float height = 35 + n * 22;
            GUILayout.BeginArea(new Rect(x, y, panelWidth + 80, height), _boxStyle);

            GUILayout.Label("══ TIRE GRIP (μ) ══", _headerStyle);
            GUILayout.Space(3);

            string[] wheelNames = { "FL", "FR", "RL", "RR" };

            for (int i = 0; i < n; i++)
            {
                string name = i < wheelNames.Length ? wheelNames[i] : $"W{i}";
                float muX = slipCalc.muX[i];
                float muY = slipCalc.muY[i];
                float totalGrip = Mathf.Sqrt(muX * muX + muY * muY);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{name}]", _labelStyle, GUILayout.Width(30));
                GUILayout.Label($"μx:{muX:F2}", _valueStyle, GUILayout.Width(60));
                GUILayout.Label($"μy:{muY:F2}", _valueStyle, GUILayout.Width(60));
                
                // Visual grip bar
                DrawBarOnly(totalGrip / 1.5f, GripColor(totalGrip), 100);
                GUILayout.Label($"{totalGrip:F2}", _labelStyle, GUILayout.Width(40));

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        Color GripColor(float grip)
        {
            if (grip > 1.0f) return new Color(0.3f, 1f, 0.3f);
            if (grip > 0.6f) return new Color(1f, 1f, 0.3f);
            return new Color(1f, 0.4f, 0.3f);
        }

        void DrawProgressBar(string label, float value, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", _labelStyle, GUILayout.Width(70));
            DrawBarOnly(value, color, 140);
            GUILayout.Label($"{value * 100:F0}%", _valueStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();
        }

        void DrawBarOnly(float value, Color color, float width)
        {
            Rect barRect = GUILayoutUtility.GetRect(width, 14);
            
            // Background
            GUI.DrawTexture(barRect, MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.2f, 0.8f)));
            
            // Fill
            float fillWidth = (barRect.width - 2) * Mathf.Clamp01(value);
            if (fillWidth > 0)
            {
                Rect fillRect = new Rect(barRect.x + 1, barRect.y + 1, fillWidth, barRect.height - 2);
                GUI.DrawTexture(fillRect, MakeTexture(1, 1, color));
            }
        }

        void DrawSteeringBar(string label, float value, float angleDeg)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", _labelStyle, GUILayout.Width(70));

            Rect barRect = GUILayoutUtility.GetRect(140, 14);
            
            // Background
            GUI.DrawTexture(barRect, MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.2f, 0.8f)));

            float center = barRect.x + barRect.width / 2;
            float fillWidth = (barRect.width / 2 - 2) * Mathf.Clamp(Mathf.Abs(value), 0f, 1f);
            
            if (fillWidth > 1)
            {
                Rect fillRect;
                if (value >= 0)
                    fillRect = new Rect(center, barRect.y + 1, fillWidth, barRect.height - 2);
                else
                    fillRect = new Rect(center - fillWidth, barRect.y + 1, fillWidth, barRect.height - 2);
                
                GUI.DrawTexture(fillRect, MakeTexture(1, 1, new Color(0.3f, 0.8f, 1f)));
            }

            // Center line
            GUI.DrawTexture(new Rect(center - 1, barRect.y, 2, barRect.height), MakeTexture(1, 1, Color.white));

            string dir = value > 0.02f ? "R" : (value < -0.02f ? "L" : "-");
            GUILayout.Label($"{dir} {Mathf.Abs(angleDeg):F1}°", _valueStyle, GUILayout.Width(55));
            GUILayout.EndHorizontal();
        }
    }
}
