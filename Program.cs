using CrmWebApi.Extensions;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseApiSerilog();

builder.Services.AddApiCaching(builder.Configuration);

builder.Services.AddApiControllers();

// Регистрация бизнес-сервисов (extension method)
builder.Services.AddServices();
builder.Services.AddApiOptions(builder.Configuration);

// OpenAPI-документация с JWT Bearer схемой авторизации
builder.Services.AddApiOpenApi();

// PostgreSQL + EF Core
builder.Services.AddDatabase(builder.Configuration);

// Health checks: БД + SMTP
builder
	.Services.AddHealthChecks()
	.AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
	.AddCheck<CrmWebApi.Health.SmtpHealthCheck>("smtp", tags: ["mail"]);

// Репозитории
builder.Services.AddRepositories();

// JWT-аутентификация
builder.Services.AddJwt(builder.Configuration);

// Авторизация (политики)
builder.Services.AddAuthorization();

builder.Services.AddApiErrorHandling();
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiRateLimiting();

builder.Services.AddApiResponseCompression();

var app = builder.Build();

app.UseApiForwardedHeaders();

// HTTPS-редирект и HSTS первыми — до любой обработки контента
if (app.Environment.IsProduction())
{
	app.UseHttpsRedirection();
	app.UseHsts();
}

app.UseApiSecurityHeaders();
app.UseApiCompressionGuards();
app.UseResponseCompression();
app.UseApiRequestLogging();

// Middleware pipeline: ошибки → CORS → rate-limit partitioning → аутентификация → rate limit → метрики → авторизация
app.UseApiErrorHandling();
app.UseCors("AllowFrontend");
app.UseApiRateLimitPartitioning();
app.UseAuthentication();
app.UseRateLimiter();
app.UseHttpMetrics();
app.UseAuthorization();

app.UseApiDocs();

// Health checks
app.MapHealthChecks("/health");

// Prometheus метрики
app.MapMetrics();

app.MapControllers();

app.Run();

public partial class Program;
