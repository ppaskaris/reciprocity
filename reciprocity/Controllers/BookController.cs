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
        private readonly IBookService _bookService;
        private readonly IRecipeService _recipeService;

        public BookController(IBookService bookService, IRecipeService recipeService)
        {
            _bookService = bookService;
            _recipeService = recipeService;
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

            var viewModel = await _bookService.GetBookViewAsync(book.BookId);
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
                Title = book.Title
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

            if (book.Title != model.Title)
            {
                await _bookService.RenameBookAsync(book.BookId, model.Title);
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
                Title = book.Title
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

            await _bookService.DeleteBookAsync(book.BookId);

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

            var recipeKey = await _recipeService.CreateRecipeAsync(
                book.BookId,
                model.Title,
                model.Servings);
            recipeKey.Token = bookKey.Token;
            return RedirectToAction("Index", "Recipe", recipeKey);
        }

        private async Task<BookModel> GetBookAsync(BookKeyModel key)
        {
            var book = await _bookService.GetBookAsync(key.BookId.Value);
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
