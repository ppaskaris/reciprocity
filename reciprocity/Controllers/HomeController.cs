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
        private readonly IDataService _dataService;

        public HomeController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("about")]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        [Route("create-book")]
        public IActionResult CreateBook()
        {
            return View(new CreateBookModel
            {
                Name = "My Recipes"
            });
        }

        [HttpPost]
        [Route("create-book")]
        public async Task<IActionResult> CreateBook(CreateBookModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var key = await _dataService.CreateBookAsync(model.Name);
            return RedirectToAction("Index", "Book", key);
        }

        [Route("error/{statusCode?}")]
        public IActionResult Error(int? statusCode)
        {
            int statusCode0 = statusCode ?? 500;
            Response.StatusCode = statusCode0;
            return View(new ErrorViewModel
            {
                StatusCode = statusCode0,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [HttpGet]
        [Route("suggestions")]
        public async Task<IActionResult> AutoSuggest(AutoSuggestModel model)
        {
            if (!ModelState.IsValid)
            {
                return NoContent();
            }

            var suggestions = await _dataService.GetSuggestionsAsync(model.Query);
            return Json(suggestions);
        }
    }
}
