using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoEFiltersBackend.Models;
using System.Net;
using System.Xml.Linq;

namespace PoEFiltersBackend.Controllers
{
    [Route("api/{game}/item")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly WikiService m_WikiService;
        private readonly ItemsService m_ItemsService;
        private readonly UserManager<User> m_UserManager;
        private readonly SignInManager<User> m_SignInManager;

        public ItemController(WikiService wikiService, ItemsService itemsService, UserManager<User> userManager, SignInManager<User> signInManager) 
        { 
            m_WikiService = wikiService;
            m_ItemsService = itemsService;
            m_UserManager = userManager;
            m_SignInManager = signInManager;
        }

        [HttpGet()]
        public async Task<IActionResult> GetItems([FromRoute(Name = "game")] string gameStr, [FromQuery] string? ids, [FromQuery] string? category = null)
        {
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
            List<string>? itemIds = ids?.Trim().Split(',').ToList();
            var items = await m_ItemsService.GetItems(game, category, itemIds);
            return Ok(items);
        }

        [HttpPatch()]
        public async Task<IActionResult> UpdateItems([FromRoute(Name = "game")] string gameStr)
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
            var items = await m_WikiService.GetItems();
            await m_ItemsService.DeleteAllItems(game);
            var newItems = items.Select(i =>
            {
                return new Item()
                {
                    Name = i.Name,
                    BaseCategory = i.Category,
                    Rarity = i.Rarity
                };
            }).ToList();
            for (int i = 0; i < newItems.Count; i++)
            {
                string? categoryId = await m_ItemsService.GetBaseCategoryId(game, newItems[i].BaseCategory);
                if (categoryId == null)
                {
                    categoryId = await m_ItemsService.AddBaseCategory(game, 
                        new ItemCategory() { Name = newItems[i].BaseCategory }
                    );
                }
                newItems[i].BaseCategory = categoryId;
            }
            await m_ItemsService.AddItems(game, newItems);
            return Ok();
        }

        [HttpPatch("categories")]
        public async Task<IActionResult> AddItemCategory([FromRoute(Name = "game")] string gameStr, [FromBody] AddItemCategoryInfo info)
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
            if (!(await m_ItemsService.AddItemCategoryToItem(game, info.Id, info.CategoryId)))
            {
                return BadRequest();
            }
            return Ok();
        }
    }
}
