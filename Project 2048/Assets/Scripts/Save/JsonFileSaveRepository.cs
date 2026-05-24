using System.IO;
using UnityEngine;

namespace Project2048.Save
{
    public class JsonFileSaveRepository : ISaveRepository
    {
        private readonly string filePath;

        public JsonFileSaveRepository(string filePath)
        {
            this.filePath = filePath;
        }

        public bool Exists()
        {
            return File.Exists(filePath);
        }

        public void Save(GameSaveData data)
        {
            if (data == null)
            {
                Debug.LogError("Save data is null.");
                return;
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
        }

        public GameSaveData Load()
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }

        public void Delete()
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
