using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CollectionUIController : MonoBehaviour
{
    [SerializeField] private Text collectionText;
    [SerializeField] private CollectionCategory category = CollectionCategory.Food;

    public void BindCollectionText(Text text)
    {
        collectionText = text;
        Refresh();
    }

    private void OnEnable()
    {
        GameEvents.OnCollectionChanged += OnCollectionChanged;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnCollectionChanged -= OnCollectionChanged;
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
