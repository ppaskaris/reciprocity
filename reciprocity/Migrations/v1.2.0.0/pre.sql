ALTER TABLE reciprocity.BookRecipeIngredient
	ALTER COLUMN Quantity DECIMAL(7,2) NOT NULL;

GO

ALTER TABLE reciprocity.BookRecipeIngredient
	ALTER COLUMN Serving DECIMAL(7,2) NOT NULL;

GO

ALTER TABLE reciprocity.BookRecipeIngredient
	ALTER COLUMN CaloriesPerServing DECIMAL(7,2) NOT NULL;

GO

DROP TYPE reciprocity.SaveBookRecipeIngredient;

CREATE TYPE reciprocity.SaveBookRecipeIngredient AS TABLE (
	IngredientNo INT NOT NULL,
	[Name] NVARCHAR(100) NOT NULL,
	Quantity DECIMAL(7,2) NOT NULL,
	QuantityType CHAR(1) NOT NULL,
	QuantityUnit VARCHAR(3) NOT NULL,
	Serving DECIMAL(7,2) NOT NULL,
	ServingType CHAR(1) NOT NULL,
	ServingUnit VARCHAR(3) NOT NULL,
	CaloriesPerServing DECIMAL(7,2) NOT NULL
);

GO

CREATE TABLE reciprocity.CNF_FoodName (
	FoodId INT NOT NULL,
	FoodDescription NVARCHAR(255) NOT NULL,

	CONSTRAINT CNF_FoodName_PK
		PRIMARY KEY (FoodId),
);

CREATE TABLE reciprocity.CNF_MeasureName (
	MeasureId INT NOT NULL,
	MeasureDescription NVARCHAR(200) NOT NULL,

	CONSTRAINT CNF_MeasureName_PK
		PRIMARY KEY (MeasureId),
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
	WITH CHANGE_TRACKING MANUAL;

GO