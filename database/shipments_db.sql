-- Reference DDL for the Shipments service.
CREATE TABLE shipments (
    "Id"                 UUID PRIMARY KEY,
    "TrackingNumber"     VARCHAR(20)   NOT NULL UNIQUE,
    sender_name          VARCHAR(120), sender_line1 VARCHAR(200), sender_city VARCHAR(100),
    sender_state         VARCHAR(10),  sender_postcode VARCHAR(10),
    recipient_name       VARCHAR(120), recipient_line1 VARCHAR(200), recipient_city VARCHAR(100),
    recipient_state      VARCHAR(10),  recipient_postcode VARCHAR(10),
    "WeightKg"           NUMERIC(10,3) NOT NULL,
    "LengthCm"           NUMERIC(10,2) NOT NULL,
    "WidthCm"            NUMERIC(10,2) NOT NULL,
    "HeightCm"           NUMERIC(10,2) NOT NULL,
    "ServiceLevel"       VARCHAR(20)   NOT NULL,
    "Price"              NUMERIC(10,2) NOT NULL,
    "Currency"           VARCHAR(3)    NOT NULL,
    "Status"             VARCHAR(20)   NOT NULL,     -- Created | PickedUp | InTransit | OutForDelivery | Delivered | Cancelled
    "CreatedAtUtc"       TIMESTAMPTZ   NOT NULL
);

CREATE TABLE shipment_status_changes (
    "Id"         UUID PRIMARY KEY,
    "ShipmentId" UUID NOT NULL REFERENCES shipments ("Id") ON DELETE CASCADE,
    "Status"     VARCHAR(20) NOT NULL,
    "Location"   VARCHAR(100),
    "AtUtc"      TIMESTAMPTZ NOT NULL
);

-- Transactional outbox: written in the same transaction as the shipment change.
CREATE TABLE outbox_messages (
    "Id"             UUID PRIMARY KEY,
    "Type"           VARCHAR(100) NOT NULL,
    "Payload"        TEXT         NOT NULL,
    "CreatedAtUtc"   TIMESTAMPTZ  NOT NULL,
    "ProcessedAtUtc" TIMESTAMPTZ,
    "Attempts"       INT          NOT NULL DEFAULT 0
);
CREATE INDEX ix_outbox_processed ON outbox_messages ("ProcessedAtUtc");
