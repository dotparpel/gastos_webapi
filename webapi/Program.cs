using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using System.Reflection;

using webapi.Application;
using webapi.JWT;
using webapi.Models;
using webapi.Repository;
using webapi.Swagger;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

var builder = WebApplication.CreateBuilder(args);

// App. parameters.
AppSettings appSettings = new AppSettings();
builder.Configuration.GetSection("App").Bind(appSettings);
builder.Services.AddSingleton<IAppSettings>(appSettings);

// Swagger parameters.
SwaggerSettings swaggerSettings = new SwaggerSettings();
builder.Configuration.GetSection("Swagger").Bind(swaggerSettings);

// Get connection parameters.
string? connActive = null;
string? connStr = null;

Dictionary<string, string> connDict = new (StringComparer.OrdinalIgnoreCase);
builder.Configuration.GetSection("ConnectionStrings").Bind(connDict);

if (connDict != null && connDict.Count > 0 && connDict.ContainsKey("active")) {
  connActive = connDict["active"].Trim().ToLower();

  if (string.IsNullOrEmpty(connActive))
    connActive = connDict.FirstOrDefault().Value;

  if (!string.IsNullOrEmpty(connActive) && connDict.ContainsKey(connActive))
    connStr = connDict[connActive];
}

// Postgres database.
if (!string.IsNullOrEmpty(connActive) && connActive.Contains("postgres"))
  // Avoid error "Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported".
  // AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
  // Entity framework context.
  builder.Services.AddDbContext<ApiContext>(opt => opt.UseNpgsql(connStr));

// SQL-Server database.
if (!string.IsNullOrEmpty(connActive) && connActive.Contains("mssql"))
  builder.Services.AddDbContext<ApiContext>(opt => opt.UseSqlServer(connStr));

// In memory database.
if (string.IsNullOrEmpty(connActive) || connActive == "inmemory" 
  || string.IsNullOrEmpty(connStr)
)
  builder.Services.AddDbContext<ApiContext>(opt => opt.UseInMemoryDatabase("Expenses"));

builder.Services.AddScoped<DbContext, ApiContext>();

// Search & repositories.
builder.Services.AddScoped(typeof(IGenericSearch<>), typeof(GenericSearch<>));
builder.Services.AddScoped(typeof(IGenericList<,>), typeof(GenericList<,>));
builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExpenseReportSearch, ExpenseReportSearch>();

IMvcBuilder controllers = builder.Services
  // Controller's method exclusion.
  .AddControllers(opt => {
    opt.Conventions.Add(new RemoveActionConvention(appSettings.CalculatedExcludedMethods()));
  })
  // OData config.
  .AddOData(opt => {
    opt.AddRouteComponents(
      appSettings.ApiVersion ?? "", GetEdmModel(appSettings)
    );
    opt.Select().Filter().OrderBy().Expand().Count().SetMaxTop(1000).SkipToken();
  });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
if (appSettings.UseSwaggerUI ?? false) {
  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddSwaggerGen(s => {
    s.SwaggerDoc($"{appSettings.ApiVersion}", new OpenApiInfo { 
      Version = appSettings.Version
      , Title = appSettings.Title
      , Description = "A Web API for managing expenses."
      , Contact = new OpenApiContact() { Name = "Dot Parpel", Email = "dotparpel@gmail.com" }
      , License = new OpenApiLicense() { Name = "MIT License", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    // Authentication.
    if (appSettings.UseAuthorization) {
      // Enable authorization using Swagger (JWT).
      s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme() {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
      });

      s.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme {
            Reference = new OpenApiReference {
              Type = ReferenceType.SecurityScheme,
              Id = "Bearer"
            }
          }
          , new string[] {}}
        });
      }

      // Consider the comments of the controller methods.
      // From: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-7.0&tabs=visual-studio
      var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
      s.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

      // Filter model properties marked with "IgnoreDataMember".
      s.SchemaFilter<SwaggerSchemaFilter>();
      
      // Add OData actions to actions with "EnableQuery".
      s.OperationFilter<SwaggerOperationFilter>();

      // Adjust the "Swagger" document.
      s.DocumentFilter<SwaggerDocumentFilter>(swaggerSettings, appSettings);

      // Sort methods.
      // From: https://stackoverflow.com/questions/46339078/how-can-i-change-order-the-operations-are-listed-in-a-group-in-swashbuckle.
      s.OrderActionsBy(e => 
        // $"{e.ActionDescriptor.RouteValues["controller"]}_{e.RelativePath}_{Array.IndexOf(methodsOrder, e.HttpMethod?.ToLower())}"
        GetMethodOrder(swaggerSettings
          , e.ActionDescriptor.RouteValues["controller"], e.RelativePath, e.HttpMethod)
      );

      // From: https://stackoverflow.com/questions/52262826/asp-net-core-swashbuckle-set-operationid
      s.CustomOperationIds(e => 
        $"{e.ActionDescriptor.RouteValues["controller"]}_{e.RelativePath}_{e.HttpMethod}");
  });
}

// Authentication.
builder.Services.AddScoped<IJWT, JWT>();

if (appSettings.UseAuthorization) {
  builder.Services.AddTransient<AppJwtBearerHandler>();

  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddScheme<JwtBearerOptions, AppJwtBearerHandler>(
      JwtBearerDefaults.AuthenticationScheme, o => { 
        o.TokenValidationParameters = new TokenValidationParameters {
          ClockSkew = TimeSpan.Zero
          , ValidateIssuer = true
          , ValidIssuer = appSettings.Jwt?.Issuer
          , ValidateAudience = true
          , ValidAudience = appSettings.Jwt?.Audience
          , ValidateLifetime = true
          , ValidateIssuerSigningKey = true
          , IssuerSigningKey 
              = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Jwt?.Key ?? ""))
        };
      }
    );
}

// CORS (1).
string corsPolicyName = "corsPolicy";

if (appSettings.AllowCORS ?? false) {
  builder.Services.AddCors(opt => {
    opt.AddPolicy(
      name: corsPolicyName
      , policy => { 
        policy.AllowAnyOrigin();
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
      });
  });
}

var app = builder.Build();

// HTTPS connections.
app.UseHttpsRedirection();
app.UseStaticFiles();

// CORS (2).
// "The call to 'UseCors' must be placed after 'UseRouting', but before 'UseAuthorization'".
if (appSettings.AllowCORS ?? false)
  app.UseCors(corsPolicyName);

if (appSettings.UseAuthorization) {
  app.UseAuthentication();
  app.UseAuthorization();
  app.MapControllers();
} else
  app.MapControllers().AllowAnonymous();

// Configure the HTTP request pipeline.
if (appSettings.UseSwaggerUI ?? false)
{
    app.UseSwagger();
    app.UseSwaggerUI(o => {
      // Do not include information from the OData service.
      o.DefaultModelsExpandDepth(-1);
      // Give the API version correctly.
      o.SwaggerEndpoint($"/swagger/{appSettings.ApiVersion}/swagger.json", $"{appSettings.Title} {appSettings.ApiVersion}"); 
      // Configure the page.
      o.InjectStylesheet("/custom.css");
      o.InjectJavascript("https://code.jquery.com/jquery-3.6.4.min.js");
      o.InjectJavascript("/custom.js");
      // Persist authorization data.
      if (swaggerSettings.PersistAuthorization ?? false)
        o.EnablePersistAuthorization();
    });
}

// Create an user when using an empty inmemory database.
if (connActive == "inmemory" || string.IsNullOrEmpty(connStr)) {
  using (var scope = app.Services.CreateScope())
  {
    var service = scope.ServiceProvider;
    IUserRepository? userRep = service.GetService<IUserRepository>();

    List<User>? userList = null;
    if (userRep != null && !string.IsNullOrEmpty(connStr))
      userList = userRep.FromConnectionStringToList(connStr);

    if (userRep != null && userList != null && userList.Count > 0)
      await userRep.InsertFromList(userList);
  }
}

app.Run();

IEdmModel GetEdmModel(IAppSettings appSettings)
{
  var builder = new ODataConventionModelBuilder();
  builder.EntitySet<User>("User");
  builder.EntitySet<Category>("Category");
  builder.EntitySet<Expense>("Expense");
  builder.EntitySet<ExpenseExpanded>("ExpenseExpanded");
  builder.EntitySet<ExpenseReport>("ExpenseReport");

  return builder.GetEdmModel();
}

string GetMethodOrder(SwaggerSettings settings, string? controller, string? path, string? httpMethod) {
  string ret = $"{controller}_{path}_{httpMethod?.ToLower()}";

  string[]? methodOrder = 
    settings?.MethodOrder?.Select(u => u.ToLower()).ToArray();

  string[]? defaultHttpMethodOrder = 
    settings?.DefaultHttpMethodOrder?.Select(u => u.ToLower()).ToArray();

  int index = -1;
  if (index == -1 && methodOrder != null)
    index = Array.IndexOf(methodOrder, ret.ToLower());

  if (index == -1 && defaultHttpMethodOrder != null)
    index = (methodOrder?.Length ?? 0) + Array.IndexOf(defaultHttpMethodOrder, httpMethod?.ToLower());

  if (index > -1)
    ret = $"{controller}_{index.ToString("D6")}_{path}_{httpMethod?.ToLower()}";

  return ret;
}

public class RemoveActionConvention : IApplicationModelConvention
{
  List<string>? _actionsToBeRemoved;

  public RemoveActionConvention(List<string>? actionsToBeRemoved = null) {
    _actionsToBeRemoved = actionsToBeRemoved;
  }

  public void Apply(ApplicationModel application)
  {
    foreach (var controller in application.Controllers) {
      var toBeRemoved = new List<ActionModel>();
      
      foreach (var action in controller.Actions) {
        if (ShouldBeRemoved(controller, action))
          toBeRemoved.Add(action);
      }

      foreach (var action in toBeRemoved)
        controller.Actions.Remove(action);
    }
  }

  private bool ShouldBeRemoved(ControllerModel controller, ActionModel action) {
    bool ret = false;

    if (_actionsToBeRemoved != null) {
      string method = controller.ControllerName + "." + action.ActionName;

      ret = _actionsToBeRemoved.Exists(a => 
        a.ToLower() == controller.ControllerName.ToLower()
        || a.ToLower() == method.ToLower()
      );
    }

    return ret;
  }
}

public partial class Program { }