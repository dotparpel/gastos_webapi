using System.Reflection;

namespace webapi.Application;

public class AppSettings : IAppSettings{
  private string? _version;
  private string? _name;

  public string? Title { get; set; }
  public string? DbEncriptionKey { get; set; }
  public string? ApiVersion { get; set; }
  public JwtSettings? Jwt { get; set; }
  public bool? UseSwaggerUI { get; set; }
  public bool? AllowCORS { get; set; }
  public List<string>? ExcludeControllerMethods {get; set;}
  public List<string>? ExportEntities {get; set;}

  public bool UseAuthorization => (Jwt != null && !string.IsNullOrEmpty(Jwt.Key));

  public string Version {
    get => _version ?? AssemblyVersion;
    set { _version = value; }
  }

  public string Name {
    get => _name ?? AssemblyName;
    set { _name = value; }
  }

// Controller's method exclusion.
public List<string>? CalculatedExcludedMethods() {
  List<string>? ret = ExcludeControllerMethods;

  // If no authorization, exclude the "Login", the "RefreshToken" and the "Logout" methods.
  if (!UseAuthorization) {
    if (!ret?.Contains("App.Login") ?? false) {
      ret ??= new List<string>();
      ret?.Add("App.Login");
    }
    if (!ret?.Contains("App.RefreshToken") ?? false) {
      ret ??= new List<string>();
      ret?.Add("App.RefreshToken");
    }
    if (!ret?.Contains("App.Logout") ?? false) {
      ret ??= new List<string>();
      ret?.Add("App.Logout");
    }
  }

  return ret;
}

  public static string AssemblyVersion {
    get {
      string ret = "0.0.0.0";

      Assembly? assembly = Assembly.GetEntryAssembly();
      AssemblyFileVersionAttribute? attr = null;

      if (assembly != null)
          attr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

      if (attr != null)
          ret = attr.Version;

      return ret;
    }
  }

  public static string AssemblyName {
    get {
      string? ret = null;

      Assembly? assembly = Assembly.GetEntryAssembly();
      
      if (assembly != null)
        ret = assembly.GetName()?.Name;

      return ret ?? "NoNamedApp";
    }
  }
}

public class JwtSettings {
  public string? Key      { get; set; }
  public string? Audience { get; set; }
  public string? Issuer   { get; set; }
  public decimal? AccessTokenExpirationMinutes { get; set; }
  public decimal? RefreshExpirationMinutes { get; set; }
  public int? RefreshKeyLength { get; set; }
}