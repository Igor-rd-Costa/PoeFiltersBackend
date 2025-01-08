using Microsoft.AspNetCore.Http;
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

        public ItemController(WikiService wikiService, ItemsService itemsService) 
        { 
            m_WikiService = wikiService;
            m_ItemsService = itemsService;
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
            for (int i = 0; i < items.Count; i++)
            {
                string categoryId = await m_ItemsService.GetCategoryId(game, items[i].Category) ?? ""; 
                items[i].Category = categoryId; 
            }
            var newItems = items.Where(i => i.Category != "").Select(i => new Item()
            {
                Name = i.Name,
                Category = i.Category,
            }).ToList();
            await m_ItemsService.AddItems(game, newItems);
            return Ok();
        }

        [HttpGet("category")]
        public async Task<IActionResult> GetCategories([FromRoute(Name = "game")] string gameStr, [FromQuery] bool includeIgnored = true)
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
            var categories = await m_ItemsService.GetCategories(game, includeIgnored);
            return Ok(categories);
        }

        [HttpPatch("category")]
        public async Task<IActionResult> UpdateCategory([FromRoute(Name = "game")] string gameStr, [FromBody] ItemCategory category)
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
            if (game == Game.POE1)
            {
                return StatusCode(501);
            }
            await m_ItemsService.UpdateCategory(game, category);
            return Ok();
        }

        [HttpPatch("category/update")]
        public async Task<IActionResult> UpdateCategories([FromRoute(Name = "game")] string gameStr)
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
            if (game == Game.POE1)
            {
                return StatusCode(501);
            }
            var categories = await m_WikiService.GetCategories();
            var dbCategories = await m_ItemsService.GetCategories(game);
            foreach (ItemCategory category in dbCategories)
            {
                categories.Remove(category.Name);
            }
            List<ItemCategory> newCategories = categories.Select(c => new ItemCategory()
            {
                Name = c,
                IgnoreItems = false,
            }).ToList();
            if (newCategories.Count > 0)
            {
                await m_ItemsService.AddCategories(game, newCategories);
                dbCategories.AddRange(newCategories);
            }
            return Ok(dbCategories);
        }
    }
}
