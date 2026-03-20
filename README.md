# discovery-testbed-hybrid

**This is a StormBoard topology test fixture, NOT a real application.**

This repository is a deliberately **ambiguous** codebase designed to trigger **low-confidence** topology detection (<50%) in StormBoard's Discovery mode. It has mixed signals from multiple topology types:

## Mixed Signals

| Signal | Suggests | Why |
|--------|----------|-----|
| Shared `AppDbContext` | Monolith | All entities in one database |
| `HybridApp.WebApi` with direct DB access | ModularMonolith | Main app with module structure |
| Two standalone services with `Program.cs` | MonorepoMultiService | Independent deployment entry points |
| Services reference only Domain, not Data | Microservices | Service isolation |
| `HybridApp.Contracts` shared interfaces | MonorepoMultiService | Service communication contracts |

## Expected Detection

Low confidence (<50%) — could be classified as ModularMonolith or MonorepoMultiService. The AI should be uncertain.

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
