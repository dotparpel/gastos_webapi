using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace webapi.Swagger;

// From: https://stackoverflow.com/questions/31351293/odata-query-in-swagger-ui
public class SwaggerOperationFilter : IOperationFilter
{
    // Afegir com a paràmetres al mètode "/odata/entitat/get" les operacions
    // específiques de l'estandard "OData".
    static List<OpenApiParameter> s_Parameters = (new List<(string Name, string Description)>()
      {
        ( "$select", "Specifies a subset of properties to return. Use a comma separated list."),
        ( "$filter", "A function that must evaluate to true for a record to be returned."),
        ( "$orderby", "Determines what values are used to order a collection of records."),
        ( "$apply", "Specify a sequence of transformations to the entity set."),
        ( "$top", "The max number of records."),
        ( "$skip", "The number of records to skip."),
        ( "$expand", "Use to add related query data.")
      }).Select(pair => new OpenApiParameter
      {
        Name = pair.Name,
        Required = false,
        Schema = new OpenApiSchema { Type = "String" },
        In = ParameterLocation.Query,
        Description = pair.Description,
      }).ToList();

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
      // Afegir els paràmetres únicament als mètodes que tinguin el decorador "EnableQuery" d'OData.
      if (context.ApiDescription.ActionDescriptor.EndpointMetadata
        .Any(em => em is Microsoft.AspNetCore.OData.Query.EnableQueryAttribute))
      {
        operation.Parameters ??= new List<OpenApiParameter>();
        foreach (var item in s_Parameters)
          operation.Parameters.Add(item);
      }
    }
}