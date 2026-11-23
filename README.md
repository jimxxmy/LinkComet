<a id="readme-top"></a>

<div align="center">
  <img src="UrlShortner/Assets/Images/Logo/icon-512.png" alt="LinkComet logo" width="80" height="80">

  <h3>LinkComet</h3>

  <p>A small URL-shortening REST API built with ASP.NET Core and Entity Framework Core.</p>

  [![Build and test](https://github.com/jimxxmy/LinkComet/actions/workflows/build.yml/badge.svg)](https://github.com/jimxxmy/LinkComet/actions/workflows/build.yml)
  [![License: Apache 2.0](https://img.shields.io/github/license/jimxxmy/LinkComet)](LICENSE.txt)
</div>

## About

LinkComet turns long URLs into short codes. You can create, look up, update and delete short URLs, follow a short link to be redirected, and see how many times each one has been used.

It's my solution to the [URL Shortening Service](https://roadmap.sh/projects/url-shortening-service) project on roadmap.sh.

### Built with

- [ASP.NET Core 10](https://dotnet.microsoft.com/apps/aspnet) Web API
- [Entity Framework Core 10](https://learn.microsoft.com/ef/core/) with SQL Server
- [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) for Swagger UI
- xUnit and `WebApplicationFactory` for tests

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server. LocalDB, which comes with Visual Studio, works out of the box, and any other SQL Server instance works if you change the connection string.
- The EF Core CLI: `dotnet tool install --global dotnet-ef`

### Run it locally

```sh
git clone https://github.com/jimxxmy/LinkComet.git
cd LinkComet/UrlShortner

# Create the database
dotnet ef database update

# Start the API
dotnet run
```

The API listens on `http://localhost:5121`, and Swagger UI is at `http://localhost:5121/swagger`.

To use a different database, change `ConnectionStrings:DefaultConnection` in `UrlShortner/appsettings.Development.json`.

### Run the tests

From the repository root:

```sh
dotnet test
```

The tests use an in-memory database, so they don't need SQL Server.

## API

All endpoints are under `/api/urlshortner`, except the redirect.

| Method | Path | What it does |
| --- | --- | --- |
| `POST` | `/api/urlshortner/shorten` | Create a short code. Returns `201`, or `200` with the existing code if the URL was already shortened. |
| `GET` | `/api/urlshortner/shorten/{code}` | Get the original URL and count an access. |
| `PUT` | `/api/urlshortner/shorten/{code}` | Point the code at a new URL. |
| `DELETE` | `/api/urlshortner/shorten/{code}` | Delete the code. Returns `204`. |
| `GET` | `/api/urlshortner/shorten/{code}/stats` | Get the details and access count without counting an access. |
| `GET` | `/{code}` | Redirect (`302`) to the original URL and count an access. |

URLs must be absolute `http` or `https` URLs, or the API returns `400`. An unknown code returns `404`.

### Example

```sh
curl -X POST http://localhost:5121/api/urlshortner/shorten \
  -H "Content-Type: application/json" \
  -d '{ "url": "https://roadmap.sh/projects/url-shortening-service" }'
```

```json
{
  "id": 1,
  "originalUrl": "https://roadmap.sh/projects/url-shortening-service",
  "shortCode": "aB3xY9",
  "createdAt": "2026-11-23T17:00:00Z",
  "updatedAt": null,
  "accessCount": 0
}
```

Then open `http://localhost:5121/aB3xY9` to be redirected.

`UrlShortner/UrlShortner.http` has a ready-made request for every endpoint, which you can run from Visual Studio, Rider or VS Code with the REST Client extension.

## Roadmap

- [ ] Custom short codes chosen by the user
- [ ] Expiry dates for short links
- [ ] Rate limiting on the create endpoint
- [ ] A small web front end

See the [open issues](https://github.com/jimxxmy/LinkComet/issues) for more.

## Contributing

Issues and pull requests are welcome. Please run `dotnet test` before opening a PR.

## License

Distributed under the Apache License 2.0. See [`LICENSE.txt`](LICENSE.txt).

## Contact

Jimit Vaghela on [LinkedIn](https://linkedin.com/in/jimit-vaghela)

Project link: [github.com/jimxxmy/LinkComet](https://github.com/jimxxmy/LinkComet)

<p align="right">(<a href="#readme-top">back to top</a>)</p>
