using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System.IO;


namespace PoEFiltersBackend.Controllers
{
    [Route("api/filter")]
    [ApiController]
    public class FilterController : ControllerBase
    {
        private readonly FiltersService m_FiltersService;
        private readonly SignInManager<User> m_SignInManager;
        private readonly UserManager<User> m_UserManager;

        public FilterController(FiltersService filtersService, SignInManager<User> signInManager, UserManager<User> userManager)
        {
            m_FiltersService = filtersService;
            m_SignInManager = signInManager;
            m_UserManager = userManager;
        }

        [HttpGet()]
        public async Task<IActionResult> GetFilters()
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            var filters = await m_FiltersService.GetAsync(userId);
            return Ok(filters);
        }

        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetFilter([FromRoute] string id)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            Filter? filter = await m_FiltersService.GetAsync(userId, id);
            if (filter == null)
            {
                return NotFound();
            }
            return Ok(filter);
        }

        [HttpGet("generate/{id:length(24)}")]
        public async Task<IActionResult> GenerateFilter([FromRoute] string id)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            return Ok(await m_FiltersService.GenerateFilter(userId, id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateFilter([FromBody] CreateFilterInfo info)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            Filter filter = new()
            {
                User = userId,
                Name = info.Name,
                Game = "PoE2",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            await m_FiltersService.AddAsync(filter);
            return Ok(filter);
        }

        [HttpPatch()]
        public async Task<IActionResult> SaveFilter([FromBody] Filter filter)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (userId == null || filter.User != userId)
            {
                return Unauthorized();
            }
            filter.ModifiedAt = DateTime.UtcNow;
            filter.User = userId;
            await m_FiltersService.UpdateAsync(filter);
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFilter([FromBody] DeleteFilterInfo info)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            await m_FiltersService.RemoveAsync(userId, info.Id);
            return Ok();
        }
    }
}
