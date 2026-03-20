# discovery-testbed-hybrid

**This is a StormBoard topology test fixture, NOT a real application.**

This repository is a deliberately **ambiguous** codebase designed to trigger **low-confidence** topology detection (<50%) in StormBoard's Discovery mode. It has roughly equal monolith and multi-service signals that should confuse the AI detector.

## Mixed Signals

| Signal | Suggests | Why |
|--------|----------|-----|
| Shared `AppDbContext` used by ALL projects | **Monolith** | WebApi, NotificationService, and ReportService all query the same DbContext |
| `SharedEventBus` — in-process pub/sub | **Monolith** | True microservices use message brokers, not in-memory event buses |
| `ServiceRegistry` — static service lookup | **Monolith** | True microservices use service discovery (Consul, K8s DNS) |
| WebApi registers all service classes in one DI container | **Monolith** | All code runs in one process |
| Two standalone services with their own `Program.cs` | **Multi-Service** | Independent deployment entry points |
| `Services/` directory with separate project structure | **Multi-Service** | Suggests independent deployables |
| `HybridApp.Contracts` shared interfaces | **Multi-Service** | Service communication contracts |
| Services reference Domain project | **Multi-Service** | Loose coupling via abstractions |

## Key Ambiguity: Shared Database Coupling

The critical signal is that NotificationService and ReportService both take `AppDbContext` as a constructor dependency and directly query the shared database. This is the strongest monolith indicator — services that share a database aren't truly independent, regardless of their project structure.

At the same time, the services have their own `Program.cs` entry points and live in a `Services/` directory, which structurally looks like independent deployables.

## Expected Detection

Low confidence (<50%) — the AI should be genuinely uncertain whether this is a monolith with service-like structure or a multi-service architecture with accidental coupling. Both interpretations are defensible.

## Purpose

Tests Spec 98: Low-Confidence Topology Selection. When confidence is below 50%, the UI should present all topology options as radio buttons instead of the normal "Confirm / Override" flow.

## Companion Test Repos

- [discovery-testbed-monolith](https://github.com/thurley1/discovery-testbed-monolith) — clear monolith topology
- [discovery-testbed-modular-monolith](https://github.com/thurley1/discovery-testbed-modular-monolith) — clear modular monolith topology
- [discovery-testbed-microservices](https://github.com/thurley1/discovery-testbed-microservices) — clear microservices topology
- [discovery-testbed-monorepo](https://github.com/thurley1/discovery-testbed-monorepo) — clear monorepo multi-service topology
- [discovery-testbed-whitelabel-dirs](https://github.com/thurley1/discovery-testbed-whitelabel-dirs) — white-label directories topology
- [discovery-testbed-whitelabel-branches](https://github.com/thurley1/discovery-testbed-whitelabel-branches) — white-label branches topology
- [discovery-testbed-minimal](https://github.com/thurley1/discovery-testbed-minimal) — minimal codebase (edge case)
- [discovery-testbed-empty](https://github.com/thurley1/discovery-testbed-empty) — empty repository (edge case)
- **discovery-testbed-hybrid** (this repo) — deliberately ambiguous, mixed topology signals
