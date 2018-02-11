-- Drop objects in reverse order, since they depend on each other.
-- A better database offers "DROP SCHEMA IF EXISTS [dbo] CASCADE" to do this.

DROP TYPE IF EXISTS SaveBookRecipeIngredient;
DROP TABLE IF EXISTS BookRecipeIngredient;
DROP TABLE IF EXISTS BookRecipe;
DROP TABLE IF EXISTS Book;
DROP TABLE IF EXISTS Unit;
DROP TABLE IF EXISTS UnitType;

-------------------------------------------------------------------------------

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
	Tier INT NOT NULL,

	CONSTRAINT Unit_PK
		PRIMARY KEY (UnitTypeCode, UnitCode),
	CONSTRAINT UnitType_Categorizes_Unit_fk
		FOREIGN KEY (UnitTypeCode)
		REFERENCES UnitType (UnitTypeCode),
);

INSERT INTO Unit
	(UnitTypeCode, UnitCode, [Name], Abbreviation, Tier)
VALUES
	('m', 'g', 'gram', 'g', 1),
	('m', 'oz', 'ounce', 'oz', 1),
	('m', 'kg', 'kilogram', 'kg', 2),
	('v', 'tsp', 'teaspoon', 'tsp', 1),
	('v', 'tbs', 'tablespoon', 'tbsp', 2),
	('v', 'oz', 'fluid ounce', 'fl oz', 3),
	('v', 'cup', 'cup', 'C', 4),
	('v', 'ml', 'milliliter', 'mL', 5),
	('v', 'l', 'liter', 'L', 6);


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