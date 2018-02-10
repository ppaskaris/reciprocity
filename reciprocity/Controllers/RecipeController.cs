using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using reciprocity.Models.Book;
using reciprocity.Models.Recipe;
using reciprocity.Services;

namespace reciprocity.Controllers
{
    [Route("{Token}/books/{BookId}/recipes/{RecipeId}")]
    public class RecipeController : Controller
    {
        private readonly IDataService _dataService;

        public RecipeController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        async public Task<IActionResult> Index(RecipeKeyModel key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(new RecipeViewModel
            {
                Book = book,
                Recipe = recipe
            });
        }

        [HttpGet]
        [Route("edit")]
        async public Task<IActionResult> Edit(RecipeKeyModel key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(new EditRecipeModel
            {
                Name = recipe.Name,
                Description = recipe.Description,
                Servings = recipe.Servings
            });
        }

        [HttpPost]
        [Route("edit")]
        async public Task<IActionResult> Edit(RecipeKeyModel key, EditRecipeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            await _dataService
                .UpdateRecipe(recipe.BookId, recipe.RecipeId, model);

            return RedirectToAction("Index");
        }

        #region Helpers

        private async Task<(BookModel, RecipeModel)> GetRecipeAsync(RecipeKeyModel key)
        {
            var book = await _dataService.GetBookAsync(key.BookId.Value);
            if (book == null)
            {
                return default;
            }
            if (!book.Token.TimingSafeEquals(key.Token))
            {
                return default;
            }
            var recipe = await _dataService
                .GetRecipeAsync(key.BookId.Value, key.RecipeId.Value);
            return (book, recipe);
        }

        #endregion
    }
}