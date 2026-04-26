using System.IO;
using UnityEngine;

namespace NoctisUltor.Prototype
{
    public static class PrototypePersistence
    {
        private const string SaveFileName = "prototype_permanent_save.json";

        public static PrototypeSaveData Load()
        {
            var path = GetSavePath();
            if (!File.Exists(path))
            {
                return CreateDefault();
            }

            var json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<PrototypeSaveData>(json) ?? CreateDefault();
            Normalize(loaded);
            return loaded;
        }

        public static void Save(PrototypeSaveData saveData)
        {
            Normalize(saveData);
            var path = GetSavePath();
            var json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(path, json);
        }

        public static void Reset()
        {
            Save(CreateDefault());
        }

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        private static PrototypeSaveData CreateDefault()
        {
            var saveData = new PrototypeSaveData();
            Normalize(saveData);
            return saveData;
        }

        private static void Normalize(PrototypeSaveData saveData)
        {
            saveData.TokenCount = Mathf.Max(0, saveData.TokenCount);
            saveData.AttackUpgradeLevel = Mathf.Clamp(saveData.AttackUpgradeLevel <= 0 ? 1 : saveData.AttackUpgradeLevel, 1, 6);
            saveData.StartingSkillPointUpgradeLevel = Mathf.Clamp(saveData.StartingSkillPointUpgradeLevel, 0, 5);
            saveData.HpUpgradeLevel = Mathf.Clamp(saveData.HpUpgradeLevel, 0, 3);
            saveData.SelectedSpirit = Mathf.Clamp(saveData.SelectedSpirit, 0, 2);
            saveData.EndlessBestStage = Mathf.Max(0, saveData.EndlessBestStage);
            saveData.UnlockedSeals ??= new System.Collections.Generic.List<int>();

            if (saveData.EquippedSeal == (int)SkillId.None)
            {
                return;
            }

            if (!saveData.UnlockedSeals.Contains(saveData.EquippedSeal))
            {
                saveData.EquippedSeal = (int)SkillId.None;
            }
        }
    }
}
