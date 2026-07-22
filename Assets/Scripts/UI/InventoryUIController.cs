using System.Text;
using UnityEngine.UI;

public sealed class InventoryUIController : BaseUIController
{
    private Text inventoryText;

    public override void BindUI()
    {
        if (inventoryText == null)
            inventoryText = FindText("Inventory Text");
    }

    public override void EventMapper()
    {
        GameEvents.OnInventoryChanged += Refresh;
        Refresh();
    }

    public override void OnOpen()
    {
        Refresh();
    }

    public override void Dismiss()
    {
        GameEvents.OnInventoryChanged -= Refresh;
        base.Dismiss();
    }

    public void BindInventoryText(Text text)
    {
        inventoryText = text;
        Refresh();
    }

    public void Refresh()
    {
        if (inventoryText == null || GameDatabase.Instance?.GetPlayerData()?.inventory == null)
            return;

        StringBuilder sb = new StringBuilder();
        foreach (var kv in GameDatabase.Instance.GetPlayerData().inventory.GetSnapshot())
            sb.AppendLine($"{kv.Key}: {kv.Value}");
        inventoryText.text = sb.ToString();
    }
}
