using OpenAiServiceClients.Core;
using OpenAiServiceClients.Core.Models;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<GatewayClient>((serviceProvider, httpClient) =>
{
	var configuration = serviceProvider.GetRequiredService<IConfiguration>();
	var baseUrl = configuration["Gateway:BaseUrl"] ?? "http://localhost:3000";
	httpClient.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

static string GetHeaderOrValue(HttpContext context, string headerName, string? fallbackValue = null)
{
	var headerValue = context.Request.Headers[headerName].ToString();
	return string.IsNullOrWhiteSpace(headerValue) ? fallbackValue ?? string.Empty : headerValue;
}

app.MapGet("/api/health", async (GatewayClient gatewayClient, CancellationToken cancellationToken) =>
{
	var payload = await gatewayClient.GetHealthAsync(cancellationToken);
	return Results.Ok(payload);
});

app.MapPost("/api/llm", async (GatewayClient gatewayClient, LlmPromptInput input, CancellationToken cancellationToken) =>
{
	if (string.IsNullOrWhiteSpace(input.ApiKey))
	{
		return Results.BadRequest(new { error = "apiKey is required." });
	}

	if (string.IsNullOrWhiteSpace(input.Model) || string.IsNullOrWhiteSpace(input.Input))
	{
		return Results.BadRequest(new { error = "model and input are required." });
	}

	try
	{
		using var payload = await gatewayClient.PostLlmAsync(
			new LlmRequest
			{
				Model = input.Model,
				Input = input.Input,
				Stream = false,
			},
			input.ApiKey,
			cancellationToken);

		return Results.Text(payload.RootElement.GetRawText(), "application/json");
	}
	catch (GatewayApiException error)
	{
		return Results.Problem(
			title: "Gateway request failed",
			detail: error.ResponseBody,
			statusCode: (int)error.StatusCode);
	}
});

app.MapPost("/api/llm/stream", async (HttpContext context, GatewayClient gatewayClient, LlmPromptInput input, CancellationToken cancellationToken) =>
{
	if (string.IsNullOrWhiteSpace(input.ApiKey))
	{
		return Results.BadRequest(new { error = "apiKey is required." });
	}

	if (string.IsNullOrWhiteSpace(input.Model) || string.IsNullOrWhiteSpace(input.Input))
	{
		return Results.BadRequest(new { error = "model and input are required." });
	}

	try
	{
		using var response = await gatewayClient.PostLlmStreamAsync(
			new LlmRequest
			{
				Model = input.Model,
				Input = input.Input,
				Stream = true,
			},
			input.ApiKey,
			cancellationToken);

		context.Response.StatusCode = StatusCodes.Status200OK;
		context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
		context.Response.Headers["X-Accel-Buffering"] = "no";
		context.Response.ContentType = "text/event-stream";

		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		await stream.CopyToAsync(context.Response.Body, cancellationToken);
		await context.Response.Body.FlushAsync(cancellationToken);
		return Results.Empty;
	}
	catch (GatewayApiException error)
	{
		return Results.Problem(
			title: "Gateway request failed",
			detail: error.ResponseBody,
			statusCode: (int)error.StatusCode);
	}
});

app.MapPost("/api/whisper", async (HttpContext context, GatewayClient gatewayClient, CancellationToken cancellationToken) =>
{
	var form = await context.Request.ReadFormAsync(cancellationToken);
	var apiKey = GetHeaderOrValue(context, "x-api-key", form["apiKey"].ToString());
	var model = form["model"].ToString();
	var file = form.Files.GetFile("file");

	if (string.IsNullOrWhiteSpace(apiKey))
	{
		return Results.BadRequest(new { error = "apiKey is required." });
	}

	if (string.IsNullOrWhiteSpace(model))
	{
		return Results.BadRequest(new { error = "model is required." });
	}

	if (file is null || file.Length == 0)
	{
		return Results.BadRequest(new { error = "file is required." });
	}

	try
	{
		await using var stream = file.OpenReadStream();
		using var payload = await gatewayClient.PostWhisperAsync(
			stream,
			file.FileName,
			model,
			apiKey,
			cancellationToken);

		return Results.Text(payload.RootElement.GetRawText(), "application/json");
	}
	catch (GatewayApiException error)
	{
		return Results.Problem(
			title: "Gateway request failed",
			detail: error.ResponseBody,
			statusCode: (int)error.StatusCode);
	}
});

app.MapGet("/api/usage", async (HttpContext context, GatewayClient gatewayClient, int limit = 20, int offset = 0, CancellationToken cancellationToken = default) =>
{
	var apiKey = GetHeaderOrValue(context, "x-api-key", context.Request.Query["apiKey"].ToString());
	if (string.IsNullOrWhiteSpace(apiKey))
	{
		return Results.BadRequest(new { error = "apiKey is required." });
	}

	try
	{
		var payload = await gatewayClient.GetUsageAsync(apiKey, limit, offset, cancellationToken);
		return Results.Ok(payload);
	}
	catch (GatewayApiException error)
	{
		return Results.Problem(
			title: "Gateway request failed",
			detail: error.ResponseBody,
			statusCode: (int)error.StatusCode);
	}
});

app.MapGet("/api/admin/usage", async (HttpContext context, GatewayClient gatewayClient, int limit = 20, int offset = 0, CancellationToken cancellationToken = default) =>
{
	var adminKey = GetHeaderOrValue(context, "x-admin-key", context.Request.Query["adminKey"].ToString());
	if (string.IsNullOrWhiteSpace(adminKey))
	{
		return Results.BadRequest(new { error = "adminKey is required." });
	}

	try
	{
		var payload = await gatewayClient.GetAdminUsageAsync(adminKey, limit, offset, cancellationToken);
		return Results.Ok(payload);
	}
	catch (GatewayApiException error)
	{
		return Results.Problem(
			title: "Gateway request failed",
			detail: error.ResponseBody,
			statusCode: (int)error.StatusCode);
	}
});

app.Run();

internal sealed record LlmPromptInput(string ApiKey, string Model, string Input);
