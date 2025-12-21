using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MenuResizer
{
    public static void ResizeAllButtons(Transform root = null)
    {
        Button[] buttons = root != null 
            ? root.GetComponentsInChildren<Button>(true) 
            : Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
        foreach (Button button in buttons)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.x < 350)
            {
                rect.sizeDelta = new Vector2(375, 75);
                
                TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null && text.fontSize != 45)
                {
                    text.fontSize = 45;
                }
            }
        }
    }
    
    public static void ResizeAllTexts(Transform root = null)
    {
        TextMeshProUGUI[] texts = root != null 
            ? root.GetComponentsInChildren<TextMeshProUGUI>(true) 
            : Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.GetComponentInParent<Button>() != null)
                continue;

            if (text.text.Contains("FULL THROTTLE") && text.fontSize < 85)
            {
                text.fontSize = 90;
            }
            else if (text.text.Contains("RACING GAME") && text.fontSize < 30)
            {
                text.fontSize = 35;
            }
            else if (text.text.Contains("SELECT TRACK") && text.fontSize < 75)
            {
                text.fontSize = 80;
            }
            else if (text.text.Contains("Choose your racing challenge") && text.fontSize < 25)
            {
                text.fontSize = 30;
            }
            else if (text.text.Contains("PAUSED") && text.fontSize < 55)
            {
                text.fontSize = 60;
            }
            else if (text.text.Contains("Press ESC to resume") && text.fontSize < 20)
            {
                text.fontSize = 22;
            }
            else if (text.text.Contains("OPTIONS") && text.fontSize < 65)
            {
                text.fontSize = 70;
            }
            else if (text.text.Contains("Settings coming soon") && text.fontSize < 25)
            {
                text.fontSize = 28;
            }
        }
    }
    
    public static void ResizePanel(string panelName, Vector2 newSize)
    {
        GameObject panel = GameObject.Find(panelName);
        if (panel != null)
        {
            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.x < newSize.x - 50)
            {
                rect.sizeDelta = newSize;
            }
        }
    }
    
    public static void ResizePanel(Transform parent, string panelName, Vector2 newSize)
    {
        Transform panel = parent.Find(panelName);
        if (panel != null)
        {
            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.x < newSize.x - 50)
            {
                rect.sizeDelta = newSize;
            }
        }
    }
    
    public static void DisableIntroAnimations()
    {
        MenuIntroAnimator[] animators = Object.FindObjectsByType<MenuIntroAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MenuIntroAnimator animator in animators)
        {
            animator.enabled = false;
        }
    }
    
    public static void EnsureHoverAnimators(Transform root = null)
    {
        Button[] buttons = root != null 
            ? root.GetComponentsInChildren<Button>(true) 
            : Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
        foreach (Button button in buttons)
        {
            if (button.GetComponent<ButtonHoverAnimator>() == null)
            {
                button.gameObject.AddComponent<ButtonHoverAnimator>();
            }
        }
    }
}
