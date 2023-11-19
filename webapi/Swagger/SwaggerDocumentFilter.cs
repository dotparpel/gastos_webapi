using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Pluralize.NET.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

using webapi.Application;

namespace webapi.Swagger;

// From: https://michael-mckenna.com/swagger-with-asp-net-core-3-1-json-patch/
public class SwaggerDocumentFilter : IDocumentFilter
{
  private SwaggerSettings _swaggerSettings;
  private AppSettings _appSettings;
  private string _urlSampleRequest;

  public SwaggerDocumentFilter(SwaggerSettings swaggerSettings, AppSettings appSettings) {
    _swaggerSettings  = swaggerSettings;
    _appSettings      = appSettings;
    _urlSampleRequest = ReplaceKnownVariables(swaggerSettings.UrlSampleRequest) ?? "";
  }

  private string? GetEntity(string path) {
    string? ret = null;

    // Eliminar el prefix del "path".
    string pathNoPrefix = PathNoPrefix(path);

    if (pathNoPrefix.StartsWith("/"))
      pathNoPrefix = pathNoPrefix.Substring(1);

    if (!string.IsNullOrEmpty(pathNoPrefix)) {
      string[]? arr = pathNoPrefix.Split('/');

      if (arr != null && arr.Length > 0 && !string.IsNullOrEmpty(arr[0]))
        ret = arr[0];
    }

    return ret;
  }

  private string GetAOrAn(string entity) {
    string ret = "a";

    List<string> vowels = new List<string>() {"a", "e", "i", "o", "u"};
    string? firstChar = null;

    if (!string.IsNullOrEmpty(entity) && entity.Length > 0)
      firstChar = entity.Substring(0, 1).ToLower();

    if (firstChar != null && vowels.Contains(firstChar))
      ret = "an";

    return ret;
  }

  private string? Capitalize(string? s) 
    => string.IsNullOrEmpty(s) ? s : s.ElementAt(0).ToString().ToUpper() + s.Substring(1);

  private string? ReplaceKnownVariables(string? s, string? path = null) {
    string? ret = s;

    // Substituir les varialbles de l'aplicació.
    if (!string.IsNullOrEmpty(ret)) {
      if (!string.IsNullOrEmpty(_appSettings.ApiVersion))
        ret = ret.Replace("{ApiVersion}", _appSettings.ApiVersion, StringComparison.CurrentCultureIgnoreCase);
      if (!string.IsNullOrEmpty(_urlSampleRequest))
        ret = ret.Replace("{UrlSampleRequest}", _urlSampleRequest, StringComparison.CurrentCultureIgnoreCase);

      ret = ret.Replace("{Version}", _appSettings.Version, StringComparison.CurrentCultureIgnoreCase);
      ret = ret.Replace("{AppName}", _appSettings.Name, StringComparison.CurrentCultureIgnoreCase);
    }

    // Substituir les variables corresponents al "path".
    if (!string.IsNullOrEmpty(ret) && !string.IsNullOrEmpty(path)) {
      // Eliminar el prefix del "path".
      string? entity = GetEntity(path);

      if (!string.IsNullOrEmpty(entity)) {
        Pluralizer pl = new Pluralizer();

        string entityLower = entity.ToLower();
        string entityPlural = pl.Pluralize(entityLower);
        string an = GetAOrAn(entity);

        ret = ret.Replace("{item}", entityLower, false, null);
        ret = ret.Replace("{Item}", Capitalize(entityLower), false, null);
        ret = ret.Replace("{items}", entityPlural, false, null);
        ret = ret.Replace("{Items}", Capitalize(entityPlural), false, null);
        ret = ret.Replace("{an}", an, false, null);
        ret = ret.Replace("{An}", Capitalize(an), false, null);
      }
    }

    return ret;
  }

  private string? GetDefinition(string definitionId) {
    string? ret = null;

    if (_swaggerSettings.Definitions != null) {
      SwaggerDefinition? defItem = _swaggerSettings.Definitions
        .Where(d => d.DefinitionId == definitionId)
        .FirstOrDefault();

      if (defItem != null && defItem.Definition != null)
        ret = string.Join(" ", defItem.Definition);
    }

    if (ret != null)
      ret = ReplaceKnownVariables(ret);

    return ret;
  }

  private string? GetExample(SwaggerMessageType messageType
    , string path, OperationType op, string content, string? example) 
  {
    // Exemples de "path":
    //  - /odata/Category
    //  - /odata/Category/$count
    //  - /odata/Category/{key}
    // OperationType: 
    //    { 0: Get, 1: Put, 2: Post, 3: Delete, 4: Options, 5: Head, 6: Patch, 7: Trace }
    string? ret = null;

    if (_swaggerSettings.Paths != null) {
      // Eliminar el prefix del "path".
      string pathNoPrefix = PathNoPrefix(path);

      // Obtenir l'operació en format "string".
      string oppStr = OpToString(op);

      SwaggerPath? pathItem = _swaggerSettings.Paths
        .Where(p => p.Path == pathNoPrefix
          && p.Operation?.ToUpper() == oppStr
          && p.Examples != null)
        .FirstOrDefault();
      
      if (pathItem != null && pathItem.Examples != null && pathItem.Examples.Count > 0) {
        SwaggerExample? exampleItem = pathItem.Examples
        .Where(e => e.Content != null 
          && e.Content.Any (
            c => content.Contains(c, StringComparison.CurrentCultureIgnoreCase)
          )
          && ((e.MessageType == null && messageType == SwaggerMessageType.Response)
            || (e.MessageType != null && e.MessageType.Any (
              m => m == messageType
            ))
          )
        )
        .FirstOrDefault();

        if (exampleItem != null && exampleItem.Example != null && exampleItem.Example.Count > 0)
          ret = string.Join(" ", exampleItem.Example);

        if (exampleItem != null && exampleItem.DefinitionId != null) {
          // Obtenir la definició.
          ret = GetDefinition(exampleItem.DefinitionId);

          if (ret != null) 
            ret = ret.Replace("{Operation}", oppStr, StringComparison.CurrentCultureIgnoreCase);
        }
      }
    }

    if (ret != null)
      ret = ReplaceKnownVariables(ret);

    return ret;
  }

  private string PathNoPrefix(string path) {
    // Eliminar el prefix del "path".
    string ret = path;

    if (!string.IsNullOrEmpty(_appSettings.ApiVersion))
      ret = ret.Replace("/" + _appSettings.ApiVersion, "", StringComparison.CurrentCultureIgnoreCase);

    return ret;
  }

  private string OpToString(OperationType op) => op.ToString().ToUpper();

  private OpenApiSchema? GetSchemaResponse(string path, OperationType op) {
    OpenApiSchema? ret = null;
    
    if (_swaggerSettings.Paths != null) {
      // Eliminar el prefix del "path".
      string pathNoPrefix = PathNoPrefix(path);

      // Obtenir l'operació en format "string".
      string oppStr = OpToString(op);

      SwaggerPath? pathItem = _swaggerSettings.Paths
        .Where(p => p.Path == pathNoPrefix
          && p.Operation?.ToUpper() == oppStr
          && !string.IsNullOrEmpty(p.SchemaResponse))
        .FirstOrDefault();
    
      if (pathItem != null) {
        OpenApiReference r = new OpenApiReference() { 
          ExternalResource = "#/components/schemas/" + pathItem.SchemaResponse
        };

        ret = new OpenApiSchema() { Reference = r };
      }
    }

    return ret;
  }

  private string? GetSummary(string path, OperationType op, string? summary) {
    string? ret = summary;

    // Eliminar el prefix del "path".
    string pathNoPrefix = PathNoPrefix(path);

    // Obtenir l'operació en format "string".
    string oppStr = OpToString(op);

    // Trobar el "path" dins de les descripcions
    if (_swaggerSettings.Paths != null) {
      SwaggerPath? pathItem = _swaggerSettings.Paths
        .Where(p => p.Path == pathNoPrefix && p.Operation?.ToUpper() == oppStr)
        .FirstOrDefault();
    
      if (pathItem != null && pathItem.Summary != null)
        ret = pathItem.Summary;
    }

    if (!string.IsNullOrEmpty(ret))
      ret = ReplaceKnownVariables(ret, path);

    return ret;
  }

  public string? GetPathDescription(string path, OperationType op, string description) {
    string? ret = null;

    // Eliminar el prefix del "path".
    string pathNoPrefix = PathNoPrefix(path);

    // Obtenir l'operació en format "string".
    string oppStr = OpToString(op);

    // Trobar el "path" dins de les descripcions
    if (_swaggerSettings.Paths != null) {
      SwaggerPath? pathItem = _swaggerSettings.Paths
        .Where(p => p.Path == pathNoPrefix && p.Operation?.ToUpper() == oppStr)
        .FirstOrDefault();
    
      if (pathItem != null && pathItem.Description != null && pathItem.Description.Count > 0)
        ret = string.Join("<br/>", pathItem.Description);
    }

    if (!string.IsNullOrEmpty(ret))
      ret = ReplaceKnownVariables(ret, path);

    return ret;
  }

  public string? GetResponseDescription(string path, OperationType op, string description) {
    string? ret = description;

    if (!string.IsNullOrEmpty(ret))
      ret = ReplaceKnownVariables(ret, path);

    return ret;
  }

  public void ReplaceSchemas(OpenApiDocument swaggerDoc) {
    if (_swaggerSettings.SchemasToRedefine != null)
      foreach (SwaggerSchema schema in _swaggerSettings.SchemasToRedefine) {
        // Esborrar l'anterior esquema.
        if (swaggerDoc.Components.Schemas.ContainsKey(schema.Schema))
          swaggerDoc.Components.Schemas.Remove(schema.Schema);

        // Crear el diccionari de propietats.
        Dictionary<string, OpenApiSchema>? properties = null;

        if (schema.Properties != null)
          foreach (SwaggerProperty prop in schema.Properties)
            if (prop.Property != null && prop.Type != null) {
              OpenApiSchema s = new OpenApiSchema() { 
                Type = prop.Type
                , Nullable = prop.Nullable ?? true
              };

              if (prop.Format != null)
                s.Format = prop.Format;

              if (properties == null)
                properties = new Dictionary<string, OpenApiSchema>();

              properties.Add(prop.Property, s);
            }
        
        // Crear el nou esquema.
        if (properties != null)
          swaggerDoc.Components.Schemas.Add(schema.Schema, new OpenApiSchema {
            Type = "object"
            , Properties = properties
          });
      }
  }

  public void HidePaths(OpenApiDocument swaggerDoc) {
    if (_swaggerSettings.Paths != null) {
      List<SwaggerPath>? pathList = _swaggerSettings.Paths
        .Where(p => (p.Hide ?? false))
        .ToList();

      if (pathList != null) {
        var removePaths = swaggerDoc.Paths.Where(p =>
          pathList.Any(
            q => q.Path == (PathNoPrefix(p.Key ?? ""))
          )
        ).ToList();

        if (removePaths != null)
          removePaths.ForEach(p => { swaggerDoc.Paths.Remove(p.Key); });
      }
    }
  }

  // https://www.fuget.org/packages/Microsoft.OpenApi/1.0.0-beta015/lib/netstandard2.0/Microsoft.OpenApi.dll/Microsoft.OpenApi.Models/OpenApiDocument
  public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
  {
    // Esborrar els esquemes "Delta", ja que no reflecteixen el que hom enten.
    ReplaceSchemas(swaggerDoc);

    // Esborrar els "paths" redundants:
    HidePaths(swaggerDoc);

    // Proporcionar exemples a les operacions.
    var paths = swaggerDoc.Paths?.ToList();

    if (paths != null) 
      foreach (var path in paths) {
        
        // Console.WriteLine($"path.Key: {path.Key}");

        OpenApiPathItem pathItem = path.Value;

        var operations = pathItem.Operations;

        foreach (var op in operations) {
          // Console.WriteLine($"  op.Key: {op.Key}");

          OpenApiOperation opItem = op.Value;

          // Tractar resum.
          opItem.Summary = GetSummary(path.Key, op.Key, opItem.Summary);

          // Tractar la descripció.
          string description = opItem.Description;

          string? newDescription = GetPathDescription(path.Key, op.Key, description);
          if (newDescription != null)
            opItem.Description = newDescription;

          // Tractar exemples de "Request".
          OpenApiRequestBody requestBody = opItem.RequestBody;

          // Console.WriteLine($"  requestBody: {requestBody}");

          if (requestBody != null) {
            var content = requestBody.Content;

            foreach (var cont in content) {
              // Console.WriteLine($"    cont.Key: {cont.Key}");
            
              OpenApiMediaType contItem = cont.Value;

              var os = (OpenApiString)(IOpenApiPrimitive) contItem.Example;
              string? example = os?.Value;

              // Console.WriteLine($"    example: {example}");

              string? newExample = GetExample(SwaggerMessageType.Request
                , path.Key, op.Key, cont.Key, example);

              if (newExample != null)
                contItem.Example = new OpenApiString(newExample);
            }
          }

          // Tractar exemples de "Responses".
          OpenApiResponses responses = opItem.Responses;

          if (responses != null)
            foreach (var resp in responses) {
              // Console.WriteLine($"    resp.Key: {resp.Key}");

              OpenApiResponse respItem = resp.Value;

              // Tractar la descripció.
              description = respItem.Description;

              newDescription = GetResponseDescription(path.Key, op.Key, description);
              if (newDescription != null)
                respItem.Description = newDescription;

              if (respItem != null) {
                var content = respItem.Content;

                foreach (var cont in content) {
                  // Console.WriteLine($"    cont.Key: {cont.Key}");
                
                  OpenApiMediaType contItem = cont.Value;

                  OpenApiSchema schema = contItem.Schema;
                  OpenApiSchema? newSchema = GetSchemaResponse(path.Key, op.Key);

                  if (newSchema != null)
                    contItem.Schema = newSchema;

                  // Correcció de la generació de codi en XML.
                  if (cont.Key.Contains("xml", StringComparison.CurrentCultureIgnoreCase)) {
                    schema = contItem.Schema;

                    if (schema.Xml == null)
                      schema.Xml = new OpenApiXml();

                    OpenApiXml xml = schema.Xml;
                    xml.Name = "NomIrrelevant";
                    xml.Wrapped = true;
                  }

                  var os = (OpenApiString)(IOpenApiPrimitive) contItem.Example;
                  string? example = os?.Value;

                  // Console.WriteLine($"    example: {example}");

                  string? newExample = GetExample(SwaggerMessageType.Response
                    , path.Key, op.Key, cont.Key, example);

                  // Console.WriteLine($"    newExample: {newExample}");

                  if (newExample != null)
                    contItem.Example = new OpenApiString(newExample);
                }
              }
            }
        }
      }
  }
}
