ALTER TABLE reciprocity.Book
	ALTER COLUMN [Name] NVARCHAR(255) NOT NULL;

ALTER TABLE reciprocity.BookRecipe
	ALTER COLUMN [Name] NVARCHAR(255) NOT NULL;

ALTER TABLE reciprocity.BookRecipeIngredient
	ALTER COLUMN [Name] NVARCHAR(255) NOT NULL;

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
	CaloriesPerServing DECIMAL(7,2) NOT NULL
);

GO