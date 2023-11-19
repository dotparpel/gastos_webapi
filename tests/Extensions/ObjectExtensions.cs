using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tests.Extensions;

public static class ObjectExtensions
{
    public static StringContent? ToStringContent(this object? obj)
    {
        StringContent? ret = null;

        string s = "";
        
        if (obj is string)
            s = (string) obj;
        else
            s = JsonSerializer.Serialize(obj
                , new JsonSerializerOptions() {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }
            );

        ret = new(s, Encoding.UTF8, "application/json");
        
        return ret;
    }
}