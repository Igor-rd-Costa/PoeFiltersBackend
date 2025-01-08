using Microsoft.AspNetCore.Http;
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
        public FilterController(FiltersService filtersService)
        {
            m_FiltersService = filtersService;
        }

        [HttpGet()]
        public async Task<IActionResult> GetFilters()
        {
            return Ok(await m_FiltersService.GetAsync());
        }

        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetFilter([FromRoute] string id)
        {
            return Ok(await m_FiltersService.GetAsync(id));
        }

        [HttpGet("generate/{id:length(24)}")]
        public async Task<IActionResult> GenerateFilter([FromRoute] string id)
        {
            return Ok(await m_FiltersService.GenerateFilter(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateFilter([FromBody] CreateFilterInfo info)
        {
            Filter filter = new()
            {
                User = null,
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
            await m_FiltersService.UpdateAsync(FilterInfoToFilter(filter));
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFilter([FromBody] DeleteFilterInfo info)
        {
            await m_FiltersService.RemoveAsync(info.Id);
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
