using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string SaveFileName = "savegame.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static bool SaveGame(GameSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("Cannot save a null game state.");
            return false;
        }

        try
        {
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Game saved to {SavePath}");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save game: {exception.Message}");
            return false;
        }
    }

    public static bool TryLoadGame(out GameSaveData saveData)
    {
        saveData = null;

        if (!HasSave())
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData == null || !saveData.HasStartedGame || string.IsNullOrWhiteSpace(saveData.CurrentSituationId))
            {
                saveData = null;
                return false;
            }

            if (saveData.Flags == null)
            {
                saveData.Flags = Array.Empty<string>();
            }

            if (saveData.InventoryItems == null)
            {
                saveData.InventoryItems = Array.Empty<string>();
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load save data: {exception.Message}");
            saveData = null;
            return false;
        }
    }
}
