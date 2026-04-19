using System;

[Serializable]
public class GameSaveData
{
    public int Version = 1;
    public bool HasStartedGame;
    public string CurrentSituationId;
    public string[] Flags = Array.Empty<string>();
    public string[] InventoryItems = Array.Empty<string>();
}
