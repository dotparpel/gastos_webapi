using System.Text.Json;

namespace tests.Client;

public class ODataEntityResponse<Entity> {
    public string Typename() => typeof(Entity).Name.ToLower();

    public string? GetData(HttpResponseMessage? response) 
        => response?.Content.ReadAsStringAsync().Result;

    public Entity? GetOdataEntity(HttpResponseMessage? response) {
        Entity? ret = default;

        string? dataStr = GetData(response);

        if (!string.IsNullOrEmpty(dataStr))
            ret = JsonSerializer.Deserialize<Entity>(dataStr);

        return ret;
    }

    public List<Entity>? GetODataEntityList(HttpResponseMessage? response) {
        List<Entity>? ret = null;

        string? dataStr = GetData(response);

        if (dataStr != null) {
            ODataEntityList<Entity>? odataValue = JsonSerializer.Deserialize<ODataEntityList<Entity>>(dataStr);
            ret = odataValue?.value;
        }

        return ret;
    }
}

public class ODataEntityList<Entity> {
    public List<Entity>? value { get; set; }
}
