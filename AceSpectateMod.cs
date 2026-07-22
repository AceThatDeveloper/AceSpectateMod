using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using GorillaLocomotion;
using UnityEngine.InputSystem;

namespace AceSpectateMod
{
    [BepInPlugin("com.ace.spectatingmod", "Ace Spectating Mod", "4.0.0")]
    public class SpectatingMod : BaseUnityPlugin
    {
        public static SpectatingMod Instance;
        public static ManualLogSource Log;

        private float camDistance = 4f;
        private float camHeight = 1.6f;
        private float followSmooth = 8f;
        private float rotateSmooth = 8f;
        private bool firstPersonMode = false;

        private bool isSpectating = false;
        private bool isShowing = false;

        private Vector3 currentCamPos;
        private Quaternion currentCamRot;
        private bool smoothedValuesInit = false;

        private List<GameObject> playerList = new List<GameObject>();
        private GameObject currentTarget;
        private float playerListTimer = 0f;
        private float playerListRefreshInterval = 0.5f;
        private bool needsRefresh = true;

        private List<string> favoritePlayers = new List<string>();
        private string searchFilter = "";

        private bool inputsLocked = false;
        private bool hasSavedOriginalTransform = false;
        private Vector3 originalGorillaPosition;
        private Quaternion originalGorillaRotation;
        private bool wasGorillaLocomotionEnabled = true;

        private int activeTab = 0;
        private string[] tabNames = { "Players", "Camera", "Effects", "Info" };
        private Vector2 scrollPosition = Vector2.zero;
        private Rect windowRect = new Rect(Screen.width - 380, 20, 380, 560);
        private int windowID;
        private bool isDragging = false;
        private Vector2 dragOffset;

        private Texture2D backgroundTexture;
        private Texture2D headerTexture;
        private Texture2D tabTexture;
        private Texture2D tabSelectedTexture;
        private Texture2D buttonTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D greenTexture;
        private Texture2D redTexture;
        private Texture2D goldTexture;
        private Texture2D panelTexture;

        private GameObject localPlayer;
        private List<Renderer> localRigRenderers = new List<Renderer>();
        private bool localRigHidden = false;

        private string notificationText = "";
        private float notificationTimer = 0f;
        private float notificationAlpha = 0f;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                GameObject.DontDestroyOnLoad(gameObject);
                Log = Logger;
                windowID = GetInstanceID();

                CreateTextures();

                Invoke(nameof(UpdatePlayerList), 1f);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        Texture2D MakeSolid(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        Texture2D MakeGradient(int height, Color top, Color bottom)
        {
            Texture2D t = new Texture2D(1, height);
            for (int y = 0; y < height; y++)
            {
                float lerp = (float)y / (height - 1);
                t.SetPixel(0, y, Color.Lerp(bottom, top, lerp));
            }
            t.Apply();
            return t;
        }

        void CreateTextures()
        {
            backgroundTexture = MakeGradient(256, new Color(0.06f, 0.06f, 0.11f, 0.97f), new Color(0.02f, 0.02f, 0.05f, 0.97f));
            headerTexture = MakeGradient(64, new Color(0.25f, 0.45f, 0.95f, 1f), new Color(0.1f, 0.2f, 0.55f, 1f));
            tabTexture = MakeSolid(new Color(0.13f, 0.13f, 0.2f, 0.85f));
            tabSelectedTexture = MakeGradient(32, new Color(0.3f, 0.55f, 1f, 1f), new Color(0.15f, 0.3f, 0.75f, 1f));
            buttonTexture = MakeGradient(32, new Color(0.22f, 0.42f, 0.85f, 1f), new Color(0.12f, 0.24f, 0.55f, 1f));
            buttonHoverTexture = MakeGradient(32, new Color(0.32f, 0.55f, 1f, 1f), new Color(0.18f, 0.35f, 0.7f, 1f));
            greenTexture = MakeGradient(32, new Color(0.25f, 0.8f, 0.35f, 1f), new Color(0.1f, 0.45f, 0.2f, 1f));
            redTexture = MakeGradient(32, new Color(0.9f, 0.25f, 0.25f, 1f), new Color(0.5f, 0.1f, 0.1f, 1f));
            goldTexture = MakeGradient(32, new Color(1f, 0.85f, 0.3f, 1f), new Color(0.7f, 0.5f, 0.1f, 1f));
            panelTexture = MakeSolid(new Color(0.1f, 0.1f, 0.16f, 0.6f));
        }

        void Update()
        {
            var kb = Keyboard.current;

            if (kb != null && kb.jKey.wasPressedThisFrame)
            {
                isShowing = !isShowing;
                if (isShowing)
                {
                    needsRefresh = true;
                    UpdatePlayerList();
                }
            }

            if (kb != null && kb.escapeKey.wasPressedThisFrame && isSpectating)
            {
                StopSpectating();
                isShowing = false;
            }

            if (isSpectating && kb != null)
            {
                if (kb.rightBracketKey.wasPressedThisFrame)
                {
                    CycleTarget(1);
                }
                if (kb.leftBracketKey.wasPressedThisFrame)
                {
                    CycleTarget(-1);
                }
                if (kb.fKey.wasPressedThisFrame)
                {
                    firstPersonMode = !firstPersonMode;
                    smoothedValuesInit = false;
                    ShowNotification(firstPersonMode ? "First Person Enabled" : "Third Person Enabled");
                }
            }

            if (isSpectating && !inputsLocked)
            {
                LockInputs();
            }
            else if (!isSpectating && inputsLocked)
            {
                UnlockInputs();
            }

            if (needsRefresh || playerListTimer <= 0f)
            {
                UpdatePlayerList();
                needsRefresh = false;
                playerListTimer = playerListRefreshInterval;
            }
            else
            {
                playerListTimer -= Time.deltaTime;
            }

            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                notificationAlpha = Mathf.Clamp01(notificationTimer / 0.5f);
            }

            if (isSpectating && currentTarget != null)
            {
                UpdateSpectatingPosition();
            }
        }

        void ShowNotification(string text)
        {
            notificationText = text;
            notificationTimer = 2.5f;
            notificationAlpha = 1f;
        }

        void CycleTarget(int direction)
        {
            if (playerList.Count == 0) return;

            int index = playerList.IndexOf(currentTarget);
            index = (index + direction + playerList.Count) % playerList.Count;
            GameObject next = playerList[index];
            if (next == null) return;

            currentTarget = next;
            smoothedValuesInit = false;
            ShowNotification($"Now Spectating {GetPlayerName(next)}");
        }

        void LockInputs()
        {
            try
            {
                if (GTPlayer.Instance != null)
                {
                    if (!hasSavedOriginalTransform)
                    {
                        originalGorillaPosition = GTPlayer.Instance.transform.position;
                        originalGorillaRotation = GTPlayer.Instance.transform.rotation;
                        hasSavedOriginalTransform = true;
                    }

                    wasGorillaLocomotionEnabled = GTPlayer.Instance.enabled;
                    GTPlayer.Instance.enabled = false;

                    Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }

                inputsLocked = true;
            }
            catch (Exception e)
            {
                Log.LogError($"Error locking inputs: {e.Message}");
            }
        }

        void UnlockInputs()
        {
            try
            {
                if (GTPlayer.Instance != null)
                {
                    GTPlayer.Instance.transform.position = originalGorillaPosition;
                    GTPlayer.Instance.transform.rotation = originalGorillaRotation;

                    GTPlayer.Instance.enabled = wasGorillaLocomotionEnabled;

                    Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                    }
                }

                inputsLocked = false;
                hasSavedOriginalTransform = false;
            }
            catch (Exception e)
            {
                Log.LogError($"Error unlocking inputs: {e.Message}");
            }
        }

        void OnGUI()
        {
            DrawNotification();

            if (!isShowing) return;

            windowRect = GUI.Window(windowID, windowRect, DrawSpectatingWindow, "", GetWindowStyle());

            if (Event.current.type == EventType.MouseDown && windowRect.Contains(Event.current.mousePosition))
            {
                isDragging = true;
                dragOffset = Event.current.mousePosition - new Vector2(windowRect.x, windowRect.y);
            }
            if (Event.current.type == EventType.MouseUp)
            {
                isDragging = false;
            }
            if (isDragging && Event.current.type == EventType.MouseDrag)
            {
                Vector2 newPos = Event.current.mousePosition - dragOffset;
                newPos.x = Mathf.Clamp(newPos.x, 0, Screen.width - windowRect.width);
                newPos.y = Mathf.Clamp(newPos.y, 0, Screen.height - windowRect.height);
                windowRect.position = newPos;
            }
        }

        void DrawNotification()
        {
            if (notificationTimer <= 0f) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(1f, 1f, 1f, notificationAlpha);

            float w = 320f;
            float h = 40f;
            Rect rect = new Rect((Screen.width - w) / 2f, 40f, w, h);

            GUI.color = new Color(1f, 1f, 1f, notificationAlpha * 0.9f);
            GUI.DrawTexture(rect, goldTexture);
            GUI.color = Color.white;
            GUI.Label(rect, notificationText, style);
        }

        GUIStyle GetWindowStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = backgroundTexture;
            style.border = new RectOffset(12, 12, 12, 12);
            return style;
        }

        void DrawSpectatingWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal(GUILayout.Height(38));
                {
                    GUI.DrawTexture(new Rect(0, 0, windowRect.width, 38), headerTexture);
                    GUILayout.Space(10);
                    GUILayout.Label("ACE SPECTATING MOD", GetHeaderStyle());
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("X", GetCloseButtonStyle(), GUILayout.Width(26), GUILayout.Height(26)))
                    {
                        isShowing = false;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);

                GUILayout.BeginHorizontal(GUI.skin.box);
                {
                    string statusText = isSpectating ? "SPECTATING" : "IDLE";
                    Color statusColor = isSpectating ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
                    GUILayout.Label($"● {statusText}", GetStatusStyle(statusColor));
                    GUILayout.FlexibleSpace();
                    if (isSpectating && currentTarget != null)
                    {
                        GUILayout.Label($"{(firstPersonMode ? "FPV" : "TPV")}  {GetPlayerName(currentTarget)}", GetInfoStyle());
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                {
                    for (int i = 0; i < tabNames.Length; i++)
                    {
                        GUIStyle style = i == activeTab ? GetTabSelectedStyle() : GetTabStyle();
                        if (GUILayout.Button(tabNames[i], style, GUILayout.Height(30)))
                        {
                            activeTab = i;
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);

                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(390));
                {
                    switch (activeTab)
                    {
                        case 0:
                            DrawPlayersTab();
                            break;
                        case 1:
                            DrawCameraTab();
                            break;
                        case 2:
                            DrawEffectsTab();
                            break;
                        case 3:
                            DrawInfoTab();
                            break;
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.Space(6);
                GUILayout.Label("J Toggle GUI   ESC Stop   [ ] Cycle Target   F Toggle View", GetSmallInfoStyle());
                if (isSpectating)
                {
                    GUILayout.Label("Movement Locked While Spectating", GetSmallInfoStyle());
                }
            }
            GUILayout.EndVertical();
        }

        void DrawPlayersTab()
        {
            GUILayout.Label("PLAYER SELECTION", GetTabHeaderStyle());
            GUILayout.Space(5);

            if (isSpectating)
            {
                if (GUILayout.Button("STOP SPECTATING", GetBigButtonStyle(redTexture)))
                {
                    StopSpectating();
                }
                GUILayout.Space(6f);
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Search", GetInfoStyle(), GUILayout.Width(50));
                searchFilter = GUILayout.TextField(searchFilter, GUILayout.Height(24));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginVertical(GUI.skin.box);
            {
                var filtered = playerList.Where(p => p != null && GetPlayerName(p).ToLower().Contains(searchFilter.ToLower())).ToList();

                GUILayout.Label($"Players In Lobby  {playerList.Count}", GetInfoStyle());
                GUILayout.Space(5);

                if (filtered.Count == 0)
                {
                    GUILayout.Label("No Players Found", GetInfoStyle());
                    if (GUILayout.Button("Refresh Player List", GetButtonStyle()))
                    {
                        UpdatePlayerList();
                    }
                }
                else
                {
                    foreach (GameObject player in filtered)
                    {
                        string playerName = GetPlayerName(player);
                        bool isTarget = player == currentTarget;
                        bool isFav = favoritePlayers.Contains(playerName);

                        GUILayout.BeginHorizontal();
                        {
                            string star = isFav ? "★ " : "☆ ";
                            if (GUILayout.Button(star, GetStarStyle(isFav), GUILayout.Width(28)))
                            {
                                if (isFav) favoritePlayers.Remove(playerName);
                                else favoritePlayers.Add(playerName);
                            }

                            GUIStyle nameStyle = isTarget ? GetHighlightStyle() : (isFav ? GetGoldStyle() : GetInfoStyle());
                            string prefix = isTarget ? "▶ " : "  ";
                            GUILayout.Label(prefix + playerName, nameStyle);

                            GUILayout.FlexibleSpace();

                            if (isTarget)
                            {
                                GUILayout.Label("ACTIVE", GetActiveStyle());
                            }
                            else
                            {
                                if (GUILayout.Button("Spectate", GetSpectateButtonStyle(), GUILayout.Width(85)))
                                {
                                    StartSpectating(player);
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
        }

        void DrawCameraTab()
        {
            GUILayout.Label("CAMERA SETTINGS", GetTabHeaderStyle());
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("View Mode", GetInfoStyle());
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(firstPersonMode ? "First Person" : "Third Person", GetButtonStyle(), GUILayout.Width(140)))
                {
                    firstPersonMode = !firstPersonMode;
                    smoothedValuesInit = false;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.Label($"Distance  {camDistance:F1}m", GetInfoStyle());
            camDistance = GUILayout.HorizontalSlider(camDistance, 1.5f, 12f);

            GUILayout.Space(4);

            GUILayout.Label($"Height  {camHeight:F1}m", GetInfoStyle());
            camHeight = GUILayout.HorizontalSlider(camHeight, 0f, 4f);

            GUILayout.Space(4);

            GUILayout.Label($"Follow Smooth  {followSmooth:F1}", GetInfoStyle());
            followSmooth = GUILayout.HorizontalSlider(followSmooth, 1f, 20f);

            GUILayout.Space(4);

            GUILayout.Label($"Rotate Smooth  {rotateSmooth:F1}", GetInfoStyle());
            rotateSmooth = GUILayout.HorizontalSlider(rotateSmooth, 1f, 20f);

            GUILayout.Space(10);

            if (GUILayout.Button("Reset To Defaults", GetButtonStyle()))
            {
                camDistance = 4f;
                camHeight = 1.6f;
                followSmooth = 8f;
                rotateSmooth = 8f;
            }
        }

        void DrawEffectsTab()
        {
            GUILayout.Label("EFFECTS", GetTabHeaderStyle());
            GUILayout.Space(8);

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label($"Local Rig Hidden  {(localRigHidden ? "Yes" : "No")}", GetInfoStyle());
                GUILayout.Space(4);
                if (GUILayout.Button(localRigHidden ? "Show Local Rig" : "Hide Local Rig", GetButtonStyle()))
                {
                    if (localRigHidden) ShowLocalRig();
                    else HideLocalRig();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Favorite Players", GetInfoStyle());
                GUILayout.Space(4);
                if (favoritePlayers.Count == 0)
                {
                    GUILayout.Label("No Favorites Yet", GetSmallInfoStyle());
                }
                else
                {
                    foreach (string fav in favoritePlayers.ToList())
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(fav, GetGoldStyle());
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Remove", GetSmallButtonStyle(), GUILayout.Width(70)))
                            {
                                favoritePlayers.Remove(fav);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
        }

        void DrawInfoTab()
        {
            GUILayout.Label("INFO", GetTabHeaderStyle());
            GUILayout.Space(8);

            GUILayout.Label($"Status  {(isSpectating ? "Active" : "Inactive")}", GetInfoStyle());
            GUILayout.Label($"Target  {(currentTarget != null ? GetPlayerName(currentTarget) : "None")}", GetInfoStyle());
            GUILayout.Label($"View Mode  {(firstPersonMode ? "First Person" : "Third Person")}", GetInfoStyle());
            GUILayout.Label($"Players In Lobby  {playerList.Count}", GetInfoStyle());
            GUILayout.Label($"Distance  {camDistance:F1}m", GetInfoStyle());
            GUILayout.Label($"Height  {camHeight:F1}m", GetInfoStyle());
            GUILayout.Label($"Inputs Locked  {(inputsLocked ? "Yes" : "No")}", GetInfoStyle());

            GUILayout.Space(15);

            GUILayout.Label("Controls", GetTabHeaderStyle());
            GUILayout.Space(3);

            GUILayout.Label("J  Toggle GUI", GetInfoStyle());
            GUILayout.Label("ESC  Stop Spectating", GetInfoStyle());
            GUILayout.Label("[ ]  Cycle Between Players", GetInfoStyle());
            GUILayout.Label("F  Toggle First / Third Person", GetInfoStyle());
            GUILayout.Label("Click Spectate On Any Player", GetInfoStyle());

            GUILayout.Space(15);
            GUILayout.Label("v4.0.0", GetSmallInfoStyle());
        }

        GUIStyle GetHeaderStyle()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 17;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.padding = new RectOffset(5, 0, 5, 5);
            return style;
        }

        GUIStyle GetCloseButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.background = redTexture;
            return style;
        }

        GUIStyle GetStatusStyle(Color color)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = 13;
            style.fontStyle = FontStyle.Bold;
            style.padding = new RectOffset(5, 5, 2, 2);
            return style;
        }

        GUIStyle GetInfoStyle()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            style.fontSize = 12;
            style.padding = new RectOffset(5, 5, 2, 2);
            return style;
        }

        GUIStyle GetSmallInfoStyle()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0.6f, 0.6f, 0.7f, 1f);
            style.fontSize = 10;
            style.padding = new RectOffset(5, 5, 2, 2);
            return style;
        }

        GUIStyle GetHighlightStyle()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0.3f, 0.85f, 1f, 1f);
            style.fontSize = 13;
            style.fontStyle = FontStyle.Bold;
            style.padding = new RectOffset(5, 5, 2, 2);
            return style;
        }

        GUIStyle GetGoldStyle()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(1f, 0.85f, 0.3f, 1f);
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.padding = new RectOffset(5, 5, 2, 2);
            return style;
        }

        GUIStyle GetStarStyle(bool active)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = active ? new Color(1f, 0.85f, 0.3f) : Color.gray;
            style.fontSize = 14;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.background = null;
            return style;
        }

        GUIStyle GetActiveStyle()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0.3f, 1f, 0.4f, 1f);
            style.fontSize = 11;
            style.fontStyle = FontStyle.Bold;
            style.padding = new RectOffset(5, 5, 2, 2);
            return style;
        }

        GUIStyle GetButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(8, 8, 5, 5);
            style.margin = new RectOffset(2, 2, 2, 2);
            style.normal.background = buttonTexture;
            style.hover.background = buttonHoverTexture;
            style.hover.textColor = Color.white;
            return style;
        }

        GUIStyle GetBigButtonStyle(Texture2D tex)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(10, 10, 8, 8);
            style.margin = new RectOffset(2, 2, 2, 2);
            style.normal.background = tex;
            return style;
        }

        GUIStyle GetSmallButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = Color.white;
            style.fontSize = 10;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(5, 5, 3, 3);
            style.margin = new RectOffset(2, 2, 2, 2);
            style.normal.background = buttonTexture;
            style.hover.background = buttonHoverTexture;
            return style;
        }

        GUIStyle GetSpectateButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = Color.white;
            style.fontSize = 11;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(6, 6, 4, 4);
            style.margin = new RectOffset(2, 2, 2, 2);
            style.normal.background = greenTexture;
            return style;
        }

        GUIStyle GetTabStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = new Color(0.7f, 0.7f, 0.8f, 1f);
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(8, 8, 4, 4);
            style.margin = new RectOffset(1, 1, 0, 0);
            style.normal.background = tabTexture;
            return style;
        }

        GUIStyle GetTabSelectedStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(8, 8, 4, 4);
            style.margin = new RectOffset(1, 1, 0, 0);
            style.normal.background = tabSelectedTexture;
            return style;
        }

        GUIStyle GetTabHeaderStyle()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0.6f, 0.8f, 1f, 1f);
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.padding = new RectOffset(5, 5, 5, 5);
            return style;
        }

        void StartSpectating(GameObject target)
        {
            if (target == null) return;

            if (isSpectating)
            {
                currentTarget = target;
                smoothedValuesInit = false;
                ShowNotification($"Now Spectating {GetPlayerName(target)}");
                return;
            }

            currentTarget = target;
            isSpectating = true;
            smoothedValuesInit = false;

            HideLocalRig();
            LockInputs();

            ShowNotification($"Now Spectating {GetPlayerName(target)}");
        }

        void StopSpectating()
        {
            isSpectating = false;
            currentTarget = null;
            smoothedValuesInit = false;

            UnlockInputs();
            ShowLocalRig();
        }

        void HideLocalRig()
        {
            if (localRigHidden) return;

            if (localPlayer == null)
            {
                VRRig[] rigs = FindObjectsOfType<VRRig>();
                foreach (VRRig rig in rigs)
                {
                    if (rig != null && rig.isOfflineVRRig)
                    {
                        localPlayer = rig.gameObject;
                        break;
                    }
                }
            }

            if (localPlayer != null)
            {
                localRigRenderers.Clear();
                localRigRenderers.AddRange(localPlayer.GetComponentsInChildren<Renderer>(true));
                foreach (Renderer renderer in localRigRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = false;
                }
                localRigHidden = true;
            }
        }

        void ShowLocalRig()
        {
            if (!localRigHidden) return;

            foreach (Renderer renderer in localRigRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
            localRigRenderers.Clear();
            localRigHidden = false;
        }

        void UpdatePlayerList()
        {
            playerList.Clear();

            try
            {
                VRRig[] rigs = FindObjectsOfType<VRRig>();
                if (rigs != null && rigs.Length > 0)
                {
                    foreach (VRRig rig in rigs)
                    {
                        if (rig == null || rig.gameObject == null) continue;
                        if (rig.isOfflineVRRig) continue;

                        PhotonView view = rig.GetComponent<PhotonView>();
                        if (view != null)
                        {
                            if (!view.IsMine)
                            {
                                if (!playerList.Contains(rig.gameObject))
                                {
                                    playerList.Add(rig.gameObject);
                                }
                            }
                        }
                        else
                        {
                            if (!playerList.Contains(rig.gameObject))
                            {
                                playerList.Add(rig.gameObject);
                            }
                        }
                    }
                }

                GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
                if (taggedPlayers != null && taggedPlayers.Length > 0)
                {
                    foreach (GameObject player in taggedPlayers)
                    {
                        if (player == null) continue;
                        if (playerList.Contains(player)) continue;

                        VRRig rig = player.GetComponent<VRRig>();
                        if (rig != null && rig.isOfflineVRRig) continue;

                        PhotonView view = player.GetComponent<PhotonView>();
                        if (view != null && view.IsMine) continue;

                        playerList.Add(player);
                    }
                }

                playerList = playerList.OrderBy(p => GetPlayerName(p)).ToList();

                if (currentTarget != null && !playerList.Contains(currentTarget))
                {
                    StopSpectating();
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error updating player list: {e.Message}");
                Log.LogError(e.StackTrace);
            }
        }

        string GetPlayerName(GameObject player)
        {
            if (player == null) return "Unknown";

            try
            {
                VRRig rig = player.GetComponent<VRRig>();
                if (rig != null)
                {
                    if (!string.IsNullOrWhiteSpace(rig.playerNameVisible))
                    {
                        return rig.playerNameVisible;
                    }

                    if (rig.OwningNetPlayer != null)
                    {
                        string nick = rig.OwningNetPlayer.NickName;
                        if (!string.IsNullOrEmpty(nick))
                            return nick;
                    }
                }

                PhotonView view = player.GetComponent<PhotonView>();
                if (view != null && view.Owner != null)
                {
                    string nick = view.Owner.NickName;
                    if (!string.IsNullOrEmpty(nick))
                        return nick;
                }

                if (PhotonNetwork.CurrentRoom != null)
                {
                    foreach (Player p in PhotonNetwork.CurrentRoom.Players.Values)
                    {
                        if (p != null && !string.IsNullOrEmpty(p.NickName))
                        {
                            if (view != null && view.Owner != null && view.Owner.ActorNumber == p.ActorNumber)
                            {
                                return p.NickName;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error getting player name: {e.Message}");
            }

            return player.name.Replace("VRRig", "").Replace("(Clone)", "").Trim();
        }

        Transform FindHeadTransform(GameObject target)
        {
            if (target == null) return null;

            try
            {
                Transform[] children = target.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in children)
                {
                    if (t != null && t.name.ToLower().Contains("head"))
                    {
                        return t;
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error finding head transform: {e.Message}");
            }

            return null;
        }

        void UpdateSpectatingPosition()
        {
            if (currentTarget == null || GTPlayer.Instance == null)
            {
                StopSpectating();
                return;
            }

            try
            {
                Transform targetTransform = currentTarget.transform;
                Vector3 desiredPos;
                Quaternion desiredRot;

                if (firstPersonMode)
                {
                    Transform head = FindHeadTransform(currentTarget);
                    desiredPos = head != null ? head.position : targetTransform.position + Vector3.up * camHeight;
                    desiredRot = head != null ? head.rotation : targetTransform.rotation;
                }
                else
                {
                    Vector3 forward = targetTransform.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude < 0.0001f)
                    {
                        forward = Vector3.forward;
                    }
                    forward.Normalize();

                    Vector3 pivot = targetTransform.position + Vector3.up * camHeight;
                    desiredPos = pivot - forward * camDistance + Vector3.up * (camDistance * 0.15f);
                    desiredRot = Quaternion.LookRotation((pivot - desiredPos).normalized, Vector3.up);
                }

                if (!smoothedValuesInit)
                {
                    currentCamPos = desiredPos;
                    currentCamRot = desiredRot;
                    smoothedValuesInit = true;
                }

                float posT = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
                float rotT = 1f - Mathf.Exp(-rotateSmooth * Time.deltaTime);

                currentCamPos = Vector3.Lerp(currentCamPos, desiredPos, posT);
                currentCamRot = Quaternion.Slerp(currentCamRot, desiredRot, rotT);

                GTPlayer.Instance.transform.position = currentCamPos;
                GTPlayer.Instance.transform.rotation = currentCamRot;
            }
            catch (Exception e)
            {
                Log.LogError($"Error updating spectate position: {e.Message}");
                StopSpectating();
            }
        }

        void OnDestroy()
        {
            if (isSpectating)
            {
                StopSpectating();
            }
            UnlockInputs();
            ShowLocalRig();
        }
    }
}
