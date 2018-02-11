-------------------------------------------------------------------------------
-- UnitType
-------------------------------------------------------------------------------

ALTER TABLE UnitType
	ADD SortOrder INT NULL;

GO

MERGE INTO UnitType target
USING (
	SELECT *
	FROM (VALUES
		('m', 'Mass', 1),
		('v', 'Volume', 2),
		('q', 'Quantity', 3))
	AS unused (UnitTypeCode, [Name], SortOrder)
) AS source
	 ON target.UnitTypeCode = source.UnitTypeCode
WHEN MATCHED THEN
	UPDATE SET
		[Name] = source.[Name],
		SortOrder = source.SortOrder
WHEN NOT MATCHED BY TARGET THEN
	INSERT (UnitTypeCode, [Name], SortOrder)
	VALUES (
		source.UnitTypeCode,
		source.[Name],
		source.SortOrder
	)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

GO

ALTER TABLE UnitType
	ALTER COLUMN SortOrder INT NOT NULL;

GO

-------------------------------------------------------------------------------
-- Unit
-------------------------------------------------------------------------------

ALTER TABLE Unit
	ALTER COLUMN Tier INT NULL;

GO

ALTER TABLE Unit
	ADD ConversionRatio DECIMAL(10, 6) NULL;

GO

MERGE INTO Unit target
USING (
	SELECT *
	FROM (VALUES
		('m', 'g', 'grams', 'g', 1),
		('m', 'oz', 'ounces', 'oz', 28),
		('m', 'kg', 'kilograms', 'kg', 1000),
		('v', 'tsp', 'teaspoons', 'tsp', 5),
		('v', 'tbs', 'tablespoons', 'tbs', 15),
		('v', 'oz', 'fluid ounces', 'fl oz', 28.41306),
		('v', 'cup', 'cups', 'C', 240),
		('v', 'ml', 'milliliters', 'mL', 1),
		('v', 'l', 'liter', 'L', 1000),
		('q', 'ea', 'each', 'ea', 1),
		('q', 'pc', 'pieces', 'pc', 1),
		('q', 'doz', 'dozen', 'doz', 12))
	AS unused (UnitTypeCode, UnitCode, [Name], Abbreviation, ConversionRatio)
) AS source
	 ON target.UnitTypeCode = source.UnitTypeCode
	AND target.UnitCode = source.UnitCode
WHEN MATCHED THEN
	UPDATE SET
		[Name] = source.[Name],
		Abbreviation = source.Abbreviation,
		ConversionRatio = source.ConversionRatio
WHEN NOT MATCHED BY TARGET THEN
	INSERT (UnitTypeCode, UnitCode, [Name], Abbreviation, ConversionRatio)
	VALUES (
		source.UnitTypeCode,
		source.UnitCode,
		source.[Name],
		source.Abbreviation,
		source.ConversionRatio
	)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

GO

ALTER TABLE Unit
	DROP COLUMN Tier;

GO

ALTER TABLE Unit
	ALTER COLUMN ConversionRatio DECIMAL(10, 6) NOT NULL;

GO

-------------------------------------------------------------------------------
-- BookRecipeStatistics
-------------------------------------------------------------------------------

CREATE VIEW BookRecipeStatistics AS
SELECT
	BookId,
	RecipeId,
	CAST(CEILING(SUM(Quantity / Serving * CaloriesPerServing / Servings)) AS INT) AS CaloriesPerServing
FROM (
	SELECT
		BookRecipe.BookId,
		BookRecipe.RecipeId,
        BookRecipe.Servings,
		BookRecipeIngredient.Quantity * QuantityUnit.ConversionRatio AS Quantity,
		BookRecipeIngredient.Serving * ServingUnit.ConversionRatio AS Serving,
		BookRecipeIngredient.CaloriesPerServing
	FROM BookRecipe
	INNER JOIN BookRecipeIngredient
        ON BookRecipeIngredient.BookId = BookRecipe.BookId
        AND BookRecipeIngredient.RecipeId = BookRecipe.RecipeId
	INNER JOIN Unit QuantityUnit
		ON QuantityUnit.UnitTypeCode = BookRecipeIngredient.QuantityType
		AND QuantityUnit.UnitCode = BookRecipeIngredient.QuantityUnit
	INNER JOIN Unit ServingUnit
		ON ServingUnit.UnitTypeCode = BookRecipeIngredient.ServingType
		AND ServingUnit.UnitCode = BookRecipeIngredient.ServingUnit
) unused
GROUP BY BookId, RecipeId;

GO