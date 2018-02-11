-- Drop objects in reverse order, since they depend on each other.
-- A better database offers "DROP SCHEMA IF EXISTS [dbo] CASCADE" to do this.

DROP VIEW IF EXISTS BookRecipeStatistics;
DROP TYPE IF EXISTS SaveBookRecipeIngredient;
DROP TABLE IF EXISTS BookRecipeIngredient;
DROP TABLE IF EXISTS BookRecipe;
DROP TABLE IF EXISTS Book;
DROP TABLE IF EXISTS Unit;
DROP TABLE IF EXISTS UnitType;

GO

-------------------------------------------------------------------------------

CREATE TABLE UnitType (
	UnitTypeCode CHAR(1) NOT NULL,
	[Name] NVARCHAR(100) NOT NULL,

	CONSTRAINT UnitType_PK
		PRIMARY KEY (UnitTypeCode),
);

INSERT INTO UnitType
	(UnitTypeCode, [Name])
VALUES
	('m', 'Mass'),
	('v', 'Volume');

CREATE TABLE Unit (
	UnitTypeCode CHAR(1) NOT NULL,
	UnitCode VARCHAR(3) NOT NULL,
	[Name] NVARCHAR(100) NOT NULL,
	Abbreviation NVARCHAR(10) NOT NULL,
	ConversionRatio DECIMAL(10, 6) NOT NULL,

	CONSTRAINT Unit_PK
		PRIMARY KEY (UnitTypeCode, UnitCode),
	CONSTRAINT UnitType_Categorizes_Unit_fk
		FOREIGN KEY (UnitTypeCode)
		REFERENCES UnitType (UnitTypeCode),
);

INSERT INTO Unit
	(UnitTypeCode, UnitCode, [Name], Abbreviation, ConversionRatio)
VALUES
	('m', 'g', 'grams', 'g', 1),
	('m', 'oz', 'ounces', 'oz', 28),
	('m', 'kg', 'kilograms', 'kg', 1000),
	('v', 'tsp', 'teaspoons', 'tsp', 5),
	('v', 'tbs', 'tablespoons', 'tbs', 15),
	('v', 'oz', 'fluid ounces', 'fl oz', 28.41306),
	('v', 'cup', 'cups', 'C', 240),
	('v', 'ml', 'milliliters', 'mL', 1),
	('v', 'l', 'liter', 'L', 1000);


CREATE TABLE Book (
	BookId UNIQUEIDENTIFIER NOT NULL,

	Token BINARY(40) NOT NULL,
	[Name] NVARCHAR(100) NOT NULL,

	CONSTRAINT Book_PK
		PRIMARY KEY (BookId),
);

CREATE TABLE BookRecipe (
	BookId UNIQUEIDENTIFIER NOT NULL,
	RecipeId UNIQUEIDENTIFIER NOT NULL,

	[Name] NVARCHAR(100) NOT NULL,
	[Description] NVARCHAR(MAX) NULL,
	Servings INT NOT NULL,
	AddedAt DATETIME NOT NULL,
	LastModifiedAt DATETIME NOT NULL,

	CONSTRAINT BookRecipe_PK
		PRIMARY KEY (BookId, RecipeId),
	CONSTRAINT Book_Contains_BookRecipe_fk
		FOREIGN KEY (BookId)
		REFERENCES Book (BookId)
		ON DELETE CASCADE,
);

CREATE TABLE BookRecipeIngredient (
	BookId UNIQUEIDENTIFIER NOT NULL,
	RecipeId UNIQUEIDENTIFIER NOT NULL,
	IngredientNo INT NOT NULL,

	[Name] NVARCHAR(100) NOT NULL,
	Quantity DECIMAL(5, 2) NOT NULL,
	QuantityType CHAR(1) NOT NULL,
	QuantityUnit VARCHAR(3) NOT NULL,
	Serving DECIMAL(5, 2) NOT NULL,
	ServingType CHAR(1) NOT NULL,
	ServingUnit VARCHAR(3) NOT NULL,
	CaloriesPerServing DECIMAL(5, 2) NOT NULL,

	CONSTRAINT BookRecipeIngredient_PK
		PRIMARY KEY (BookId, RecipeId, IngredientNo),
	CONSTRAINT BookRecipe_Collects_BookRecipeIngredient_fk
		FOREIGN KEY (BookId)
		REFERENCES Book (BookId)
		ON DELETE CASCADE,
	CONSTRAINT Unit_Measures_Quantity_fk
		FOREIGN KEY (QuantityType, QuantityUnit)
		REFERENCES Unit (UnitTypeCode, UnitCode),
	CONSTRAINT Unit_Measures_Serving_fk
		FOREIGN KEY (ServingType, ServingUnit)
		REFERENCES Unit (UnitTypeCode, UnitCode),
	CONSTRAINT QuantityType_Matches_ServingType_ck
		CHECK (QuantityType = ServingType),
);

CREATE TYPE SaveBookRecipeIngredient AS TABLE (
	IngredientNo INT NOT NULL,
	[Name] NVARCHAR(100) NOT NULL,
	Quantity DECIMAL(5, 2) NOT NULL,
	QuantityType CHAR(1) NOT NULL,
	QuantityUnit VARCHAR(3) NOT NULL,
	Serving DECIMAL(5, 2) NOT NULL,
	ServingType CHAR(1) NOT NULL,
	ServingUnit VARCHAR(3) NOT NULL,
	CaloriesPerServing DECIMAL(5, 2) NOT NULL
);

GO

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