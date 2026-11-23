using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortner.Data;
using UrlShortner.Data.Context;

namespace UrlShortner.Controllers
{
    [Route("api/urlshortner")]
    [ApiController]
    public class UrlShortnerController : ControllerBase
    {
        private const string ShortCodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private const int ShortCodeLength = 6;
        private const int MaxGenerationAttempts = 10;

        private readonly UrlShortnerContext _context;

        public UrlShortnerController(UrlShortnerContext context)
        {
            _context = context;
        }

        /// <summary>Creates a short code for a URL. Returns the existing one if the URL was already shortened.</summary>
        [HttpPost("shorten")]
        [ProducesResponseType<ShortUrl>(StatusCodes.Status201Created)]
        [ProducesResponseType<ShortUrl>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ShortenUrl([FromBody] UrlRequest request)
        {
            if (!IsValidUrl(request.Url))
                return InvalidUrl();

            var existing = await _context.ShortenUrls.FirstOrDefaultAsync(su => su.OriginalUrl == request.Url);
            if (existing != null)
                return Ok(existing);

            var shortUrl = new ShortUrl
            {
                OriginalUrl = request.Url,
                ShortCode = await GenerateUniqueShortCodeAsync(),
                CreatedAt = DateTime.UtcNow,
                AccessCount = 0
            };

            _context.ShortenUrls.Add(shortUrl);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOriginalUrlFromShortCode), new { shortCode = shortUrl.ShortCode }, shortUrl);
        }

        /// <summary>Looks up a short code and counts it as an access.</summary>
        [HttpGet("shorten/{shortCode}")]
        [ProducesResponseType<ShortUrl>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOriginalUrlFromShortCode(string shortCode)
        {
            var shortUrl = await FindAsync(shortCode);
            if (shortUrl == null)
                return NotFound();

            shortUrl.AccessCount += 1;
            await _context.SaveChangesAsync();
            return Ok(shortUrl);
        }

        /// <summary>Points an existing short code at a new URL.</summary>
        [HttpPut("shorten/{shortCode}")]
        [ProducesResponseType<ShortUrl>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateShortenedUrl(string shortCode, [FromBody] UrlRequest request)
        {
            if (!IsValidUrl(request.Url))
                return InvalidUrl();

            var shortUrl = await FindAsync(shortCode);
            if (shortUrl == null)
                return NotFound();

            shortUrl.OriginalUrl = request.Url;
            shortUrl.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(shortUrl);
        }

        /// <summary>Deletes a short code.</summary>
        [HttpDelete("shorten/{shortCode}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteShortenedUrl(string shortCode)
        {
            var shortUrl = await FindAsync(shortCode);
            if (shortUrl == null)
                return NotFound();

            _context.ShortenUrls.Remove(shortUrl);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Returns a short code's details, including its access count, without counting an access.</summary>
        [HttpGet("shorten/{shortCode}/stats")]
        [ProducesResponseType<ShortUrl>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatisticsForShortCode(string shortCode)
        {
            var shortUrl = await _context.ShortenUrls.AsNoTracking().SingleOrDefaultAsync(su => su.ShortCode == shortCode);
            return shortUrl == null ? NotFound() : Ok(shortUrl);
        }

        /// <summary>Redirects a short link, such as /aB3xY9, to its original URL.</summary>
        [HttpGet("/{shortCode:regex(^[[A-Za-z0-9]]+$)}")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RedirectToOriginalUrl(string shortCode)
        {
            var shortUrl = await FindAsync(shortCode);
            if (shortUrl == null)
                return NotFound();

            shortUrl.AccessCount += 1;
            await _context.SaveChangesAsync();
            return Redirect(shortUrl.OriginalUrl);
        }

        private Task<ShortUrl?> FindAsync(string shortCode) =>
            _context.ShortenUrls.SingleOrDefaultAsync(su => su.ShortCode == shortCode);

        private static bool IsValidUrl(string? url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private BadRequestObjectResult InvalidUrl() =>
            BadRequest(new { error = "Url must be an absolute http or https URL." });

        private async Task<string> GenerateUniqueShortCodeAsync()
        {
            for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                var code = RandomNumberGenerator.GetString(ShortCodeChars, ShortCodeLength);
                if (!await _context.ShortenUrls.AnyAsync(su => su.ShortCode == code))
                    return code;
            }

            throw new InvalidOperationException("Could not generate a unique short code.");
        }
    }
}
