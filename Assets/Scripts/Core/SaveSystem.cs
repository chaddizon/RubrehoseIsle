using System.IO;
using UnityEngine;

namespace Rubrehose.Core
{
    public static class SaveSystem
    {
        private const string SaveFileName = "rubrehose_save.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(PlayerState state)
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(state));
        }

        public static PlayerState Load()
        {
            if (!File.Exists(SavePath)) return null;
            return JsonUtility.FromJson<PlayerState>(File.ReadAllText(SavePath));
        }
    }
}
