using Microsoft.AspNetCore.Mvc;
using System.Net;
using UrlShortner.Data;
using UrlShortner.Data.Context;

namespace UrlShortner.Controllers
{
    [Route("api/urlshortner")]
    [ApiController]
    public class UrlShortnerController : ControllerBase
    {
        private readonly UrlShortnerContext _context;
        public UrlShortnerController(UrlShortnerContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Route("shorten")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public IActionResult ShortenUrl([FromBody] string url)
        {
            var isExists = _context.ShortenUrls.Any(su => su.OriginalUrl.Equals(url));
            if (isExists)
                return BadRequest();

            ShortUrl shortUrl = new()
            {
                OriginalUrl = url,
                ShortCode = GenerateShortCode(),
                CreatedAt = DateTime.UtcNow,
                AccessCount = 0
            };

            _context.ShortenUrls.Add(shortUrl);
            _context.SaveChanges();
            return StatusCode(201, shortUrl);
        }

        [HttpGet]
        [Route("shorten/{shortCode}")]
        [ProducesResponseType<int>(StatusCodes.Status200OK)]
        public IActionResult GetOriginalUrlFromShortCode(string shortCode)
        {
            var shortUrl = _context.ShortenUrls.SingleOrDefault(su => su.ShortCode == shortCode);
            if (shortUrl == null)
                return NotFound();
            shortUrl.AccessCount += 1;
            _context.SaveChanges();
            return Ok(shortUrl);
        }

        [HttpPut]
        [Route("shorten/{shortCode}")]
        [ProducesResponseType<int>(StatusCodes.Status200OK)]
        public IActionResult UpdateShortenedUrl(string shortCode, string url)
        {
            var isExists = _context.ShortenUrls.Any(su => su.ShortCode == shortCode);
            if (!isExists) return NotFound();

            var existingShortUrl = _context.ShortenUrls.Where(su => su.ShortCode == shortCode)
                .Single();

            existingShortUrl.OriginalUrl = url;
            existingShortUrl.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return Ok(existingShortUrl);
        }

        [HttpDelete]
        [Route("shorten/{shortCode}")]
        [ProducesResponseType<int>(StatusCodes.Status204NoContent)]
        public IActionResult DeleteShortenedUrl(string shortCode)
        {
            var isExists = _context.ShortenUrls.Any(su => su.ShortCode == shortCode);
            if (!isExists) return NotFound();

            _context.ShortenUrls.Remove(_context.ShortenUrls.Where(su => su.ShortCode == shortCode).Single());
            return NoContent();
        }

        [HttpGet]
        [Route("shorten/{shortCode}/stats")]
        [ProducesResponseType<int>(StatusCodes.Status200OK)]
        public IActionResult GetStatisticsForShortCode(string shortCode)
        {
            var isExists = _context.ShortenUrls.Any(su => su.ShortCode == shortCode);
            if (!isExists) return NotFound();

            var shortUrl = _context.ShortenUrls.Where(su => su.ShortCode == shortCode).Single();
            return Ok(shortUrl); 
        }

        private static string GenerateShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 5)
                                        .Select(s => s[new Random().Next(s.Length)]).ToArray());
        }
    }
}
