using System.Text.Json;
using System.Text.Json.Nodes;

namespace CSharpLegendary
{
    public static class TokenStore
    {
        private static string FileName
        {
            get
            {
                var userpath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var legendarypath = Path.Combine(userpath, ".config", "legendary", "user.json");
                
                if (File.Exists(legendarypath)) return legendarypath;

                var persAUpath = Path.Combine(userpath, "AppData", "LocalLow", "Innersloth", "Among Us");

                if (Directory.Exists(persAUpath))
                {
                    return Path.Combine(persAUpath, "EGSAuth.json");
                }

                return "EGSAuth.json";
            }
        }

        public static async Task SaveTokensAsync(JsonNode sessionNode)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = sessionNode.ToJsonString(options);
            await File.WriteAllTextAsync(FileName, json);
            Console.WriteLine($"[TokenStore] Session saved to {FileName}");
        }

        public static async Task<JsonNode?> LoadTokensAsync()
        {
            if (!File.Exists(FileName)) return null;

            try
            {
                string json = await File.ReadAllTextAsync(FileName);
                return JsonNode.Parse(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}