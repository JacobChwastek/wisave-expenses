# KurrentDB to RabbitMQ Committed Events Runbook

## Runtime Path

1. `WebApi` publishes a command to RabbitMQ.
2. `Worker.Domain` consumes the command and appends events to KurrentDB.
3. `Worker.Domain` forwarder connects to a Kurrent persistent subscription group on `$all`.
4. The forwarder republishes recognized events to RabbitMQ and acknowledges them back to Kurrent only after successful publish.
5. `Worker.Projections` consumes the republished events and updates Postgres read models.

The subscription group is configured with:
- `MaxSubscriberCount = 1`
- `ConsumerStrategyName = DispatchToSingle`

## Local Configuration

- `ConnectionStrings__EventStore=esdb://kurrentdb:2113?tls=false`
- `ConnectionStrings__Postgres=Host=postgres;Database=wisave_expenses;Username=wisave;Password=wisave_dev`
- `RabbitMq__Host=host.docker.internal`
- `RabbitMq__VirtualHost=expenses`
- `KurrentForwarder__GroupName=expenses-forwarder`
- `KurrentForwarder__FromStartWhenCreated=true`
- `KurrentForwarder__MaxSubscriberCount=1`
- `KurrentForwarder__ConsumerStrategyName=DispatchToSingle`

## Verification

1. Run projections migrations:
   `dotnet run --project src/WiSave.Expenses.Projections.Migrations -- "Host=postgres;Database=wisave_expenses;Username=wisave;Password=wisave_dev"`
2. Run automated tests:
   `dotnet test WiSave.Expenses.slnx -v minimal`
3. Start runtime services:
   `docker compose up --build worker-domain worker-projections`
4. Trigger one command through `WebApi`, then verify:
   - an event was appended to KurrentDB
   - `worker-domain` logs show persistent-subscription startup and committed event forwarding
   - `worker-projections` logs show message consumption
   - the corresponding row exists in the projections schema

## Restart Check

1. Stop `worker-domain`.
2. Trigger another command only after restarting `worker-domain`.
3. Verify the forwarder resumes from the persistent subscription state and does not republish already acknowledged events.
