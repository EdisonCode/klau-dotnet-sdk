# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

Official .NET SDK for the Klau API — a roll-off waste hauling platform. The SDK wraps REST endpoints into typed C# clients. Dependencies: `Microsoft.Extensions.Logging.Abstractions` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

## Build & Test Commands

```bash
dotnet build                                          # Build solution
dotnet test                                           # Run all tests
dotnet test --filter "FullyQualifiedName~JobClient"   # Run tests matching a pattern
dotnet test --filter "DisplayName=ListAsync_SendsCorrectPath"  # Run a single test
dotnet pack -c Release                                # Create NuGet package
```

Target framework: .NET 9.0. Solution file: `Klau.Sdk.sln`.

## Architecture

### Project structure
- `src/Klau.Sdk/` — The SDK library (ships as NuGet package `Klau.Sdk`)
- `tests/Klau.Sdk.Tests/` — xUnit tests using `MockHttpHandler` (no mocking framework)
- `examples/CsvJobImport/` — Console app: CSV → batch create → optimize → read assignments
- `examples/WebhookIntegration/` — Kestrel web app: bidirectional sync with webhooks

### Entry point: `KlauClient`
`KlauClient` is the single entry point. It owns a `KlauHttpClient` and exposes domain-specific clients as properties (`Jobs`, `Customers`, `Dispatches`, `Orders`, `Materials`, `Storefronts`, `DumpTickets`, `Proposals`, `Divisions`, `Webhooks`, `Auth`).

**Construction patterns** (in preference order):
1. `services.AddKlauClient(opts => config.GetSection("Klau").Bind(opts))` — ASP.NET Core DI
2. `KlauClient.CreateFromEnvironment()` — Reads `KLAU_API_KEY` env var
3. `KlauClient.Create(new KlauClientOptions { ... })` — Explicit options
4. `new KlauClient("kl_live_...")` — Direct construction

API keys must start with `kl_live_` — validation happens at construction time (fail-fast). The `KlauClientOptions` class controls `BaseUrl`, `TimeoutSeconds`, and `WebhookSecret` in addition to the key.

Enterprise multi-tenant: `KlauClient.ForTenant(id)` returns a `TenantScope` — an isolated set of sub-clients that pass the tenant ID as a per-request header without mutating the parent client. `SetTenant`/`ClearTenant` mutate default headers instead.

### Shared HTTP layer: `Common/KlauHttpClient`
All HTTP goes through `KlauHttpClient`. It handles:
- Auth via Bearer token (API keys starting with `kl_live_`)
- User-Agent header (`Klau-DotNet-SDK/{version}`)
- Tenant header injection (`Klau-Tenant-Id`)
- JSON serialization with `camelCase` properties + `SNAKE_CASE_UPPER` enums
- Configurable request timeout (default 30s for SDK-created HttpClients)
- Automatic retry (3 retries with exponential backoff) for 429, 502, 503, 504, and network errors
- API envelope unwrapping: all responses are `{ "data": T, "meta": { ... } }`
- Error mapping to `KlauApiException`

### Adding a new domain client

Each domain follows the same file pair pattern:
1. `{Domain}Models.cs` — Immutable `sealed record` types with `[JsonPropertyName]` on every property. Response models use `{ get; init; }`. Request models use `required` on mandatory fields.
2. `{Domain}Client.cs` — Stateless client that takes `KlauHttpClient` + optional `tenantId` in its constructor. Methods delegate to `_http.GetAsync<T>`, `PostAsync<T>`, `PatchAsync<T>`, etc.

After creating the pair, wire it into `KlauClient` (constructor + property) and `TenantScope` (if tenant-scopeable).

### Webhooks (`Webhooks/`)
Webhook support has two halves:
- **Receiving events** — `KlauWebhookValidator` verifies HMAC-SHA256 signatures (`Klau-Signature: t={ts},v1={hex}`) and parses into typed event models (`JobAssignedEvent`, `JobCompletedEvent`, `DispatchOptimizedEvent`, etc.). This is standalone — no `KlauClient` needed.
- **Managing endpoints** — `WebhookClient` (on `KlauClient.Webhooks`) calls the Developer Settings API to create/delete/test webhook registrations.

### Common infrastructure (`Common/`)
- `ApiResponse<T>` / `ResponseMeta` — API envelope deserialization
- `ApiError` / `KlauApiException` — Structured error handling
- `PagedResult<T>` — List endpoint pagination wrapper (constructed from `ResponseMeta`)
- `QueryBuilder` — Builds URL query strings from optional params
- `Enums.cs` — All shared enums (`JobType`, `JobStatus`, `OrderStatus`, etc.)

### Test conventions
Tests use `MockHttpHandler` (a `DelegatingHandler` in `tests/Helpers/`) instead of a mocking library. Pattern:
```csharp
var handler = new MockHttpHandler();
var httpClient = new HttpClient(handler);
var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
handler.EnqueueResponse(HttpStatusCode.OK, responseBody, optionalMeta);
// call client method, then assert on handler.SentRequests / handler.SentBodies
```

Responses passed to `EnqueueResponse` are automatically wrapped in `{ "data": ... }` envelope. Use `EnqueueRawResponse` for non-JSON error cases.

## Key Conventions

- API keys must start with `kl_live_` — validated at construction, not at first request
- All models are `sealed record` with `init`-only properties — never mutable classes
- Every JSON property gets an explicit `[JsonPropertyName("...")]` attribute (camelCase)
- Enums serialize as `SNAKE_CASE_UPPER` strings (configured in `KlauHttpClient.JsonOptions`)
- API paths follow `api/v1/{resource}` pattern
- All async methods accept an optional `CancellationToken ct` as last parameter
- List endpoints that return paginated data use `GetResponseAsync<List<T>>` + `PagedResult<T>`
- The SDK does NOT dispose a caller-provided `HttpClient` (ownership tracking via `_ownsHttpClient`)
- Environment variable fallbacks: `KLAU_API_KEY` for the API key, `KLAU_WEBHOOK_SECRET` for webhook signing
