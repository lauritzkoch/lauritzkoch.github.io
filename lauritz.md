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

## AI Engineering & Agentic Development

**Deep expertise in AI-driven development:**
- **Agentic workflows** — Extensive experience delegating complex development tasks to autonomous AI agents (Devin), including multi-step feature implementation, refactoring, and debugging.
- **AI-first development cycles** — Spent entire work weeks without manually writing code, leveraging AI agents for end-to-end feature delivery while maintaining architectural oversight.
- **GitHub integration for AI** — Set up Git access and repository context for AI agents, enabling seamless integration between AI-generated code and version control workflows.
- **Multi-agent orchestration** — Coordinated multiple AI agents (Copilot, Windsurf, Devin) for different development phases, understanding when to use inline suggestions vs. agentic execution.

**Tools & platforms:**
- **GitHub Copilot** — IDE-integrated inline code completion and generation for rapid prototyping.
- **Windsurf** — Agentic AI coding assistant for larger refactoring, feature implementation, and architectural decisions.
- **Devin (app.devin.ai)** — Autonomous AI software engineer for delegated development tasks, including complex multi-file changes and integration work.
- **GitHub integration** — Connected AI tooling to repositories for context-aware, repository-aware assistance.

**Quality & ownership:**
- Maintained full ownership of code quality and architectural decisions while leveraging AI for implementation.
- Evaluated multiple AI coding tools, understanding their respective strengths and limitations.
- Reviewed, tested, and adapted AI-generated code to project conventions and layered architecture rules.
- Developed workflows for effective human-AI collaboration in software development.

**Continuous learning & thought leadership:**
- Actively studying *Vibe Coding: Building Production-Grade Software With GenAI, Chat, Agents, and Beyond* (Gene Kim & Steve Yegge, IT Revolution, 2025) to apply emerging best practices for AI-assisted software development.
- Staying current with rapidly evolving AI engineering practices and tools.
- Exploring the intersection of AI agents, code quality, and architectural patterns.

## Experience

### FirstAgenda A/S — Software Developer
**August 2023 – Present**
- Full-stack development on enterprise SaaS platform using .NET and TypeScript
- Meeting planning and scheduling systems with complex domain modeling
- Access control and authorization refactoring for multi-tenant architecture
- Custom Web Components and TypeScript frontend work
- **AI-driven development:** Leveraged agentic AI (Devin, Windsurf, Copilot) for feature implementation, refactoring, and debugging; spent entire work weeks without manual coding while maintaining architectural oversight
- Set up GitHub integration for AI agents, enabling seamless AI-assisted workflows
- Evaluated and integrated multiple AI coding tools into development processes

---

## Education & Certifications
- Bachelor in Computer Science (Datalogi)

---

## Links
- **LinkedIn:** https://www.linkedin.com/in/lauritz-%F0%9F%9A%80-fokdal-koch-1a4026192/
- **Email:** Lauritzk@gmail.com
- **Phone:** +45 29 78 04 76

---

## Notes / To Expand
- Add: additional certifications.
- Add: GitHub, portfolio links.
- Add: language proficiencies (Danish/English/etc).
- Tailor sections per job application.