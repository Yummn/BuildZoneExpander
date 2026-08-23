using System;
using Modding;
using UnityEngine;

namespace BuildZoneExpander
{
    public sealed class Mod : ModEntryPoint
    {
        public override void OnLoad()
        {
            GameObject host = GameObject.Find("BuildZoneExpander.Controller");
            if (host == null)
            {
                host = new GameObject("BuildZoneExpander.Controller");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<BuildZoneExpanderController>();
            }
        }
    }

    public sealed class BuildZoneExpanderController : MonoBehaviour
    {
        private const string EnabledKey = "BuildZoneExpander.Enabled";
        private const string FactorKey = "BuildZoneExpander.Factor";

        private BoundingBoxController controller;
        private Vector3 originalScale;
        private bool enabledForLevel;
        private float factor;
        private Rect windowRect = new Rect(20f, 110f, 270f, 150f);
        private int windowId;
        private string factorText;
        private bool hasAppliedScale;
        private bool hasCapturedOriginalScale;

        private void Awake()
        {
            windowId = ModUtility.GetWindowId();
            enabledForLevel = PlayerPrefs.GetInt(EnabledKey, 1) != 0;
            factor = Mathf.Clamp(PlayerPrefs.GetFloat(FactorKey, 2f), 1f, 10f);
            factorText = factor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            Debug.Log("[Build Zone Expander] Controller loaded.");
        }

        private void Update()
        {
            BoundingBoxController current = UnityEngine.Object.FindObjectOfType<BoundingBoxController>();
            if (current != controller)
            {
                RestoreOriginalScale();
                controller = current;
                hasAppliedScale = false;
                hasCapturedOriginalScale = false;
            }

            if (controller == null || controller.machine == null)
                return;

            if (!hasCapturedOriginalScale)
            {
                originalScale = controller.transform.localScale;
                hasCapturedOriginalScale = true;
                Debug.Log("[Build Zone Expander] Captured build box scale " + originalScale + ".");
            }

            if (StatMaster.isMP || !enabledForLevel || factor <= 1.001f)
            {
                RestoreOriginalScale();
                return;
            }

            Vector3 wanted = originalScale * factor;
            if (!hasAppliedScale || (controller.transform.localScale - wanted).sqrMagnitude > 0.0001f)
            {
                controller.transform.localScale = wanted;
                controller.SetFloorPos(true);
                controller.Check(controller.machine, true);
                hasAppliedScale = true;
            }
        }

        private void RestoreOriginalScale()
        {
            if (controller != null && hasAppliedScale && hasCapturedOriginalScale)
            {
                controller.transform.localScale = originalScale;
                controller.SetFloorPos(true);
                if (controller.machine != null)
                    controller.Check(controller.machine, true);
            }

            hasAppliedScale = false;
        }

        private void OnGUI()
        {
            if (controller == null || !hasCapturedOriginalScale || StatMaster.hudHidden || StatMaster.levelSimulating)
                return;

            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "Build Zone Expander");
        }

        private void DrawWindow(int id)
        {
            bool nextEnabled = GUILayout.Toggle(enabledForLevel, "Enlarge campaign build box");
            if (nextEnabled != enabledForLevel)
            {
                if (!nextEnabled)
                    RestoreOriginalScale();
                enabledForLevel = nextEnabled;
                PlayerPrefs.SetInt(EnabledKey, enabledForLevel ? 1 : 0);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Multiplier", GUILayout.Width(75f));
            factorText = GUILayout.TextField(factorText, GUILayout.Width(55f));
            if (GUILayout.Button("Apply", GUILayout.Width(55f)))
                ApplyTypedFactor();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawPreset(1.5f);
            DrawPreset(2f);
            DrawPreset(3f);
            DrawPreset(5f);
            GUILayout.EndHorizontal();

            GUILayout.Label(StatMaster.isMP
                ? "Disabled in multiplayer."
                : "Bounds stay enabled; campaign completion is not marked as a cheat.");

            GUI.DragWindow();
        }

        private void DrawPreset(float value)
        {
            if (GUILayout.Button(value.ToString("0.#") + "x"))
                SetFactor(value);
        }

        private void ApplyTypedFactor()
        {
            float value;
            if (float.TryParse(factorText, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                SetFactor(value);
            }
            else if (float.TryParse(factorText, out value))
            {
                SetFactor(value);
            }
            else
            {
                factorText = factor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private void SetFactor(float value)
        {
            factor = Mathf.Clamp(value, 1f, 10f);
            factorText = factor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            PlayerPrefs.SetFloat(FactorKey, factor);
        }

        private void OnDestroy()
        {
            RestoreOriginalScale();
        }
    }
}
