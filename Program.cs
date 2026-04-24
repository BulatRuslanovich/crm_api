using CrmWebApi.Extensions;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseApiSerilog();

builder.Services.AddApiCaching(builder.Configuration);
builder.Services.AddApiControllers();
builder.Services.AddServices();
builder.Services.AddApiOptions(builder.Configuration);
builder.Services.AddCustomSwagger();
builder.Services.AddDatabase(builder.Configuration);

builder
	.Services.AddHealthChecks()
	.AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
	.AddCheck<CrmWebApi.Health.SmtpHealthCheck>("smtp", tags: ["mail"]);

builder.Services.AddRepositories();
builder.Services.AddJwt(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddApiErrorHandling();
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiRateLimiting();

builder.Services.AddApiResponseCompression();

var app = builder.Build();

app.UseApiForwardedHeaders();

if (app.Environment.IsProduction())
{
	app.UseHttpsRedirection();
	app.UseHsts();
}

app.UseApiSecurityHeaders();
app.UseApiCompressionGuards();
app.UseResponseCompression();
app.UseApiRequestLogging();

app.UseApiErrorHandling();
app.UseCors("AllowFrontend");
app.UseApiRateLimitPartitioning();
app.UseAuthentication();
app.UseRateLimiter();
app.UseHttpMetrics();
app.UseAuthorization();

app.UseApiDocs();

app.MapHealthChecks("/health");
app.MapMetrics();

app.MapControllers();

app.Run();

public partial class Program;
