using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PoEFiltersBackend.Controllers
{
    [Route("api/{game}/filter-structure")]
    [ApiController]
    public class FilterStructureController : ControllerBase
    {
        private readonly UserManager<User> m_UserManager;
        private readonly FilterStructureService m_FilterStructureService;
        private readonly FilterDiffService m_FilterStructureDiffService;
        private readonly DefaultFilterService m_DefaultFilterService;
        private readonly FilterDiffService m_FilterDiffService;
        public FilterStructureController(UserManager<User> userManager, FilterStructureService filterStructureService,
            FilterDiffService filterStructureDiffService, DefaultFilterService defaultFilterService, FilterDiffService filterDiffService) 
        {
            m_UserManager = userManager;
            m_FilterStructureService = filterStructureService;
            m_FilterStructureDiffService = filterStructureDiffService;
            m_DefaultFilterService = defaultFilterService;
            m_FilterDiffService = filterDiffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFilterStructure([FromRoute(Name = "game")] string gameStr)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || user.IsAdmin == false)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            FilterStructure? structure = await m_FilterStructureService.GetStructureAsync(game);
            if (structure == null)
            {
                return Ok(await m_FilterStructureService.AddAsync(game));
            }
            return Ok(structure);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromRoute(Name = "game")] string gameStr, [FromBody] FilterStructure structure)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || user.IsAdmin == false)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            FilterStructure? currentStructure = await m_FilterStructureService.GetStructureAsync(game);
            if (currentStructure != null)
            {
                structure.StructureVersion = currentStructure.StructureVersion + 1;
                FilterStructureDiff diff = await m_FilterStructureDiffService.MakeDiffAsync(currentStructure, structure);
                if (diff.SectionChanges.Changed != null || diff.SectionChanges.Added != null || diff.SectionChanges.Removed != null)
                {
                    await m_FilterStructureDiffService.SaveAsync(game, diff);
                }
            }
            else
            {
                structure.StructureVersion = 0;
            }
            await m_FilterStructureService.ReplaceStructureAsync(game, structure);
            return Ok();
        }


        [HttpDelete]
        public async Task<IActionResult> Reset([FromRoute(Name = "game")] string gameStr)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || user.IsAdmin == false)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            await m_FilterStructureService.Reset(game);
            return Ok();
        }

        [HttpPatch]
        public async Task<IActionResult> Test([FromRoute(Name = "game")] string gameStr)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || user.IsAdmin == false)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }

            Guid sectionOneId = Guid.NewGuid();
            Guid unnamedSectionId = Guid.NewGuid();
            Guid currencyBlockId = Guid.NewGuid();
            Guid toDeleteId = Guid.NewGuid();
            Guid unnamedBlockId = Guid.NewGuid();
            Guid unnamedBlockTwoId = Guid.NewGuid();

            Guid mirrorRuleId = Guid.NewGuid();

            DefaultFilter defaultFilter = new()
            {
                StructureVersion = 0,
                Sections = [
                    new() {
                        Id = sectionOneId,
                        Name = "Unnamed Section"
                    }
                ]
            };

            FilterStructureDiff diffOne = new(0) 
            {
                SectionChanges = new()
                {
                    Added = null,
                    Removed = null,
                    Changed = [
                        new (sectionOneId)
                        {
                            Name = "General",
                            BlockChanges = new()
                            {
                                Added = [
                                    new()
                                    {
                                        Id = currencyBlockId,
                                        Name = "Currency",
                                        Position = 0,
                                        ImgSrc = "/poe2_exalted_orb.png",
                                        RulesType = FilterRuleType.RULE_MINIMAL,
                                        AllowedCategories = ["67ca2ed20c783aa0fefeb539"],
                                        AllowUserCreatedRules = true,
                                        Rules = []
                                    },
                                    new() {
                                        Id = toDeleteId,
                                        Name = "ToDelete",
                                        Position = 1,
                                        RulesType = FilterRuleType.RULE_MINIMAL,
                                    },
                                    new() {
                                        Id = unnamedBlockId,
                                        Name = "Unnamed Block",
                                        Position= 2,
                                    }
                                ]
                            }
                        }
                    ]
                }
            };

            FilterStructureDiff diffTwo = new(1)
            {
                SectionChanges = new()
                {
                    Added = null,
                    Removed = null,
                    Changed = [
                        new(sectionOneId)
                        {
                            Name = "GeneralTwo",
                            BlockChanges = new()
                            {
                                Added = null,
                                Removed = [toDeleteId.ToString()],
                                Changed = [
                                    new(unnamedBlockId)
                                    {
                                        Name = "New Name",
                                        AllowUserCreatedRules = true
                                    }
                                ]
                            }
                        }
                    ]
                }
            };

            List<FilterStructureDiff> structureDiff = [
                diffOne,
                diffTwo
            ];
            FilterStructureDiff diff = m_FilterStructureDiffService.MergeDiffs(structureDiff);

            await m_FilterDiffService.ApplyDiffAsync(defaultFilter, diff);
            return Ok();
        }
    }
}
