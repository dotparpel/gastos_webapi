using System.Text.Json;

namespace tests.Client;

public class ODataEntityKeyResponse<Entity, TKey> : ODataEntityResponse<Entity> {
    public int? GetODataInt(HttpResponseMessage? response) {
        int? ret = null;

        string? dataStr = GetData(response);

        if (dataStr != null) {
            ODataInt? odataValue = JsonSerializer.Deserialize<ODataInt>(dataStr);
            ret = odataValue?.value;
        }

        return ret;
    }

    public TKey? GetODataKey(HttpResponseMessage? response) {
        object? ret = null;

        if (typeof(TKey?).Equals(typeof(int?)))
            ret = GetODataInt(response);

        return (TKey?) ret;
    }
}

public class ODataInt {
    public int? value { get; set; }
}