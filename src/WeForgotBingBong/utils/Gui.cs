using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

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

        private Canvas canvas = null!;
        private TextMeshProUGUI statusTextTMP = null!;
        private TextMeshProUGUI timerTextTMP = null!;
        private TextMeshProUGUI distanceTextTMP = null!;
        private static TMP_FontAsset? gameFont;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);

                CreateUI();  
                StartCoroutine(TryFindFontUntilSuccess());  
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        private IEnumerator TryFindFontUntilSuccess()
        {
            while (true)
            {
                var newFont = CacheGameFont();
                if (newFont != null)
                {
                    if (newFont != gameFont)
                    {
                        gameFont = newFont;
                        ApplyFontToAllTexts();
                        Debug.Log($"[WeForgotBingBong] Apply game font: {gameFont.name}");
                    }
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }
        }

        private TMP_FontAsset? CacheGameFont()
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts.Length == 0)
            {
                return null;
            }

            var candidate = fonts.FirstOrDefault(f =>
                f.name.Contains("DarumaDropOne-Regular", System.StringComparison.OrdinalIgnoreCase));

            return candidate;
        }

        private void ApplyFontToAllTexts()
        {
            if (gameFont == null) return;
            foreach (var tmp in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                tmp.font = gameFont;
                tmp.fontMaterial = gameFont.material;
            }
        }

        private void CreateUI()
        {
            GameObject canvasObj = new GameObject("UIManagerCanvas");
            canvasObj.transform.SetParent(this.transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            statusTextTMP = CreateTMPText("StatusText", new Vector2(0, 20), 22, TextAlignmentOptions.Center);
            timerTextTMP = CreateTMPText("TimerText", new Vector2(0, 50), 28, TextAlignmentOptions.Center);
            distanceTextTMP = CreateTMPText("DistanceText", new Vector2(0, 70), 18, TextAlignmentOptions.Center);
        }

        private TextMeshProUGUI CreateTMPText(string name, Vector2 anchoredPos, int fontSize, TextAlignmentOptions align)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(canvas.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;

            if (gameFont != null)
            {
                tmp.font = gameFont;
                tmp.fontMaterial = gameFont.material;
            }

            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(800, 50);

            return tmp;
        }

        public void SetBingBongStatus(bool held, float dist)
        {
            isHeld = held;
            distance = Mathf.Max(0f, dist);
            hasValidData = true;
            UpdateUI();
        }

        public void SetCurseInfo(string curseType, float timer)
        {
            currentCurseType = curseType;
            curseTimer = timer;
            UpdateUI();
        }

        public void SetInvincibilityInfo(bool active, float remainingTime)
        {
            isInvincible = active;
            invincibleTimer = remainingTime;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (!hasValidData) return;

            if (isHeld)
                statusTextTMP.text = "<color=#5CB338>BingBong is being carried</color>";
            else
                statusTextTMP.text = "<color=#FB4141>BingBong has been forgotten</color>";

            if (isInvincible)
            {
                if (invincibleTimer > 3f)
                    timerTextTMP.color = Color.gray;
                else if (invincibleTimer > 1f)
                    timerTextTMP.color = new Color(0.93f, 0.91f, 0.32f);
                else
                    timerTextTMP.color = new Color(0.98f, 0.26f, 0.25f);

                timerTextTMP.text = $"Invincible {invincibleTimer:F1}s";
                distanceTextTMP.text = (distance > 0f) ? $"({distance:F1}m Away from BingBong)" : "";
            }
            else if (!isHeld)
            {
                float remaining = Mathf.Max(0f, curseTimer);

                if (remaining > 3f)
                    timerTextTMP.color = Color.gray;
                else if (remaining > 1f)
                    timerTextTMP.color = new Color(0.93f, 0.91f, 0.32f);
                else
                    timerTextTMP.color = new Color(0.98f, 0.26f, 0.25f);

                timerTextTMP.text = $"Curse in {remaining:F1}s";
                distanceTextTMP.text = (distance > 0f) ? $"({distance:F1}m Away from BingBong)" : "";
            }
            else
            {
                timerTextTMP.text = "";
                distanceTextTMP.text = "";
            }
        }
    }
}
