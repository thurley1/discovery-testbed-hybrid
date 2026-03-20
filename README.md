# discovery-testbed-hybrid

**This is a StormBoard topology test fixture, NOT a real application.**

This repository is a deliberately **architecturally cursed** codebase designed to trigger **very low confidence** (<30%) topology detection in StormBoard's Discovery mode. It has contradictory signals from every topology type simultaneously.

## Why It's Unrecognizable

- 3 entry points (monolith? multi-service? background worker?)
- Circular project references (Workers -> Api -> CursedApp, Workers -> CursedApp)
- A god class that does everything (raw SQL, HTTP, email, auth, caching)
- All models in one file
- Vendored source code in lib/ (not NuGet packages)
- SQL scripts mixed with source code
- One controller handling all endpoints
- One middleware doing auth + logging + caching + transformation

No architecture pattern fits. That's the point.

## Expected Detection

Very low confidence (<30%). The AI should struggle to classify this as any topology.

## Purpose

Tests Spec 98: Low-Confidence Topology Selection. When confidence is below 50%, the UI presents all topology options as radio buttons.

## Companion Test Repos

| Repo | Topology | Expected Confidence |
|------|----------|-------------------|
| `discovery-testbed-monolith` | Monolith | High (>80%) |
| `discovery-testbed-modular` | Modular Monolith | High (>80%) |
| `discovery-testbed-microservices` | Microservices | High (>80%) |
| `discovery-testbed-whitelabel-dirs` | White-Label (Directories) | High (>80%) |
| `discovery-testbed-whitelabel-branches` | White-Label (Branches) | High (>80%) |
| `discovery-testbed-monorepo` | Monorepo Multi-Service | High (>80%) |
| **`discovery-testbed-hybrid`** | **Unrecognizable** | **Very low (<30%)** |
