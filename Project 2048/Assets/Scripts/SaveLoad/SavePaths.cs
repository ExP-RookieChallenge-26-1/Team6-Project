using System.IO;
using UnityEngine;

namespace Project2048.Save
{
    public static class SavePaths
    {
        private const string SaveDirectoryName = "saves";
        private const string DefaultSaveFileName = "save_slot_0.json";

        public static string DefaultSaveFilePath =>
            Path.Combine(Application.persistentDataPath, SaveDirectoryName, DefaultSaveFileName);
    }
}
