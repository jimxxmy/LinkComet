using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlShortner.Data;
using UrlShortner.Data.Context;

namespace UrlShortner.Tests;

public class UrlShortnerApiTests : IClassFixture<UrlShortnerApiTests.ApiFactory>
{
    private const string BaseRoute = "/api/urlshortner/shorten";
    private readonly HttpClient _client;

    public UrlShortnerApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Shorten_ReturnsCreatedWithShortCode()
    {
        var response = await _client.PostAsJsonAsync(BaseRoute, new UrlRequest(UniqueUrl()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShortUrl>();
        Assert.NotNull(body);
        Assert.Matches("^[A-Za-z0-9]{6}$", body.ShortCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Shorten_SameUrlTwice_ReturnsExistingCode()
    {
        var url = UniqueUrl();
        var first = await CreateAsync(url);

        var response = await _client.PostAsJsonAsync(BaseRoute, new UrlRequest(url));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var second = await response.Content.ReadFromJsonAsync<ShortUrl>();
        Assert.Equal(first.ShortCode, second!.ShortCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("/relative/path")]
    public async Task Shorten_InvalidUrl_ReturnsBadRequest(string url)
    {
        var response = await _client.PostAsJsonAsync(BaseRoute, new UrlRequest(url));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_IncrementsAccessCount_StatsDoesNot()
    {
        var created = await CreateAsync(UniqueUrl());

        await _client.GetAsync($"{BaseRoute}/{created.ShortCode}");
        await _client.GetAsync($"{BaseRoute}/{created.ShortCode}");
        var stats = await _client.GetFromJsonAsync<ShortUrl>($"{BaseRoute}/{created.ShortCode}/stats");
        var statsAgain = await _client.GetFromJsonAsync<ShortUrl>($"{BaseRoute}/{created.ShortCode}/stats");

        Assert.Equal(2, stats!.AccessCount);
        Assert.Equal(2, statsAgain!.AccessCount);
    }

    [Fact]
    public async Task Update_ChangesOriginalUrl()
    {
        var created = await CreateAsync(UniqueUrl());
        var newUrl = UniqueUrl();

        var response = await _client.PutAsJsonAsync($"{BaseRoute}/{created.ShortCode}", new UrlRequest(newUrl));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ShortUrl>();
        Assert.Equal(newUrl, updated!.OriginalUrl);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task Update_InvalidUrl_ReturnsBadRequest()
    {
        var created = await CreateAsync(UniqueUrl());

        var response = await _client.PutAsJsonAsync($"{BaseRoute}/{created.ShortCode}", new UrlRequest("nope"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesShortUrl()
    {
        var created = await CreateAsync(UniqueUrl());

        var delete = await _client.DeleteAsync($"{BaseRoute}/{created.ShortCode}");
        var after = await _client.GetAsync($"{BaseRoute}/{created.ShortCode}/stats");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Redirect_SendsToOriginalUrlAndCountsAccess()
    {
        var url = UniqueUrl();
        var created = await CreateAsync(url);

        var response = await _client.GetAsync($"/{created.ShortCode}");
        var stats = await _client.GetFromJsonAsync<ShortUrl>($"{BaseRoute}/{created.ShortCode}/stats");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(url, response.Headers.Location!.ToString());
        Assert.Equal(1, stats!.AccessCount);
    }

    [Theory]
    [InlineData("GET", "/api/urlshortner/shorten/missing")]
    [InlineData("GET", "/api/urlshortner/shorten/missing/stats")]
    [InlineData("DELETE", "/api/urlshortner/shorten/missing")]
    [InlineData("GET", "/missing")]
    public async Task UnknownShortCode_ReturnsNotFound(string method, string path)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ShortUrl> CreateAsync(string url)
    {
        var response = await _client.PostAsJsonAsync(BaseRoute, new UrlRequest(url));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShortUrl>())!;
    }

    private static string UniqueUrl() => $"https://example.com/{Guid.NewGuid():N}";

    public class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"UrlShortnerTests-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<UrlShortnerContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<UrlShortnerContext>>();
                services.AddDbContext<UrlShortnerContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
