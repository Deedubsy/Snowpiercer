using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VampireUpgradeUI : MonoBehaviour
{
    public VampireUpgradeManager upgradeManager;
    public GameObject upgradeRowPrefab;
    public Transform upgradesParent;
    public Text bloodText;
    public Button continueButton;

    private List<GameObject> rows = new List<GameObject>();

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        foreach (var row in rows)
            Destroy(row);
        rows.Clear();

        if (upgradeManager == null || upgradeManager.vampireStats == null) return;
        float currentBlood = GameManager.instance != null ? GameManager.instance.GetCurrentBlood() : 0f;
        bloodText.text = $"Blood: {currentBlood:0}";

        foreach (var upgrade in upgradeManager.upgrades)
        {
            GameObject row = Instantiate(upgradeRowPrefab, upgradesParent);
            rows.Add(row);
            var texts = row.GetComponentsInChildren<Text>();
            if (texts.Length > 0) texts[0].text = upgrade.displayName;
            if (texts.Length > 1) texts[1].text = $"{upgrade.currentValue:0.##} (Lv {upgrade.currentLevel}/{upgrade.maxLevel})";
            if (texts.Length > 2) texts[2].text = $"Cost: {upgrade.GetCurrentCost()}";
            Button btn = row.GetComponentInChildren<Button>();
            if (btn != null)
            {
                float availableBlood = GameManager.instance != null ? GameManager.instance.GetCurrentBlood() : 0f;
                btn.interactable = upgrade.CanUpgrade() && availableBlood >= upgrade.GetCurrentCost();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    if (upgradeManager.TryUpgradeStat(upgrade.statType))
                        RefreshUI();
                });
            }
        }
    }

    public void OnContinue()
    {
        gameObject.SetActive(false);
    }
} 