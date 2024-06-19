using System;
using System.IO;
using System.Reflection;

namespace FerryDisplayApp.Helpers
{
    public static class JsonFileHelper
    {
        private static string jsonFilePath;

        static JsonFileHelper()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (var name in resourceNames)
            {
                Console.WriteLine(name);
            }

            InitializeJsonFilePath();
        }

        private static void InitializeJsonFilePath()
        {
            string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appName = "FerryImageDisplayApp";
            string folderPath = Path.Combine(appFolder, appName);
            Directory.CreateDirectory(folderPath);
            jsonFilePath = Path.Combine(folderPath, "FerriesList.json");

            if (!File.Exists(jsonFilePath))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "FerryImageDisplayApp.Resources.FerriesList.json";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string jsonString = reader.ReadToEnd();
                    File.WriteAllText(jsonFilePath, jsonString);
                }
            }
        }

        public static string ReadJsonFile()
        {
            return File.ReadAllText(jsonFilePath);
        }

        public static void WriteJsonFile(string content)
        {
            File.WriteAllText(jsonFilePath, content);
        }

        public static string GetFilePath()
        {
            return jsonFilePath;
        }
    }
}
