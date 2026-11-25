-- Reference DDL + seed data for the Pricing service (EF Core creates the same schema on startup).
CREATE TABLE rate_cards (
    "Id"            SERIAL PRIMARY KEY,
    "ServiceLevel"  VARCHAR(20)   NOT NULL UNIQUE,   -- Standard | Express | Overnight
    "BasePrice"     NUMERIC(10,2) NOT NULL,          -- covers the first chargeable kg
    "PerKgPrice"    NUMERIC(10,2) NOT NULL,          -- each additional chargeable kg
    "EstimatedDays" INT           NOT NULL
);

INSERT INTO rate_cards ("Id", "ServiceLevel", "BasePrice", "PerKgPrice", "EstimatedDays") VALUES
    (1, 'Standard',  9.50, 1.80, 4),
    (2, 'Express',  14.50, 2.60, 2),
    (3, 'Overnight',22.00, 3.90, 1);
