using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnowledgeMining.UI.Services.State
{
    public record StateModel(string Type, string Values);

    public class StateService
    {
        public string GenerateHash<T>(T model) where T : class
        {
            var json = JsonSerializer.Serialize<T>(model, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }) ;

            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json.Trim()));
        }

        public StateModel DecodeHash(string hash)
        {
            var bytes = System.Convert.FromBase64String(hash);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<StateModel>(json);
        }
    }
}
