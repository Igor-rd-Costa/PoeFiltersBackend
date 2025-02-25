using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace PoEFiltersBackend.Controllers
{
    [Route("api/{game}/item-category")]
    [ApiController]
    public class ItemCategoryController : ControllerBase
    {
        private readonly ItemsService m_ItemsService;
        private readonly UserManager<User> m_UserManager;
        private readonly SignInManager<User> m_SignInManager;
        private readonly WikiService m_WikiService;

        public ItemCategoryController(ItemsService itemsService, UserManager<User> userManager, 
            SignInManager<User> signInManager, WikiService wikiService)
        {
            m_ItemsService = itemsService;
            m_UserManager = userManager;
            m_SignInManager = signInManager;
            m_WikiService = wikiService;
        }

        [HttpGet("base")]
        public async Task<IActionResult> GetBaseItemGategories([FromRoute(Name = "game")] string gameStr, [FromQuery] string? id)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || user.IsAdmin == false)
            {
                return Unauthorized();
            }
            gameStr = gameStr.ToLower();
            if (gameStr != "poe1" && gameStr != "poe" && gameStr != "poe2")
            {
                return BadRequest($"Invalid route parameter game -> {gameStr}");
            }
            Game game = Game.POE1;
            if (gameStr == "poe2")
            {
                game = Game.POE2;
            }
            if (id == null)
            {
                return Ok(await m_ItemsService.GetBaseItemCategories(game));
            }
            ItemCategory? category = await m_ItemsService.GetBaseItemCategory(game, id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        [HttpGet("custom")]
        public async Task<IActionResult> GetItemGategories([FromRoute(Name = "game")] string gameStr, [FromQuery] string? id)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || user.IsAdmin == false)
            {
                return Unauthorized();
            }
            gameStr = gameStr.ToLower();
            if (gameStr != "poe1" && gameStr != "poe" && gameStr != "poe2")
            {
                return BadRequest($"Invalid route parameter game -> {gameStr}");
            }
            Game game = Game.POE1;
            if (gameStr == "poe2")
            {
                game = Game.POE2;
            }
            if (id == null)
            {
                return Ok(await m_ItemsService.GetItemCategories(game));
            }
            ItemCategory? category = await m_ItemsService.GetItemCategory(game, id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        [HttpPost("custom")]
        public async Task<IActionResult> CreateItemCategory([FromRoute(Name = "game")] string gameStr, [FromBody] AddBaseItemCategoryInfo info)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || user.IsAdmin == false)
            {
                return Unauthorized();
            }
            gameStr = gameStr.ToLower();
            if (gameStr != "poe1" && gameStr != "poe" && gameStr != "poe2")
            {
                return BadRequest($"Invalid route parameter game -> {gameStr}");
            }
            Game game = Game.POE1;
            if (gameStr == "poe2")
            {
                game = Game.POE2;
            }
            string? id = await m_ItemsService.AddItemCategory(game, info.Name);
            if (id == null)
            {
                return BadRequest();
            }
            return Ok(id);
        }

        [HttpPatch("base")]
        public async Task<IActionResult> UpdateBaseCategories([FromRoute(Name = "game")] string gameStr)
        {
            User? user = await m_UserManager.GetUserAsync(User);
            if (user == null || !user.IsAdmin)
            {
                return Unauthorized();
            }
            gameStr = gameStr.ToLower();
            if (gameStr != "poe1" && gameStr != "poe" && gameStr != "poe2")
            {
                return BadRequest($"Invalid route parameter game -> {gameStr}");
            }
            Game game = Game.POE1;
            if (gameStr == "poe2")
            {
                game = Game.POE2;
            }
            if (game == Game.POE1)
            {
                return StatusCode(501);
            }
            var categories = await m_WikiService.GetCategories();
            var dbCategories = await m_ItemsService.GetBaseItemCategories(game);
            foreach (ItemCategory category in dbCategories)
            {
                categories.Remove(category.Name);
            }
            List<ItemCategory> newCategories = categories.Select(c => new ItemCategory()
            {
                Name = c,
            }).ToList();
            if (newCategories.Count > 0)
            {
                await m_ItemsService.AddBaseCategories(game, newCategories);
                dbCategories.AddRange(newCategories);
            }
            return Ok(dbCategories);
        }
    }
}
