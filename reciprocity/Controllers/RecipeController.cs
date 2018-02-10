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
        private readonly IBookService _bookService;
        private readonly IRecipeService _recipeService;

        public RecipeController(IBookService bookService, IRecipeService recipeService)
        {
            _bookService = bookService;
            _recipeService = recipeService;
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

        private async Task<(BookModel, RecipeModel)> GetRecipeAsync(RecipeKeyModel key)
        {
            var book = await _bookService.GetBookAsync(key.BookId.Value);
            if (book == null)
            {
                return default;
            }
            if (!book.Token.TimingSafeEquals(key.Token))
            {
                return default;
            }
            var recipe = await _recipeService.GetRecipeAsync(
                key.BookId.Value,
                key.RecipeId.Value);
            return (book, recipe);
        }
    }
}