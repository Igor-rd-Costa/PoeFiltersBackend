using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System.IO;


namespace PoEFiltersBackend.Controllers
{
    [Route("api/{game}/filter")]
    [ApiController]
    public class FilterController : ControllerBase
    {
        private readonly FiltersService m_FiltersService;
        private readonly SignInManager<User> m_SignInManager;
        private readonly UserManager<User> m_UserManager;
        private readonly DefaultFilterService m_DefaultFilterService;

        public FilterController(FiltersService filtersService, SignInManager<User> signInManager, 
            UserManager<User> userManager, DefaultFilterService defaultFilterService)
        {
            m_FiltersService = filtersService;
            m_SignInManager = signInManager;
            m_UserManager = userManager;
            m_DefaultFilterService = defaultFilterService;
        }

        [HttpGet()]
        public async Task<IActionResult> GetFilters([FromRoute(Name = "game")] string gameStr)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            var filters = await m_FiltersService.GetAsync(game, userId);
            return Ok(filters);
        }

        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetFilter([FromRoute(Name = "game")] string gameStr, [FromRoute] string id)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            Filter? filter = await m_FiltersService.GetAsync(game, userId, id);
            if (filter == null)
            {
                return NotFound();
            }
            return Ok(filter);
        }

        [HttpGet("generate/{id:length(24)}")]
        public async Task<IActionResult> GenerateFilter([FromRoute(Name = "game")] string gameStr, [FromRoute] string id)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            return Ok(await m_FiltersService.GenerateFilter(game, userId, id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateFilter([FromRoute(Name = "game")] string gameStr, [FromBody] CreateFilterInfo info)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }

            DefaultFilter? defaultFilter = await m_DefaultFilterService.GetAsync(game, info.Strictness);
            if (defaultFilter == null)
            {
                return BadRequest();
            }
            Filter filter = new()
            {
                User = userId,
                Name = info.Name,
                Strictness = info.Strictness,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                DefaultVersion = defaultFilter.DefaultVersion,
                Sections = defaultFilter.Sections
            };
            await m_FiltersService.AddAsync(game, filter);
            return Ok(filter);
        }

        [HttpPatch()]
        public async Task<IActionResult> SaveFilter([FromRoute(Name = "game")] string gameStr, [FromBody] Filter filter)
        {
            string? userId = m_UserManager.GetUserId(User);
            //TODO checking Filter's ownership from user provided filter object. Not safe .-.
            if (userId == null || filter.User != userId)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            filter.ModifiedAt = DateTime.UtcNow;
            filter.User = userId;
            await m_FiltersService.UpdateAsync(game, filter);
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFilter([FromRoute(Name = "game")] string gameStr, [FromBody] DeleteFilterInfo info)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            await m_FiltersService.RemoveAsync(game, userId, info.Id);
            return Ok();
        }
    }
}
