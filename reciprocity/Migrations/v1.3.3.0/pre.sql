ALTER TABLE reciprocity.Book
	ALTER COLUMN [Name] NVARCHAR(255) NOT NULL;

ALTER TABLE reciprocity.BookRecipe
	ALTER COLUMN [Name] NVARCHAR(255) NOT NULL;

ALTER TABLE reciprocity.BookRecipeIngredient
	ALTER COLUMN [Name] NVARCHAR(255) NOT NULL;

ALTER TABLE reciprocity.BookRecipeIngredient
	ADD ProteinPerServing DECIMAL(7, 2) NOT NULL
		CONSTRAINT DEF_ZeroProteinPerServing DEFAULT (0.00);

GO

DROP TYPE reciprocity.SaveBookRecipeIngredient;

CREATE TYPE reciprocity.SaveBookRecipeIngredient AS TABLE (
	IngredientNo INT NOT NULL,
	[Name] NVARCHAR(255) NOT NULL,
	Quantity DECIMAL(7,2) NOT NULL,
	QuantityType CHAR(1) NOT NULL,
	QuantityUnit VARCHAR(3) NOT NULL,
	Serving DECIMAL(7,2) NOT NULL,
	ServingType CHAR(1) NOT NULL,
	ServingUnit VARCHAR(3) NOT NULL,
	CaloriesPerServing DECIMAL(7,2) NOT NULL,
	ProteinPerServing DECIMAL(7,2) NOT NULL
);

GO

CREATE INDEX CNF_NutrientName_NutrientSymbol
	ON reciprocity.CNF_NutrientName (NutrientSymbol);

GO

ALTER VIEW reciprocity.BookRecipeStatistics AS
SELECT
	BookId,
	RecipeId,
	CAST(ROUND(SUM(Quantity / Serving * CaloriesPerServing / Servings), 0) AS INT) AS CaloriesPerServing,
	CAST(ROUND(SUM(Quantity / Serving * ProteinPerServing / Servings), 2) AS DECIMAL(7, 2)) AS ProteinPerServing
FROM (
	SELECT
		BookRecipe.BookId,
		BookRecipe.RecipeId,
        BookRecipe.Servings,
		BookRecipeIngredient.Quantity * QuantityUnit.ConversionRatio AS Quantity,
		BookRecipeIngredient.Serving * ServingUnit.ConversionRatio AS Serving,
		BookRecipeIngredient.CaloriesPerServing,
		BookRecipeIngredient.ProteinPerServing
	FROM reciprocity.BookRecipe
	INNER JOIN reciprocity.BookRecipeIngredient
        ON BookRecipeIngredient.BookId = BookRecipe.BookId
        AND BookRecipeIngredient.RecipeId = BookRecipe.RecipeId
	INNER JOIN reciprocity.Unit QuantityUnit
		ON QuantityUnit.UnitTypeCode = BookRecipeIngredient.QuantityType
		AND QuantityUnit.UnitCode = BookRecipeIngredient.QuantityUnit
	INNER JOIN reciprocity.Unit ServingUnit
		ON ServingUnit.UnitTypeCode = BookRecipeIngredient.ServingType
		AND ServingUnit.UnitCode = BookRecipeIngredient.ServingUnit
) unused
GROUP BY BookId, RecipeId;

GO