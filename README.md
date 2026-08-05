# The Hotel API

Working proof-of-concept for hotel CRUD and prompt-based hotel search, built with ASP.NET Core 9 and a pragmatic Clean Architecture structure.

## Run in Visual Studio

1. Open `TheHotelAPI.sln` in Visual Studio 2022 with the ASP.NET workload and .NET 9 SDK.
2. Set `TheHotelAPI.Api` as the startup project.
3. Select the `http` launch profile and press F5.
4. Open `/swagger` or run requests from `src/TheHotelAPI.Api/TheHotelAPI.Api.http`.

The development API key is `development-only-key` and is sent in the `X-Api-Key` header for create, update, and delete operations. Replace it with user secrets or an environment variable outside this PoC.

CLI alternative:

```powershell
dotnet run --project src/TheHotelAPI.Api
dotnet test TheHotelAPI.sln
```

## API

| Method | Route | Purpose | API key |
|---|---|---|---|
| POST | `/api/v1/hotels` | Create hotel | Yes |
| GET | `/api/v1/hotels/{id}` | Get hotel | No |
| GET | `/api/v1/hotels?page=1&pageSize=20` | List hotels | No |
| PUT | `/api/v1/hotels/{id}` | Replace hotel | Yes |
| DELETE | `/api/v1/hotels/{id}` | Delete hotel | Yes |
| POST | `/api/v1/hotel-searches` | Search hotels | No |

Search prompt examples: `hotel in Split under 150 EUR` and `Tražim hotel u Dubrovniku do 120 €`. Supported cities are Dubrovnik, Split, Zagreb, Zadar, Rijeka, Pula, and Osijek.

## Ranking

Hotels in another currency or above budget are filtered out. Distance uses the Haversine formula. Remaining hotels receive this score (lower is better):

```text
priceScore = price / budget
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

## AI-assisted development

AI was used to analyze requirements, propose architecture, scaffold repetitive code, identify edge cases, and draft tests/documentation. All generated behavior is verified by compilation and automated tests; the ranking rule and security boundary remain explicit and reviewable.

## Production follow-ups

Use OAuth2/OIDC, secret storage, rate limiting, persistent storage with a spatial index, structured telemetry, currency conversion, and a production geocoder. The in-memory store intentionally resets when the process stops.
