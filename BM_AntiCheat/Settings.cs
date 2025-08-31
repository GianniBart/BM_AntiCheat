using UnityEngine;
using System.Collections.Generic;
using static BM_AntiCheat.Translator;
using BM_AntiCheat;

namespace BM_AntiCheat
{
    public class Settings : MonoBehaviour
    {

        // ToggleInfo modificato per usare label dinamica
        public class ToggleInfo
        {
            public System.Func<string> getLabel;
            public System.Func<bool> getState;
            public System.Action<bool> setState;

            public ToggleInfo(System.Func<string> getLabel, System.Func<bool> getState, System.Action<bool> setState)
            {
                this.getLabel = getLabel;
                this.getState = getState;
                this.setState = setState;
            }
        }

        public List<ToggleInfo> toggles = new List<ToggleInfo>();
        private bool isGUIActive = false;
        private Rect windowRect = new Rect(10, 10, 300, 400);
        private GUIStyle toggleStyle;

        private void Start()
        {
            toggles.Add(new ToggleInfo(() => GetAuto("kickCheater"), () => main.kickCheater, x => {main.kickCheater = x;
                if (x) main.banCheater = false;
            }));

            toggles.Add(new ToggleInfo(() => GetAuto("banCheater"), () => main.banCheater, x => {main.banCheater = x;
                    if (x) main.kickCheater = false;
            }));


            toggles.Add(new ToggleInfo(() => GetAuto("SentWarning"),() => main.SentWarning, x => main.SentWarning = x));



        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                ToggleGUI();
            }
        }

        private void OnGUI()
        {
            if (!isGUIActive) return;

            if (toggleStyle == null)
            {
                toggleStyle = new GUIStyle(GUI.skin.toggle);
                toggleStyle.fontSize = 18;
            }

            // Altezza finestra: toggles + spazio extra per header più alto
            windowRect.height = toggles.Count * 40 + 70;

            windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)WindowFunction, "");
        }

        private void WindowFunction(int windowID)
        {
            // Header alto con scritta neutra
            Rect headerRect = new Rect(0, 0, windowRect.width, 60);
            GUIStyle headerStyle = new GUIStyle(GUI.skin.box);
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.fontSize = 22;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(1f, 0.5f, 0f); // arancione
            GUI.Box(headerRect, "BM_ANTICHEAT", headerStyle);

            int y = 70;
            int width = 280;
            int height = 30;
            Rect toggleRect;

            // Stile testo toggle (sempre bianco e grassetto)
            GUIStyle toggleLabelStyle = new GUIStyle(GUI.skin.label);
            toggleLabelStyle.fontSize = 20;
            toggleLabelStyle.alignment = TextAnchor.MiddleLeft;
            toggleLabelStyle.fontStyle = FontStyle.Bold;
            toggleLabelStyle.normal.textColor = Color.white;

            // Stile testo ON (verde) e OFF (rosso)
            GUIStyle onStyle = new GUIStyle(GUI.skin.label);
            onStyle.fontSize = 18;
            onStyle.alignment = TextAnchor.MiddleRight;
            onStyle.fontStyle = FontStyle.Bold;
            onStyle.normal.textColor = Color.green;

            GUIStyle offStyle = new GUIStyle(GUI.skin.label);
            offStyle.fontSize = 18;
            offStyle.alignment = TextAnchor.MiddleRight;
            offStyle.fontStyle = FontStyle.Bold;
            offStyle.normal.textColor = Color.red;

            foreach (var toggle in toggles)
            {
                bool isOn = toggle.getState();

                toggleRect = new Rect(10, y, width, height);
                GUI.Box(toggleRect, GUIContent.none);  // Riquadro neutro

                // Rendi tutta la riga cliccabile per toggle
                if (GUI.Button(toggleRect, "", GUIStyle.none))
                {
                    toggle.setState(!isOn);
                }

                // Testo del toggle (sempre bianco e grassetto)
                Rect labelRect = new Rect(toggleRect.x + 5, y + 5, width - 60, height);
                GUI.Label(labelRect, toggle.getLabel(), toggleLabelStyle);

                // ON / OFF a destra, colore verde o rosso
                Rect onOffRect = new Rect(toggleRect.x + width - 50, y + 5, 45, height);
                GUI.Label(onOffRect, isOn ? "ON" : "OFF", isOn ? onStyle : offStyle);

                y += height + 10;
            }

            GUI.DragWindow();
        }

        public void ToggleGUI()
        {
            isGUIActive = !isGUIActive;

            if (isGUIActive)
            {
                windowRect.position = new Vector2(
                    (Screen.width - windowRect.width) / 2,
                    (Screen.height - windowRect.height) / 2
                );
            }
        }
        public static Settings Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

    }
}
