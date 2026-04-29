# Lauritz Fokdal Koch — Professional Profile

## Summary
Full-stack .NET developer with experience building enterprise SaaS applications,
including both multi-tenant (shared database, tenant-isolated) and single-tenant
(dedicated instance per customer) architectures. Strong focus on layered
architecture, code quality, and effective integration of AI-assisted development
tools.

---

## Core Technical Skills

| Area          | Technologies                                                                 |
|---------------|------------------------------------------------------------------------------|
| Backend       | .NET 8, ASP.NET Core MVC, Entity Framework Core 8, Dependency Injection, Hangfire |
| Architecture  | Layered architecture, Result/Option patterns, Domain Events, CQRS-lite (Query/Repository split) |
| Frontend      | TypeScript (strict), Web Components (Custom Elements API), SCSS, esbuild     |
| Database      | SQL Server, migration-based schema evolution (DbUp), query tagging/diagnostics |
| Testing       | xUnit, integration tests with real DB fixtures, Cypress E2E                  |
| SaaS Models   | Multi-tenant (shared DB w/ tenant ID isolation), Single-tenant (dedicated DB per customer) |

---

## Key Project Contributions

### Meeting Planning Module — Full-Stack Feature Development
Designed and implemented a meeting plan scheduling system within an enterprise
ASP.NET Core MVC application:
- **Placeholder architecture** — Modeled meeting plans with placeholder agenda
  items that propagate to concrete meetings upon instantiation, supporting
  forward-planning workflows without premature data commitment.
- **Cross-committee parallelism** — Extended the domain model to support
  concurrent meetings across organizational units, handling FK constraints,
  access scoping, and query isolation via EF Core.
- **Service layer orchestration** — Built service following strict layered
  architecture (Controller → Service → ValidationService → Queries/Repositories)
  with `Result<T>` return types for explicit success/failure handling.

### Access Control Refactoring — Query Layer Consolidation
Refactored authorization logic into a centralized access query component:
- Consolidated scattered permission checks into composable, reusable query
  methods with `AsNoTracking()` and `.TagWith()` for diagnostics.
- Eliminated N+1 access check patterns by batching permission evaluation at
  the query level.
- Established a single source of truth for access decisions, simplifying
  auditing and reducing security surface area.

### Custom Web Components — TypeScript / Vanilla JS
Built reusable Custom Elements (`XChooser`, `XTemplateChooser`) extending a
shared base element class:
- Implemented with NodeNext module resolution (`.js` imports), no jQuery.
- Encapsulated search, filtering, async data fetching, and keyboard navigation.
- Designed for composition — components communicate via custom events and
  slot-based content projection.
- Iterated through multiple UX refinement cycles, optimizing for accessibility
  and responsiveness.

### Backend Validation Framework — Unified Error Handling
Architected a validation pipeline spanning backend rules to frontend display:
- **ValidationResult** — Immutable result type with factory methods
  (`Success()`, `Error()`, `Warning()`) aggregating keyed error messages.
- **ValidationService + Rules separation** — Services fetch required data via
  Queries, then delegate to pure rule classes with no DB access for testability.
- **Problem Details response** — Controllers return `ValidationProblem()` with
  structured `ModelState` errors on failure.
- **ValidationDialogService (TypeScript)** — Frontend service consuming
  `ProblemDetails` JSON and rendering errors via a dedicated Web Component.

---

## AI-Assisted Development

**Tools used in daily workflow:**
- **GitHub Copilot** — IDE-integrated inline code completion and generation.
- **Windsurf** — Agentic AI coding assistant for larger refactoring and
  feature implementation.
- **Devin (app.devin.ai)** — Autonomous AI software engineer for delegated
  development tasks.
- **GitHub integration** — Connected AI tooling to repositories for
  context-aware assistance.

**Approach:**
- Integrated AI assistants into daily development while maintaining ownership
  of code quality and architectural decisions.
- Evaluated multiple AI coding tools, understanding their respective strengths
  (inline suggestions vs. agentic execution).
- Reviewed, tested, and adapted AI-generated code to project conventions and
  layered architecture rules.

**Continuous learning:**
- Reading *Vibe Coding: Building Production-Grade Software With GenAI, Chat,
  Agents, and Beyond* (Gene Kim & Steve Yegge, IT Revolution, 2025) to apply
  emerging best practices for AI-assisted software development.

---

## Notes / To Expand
- Add: years of experience, employer names, dates.
- Add: education, certifications.
- Add: links (LinkedIn, GitHub, portfolio).
- Add: language proficiencies (Danish/English/etc).
- Tailor sections per job application.