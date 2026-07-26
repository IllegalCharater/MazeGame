using UnityEngine.UI;

public sealed class ProfileUIController : BaseUIController
{
    private Text profileText;

    public override void BindUI()
    {
        if (profileText == null)
            profileText = FindText("Profile Text");
    }

    public override void OnOpen()
    {
        Refresh();
    }

    public void Refresh()
    {
        PlayerProfile profile = GameDatabase.Instance?.GetPlayerData()?.profile;
        if (profileText == null || profile == null)
            return;

        profileText.text = $"Player: {profile.playerId}\nLevel: {profile.level}\nOutfit: {profile.equippedOutfitId}";
    }
}
