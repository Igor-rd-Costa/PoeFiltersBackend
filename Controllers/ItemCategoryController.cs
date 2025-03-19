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
        private readonly WikiService m_WikiService;

        public ItemCategoryController(ItemsService itemsService, UserManager<User> userManager, 
            WikiService wikiService)
        {
            m_ItemsService = itemsService;
            m_UserManager = userManager;
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
            var newCategories = await m_WikiService.GetCategories();
            var oldCategories = await m_ItemsService.GetBaseItemCategories(game);
            List<ItemCategory> addCategories = [];
            List<string> removeCategories = [];
            for (int i = 0; i < newCategories.Count; i++)
            {
                string newCategoryName = newCategories[i];
                bool found = false;
                for (int j = 0; j < oldCategories.Count; j++)
                {
                    if (newCategoryName.Equals(oldCategories[j].Name))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    addCategories.Add(new ItemCategory() { Name = newCategoryName });
                }
            }
            for (int i = 0; i < oldCategories.Count; i++)
            {
                ItemCategory oldCategory = oldCategories[i];
                bool found = false;
                for (int j = 0; j < newCategories.Count; j++)
                {
                    if (oldCategory.Name.Equals(newCategories[j]))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    removeCategories.Add(oldCategory.Id.ToString());
                    oldCategories.RemoveAt(i);
                    i--;
                }
            }
            if (removeCategories.Count > 0)
            {
                await m_ItemsService.DeleteBaseCategories(game, removeCategories);
            }
            if (addCategories.Count > 0)
            {
                await m_ItemsService.AddBaseCategories(game, addCategories);
                oldCategories.AddRange(addCategories);
            }
            return Ok(oldCategories);
        }
    }
}
