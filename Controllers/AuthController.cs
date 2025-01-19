using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace PoEFiltersBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly HttpClient m_Http;
        private readonly MongoDbContext m_Context;
        private readonly AuthConfig m_Config;
        private readonly EncryptionService m_EncryptionService;
        private readonly SignInManager<User> m_SignInManager;
        private readonly UserManager<User> m_UserManager;
        public AuthController(IOptions<AuthConfig> config, HttpClient http, EncryptionService encryptionService,
            UserManager<User> userManager, SignInManager<User> signInManager, MongoDbContext context)
        {
            m_Http = http;
            m_Config = config.Value;
            m_Context = context;
            m_SignInManager = signInManager;
            m_UserManager = userManager;
            m_EncryptionService = encryptionService;
        }

        [HttpGet("isLogged")]
        public IActionResult IsLogged()
        {
            return Ok(m_SignInManager.IsSignedIn(User));
        }

        [HttpGet("info")]
        public async Task<IActionResult> Info()
        {
            if (m_SignInManager.IsSignedIn(User) == false)
            {
                return Unauthorized();
            }
            User? user = await m_UserManager.GetUserAsync(User);
            return Ok(user);
        }

        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            byte[] stateBuffer = new byte[256];
            new Random().NextBytes(stateBuffer);
            string state = Convert.ToBase64String(stateBuffer)
                .Replace("+", "0")
                .Replace("/", "1")
                .Replace("=", "2")
                .Replace(" ", "-");
            HttpContext.Session.SetString("OAuthState", state);
            return Redirect($"https://github.com/login/oauth/authorize?client_id={m_Config.ClientId}&redirect_uri={m_Config.RedirectUrl}&state={state}");
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Login([FromQuery] string code, [FromQuery] string state)
        {
            string? sessionState = HttpContext.Session.GetString("OAuthState");
            if (sessionState != state)
            {
                return Redirect("http://localhost:4200/?error=oauthValidationFailed");
            }
            HttpContext.Session.Remove("OAuthState");
            var token = await ExchangeCodeForTokenGithub(code);
            if (token == null)
            {
                return Redirect("http://localhost:4200/?error=oauthValidationFailed");
            }

            var userInfo = await GetUserInfoGithub(token);
            if (userInfo == null)
            {
                return Redirect("http://localhost:4200/?error=oauthValidationFailed");
            }

            var existingUser = await m_Context.Users.Find(x => x.ProviderId == userInfo.ProviderId).FirstOrDefaultAsync();
            if (existingUser == null)
            {
                await m_Context.Users.InsertOneAsync(userInfo);
            }
            User user = existingUser ?? userInfo;
            if (false) // Store tokens if the app uses the provider's API
            {
                token.UserId = user.Id;
                token.AccessToken = m_EncryptionService.Encrypt(token.AccessToken);
                token.RefreshToken = m_EncryptionService.Encrypt(token.RefreshToken);
                await RegisterProviderToken(token);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("amr", "OAuth")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await m_SignInManager.SignInWithClaimsAsync(user, false, identity.Claims);
            return Redirect("http://localhost:4200");
        }

        private async Task<ProviderToken?> ExchangeCodeForTokenGithub(string code)
        {
            using var client = new HttpClient();
            var tokenRequest = new Dictionary<string, string> {
                { "client_id", m_Config.ClientId },
                { "client_secret", m_Config.ClientSecret },
                { "code", code },
                { "redirect_uri", m_Config.RedirectUrl }
            };

            var response = await client.PostAsync("https://github.com/login/oauth/access_token",
                new FormUrlEncodedContent(tokenRequest));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var queryParams = System.Web.HttpUtility.ParseQueryString(content);

            var accessToken = queryParams["access_token"];
            var refreshToken = queryParams["refresh_token"];
            var expiresIn = queryParams["expires_in"];
            if (accessToken != null && refreshToken != null && expiresIn != null)
            {
                int expireSeconds;
                if (int.TryParse(expiresIn, out expireSeconds) == false)
                {
                    return null;
                }
                ProviderToken token = new()
                {
                    Provider = "Github",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiration = DateTime.UtcNow.AddSeconds(expireSeconds)
                };
                return token;
            }
            return null;
        }

        private async Task<User?> GetUserInfoGithub(ProviderToken token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PoeFilters", "1.0"));

            var response = await client.GetAsync("https://api.github.com/user");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var userData = await response.Content.ReadAsStringAsync();
            var userJson = JsonDocument.Parse(userData);

            return new User
            {
                ProviderId = userJson.RootElement.GetProperty("id").GetInt32(),
                Username = userJson.RootElement.GetProperty("login").GetString()!,
            };
        }

        private async Task RegisterProviderToken(ProviderToken token)
        {
            var filter = Builders<ProviderToken>.Filter.Where(
                    t => t.UserId == token.UserId && t.Provider == token.Provider
                );
            var existingToken = (await m_Context.UserTokens.FindAsync(filter)).FirstOrDefault();
            if (existingToken != null)
            {
                token.Id = existingToken.Id;
                await m_Context.UserTokens.ReplaceOneAsync(filter, token);
            }
            else
            {
                await m_Context.UserTokens.InsertOneAsync(token);
            }
        }
    }
}
