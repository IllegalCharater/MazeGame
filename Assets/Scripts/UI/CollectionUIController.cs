using System.Text;
using UnityEngine.UI;

public sealed class CollectionUIController : BaseUIController
{
    private Text collectionText;
    private CollectionCategory category = CollectionCategory.Food;

    public override void BindUI()
    {
        if (collectionText == null)
            collectionText = FindText("Collection Text");
    }

    public override void EventMapper()
    {
        GameEvents.OnCollectionChanged += OnCollectionChanged;
        Refresh();
    }

    public override void OnOpen()
    {
        Refresh();
    }

    public override void Dismiss()
    {
        GameEvents.OnCollectionChanged -= OnCollectionChanged;
        base.Dismiss();
    }

    public void BindCollectionText(Text text)
    {
        collectionText = text;
        Refresh();
    }

    private void OnCollectionChanged(CollectionCategory changedCategory)
    {
        if (changedCategory == category)
            Refresh();
    }

    public void Refresh()
    {
        if (collectionText == null || GameManager.Instance?.Services?.Collections == null)
            return;

        StringBuilder sb = new StringBuilder();
        foreach (string id in GameManager.Instance.Services.Collections.GetUnlocked(category))
            sb.AppendLine(id);
        collectionText.text = sb.ToString();
    }
}
