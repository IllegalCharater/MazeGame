using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InventoryUIController : MonoBehaviour
{
    [SerializeField] private Text inventoryText;

    public void BindInventoryText(Text text)
    {
        inventoryText = text;
        Refresh();
    }

    private void OnEnable()
    {
        GameEvents.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        if (inventoryText == null || GameDatabase.Instance.GetPlayerData().inventory == null)
            return;

        StringBuilder sb = new StringBuilder();
        foreach (var kv in GameDatabase.Instance.GetPlayerData().inventory.GetSnapshot())
            sb.AppendLine($"{kv.Key}: {kv.Value}");
        inventoryText.text = sb.ToString();
    }
}
