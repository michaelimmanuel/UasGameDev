using UnityEngine;
using UnityEngine.UI;

public sealed class MenuButtonSetup : MonoBehaviour
{
    [ContextMenu("Add Hover Animators to All Buttons")]
    public void AddHoverAnimatorsToAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        int addedCount = 0;
        
        foreach (Button button in buttons)
        {
            if (button.GetComponent<ButtonHoverAnimator>() == null)
            {
                button.gameObject.AddComponent<ButtonHoverAnimator>();
                addedCount++;
            }
        }
        
        Debug.Log($"Added ButtonHoverAnimator to {addedCount} buttons in {gameObject.name}");
    }
    
    private void Awake()
    {
        AddHoverAnimatorsToAllButtons();
    }
}
