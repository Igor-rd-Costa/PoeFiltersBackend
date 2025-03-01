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

        private FilterRule FilterRuleInfoToFilterRule(FilterRuleInfo ruleInfo)
        {
            return new FilterRule()
            {
                Id = ruleInfo.Id,
                Type = FilterRuleItemType.RULE,
                Name = ruleInfo.Name,
                ImgSrc = ruleInfo.ImgSrc,
                State = ruleInfo.State,
                Style = ruleInfo.Style,
                Items = ruleInfo.Items,
                AllowedCategories = ruleInfo.AllowedCategories,
            };
        }

        private FilterRuleBlock FilterRuleBlockInfoToFilterRuleBlock(FilterRuleBlockInfo ruleBlockInfo)
        {
            var ruleBlock = new FilterRuleBlock()
            {
                Id = ruleBlockInfo.Id,
                Type = FilterRuleItemType.RULE_BLOCK,
                Name = ruleBlockInfo.Name,
                AllowUserCreatedRules = ruleBlockInfo.AllowUserCreatedRules,
                Rules = []
            };
            ruleBlockInfo.Rules.Sort(ItemPositionSortFn);
            for (int i = 0; i < ruleBlockInfo.Rules.Count; i++)
            {
                ruleBlock.Rules.Add(FilterRuleInfoToFilterRule(ruleBlockInfo.Rules[i]));
            }
            return ruleBlock;
        }

        private IFilterRuleItem IFilterRuleItemInfoToIFilterRuleItem(IFilterRuleItemInfo ruleItemInfo)
        {
            return (ruleItemInfo.Type == FilterRuleItemType.RULE)
                ? FilterRuleInfoToFilterRule((FilterRuleInfo)ruleItemInfo)
                : FilterRuleBlockInfoToFilterRuleBlock((FilterRuleBlockInfo)ruleItemInfo);
        }

        private FilterBlock FilterBlockInfoToFilterBlock(FilterBlockInfo blockInfo)
        {
            FilterBlock block = new FilterBlock()
            {
                Id = blockInfo.Id,
                Name = blockInfo.Name,
                ImgSrc = blockInfo.ImgSrc,
                AllowedCategories = blockInfo.AllowedCategories,
                RulesType = blockInfo.RulesType,
                Rules = []
            };
            blockInfo.Rules.Sort(ItemPositionSortFn);
            for (int i = 0; i < blockInfo.Rules.Count; ++i)
            {
                block.Rules.Add(IFilterRuleItemInfoToIFilterRuleItem(blockInfo.Rules[i]));
            }
            return block;
        }

        private FilterSection FilterSectionInfoToFilterSection(FilterSectionInfo sectionInfo)
        {
            FilterSection section = new FilterSection()
            {
                Id = sectionInfo.Id,
                Name = sectionInfo.Name,
                Blocks = []
            };
            sectionInfo.Blocks.Sort(ItemPositionSortFn);
            for (int i = 0; i < sectionInfo.Blocks.Count; i++)
            {
                section.Blocks.Add(FilterBlockInfoToFilterBlock(sectionInfo.Blocks[i]));
            }
            return section;
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
                filter.Sections.Add(FilterSectionInfoToFilterSection(filterInfo.Sections[i]));
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
