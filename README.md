# Savings Platform PoC on Dapr

Repository contains a proof-of-concept system for savings platform. There are multiple services involved that represent different modules / functional areas of the system.

## SavingsPlatform API

Internal API that exposes endpoints for savings platform management (account creation, executing money transfer between accounts). 
The other important part of this service is integration with events using Dapr PubSub mechanism and handling multi-step processes using Dapr Actors (interest accrual, deposit transfer).

## SavingsPlatform Payment Proxy

API meant to serve as a proxy with external system. Exposes endpoints for inbound and outbound payments and manages store for mapping `ExternalRef` to internal `AccountId`.
Uses Dapr PubSub mechanism and Dapr Service Invocation.

## SavingsPlatform Event Store

PoC of Event Store based on Marten (https://martendb.io/events/). This service consumes events from `SavingsPlatform API` and manages Event Store.
Uses Dapr PubSub to subscribe to events.
