using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ProfileUIController : MonoBehaviour
{
    [SerializeField] private Text profileText;

    public void BindProfileText(Text text)
    {
        profileText = text;
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        PlayerProfile profile = GameDatabase.Instance?.GetPlayerData().profile;
        if (profileText == null || profile == null)
            return;

        profileText.text = $"Player: {profile.playerId}\nLevel: {profile.level}\nOutfit: {profile.equippedOutfitId}";
    }
}
