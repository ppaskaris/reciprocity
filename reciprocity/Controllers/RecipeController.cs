using Dapper;
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
        public async Task<IActionResult> Index(RecipeKeyModel key)
        {
            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var ingredients = await _dataService
                .GetIngredientsViewAsync(recipe.BookId, recipe.RecipeId);

            return View(new RecipeViewModel
            {
                Book = book,
                Recipe = recipe,
                Ingredients = ingredients
            });
        }

        [HttpGet]
        [Route("edit")]
        public async Task<IActionResult> Edit(RecipeKeyModel key)
        {
            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
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
        public async Task<IActionResult> Edit(RecipeKeyModel key, EditRecipeModel model)
        {
            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _dataService
                .UpdateRecipeAsync(recipe.BookId, recipe.RecipeId, model);

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("delete")]
        public async Task<IActionResult> Delete(RecipeKeyModel key)
        {
            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            return View(recipe);
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> Delete(RecipeKeyModel key, DeleteRecipeModel model)
        {
            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(recipe);
            }

            await _dataService.DeleteRecipeAsync(recipe.BookId, recipe.RecipeId);

            return RedirectToAction("Index", "Book");
        }

        [HttpGet]
        [Route("ingredients")]
        public async Task<IActionResult> EditIngredients(RecipeKeyModel key, bool addNew)
        {
            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var units = await _dataService.GetUnitsAsync();
            var ingredients0 = await _dataService
                .GetIngredientsAsync(recipe.BookId, recipe.RecipeId);
            var ingredients = ingredients0.AsList();

            if (ingredients.Count <= 0)
            {
                addNew = true;
            }

            var showBulkActions = !addNew;

            var viewModels = ingredients0
                .Select(ingredient => new EditIngredientViewModel
                {
                    IngredientNo = ingredient.IngredientNo,
                    Name = ingredient.Name,
                    Quantity = ingredient.Quantity,
                    QuantityUnit = $"{ingredient.QuantityType},{ingredient.QuantityUnit}",
                    Serving = ingredient.Serving,
                    ServingUnit = $"{ingredient.ServingType},{ingredient.ServingUnit}",
                    CaloriesPerServing = ingredient.CaloriesPerServing
                })
                .ToList();

            if (addNew == true)
            {
                int lastIngredientNo = viewModels.LastOrDefault()?.IngredientNo ?? 0;
                viewModels.Add(new EditIngredientViewModel
                {
                    IngredientNo = lastIngredientNo + 1,
                    AutoFocus = true
                });
            }

            return View(new EditIngredientsViewModel
            {
                Book = book,
                Recipe = recipe,
                Ingredients = viewModels,
                Units = units,
                AddNew = addNew,
                ShowBulkActions = showBulkActions
            });
        }

        [HttpPost]
        [Route("ingredients")]
        public async Task<IActionResult> EditIngredients(RecipeKeyModel key, EditIngredientsModel model)
        {
            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var units = await _dataService.GetUnitsAsync();
                var showBulkActions = !model.AddNew;
                var viewModels = model.Ingredients
                    .Select(ingredient => new EditIngredientViewModel
                    {
                        Checked = ingredient.Checked,
                        IngredientNo = ingredient.IngredientNo,
                        Name = ingredient.Name,
                        Quantity = ingredient.Quantity,
                        QuantityUnit = ingredient.QuantityUnit,
                        Serving = ingredient.Serving,
                        ServingUnit = ingredient.ServingUnit,
                        CaloriesPerServing = ingredient.CaloriesPerServing
                    })
                    .ToList();

                return View(new EditIngredientsViewModel
                {
                    Book = book,
                    Recipe = recipe,
                    Ingredients = viewModels,
                    Units = units,
                    AddNew = model.AddNew,
                    ShowBulkActions = showBulkActions
                });
            }

            IEnumerable<EditIngredientModel> ingredients = model.Ingredients;
            if (model.SaveAction == SaveActionType.RemoveChecked)
            {
                ingredients = ingredients
                    .Where(ingredient => ingredient.Checked == false);
            }

            await _dataService
                .SaveIngredientsAsync(recipe.BookId, recipe.RecipeId, ingredients);

            switch (model.SaveAction)
            {
                case SaveActionType.AddNew:
                    return RedirectToAction("EditIngredients", new { AddNew = true });
                case SaveActionType.RemoveChecked:
                    return RedirectToAction("EditIngredients", new { AddNew = false });
                default:
                    return RedirectToAction("Index");
            }
        }

        #region Helpers

        private async Task<(BookModel, RecipeModel)> GetRecipeAsync(RecipeKeyModel key)
        {
            if (key == null || key.BookId == null || key.Token == null || key.RecipeId == null)
            {
                return default;
            }
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