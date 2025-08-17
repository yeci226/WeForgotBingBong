using UnityEngine;

namespace Giu
{
  public class UIManager : MonoBehaviour
  {
    public static UIManager instance = null!;
    private bool isHeld;
    private float distance;
    private bool hasValidData = false;
    private string currentCurseType = "None";
    private float curseTimer = 0f;
    private float curseInterval = 2f;

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
      distance = dist;
      hasValidData = true;
    }

    public void SetCurseInfo(string curseType, float timer, float interval)
    {
      currentCurseType = curseType;
      curseTimer = timer;
      curseInterval = interval;
    }

    void OnGUI()
    {
      if (instance == null || !hasValidData) return;

      GUI.skin.label.fontSize = 14;
      GUI.skin.label.fontStyle = FontStyle.Bold;

      GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
      GUI.Box(new Rect(15, 15, 350, 80), "");

      string statusText;
      Color statusColor;
      if (isHeld)
      {
        statusText = "✅ BingBong 正在被玩家攜帶";
        statusColor = Color.green;
      }
      else
      {
        statusText = "❌ BingBong 未被攜帶";
        statusColor = Color.red;
      }

      GUI.color = statusColor;
      GUI.Label(new Rect(20, 20, 340, 25), statusText);

      GUI.color = Color.white;
      GUI.Label(new Rect(20, 45, 340, 20), $"詛咒類型: {currentCurseType}");

      if (!isHeld)
      {
        float progress = curseTimer / curseInterval;
        GUI.color = Color.yellow;
        GUI.Label(new Rect(20, 65, 340, 20), $"下次詛咒: {curseTimer:F1}s / {curseInterval:F1}s");

        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        GUI.Box(new Rect(20, 85, 340 * progress, 8), "");
        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.Box(new Rect(20 + 340 * progress, 85, 340 * (1 - progress), 8), "");
      }

      GUI.color = Color.white;
      GUI.backgroundColor = Color.white;
    }
  }
}