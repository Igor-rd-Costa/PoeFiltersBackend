using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;


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
            return Ok(await m_FiltersService.GetAsync(userId, id));
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
        public async Task<IActionResult> SaveFilter([FromBody] FilterInfo filter)
        {
            string? userId = m_UserManager.GetUserId(User);
            if (!m_SignInManager.IsSignedIn(User) || userId == null)
            {
                return Unauthorized();
            }
            var f = FilterInfoToFilter(filter);
            f.ModifiedAt = DateTime.UtcNow;
            f.User = userId;
            await m_FiltersService.UpdateAsync(f);
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

        private Filter FilterInfoToFilter(FilterInfo filterInfo)
        {
            Filter filter = new Filter()
            {
                Id = filterInfo.Id,
                User = filterInfo.User,
                Name = filterInfo.Name,
                CreatedAt = filterInfo.CreatedAt,
                ModifiedAt = filterInfo.ModifiedAt,
                Game = filterInfo.Game,
                Sections = []
            };
            filterInfo.Sections.Sort(ItemPositionSortFn);
            for (int i = 0; i < filterInfo.Sections.Count; i++)
            {
                var section = filterInfo.Sections[i];
                filter.Sections.Add(new FilterSection()
                {
                    Id = section.Id,
                    Name = section.Name,
                    Blocks = []
                });
                section.Blocks.Sort(ItemPositionSortFn);
                for (int j = 0; j < section.Blocks.Count; j++)
                {
                    var block = section.Blocks[j];
                    filter.Sections[i].Blocks.Add(new FilterBlock()
                    {
                        Id = block.Id,
                        Name = block.Name,
                        ImgSrc = block.ImgSrc,
                        AllowedCategories = block.AllowedCategories,
                        Rules = []
                    });
                    block.Rules.Sort(ItemPositionSortFn);
                    for (int k = 0; k < block.Rules.Count; k++)
                    {
                        var rule = block.Rules[k];
                        filter.Sections[i].Blocks[j].Rules.Add(new FilterRule()
                        {
                            Id = rule.Id,
                            Name = rule.Name,
                            ImgSrc = rule.ImgSrc,
                            State = rule.State,
                            AllowedCategories = rule.AllowedCategories,
                            Items = rule.Items,
                            Style = rule.Style
                        });
                    }
                }
            }
            return filter;
        }

        private int ItemPositionSortFn<T>(T a, T b) where T : IPositionable
        {
            if (a.Position > b.Position)
            {
                return 1;
            }
            return -1;
        }
    }
}
