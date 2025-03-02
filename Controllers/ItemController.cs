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
            var newItems = (await m_WikiService.GetItems()).Select(i =>
            {
                return new Item()
                {
                    Name = i.Name,
                    BaseCategory = i.Category,
                    Rarity = i.Rarity
                };
            }).ToList();
            var oldItems = await m_ItemsService.GetItems(game);

            List<Item> addItems = [];
            List<string> removeItems = [];
            for (int i = 0; i < newItems.Count(); i++)
            {
                Item newItem = newItems[i];
                bool found = false;
                for (int j = 0; j < oldItems.Count(); j++)
                {
                    Item oldItem = oldItems[j];
                    if (newItem.Name.Equals(oldItem.Name))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    addItems.Add(newItem);
                }
            }
            for (int i = 0; i < oldItems.Count(); i++)
            {
                Item oldItem = oldItems[i];
                bool found = false;
                for (int j = 0; j < newItems.Count(); j++)
                {
                    Item newItem = newItems[j];
                    if (newItem.Name.Equals(oldItem.Name))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    removeItems.Add(oldItem.Id);
                    oldItems.RemoveAt(i);
                    i--;
                }
            }

            if (removeItems.Count > 0)
            {
                await m_ItemsService.DeleteItems(game, removeItems);
            }
            if (addItems.Count > 0)
            {
                for (int i = 0; i < addItems.Count; i++)
                {
                    string? categoryId = await m_ItemsService.GetBaseCategoryId(game, addItems[i].BaseCategory);
                    if (categoryId == null)
                    {
                        categoryId = await m_ItemsService.AddBaseCategory(game, 
                            new ItemCategory() { Name = newItems[i].BaseCategory }
                        );
                    }
                    newItems[i].BaseCategory = categoryId;
                }
                await m_ItemsService.AddItems(game, addItems);
                oldItems.AddRange(addItems);
            }
            return Ok(oldItems);
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
