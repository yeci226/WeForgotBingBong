using ConfigSpace;
using UnityEngine;

namespace Gui
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager instance = null!;
        private bool isHeld;
        private float distance;
        private bool hasValidData = false;
        private string currentCurseType = "None";
        private float curseTimer = 0f;
        private bool isInvincible = false;
        private float invincibleTimer = 0f;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        public void SetBingBongStatus(bool held, float dist)
        {
            isHeld = held;
            distance = (dist < 0f) ? 0f : dist;
            hasValidData = true;
        }

        public void SetCurseInfo(string curseType, float timer)
        {
            currentCurseType = curseType;
            curseTimer = timer;
        }

        public void SetInvincibilityInfo(bool active, float remainingTime)
        {
            isInvincible = active;
            invincibleTimer = remainingTime;
        }

        void OnGUI()
        {
            if (instance == null || !hasValidData) return;

            GUIStyle baseStyle = new GUIStyle(GUI.skin.label);
            baseStyle.fontStyle = FontStyle.Bold;
            baseStyle.alignment = TextAnchor.MiddleCenter;

            GUI.backgroundColor = Color.clear;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float bottomY = screenHeight - 50;
            float elementHeight = 30;
            float spacing = 10;

            string statusText;
            Color statusColor;
            if (isHeld)
            {
                statusText = "BingBong is being carried";
                statusColor = new Color(0.36f, 0.70f, 0.22f); // #5CB338
            }
            else
            {
                statusText = "BingBong has been forgotten";
                statusColor = new Color(0.98f, 0.26f, 0.25f); // #FB4141
            }

            GUIStyle statusStyle = new GUIStyle(baseStyle);
            statusStyle.fontSize = 22;
            statusStyle.normal.textColor = statusColor;
            GUI.Label(new Rect(0, bottomY, screenWidth, elementHeight), statusText, statusStyle);

            float timeY = bottomY - elementHeight - spacing;
            float distanceY = timeY - elementHeight - spacing;

            GUIStyle distanceStyle = new GUIStyle(baseStyle);
            distanceStyle.fontSize = 18;
            distanceStyle.normal.textColor = new Color(165f/255f, 165f/255f, 165f/255f, 0.8f); //rgba(165, 165, 165, 0.5)

            GUIStyle timeStyle = new GUIStyle(baseStyle);
            timeStyle.fontSize = 28;

            if (isInvincible)
            {
                timeStyle.normal.textColor = new Color(128f/255f, 128f/255f, 128f/255f);
                if (invincibleTimer > 3f)
                    timeStyle.normal.textColor = new Color(128f/255f, 128f/255f, 128f/255f);
                else if (invincibleTimer > 1f)
                    timeStyle.normal.textColor = new Color(0.93f, 0.91f, 0.32f); 
                else
                    timeStyle.normal.textColor = new Color(0.98f, 0.26f, 0.25f);
                GUI.Label(new Rect(0, timeY, screenWidth, elementHeight),
                    $"Invincible {invincibleTimer:F1}s", timeStyle);

                if (distance > 0f)
                {
                    GUI.Label(new Rect(0, distanceY, screenWidth, elementHeight),
                        $"({distance:F1}m Away from BingBong)", distanceStyle);
                }
            }
            else if (!isHeld)
            {
                float remaining = Mathf.Max(0f, curseTimer);
                if (remaining > 3f)
                    timeStyle.normal.textColor = new Color(128f/255f, 128f/255f, 128f/255f);
                else if (remaining > 1f)
                    timeStyle.normal.textColor = new Color(0.93f, 0.91f, 0.32f); 
                else
                    timeStyle.normal.textColor = new Color(0.98f, 0.26f, 0.25f);

                GUI.Label(new Rect(0, timeY, screenWidth, elementHeight),
                    $"Curse in {remaining:F1}s", timeStyle);

                if (distance > 0f)
                {
                    GUI.Label(new Rect(0, distanceY, screenWidth, elementHeight),
                        $"({distance:F1}m Away from BingBong)", distanceStyle);
                }
            }
        }
    }
}