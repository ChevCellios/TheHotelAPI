# The Hotel API

Working proof-of-concept for hotel CRUD and prompt-based hotel search, built with ASP.NET Core 9 and a pragmatic Clean Architecture structure.

## Run in Visual Studio

1. Open `TheHotelAPI.sln` in Visual Studio 2022 with the ASP.NET workload and .NET 9 SDK.
2. Set `TheHotelAPI.Api` as the startup project.
3. Select the `https` launch profile and press F5.
4. Open `/swagger` or run requests from `src/TheHotelAPI.Api/TheHotelAPI.Api.http`.

Set a strong local development API key once with `dotnet user-secrets set "ApiKey" "<your-strong-local-key>" --project src/TheHotelAPI.Api`. Copy the same local value into the `@apiKey` variable of the `.http` file when testing, but do not commit that replacement. The key is sent in the `X-Api-Key` header for create, update, and delete operations and is not stored in tracked configuration. Use a secret manager or environment variable in deployed environments.

CLI alternative:

```powershell
dotnet run --project src/TheHotelAPI.Api
dotnet test TheHotelAPI.sln
```

The `http` launch profile is convenient for local-only development at `http://localhost:5193`. API keys sent over HTTP are not encrypted, so use the `https` profile for shared networks and every deployed environment.

## API

| Method | Route | Purpose | API key |
|---|---|---|---|
| POST | `/api/v1/hotels` | Create hotel | Yes |
| GET | `/api/v1/hotels/{id}` | Get hotel | No |
| GET | `/api/v1/hotels?page=1&pageSize=20` | List hotels | No |
| PUT | `/api/v1/hotels/{id}` | Replace hotel | Yes |
| DELETE | `/api/v1/hotels/{id}` | Delete hotel | Yes |
| POST | `/api/v1/hotel-searches` | Search hotels | No |

Create and update requests accept a city instead of coordinates. The API resolves and validates the geographic location before storing the hotel:

```json
{
  "name": "Split Central Hotel",
  "pricePerNight": { "amount": 95, "currency": "EUR" },
  "city": "Split"
}
```

The resolved latitude and longitude are returned in the hotel response and retained by the domain model. Common Croatian cities are resolved from an offline lookup; other cities use OpenStreetMap Nominatim, so clients never need to supply hotel coordinates.

For search, send the traveller's starting point in `originCity` and the hotel location in `destinationCity`, for example `{ "prompt": "hotel under 150 EUR", "originCity": "Zagreb", "destinationCity": "Split" }`. The API returns only hotels from the destination city and calculates their `distanceKm` from the origin. If `destinationCity` is omitted, it can still be extracted from a prompt such as `hotel in Split under 150 EUR`. Clients never need to enter coordinates.

## Ranking

All CRUD-managed hotels are returned. The PoC uses EUR consistently so prices remain comparable. Distance uses the Haversine formula between the resolved origin city and each hotel's stored location. Hotels receive this score (lower is better):

```text
priceScore = price / budget (values above 1 indicate an over-budget hotel)
distanceScore = min(distanceKm / 100km, 1)
score = 0.5 * priceScore + 0.5 * distanceScore
```

Ties are resolved by price, distance, and id. Current in-memory search is `O(n log n)` due to sorting.

## Architecture and extension points

- `Domain`: validated Hotel, Money, and GeoLocation models.
- `Application`: use cases, contracts, ranking, and repository/parser interfaces.
- `Infrastructure`: thread-safe in-memory repository and deterministic prompt parser.
- `Api`: REST controllers, authentication middleware, Problem Details, Swagger, and health checks.

`IHotelRepository` allows replacing memory storage with EF Core/PostgreSQL. `ISearchPromptParser` allows adding geocoding or an LLM while keeping validation and ranking deterministic.

## Assignment traceability

| Requirement | Implementation |
|---|---|
| JSON REST CRUD | Versioned `/api/v1/hotels` POST, GET, list, PUT, and DELETE endpoints with standard status codes |
| Required hotel data | CRUD accepts name, EUR price, and city; the API resolves the city and the validated `Hotel` aggregate stores geographic coordinates |
| Prompt search | `/api/v1/hotel-searches` extracts the budget and resolves `originCity` (or a city in the prompt) without client-supplied coordinates |
| Search only managed hotels | Search reads exclusively through the same `IHotelRepository` used by CRUD |
| Name, price, distance output | Every search item contains these values; `id` and explainable ranking `score` are additional metadata |
| Cheaper/closer ordering | Documented normalized score with deterministic tie-breakers |
| Paging bonus | CRUD list and search support `page` and `pageSize` (maximum 100) and return `totalCount` |
| Persistence extensibility | Application depends on `IHotelRepository`; the concurrent in-memory implementation can be replaced in Infrastructure |
| Clean architecture/DDD | Dependencies point Domain <- Application <- Infrastructure/API; entities and value objects enforce invariants |
| Secure coding | Defensive validation, fixed-time API-key comparison, write protection, rate limiting, security headers, generic 500 responses, and no tracked secret |
| Operations | Health endpoints, structured ASP.NET Core logging, Swagger, and GitHub Actions build/test/coverage workflow |

## Tests

Run all automated tests with:

```powershell
dotnet test TheHotelAPI.sln
```

The suites cover domain invariants (including invalid and non-finite coordinates), distance calculation, price/currency rules, ranking and deterministic inclusion of over-budget hotels, origin-city resolution, paging-related behavior, invalid prompts, API-key rejection, end-to-end CRUD-to-search flow, and health checks. GitHub Actions restores, performs a warning-free Release build, runs all tests, collects coverage, and uploads test artifacts for every pull request and push.

## Security model

Public reads and searches require no credentials. Hotel create, replace, and delete operations require `X-Api-Key`; comparison is performed in constant time. Validation failures use RFC 7807 Problem Details without stack traces, unexpected exceptions are logged server-side and return a generic response, and requests are rate-limited per remote IP. The API key is intentionally a small PoC authentication/authorization boundary, not a substitute for user identities and roles.

## AI-assisted development

AI was used to analyze the assignment, compare design alternatives, scaffold repetitive code, identify corner cases (for example non-finite coordinates and deterministic paging), and draft tests and documentation. AI output was treated as a proposal: domain rules, ranking, authentication boundaries, and generated changes were reviewed and verified through compilation and automated tests. No API key or production/customer data is included in prompts, source control, or tracked configuration.

## Production follow-ups

Replace the PoC API key with OAuth2/OIDC identities and role/claim policies, use a managed secret store, add persistent storage with optimistic concurrency and a spatial index, distributed rate limiting, structured telemetry/tracing, currency conversion, and a production geocoder with resilience policies. For large datasets, move filtering, distance calculation, ordering, and paging into the persistence engine; the current in-memory search is `O(n log n)` and intentionally resets when the process stops.
