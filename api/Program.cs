using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()
  .ConfigureServices((ctx, services) =>
  {
      services.AddSingleton(sp =>
      {
          var cfg = sp.GetRequiredService<IConfiguration>();
          var conn = cfg["StorageConnectionString"];
          if (string.IsNullOrWhiteSpace(conn))
              throw new InvalidOperationException("StorageConnectionString is missing.");
          return new TableServiceClient(conn);
      });

        services.AddSingleton<AppConfig>();
        services.AddSingleton<TableStore>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<CookieService>();
        services.AddSingleton<PasswordHasher>();
        services.AddSingleton<HCaptchaClient>();
  })
  .Build();

host.Run();

public sealed class AppConfig
{
    public string JwtSecret { get; }
    public string JwtIssuer { get; }
    public string JwtAudience { get; }
    public int JwtMinutes { get; }
    public bool UseSecureCookies { get; }

    public AppConfig(IConfiguration cfg)
    {
        JwtSecret = cfg["JwtSecret"] ?? throw new InvalidOperationException("JwtSecret is missing.");
        JwtIssuer = cfg["JwtIssuer"] ?? "hcaptcha-demo";
        JwtAudience = cfg["JwtAudience"] ?? "hcaptcha-demo";
        JwtMinutes = int.TryParse(cfg["JwtMinutes"], out var m) ? m : 60;
        UseSecureCookies = (cfg["UseSecureCookies"] ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
