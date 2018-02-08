using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using reciprocity.Models.Home;
using reciprocity.Services;

namespace reciprocity.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        private readonly IBookService _bookService;

        public HomeController(IBookService bookService)
        {
            _bookService = bookService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet, Route("about")]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet, Route("create-new-book")]
        public IActionResult CreateNewBook()
        {
            return View(new CreateNewBookModel());
        }

        [HttpPost, Route("create-new-book")]
        public async Task<IActionResult> CreateNewBookAsync(CreateNewBookModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var book = await _bookService.CreateNewBookAsync(model.Title);
            return RedirectToAction("Index", "Book", new
            {
                id = book.Id,
                token = book.Token
            });
        }

        [HttpGet, Route("error/{statusCode?}")]
        public IActionResult Error(int? statusCode)
        {
            return View(new ErrorViewModel
            {
                StatusCode = statusCode ?? 500,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
