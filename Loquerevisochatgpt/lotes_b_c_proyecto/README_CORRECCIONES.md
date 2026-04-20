# Correcciones aplicadas al lote B/C

Se ajustó el material fuente para que sea más seguro al integrarlo con **.NET 8 + EF Core 8 + Pomelo/MySQL**.

## Cambios aplicados

1. **Timestamps compatibles con MySQL**
   - Se cambió el patrón de configuración EF Core de:
     - `.HasDefaultValueSql("CURRENT_TIMESTAMP")`
   - a:
     - `.HasColumnType("timestamp")`
     - `.HasDefaultValueSql("CURRENT_TIMESTAMP")`
   - Esto evita el error `Invalid default value for 'created_at'` cuando MySQL no acepta `CURRENT_TIMESTAMP` sobre `datetime(6)`.

2. **Índice único de `Customer.Email`**
   - Se eliminó `.HasFilter("email IS NOT NULL")`.
   - En MySQL los índices `UNIQUE` ya permiten múltiples `NULL`, así que el filtro no es necesario y puede romper compatibilidad.

## Ajustes recomendados antes de integrar al proyecto real

Los archivos del lote siguen siendo **fuentes Markdown**, no el proyecto C# ya integrado. Antes de pegarlos en el proyecto, agrega relaciones EF Core explícitas (`HasOne / WithMany / HasForeignKey / OnDelete`) donde haya columnas FK.

### Mapa recomendado de relaciones

- **BaseFlight**
  - `AirlineId` -> `AirlineEntity`
  - `RouteId` -> `RouteEntity`

- **RouteSchedule**
  - `BaseFlightId` -> `BaseFlightEntity`

- **ScheduledFlight**
  - `BaseFlightId` -> `BaseFlightEntity`
  - `FlightStatusId` -> `FlightStatusEntity`
  - `AircraftId` -> `AircraftEntity`
  - `TerminalId` -> `TerminalEntity`
  - `GateId` -> `GateEntity`

- **FlightCrew**
  - `ScheduledFlightId` -> `ScheduledFlightEntity`
  - `EmployeeId` -> `EmployeeEntity`
  - `CrewRoleId` -> `CrewRoleEntity`

- **SeatMap**
  - `AircraftTypeId` -> `AircraftTypeEntity`
  - `CabinClassId` -> `CabinClassEntity`

- **FlightSeat**
  - `ScheduledFlightId` -> `ScheduledFlightEntity`
  - `SeatMapId` -> `SeatMapEntity`
  - `SeatStatusId` -> `SeatStatusEntity`

- **Customer**
  - `PersonId` -> `PersonEntity`

- **Reservation**
  - `CustomerId` -> `CustomerEntity`
  - `ReservationStatusId` -> `ReservationStatusEntity`

- **ReservationDetail**
  - `ReservationId` -> `ReservationEntity`
  - `ScheduledFlightId` -> `ScheduledFlightEntity`
  - `PassengerId` -> `PassengerEntity`
  - `CabinClassId` -> `CabinClassEntity`

- **PassengerDiscount**
  - `PassengerId` -> `PassengerEntity`
  - `DiscountTypeId` -> `DiscountTypeEntity`

- **FlightDelay**
  - `ScheduledFlightId` -> `ScheduledFlightEntity`
  - `DelayReasonId` -> `DelayReasonEntity`

- **FlightCancellation**
  - `ScheduledFlightId` -> `ScheduledFlightEntity`
  - `CancellationReasonId` -> `CancellationReasonEntity`

- **Payment**
  - `ReservationId` -> `ReservationEntity` (nullable)
  - `TicketId` -> `TicketEntity` (nullable)
  - `CurrencyId` -> `CurrencyEntity`
  - `PaymentStatusId` -> `PaymentStatusEntity`
  - `PaymentMethodId` -> `PaymentMethodEntity`

- **Refund**
  - `PaymentId` -> `PaymentEntity`
  - `RefundStatusId` -> `RefundStatusEntity`

- **Ticket**
  - `ReservationDetailId` -> `ReservationDetailEntity`
  - `TicketStatusId` -> `TicketStatusEntity`
  - `BaggageAllowanceId` -> `BaggageAllowanceEntity` (nullable)

- **TicketBaggage**
  - `TicketId` -> `TicketEntity`
  - `BaggageTypeId` -> `BaggageTypeEntity`

- **CheckIn**
  - `TicketId` -> `TicketEntity`
  - `CheckInStatusId` -> `CheckInStatusEntity`

- **BoardingPass**
  - `CheckInId` -> `CheckInEntity`

- **LoyaltyTier**
  - `LoyaltyProgramId` -> `LoyaltyProgramEntity`

- **LoyaltyAccount**
  - `CustomerId` -> `CustomerEntity`
  - `LoyaltyProgramId` -> `LoyaltyProgramEntity`
  - `LoyaltyTierId` -> `LoyaltyTierEntity`

- **LoyaltyTransaction**
  - `LoyaltyAccountId` -> `LoyaltyAccountEntity`
  - `TicketId` -> `TicketEntity` (nullable si tu diseño lo permite)

## Sugerencia de `OnDelete(...)`

Para evitar cascadas peligrosas en MySQL, usa preferentemente:

- `DeleteBehavior.Restrict` en relaciones operativas/históricas
- `DeleteBehavior.SetNull` solo cuando la FK sea nullable y tenga sentido funcional

## Estado del lote corregido

- **Sí** queda más seguro para integrar.
- **No** reemplaza una compilación real del proyecto, porque este zip contiene Markdown fuente, no el repositorio C# completo.
