using System;
using System.IO;
using UnityEngine;

namespace AmbitionControl
{
    [Serializable]
    internal sealed class TrainerSettings
    {
        public float windowX = 80f;
        public float windowY = 80f;
        public int selectedTab;
        public bool visible;

        internal static string DirectoryPath => Path.Combine(Application.persistentDataPath, "AmbitionControl");
        internal static string FilePath => Path.Combine(DirectoryPath, "settings.json");

        internal static TrainerSettings Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new TrainerSettings();
                return JsonUtility.FromJson<TrainerSettings>(File.ReadAllText(FilePath)) ?? new TrainerSettings();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AmbitionControl] Unable to load menu settings: " + ex.Message);
                return new TrainerSettings();
            }
        }

        internal void Save()
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                string temporaryFile = FilePath + ".tmp";
                File.WriteAllText(temporaryFile, JsonUtility.ToJson(this, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temporaryFile, FilePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AmbitionControl] Unable to save menu settings: " + ex.Message);
            }
        }
    }
}
