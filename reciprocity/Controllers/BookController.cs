using Microsoft.AspNetCore.Mvc;
using reciprocity.Models.Book;
using reciprocity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Controllers
{
    [Route("{token}/book/{id}")]
    public class BookController : Controller
    {
        private readonly IBookService _bookService;

        public BookController(IBookService bookService)
        {
            _bookService = bookService;
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

            return View(book);
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
                await _bookService.RenameBookAsync(book.Id, model.Title);
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

            await _bookService.DeleteBookAsync(book.Id);

            return RedirectToAction("Index", "Home");
        }

        private async Task<BookModel> GetBookAsync(BookKeyModel key)
        {
            var book = await _bookService.GetBookAsync(key.Id.Value);
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
