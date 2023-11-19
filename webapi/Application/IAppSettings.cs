namespace webapi.Application;

public interface IAppSettings {
  string? Title       { get; set; }
  string? DbEncriptionKey { get; set; }
  string? ApiVersion  { get; set; }
  JwtSettings? Jwt    { get; set; }
  bool? UseSwaggerUI  { get; set; }
  List<string>? ExcludeControllerMethods {get; set;}
  List<string>? ExportEntities {get; set;}

  bool UseAuthorization {get;}
  string Version      { get; set; }
  string Name         { get; set; }

  List<string>? CalculatedExcludedMethods();
}