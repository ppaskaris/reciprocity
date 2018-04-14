-- Drop objects in reverse order, since they depend on each other.
-- A better database offers "DROP SCHEMA IF EXISTS [dbo] CASCADE" to do this.

IF EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.[object_id] = OBJECT_ID(N'reciprocity.CNF_FoodName'))
	DROP FULLTEXT INDEX ON reciprocity.CNF_FoodName;

GO

IF EXISTS (SELECT * FROM sys.fulltext_catalogs ftc WHERE ftc.[Name] = 'reciprocity_FTS')
	DROP FULLTEXT CATALOG reciprocity_FTS;

GO

DROP TABLE IF EXISTS reciprocity.CNF_NutrientAmount;
DROP TABLE IF EXISTS reciprocity.CNF_NutrientName;
DROP TABLE IF EXISTS reciprocity.CNF_ConversionFactor;
DROP TABLE IF EXISTS reciprocity.CNF_Unit;
DROP TABLE IF EXISTS reciprocity.CNF_MeasureName;
DROP TABLE IF EXISTS reciprocity.CNF_FoodName;

GO

DROP VIEW IF EXISTS reciprocity.BookRecipeStatistics;

GO

DROP TYPE IF EXISTS reciprocity.SaveBookRecipeIngredient;
DROP TABLE IF EXISTS reciprocity.BookRecipeIngredient;
DROP TABLE IF EXISTS reciprocity.BookRecipe;
DROP TABLE IF EXISTS reciprocity.Book;
DROP TABLE IF EXISTS reciprocity.Unit;
DROP TABLE IF EXISTS reciprocity.UnitType;

GO

DROP SCHEMA IF EXISTS reciprocity;

GO

DROP USER IF EXISTS reciprocity;

GO

-------------------------------------------------------------------------------

CREATE SCHEMA reciprocity;

GO

CREATE USER reciprocity FOR LOGIN reciprocity WITH DEFAULT_SCHEMA = reciprocity;

GO

GRANT SELECT ON SCHEMA :: reciprocity TO reciprocity;
GRANT INSERT ON SCHEMA :: reciprocity TO reciprocity;
GRANT DELETE ON SCHEMA :: reciprocity TO reciprocity;
GRANT UPDATE ON SCHEMA :: reciprocity TO reciprocity;
GRANT EXECUTE ON SCHEMA :: reciprocity TO reciprocity;

GO

CREATE TABLE reciprocity.UnitType (
	UnitTypeCode CHAR(1) NOT NULL,
	[Name] NVARCHAR(100) NOT NULL,
	SortOrder INT NOT NULL,

	CONSTRAINT UnitType_PK
		PRIMARY KEY (UnitTypeCode),
);

INSERT INTO reciprocity.UnitType
	(UnitTypeCode, [Name], SortOrder)
VALUES
	('m', 'Mass', 1),
	('v', 'Volume', 2),
	('q', 'Quantity', 3);

CREATE TABLE reciprocity.Unit (
	UnitTypeCode CHAR(1) NOT NULL,
	UnitCode VARCHAR(3) NOT NULL,
	[Name] NVARCHAR(100) NOT NULL,
	Abbreviation NVARCHAR(10) NOT NULL,
	ConversionRatio DECIMAL(10, 6) NOT NULL,

	CONSTRAINT Unit_PK
		PRIMARY KEY (UnitTypeCode, UnitCode),
	CONSTRAINT UnitType_Categorizes_Unit_fk
		FOREIGN KEY (UnitTypeCode)
		REFERENCES reciprocity.UnitType (UnitTypeCode),
);

INSERT INTO reciprocity.Unit
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
	('v', 'l', 'liter', 'L', 1000),
	('q', 'ea', 'each', 'ea', 1),
	('q', 'pc', 'pieces', 'pc', 1),
	('q', 'doz', 'dozen', 'doz', 12);


CREATE TABLE reciprocity.Book (
	BookId UNIQUEIDENTIFIER NOT NULL,

	Token BINARY(40) NOT NULL,
	[Name] NVARCHAR(255) NOT NULL,

	CONSTRAINT Book_PK
		PRIMARY KEY (BookId),
);

CREATE TABLE reciprocity.BookRecipe (
	BookId UNIQUEIDENTIFIER NOT NULL,
	RecipeId UNIQUEIDENTIFIER NOT NULL,

	[Name] NVARCHAR(255) NOT NULL,
	[Description] NVARCHAR(MAX) NULL,
	Servings INT NOT NULL,
	AddedAt DATETIME NOT NULL,
	LastModifiedAt DATETIME NOT NULL,

	CONSTRAINT BookRecipe_PK
		PRIMARY KEY (BookId, RecipeId),
	CONSTRAINT Book_Contains_BookRecipe_fk
		FOREIGN KEY (BookId)
		REFERENCES reciprocity.Book (BookId)
		ON DELETE CASCADE,
);

CREATE TABLE reciprocity.BookRecipeIngredient (
	BookId UNIQUEIDENTIFIER NOT NULL,
	RecipeId UNIQUEIDENTIFIER NOT NULL,
	IngredientNo INT NOT NULL,

	[Name] NVARCHAR(255) NOT NULL,
	Quantity DECIMAL(7,2) NOT NULL,
	QuantityType CHAR(1) NOT NULL,
	QuantityUnit VARCHAR(3) NOT NULL,
	Serving DECIMAL(7,2) NOT NULL,
	ServingType CHAR(1) NOT NULL,
	ServingUnit VARCHAR(3) NOT NULL,
	CaloriesPerServing DECIMAL(7,2) NOT NULL,
	ProteinPerServing DECIMAL(7,2) NOT NULL,

	CONSTRAINT BookRecipeIngredient_PK
		PRIMARY KEY (BookId, RecipeId, IngredientNo),
	CONSTRAINT BookRecipe_Collects_BookRecipeIngredient_fk
		FOREIGN KEY (BookId)
		REFERENCES reciprocity.Book (BookId)
		ON DELETE CASCADE,
	CONSTRAINT Unit_Measures_Quantity_fk
		FOREIGN KEY (QuantityType, QuantityUnit)
		REFERENCES reciprocity.Unit (UnitTypeCode, UnitCode),
	CONSTRAINT Unit_Measures_Serving_fk
		FOREIGN KEY (ServingType, ServingUnit)
		REFERENCES reciprocity.Unit (UnitTypeCode, UnitCode),
	CONSTRAINT QuantityType_Matches_ServingType_ck
		CHECK (QuantityType = ServingType),
);

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

CREATE VIEW reciprocity.BookRecipeStatistics AS
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

CREATE TABLE reciprocity.CNF_FoodName (
	FoodId INT NOT NULL,
	FoodDescription NVARCHAR(255) NOT NULL,

	INDEX CNF_FoodName_FoodDescription (FoodDescription),

	CONSTRAINT CNF_FoodName_PK
		PRIMARY KEY (FoodId),
);

CREATE TABLE reciprocity.CNF_MeasureName (
	MeasureId INT NOT NULL,
	MeasureDescription NVARCHAR(200) NOT NULL,

	CONSTRAINT CNF_MeasureName_PK
		PRIMARY KEY (MeasureId),
);

CREATE TABLE reciprocity.CNF_Unit (
	MeasureId INT NOT NULL,
	Serving DECIMAL(7,2) NOT NULL,
	ServingType CHAR(1) NOT NULL,
	ServingCode VARCHAR(3) NOT NULL,
	Parenthetical NVARCHAR(127) NULL,

	INDEX CNF_Unit_MeasureId (MeasureId),

	CONSTRAINT Unit_CorrespondsTo_CNF_Unit
		FOREIGN KEY (ServingType, ServingCode)
		REFERENCES reciprocity.Unit (UnitTypeCode, UnitCode),
);

CREATE TABLE reciprocity.CNF_ConversionFactor (
	FoodId INT NOT NULL,
	MeasureId INT NOT NULL,
	ConversionFactorValue DECIMAL(10, 6) NOT NULL,

	CONSTRAINT CNF_ConversionFactor_PK
		PRIMARY KEY (FoodId, MeasureId),
	CONSTRAINT CNF_FoodName_Describes_ConversionFactor_fk
		FOREIGN KEY (FoodId)
		REFERENCES reciprocity.CNF_FoodName (FoodId),
	CONSTRAINT CNF_MeasureName_Describes_ConversionFactor_fk
		FOREIGN KEY (MeasureId)
		REFERENCES reciprocity.CNF_MeasureName (MeasureId),
);

CREATE TABLE reciprocity.CNF_NutrientName (
	NutrientId INT NOT NULL,
	NutrientSymbol VARCHAR(15) NOT NULL,
	NutrientUnit VARCHAR(8) NOT NULL,
	NutrientName NVARCHAR(200) NOT NULL,
	NutrientDecimals INT NOT NULL,

	CONSTRAINT CNF_NutrientName_PK
		PRIMARY KEY (NutrientId),

	INDEX CNF_NutrientName_NutrientSymbol (NutrientSymbol)
);

CREATE TABLE reciprocity.CNF_NutrientAmount (
	FoodId INT NOT NULL,
	NutrientId INT NOT NULL,
	NutrientValue DECIMAL(12, 5),

	CONSTRAINT CNF_NutrientAmount_PK
		PRIMARY KEY (FoodId, NutrientId),
	CONSTRAINT CNF_FoodName_Describes_NutrientAmount_fk
		FOREIGN KEY (FoodId)
		REFERENCES reciprocity.CNF_FoodName (FoodId),
	CONSTRAINT CNF_NutrientName_Describes_NutrientAmount_fk
		FOREIGN KEY (NutrientId)
		REFERENCES reciprocity.CNF_NutrientName (NutrientId),
);

GO

CREATE FULLTEXT CATALOG reciprocity_FTS;

GO

CREATE FULLTEXT INDEX ON reciprocity.CNF_FoodName (FoodDescription)
	KEY INDEX CNF_FoodName_PK ON reciprocity_FTS
	WITH CHANGE_TRACKING MANUAL, STOPLIST OFF;

GO