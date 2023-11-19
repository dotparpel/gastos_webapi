using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Runtime.Serialization;

namespace webapi.Swagger;

// From: https://dev.to/kenakamu/c-aspnet-hide-model-properties-from-swagger-doc-626
public class SwaggerSchemaFilter : ISchemaFilter
{
  public void Apply(OpenApiSchema schema, SchemaFilterContext context)
  {
    if (schema?.Properties == null)
        return;

    // Ignorar les propietats dels models marcades com a "IgnoreDataMember".
    var ignoreDataMemberProperties = context.Type.GetProperties()
      .Where(t => t.GetCustomAttribute<IgnoreDataMemberAttribute>() != null);

    foreach (var property in ignoreDataMemberProperties) {
      var propertyToHide = schema.Properties.Keys
        .SingleOrDefault(x => x.ToLower() == property.Name.ToLower());

      if (propertyToHide != null)
        schema.Properties.Remove(propertyToHide);
    }
  }
}
