CREATE INDEX CNF_FoodName_FoodDescription
	ON reciprocity.CNF_FoodName (FoodDescription);

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