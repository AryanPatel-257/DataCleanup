using System.IO;
using System.Text.Json;
using MetadataDataEngine.Models;

namespace MetadataDataEngine.Services
{
    public class ExcelParsingService
    {
        public ClientConfig LoadClientConfigurationMatrix(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Target json metadata blueprint file location not found.");

            string jsonContent = File.ReadAllText(jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            return JsonSerializer.Deserialize<ClientConfig>(jsonContent, options) ?? new ClientConfig();
        }
    }
}
