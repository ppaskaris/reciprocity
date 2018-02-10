using Microsoft.AspNetCore.Mvc;
using reciprocity.Models.Book;
using reciprocity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Controllers
{
    [Route("{Token}/books/{BookId}")]
    public class BookController : Controller
    {
        private readonly IDataService _dataService;

        public BookController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(BookKeyModel key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var book = await GetBookAsync(key);
            if (book == null)
            {
                return NotFound();
            }

            var viewModel = await _dataService.GetBookViewAsync(book.BookId);
            return View(viewModel);
        }

        [HttpGet]
        [Route("edit")]
        public async Task<IActionResult> Edit(BookKeyModel key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var book = await GetBookAsync(key);
            if (book == null)
            {
                return NotFound();
            }

            return View(new EditBookModel
            {
                Name = book.Name
            });
        }

        [HttpPost]
        [Route("edit")]
        public async Task<IActionResult> Edit(BookKeyModel key, EditBookModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var book = await GetBookAsync(key);
            if (book == null)
            {
                return NotFound();
            }

            if (book.Name != model.Name)
            {
                await _dataService.RenameBookAsync(book.BookId, model.Name);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("delete")]
        public async Task<IActionResult> Delete(BookKeyModel key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var book = await GetBookAsync(key);
            if (book == null)
            {
                return NotFound();
            }

            return View(new DeleteBookViewModel
            {
                Name = book.Name
            });
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> Delete(BookKeyModel key, DeleteBookModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var book = await GetBookAsync(key);
            if (book == null)
            {
                return NotFound();
            }

            await _dataService.DeleteBookAsync(book.BookId);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Route("add-recipe")]
        public async Task<IActionResult> AddRecipe(BookKeyModel key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var book = await GetBookAsync(key);
            if (book == null)
            {
                return NotFound();
            }

            return View(new AddRecipeModel
            {
                Servings = 4
            });
        }

        [HttpPost]
        [Route("add-recipe")]
        public async Task<IActionResult> AddRecipe(BookKeyModel bookKey, AddRecipeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var book = await GetBookAsync(bookKey);
            if (book == null)
            {
                return NotFound();
            }

            var recipeKey = await _dataService.CreateRecipeAsync(book.BookId, model);
            recipeKey.Token = bookKey.Token;
            return RedirectToAction("Index", "Recipe", recipeKey);
        }

        private async Task<BookModel> GetBookAsync(BookKeyModel key)
        {
            var book = await _dataService.GetBookAsync(key.BookId.Value);
            if (book == null)
            {
                return null;
            }
            if (!book.Token.TimingSafeEquals(key.Token))
            {
                return null;
            }
            return book;
        }
    }
}
