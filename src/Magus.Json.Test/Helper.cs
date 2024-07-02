using System.Text.Json;
using Json.Schema;

namespace Magus.Json.Test;

public static class Helper
{
    public static string ToJsonString(this EvaluationResults self)
    {
        return JsonSerializer.Serialize(self,
            new JsonSerializerOptions
            {
                Converters = { new EvaluationResultsJsonConverter() },
                WriteIndented = true
            });
    }
}