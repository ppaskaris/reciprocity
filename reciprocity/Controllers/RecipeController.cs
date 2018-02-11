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
        [Route("ingredients/add")]
        public async Task<IActionResult> AddIngredients(RecipeKeyModel key)
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

            var ingredients0 = await _dataService
                .GetIngredientsAsync(recipe.BookId, recipe.RecipeId);
            var ingredients = ingredients0.AsList();

            var viewModels = ingredients
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

            int lastIngredientNo = ingredients.Count > 0
                ? ingredients[ingredients.Count - 1].IngredientNo
                : 0;
            viewModels.Add(new EditIngredientViewModel
            {
                IngredientNo = lastIngredientNo + 1,
                AutoFocus = true
            });

            var units = await _dataService.GetUnitsAsync();

            return View("EditIngredients", new EditIngredientsViewModel
            {
                Book = book,
                Recipe = recipe,
                Ingredients = viewModels,
                Units = units,
                IsAddingMode = true,
                ShowBulkActions = false
            });
        }

        [HttpGet]
        [Route("ingredients/edit")]
        public async Task<IActionResult> EditIngredients(RecipeKeyModel key)
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

            var ingredients0 = await _dataService
                .GetIngredientsAsync(recipe.BookId, recipe.RecipeId);
            var ingredients = ingredients0.AsList();

            if (ingredients.Count <= 0)
            {
                return RedirectToAction("AddIngredients");
            }

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

            var units = await _dataService.GetUnitsAsync();

            return View("EditIngredients", new EditIngredientsViewModel
            {
                Book = book,
                Recipe = recipe,
                Ingredients = viewModels,
                Units = units,
                IsAddingMode = false,
                ShowBulkActions = true
            });
        }

        [HttpPost]
        [Route("ingredients")]
        public async Task<IActionResult> SaveIngredients(RecipeKeyModel key, EditIngredientsModel model)
        {
            var (book, recipe) = await GetRecipeAsync(key);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var units = await _dataService.GetUnitsAsync();
                var showBulkActions = !model.IsAddingMode;
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

                return View("EditIngredients", new EditIngredientsViewModel
                {
                    Book = book,
                    Recipe = recipe,
                    Ingredients = viewModels,
                    Units = units,
                    IsAddingMode = model.IsAddingMode,
                    ShowBulkActions = showBulkActions
                });
            }

            List<EditIngredientModel> ingredients = model.Ingredients;
            if (model.SaveAction == SaveActionType.RemoveChecked)
            {
                ingredients = ingredients
                    .Where(ingredient => ingredient.Checked == false)
                    .ToList();
            }

            await _dataService
                .SaveIngredientsAsync(recipe.BookId, recipe.RecipeId, ingredients);

            switch (model.SaveAction)
            {
                case SaveActionType.AddNew:
                    return RedirectToAction("AddIngredients");
                case SaveActionType.RemoveChecked:
                    if (ingredients.Count > 0)
                    {
                        return RedirectToAction("EditIngredients");
                    }
                    else
                    {
                        return RedirectToAction("AddIngredients");
                    }
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