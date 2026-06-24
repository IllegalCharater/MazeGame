[System.Serializable]
public class ActiveBuff
{
    public string buffId;
    public BuffCategory category;
    public float value;
    public float remainingSeconds;

    public ActiveBuff(string buffId, BuffCategory category, float value, float remainingSeconds)
    {
        this.buffId = buffId;
        this.category = category;
        this.value = value;
        this.remainingSeconds = remainingSeconds;
    }
}
