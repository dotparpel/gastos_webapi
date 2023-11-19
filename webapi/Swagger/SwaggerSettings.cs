namespace webapi.Swagger;

public class SwaggerSettings
{
  public string?                  UrlSampleRequest  { get; set; }
  public bool?                    PersistAuthorization { get; set; }
  public string[]?                DefaultHttpMethodOrder { get; set; }
  public string[]?                MethodOrder { get; set; }
  public List<SwaggerDefinition>? Definitions       { get; set; }
  public List<SwaggerSchema>?     SchemasToRedefine { get; set; }
  public List<SwaggerPath>?       Paths             { get; set; }
}

public class SwaggerDefinition {
  public string?        DefinitionId  { get; set; }
  public List<string>?  Definition    { get; set; }
}

public class SwaggerSchema {
  public string?                Schema      { get; set; }
  public List<SwaggerProperty>? Properties  { get; set; }
}

public class SwaggerProperty {
  public string?  Property { get; set; }
  public string?  Type     { get; set; }
  public bool?    Nullable { get; set; }
  public string?  Format   { get; set; }
}

public class SwaggerPath {
  public string?                Path            { get; set; }
  public bool?                  Hide            { get; set; }
  public string?                Operation       { get; set; }
  public string?                Summary         { get; set; }
  public List<string>?          Description     { get; set; }
  public string?                SchemaResponse  { get; set; }
  public List<SwaggerExample>?  Examples        { get; set; }
}

public enum SwaggerMessageType {
  Request
  , Response
}

public class SwaggerExample {
  public List<SwaggerMessageType>? MessageType  { get; set; }
  public List<string>? Content                  { get; set; }
  public List<string>? Example                  { get; set; }
  public string?       DefinitionId             { get; set; }
}
