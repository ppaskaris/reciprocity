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

        [HttpGet, Route("create-book")]
        public IActionResult CreateBook()
        {
            return View(new CreateBookModel
            {
                Title = "My Recipes"
            });
        }

        [HttpPost, Route("create-book")]
        public async Task<IActionResult> CreateBook(CreateBookModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var key = await _bookService.CreateBookAsync(model.Title);
            return RedirectToAction("Index", "Book", key);
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
