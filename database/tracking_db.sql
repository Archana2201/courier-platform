-- Reference DDL for the Tracking service (a read model built from shipment events).
CREATE TABLE tracking_events (
    "Id"            UUID PRIMARY KEY,
    "TrackingNumber" VARCHAR(20)  NOT NULL,
    "Status"        VARCHAR(20)  NOT NULL,
    "Location"      VARCHAR(100),
    "Description"   VARCHAR(300) NOT NULL,
    "OccurredAtUtc" TIMESTAMPTZ  NOT NULL,
    "SourceEventId" UUID         NOT NULL UNIQUE   -- idempotency key for redelivered events
);
CREATE INDEX ix_tracking_number ON tracking_events ("TrackingNumber");
