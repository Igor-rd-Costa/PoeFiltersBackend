using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PoEFiltersBackend.Controllers
{
    [Route("api/{game}/default-filters")]
    [ApiController]
    public class DefaultFiltersController : ControllerBase
    {
        private readonly UserManager<User> m_UserManager;
        private readonly DefaultFilterService m_DefaultFilterService;
        private readonly FilterStructureService m_FilterStructureService;
        private readonly FilterDiffService m_FilterDiffService;

        public DefaultFiltersController(UserManager<User> userManager, DefaultFilterService defaultFilterService,
            FilterStructureService filterStructureService, FilterDiffService filterDiffService)
        {
            m_UserManager = userManager;
            m_DefaultFilterService = defaultFilterService;
            m_FilterStructureService = filterStructureService;
            m_FilterDiffService = filterDiffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDefaultFilter([FromRoute(Name = "game")] string gameStr, [FromQuery] FilterStrictness strictness)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || !user.IsAdmin)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            DefaultFilter? df = await m_DefaultFilterService.GetAsync(game, strictness);
            if (df == null)
            {
                df = await m_DefaultFilterService.Add(game, strictness);
            }
            uint currentVersion = await m_FilterStructureService.GetStructureVersionAsync(game);
            if (currentVersion != df.StructureVersion)
            {
                FilterStructureDiff? diff = await m_FilterDiffService.GetStructureDiffAsync(game, df.StructureVersion);
                if (diff != null)
                {
                    await m_FilterDiffService.ApplyDiffAsync(df, diff);
                    df.StructureVersion = diff.Version;
                    await m_DefaultFilterService.SaveAsync(game, df);
                }
            }
            return Ok(df);
        }

        [HttpGet("version")]
        public async Task<IActionResult> GetDefaultFilterVersion([FromRoute(Name = "game")] string gameStr, [FromQuery] FilterStrictness strictness)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || !user.IsAdmin)
            {
                return Unauthorized();
            }
            Game game;
            if (!FilterHelpers.ParseGameString(gameStr, out game))
            {
                return BadRequest();
            }
            uint version = await m_DefaultFilterService.GetVersionAsync(game, strictness);
            return Ok(version);
        }

        [HttpPatch]
        public async Task<IActionResult> SaveDefaultFilter([FromRoute(Name = "game")] string gameStr)
        {
            return Ok();
        }
    }
}
