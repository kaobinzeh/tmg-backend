# AGENTS

This repository is a `.NET 10` backend template. Any agent working here should follow the conventions below.

## Verification

- Always run `build` and `test` sequentially.
- Never run `dotnet build` and `dotnet test` in parallel in this repository.
- Reason: parallel execution can leave `testhost` processes holding DLL locks and cause transient copy failures during build.
- Preferred verification flow:
  1. `dotnet build TMG.slnx --no-restore`
  2. `dotnet test TMG.slnx --no-build`

## Architecture

The solution uses DDD project boundaries with vertical slices inside the application and WebAPI layers.

### Project boundaries

- `src/TMG.Domain`
  Contains domain entities, value objects, domain interfaces, and domain-level persistence contracts.
- `src/TMG.Application`
  Contains application use cases organized by feature.
- `src/TMG.Infrastructure`
  Contains implementations for persistence, caching, identity integrations, API clients, and other external dependencies.
- `src/TMG.WebAPI`
  Contains the HTTP host and controller layer only.
- `src/TMG.Consumer`
  Contains the async subscriber host.
- `src/TMG.Jobs`
  Contains the scheduled job host.
- `src/TMG.DatabaseMigrator`
  Contains database migration, pre-deploy, and post-deploy execution logic.

### DDD rules

- Infrastructure concerns live in `Infrastructure`.
- Interfaces for infrastructure-backed behavior live in `Domain`.
- Do not place caching, API client implementations, EF Core implementations, Redis code, or other external dependency code in `Application` or `WebAPI`.
- Domain abstractions should stay narrow and reflect only what the application currently needs.

### Vertical slice rules

- Organize application code by feature, not by technical type.
- Each feature gets its own folder.
- Keep command, response, handler, and result in separate files within the same application feature folder.
- Keep WebAPI request DTOs and request validators in the matching controller feature folder at the HTTP edge.
- Keep feature-specific helper classes in the same feature folder when they are only used by that feature.
- WebAPI controllers should also be organized by feature folder.
- Prefer controller-based endpoints, not minimal APIs.
- Routes and controllers should use resource-oriented naming.
- Prefer `record` over `class` for data transfer objects.
- Treat commands, requests, responses, results, and similar transport/application DTOs as records by default.
- Prefer positional record syntax for DTOs, for example `public sealed record SignInCommand(string Email, string Password);`

### Current feature layout expectation

Example:

`src/TMG.Application/Authentication/Features/SignUp`

Expected files in a feature folder:

- `SignUpCommand.cs`
- `SignUpResponse.cs`
- `SignUpResult.cs`
- `SignUpValidator.cs`
- `SignUpHandler.cs`

Matching WebAPI controller location:

`src/TMG.WebAPI/Features/Authentication/Registrations/RegistrationsController.cs`

Matching WebAPI request DTO location:

`src/TMG.WebAPI/Features/Authentication/Registrations/SignUpRequest.cs`

## Testing

### Test project layout

- Use separate unit test projects for:
  - `Application`
  - `Domain`
  - `Infrastructure`
  - `WebAPI`
  - `Consumer`
  - `Jobs`
- Use separate integration test projects for:
  - `WebAPI`
  - `Consumer`
  - `Jobs`
- Within a test project, mirror the production project structure where practical.
- Each feature worked on should have its own folder in both the relevant unit test project and integration test project, consistent with the repository's vertical slice architecture.
- All test case files for a feature should live inside that feature folder.
- Application unit tests must not be kept flat at the project root. Place them in folders that mirror the corresponding application feature path.
- If a feature controller exposes multiple action methods or endpoints, create a subfolder per endpoint beneath that controller's feature folder.
- If a controller exposes only one endpoint, keep that integration test directly inside the controller's feature folder rather than creating a one-off endpoint subfolder.
- Put the integration test file for each endpoint inside the appropriate feature folder or endpoint subfolder.
- Example: `Authentication/Sessions/SignIn`, `Authentication/Sessions/Refresh`, and `Authentication/Sessions/Logout` should each hold the integration tests for that specific endpoint flow.
- Example: `ReferenceData/Countries` can hold the integration test file directly when `CountriesController` exposes only one endpoint.
- Example: `Jobs` tests should group scenarios under folders like `HealthChecks`, `OutboxProcessing`, and `Infrastructure` instead of keeping all files flat.

### Unit test rules

- Use `NSubstitute` for mocks, substitutes, and fakes.
- Use `Shouldly` for assertions.
- Keep one test case per file.
- Name unit test files and classes with `When_{ActionUnderTest}_With{ParametersOfTest}_Should`.
- Keep the class name focused on the action and scenario, and move the outcome into the test method name.
- Do not append the asserted outcome to the file name or class name after `Should`.
- The file name and class name must stop at `Should`. The outcome belongs only in the test method name.
- Example file and class name: `When_CompletingPasswordReset_WithValidOtp_Should`
- Example test method name: `ResetPassword`
- For every WebAPI controller created, add a set of unit tests for the controller that covers happy paths, failure paths, and edge cases.
- For every handler created in the application layer, add a set of unit tests for the handler that covers happy paths, failure paths, and edge cases.
- For every handler created in the Consumer layer, add a set of unit tests for the handler that covers happy paths, failure paths, and edge cases.
- For every background service created in the Jobs layer, add a set of unit tests for the background service that covers happy paths, failure paths, and edge cases.
- Prefer method-local scenario variables over repeated string literals.
- Reuse helper factory methods or test context builders where available.
- Keep unit tests focused on the behavior of the unit under test, not infrastructure wiring.

### Integration test rules

- Integration tests should cover the happy path only.
- For each endpoint, keep a single happy path test that covers the most logical end-to-end behavior of that flow.
- Focus on the path that gives the most meaningful end-to-end code-path coverage.
- When additional integration tests exist around related behaviors, the primary expectation is still that the happy path is what defines the core scenario coverage.
- For every WebAPI action method added to any controller, add an integration test for that action method.
- When a controller has multiple action methods, organize those integration tests under endpoint-specific subfolders inside the controller's feature folder instead of keeping the files flat.
- When a controller has only one action method, keep that integration test in the controller's feature folder without adding an extra endpoint folder.
- For every handler added to the Consumer project, add an integration test for that handler.
- For every background service added to the Jobs project, add an integration test for that background service.
- Keep one endpoint scenario per file.
- Prefer the same naming convention as unit tests for new integration tests when practical:
  `When_{ActionUnderTest}_With{ParametersOfTest}_Should`
  Then use the test method name for the outcome being asserted.
- Do not append the asserted outcome to integration test file names or class names after `Should`.
- Each integration test class must implement `IAsyncLifetime`.
- Use `InitializeAsync` for:
  - seeding data required by the scenario
  - creating prerequisite records
  - creating any SQL view, function, or stored procedure needed by the test
- Use `DisposeAsync` for:
  - deleting records created or touched by the test
  - clearing test doubles or in-memory stores used by the test
- Cleanup should be explicit and targeted.
- Do not use vague global wipes when only a small set of records were touched.
- Use `Given / When / Then` structure only inside the method under test, typically as local anonymous functions inside the test method.
- Helper methods outside `Verify()` should use normal verb-based names.

### Current integration testing conventions

- Testcontainers are used for integration dependencies.
- Shared container setup stays in the project fixture.
- Per-test data setup and cleanup belongs in the test class via `IAsyncLifetime`.
- WebAPI auth integration tests should delete the authentication records for the email used by the scenario.
- Consumer and Jobs integration tests currently validate health endpoints and normally do not create application records, so their cleanup is usually limited to response disposal and host disposal.

## Additional repository conventions

- Use `TimeProvider` for time-related behavior instead of direct `DateTime.UtcNow` in application code.
- Use `Guid.CreateVersion7()` for generated GUID values across application and test code instead of `Guid.NewGuid()`.
- In method signatures, use `CancellationToken cancellationToken` and do not use a default value (do not write `= default`).
- ASP.NET Core Identity is the authentication base. Do not reintroduce custom authentication flows when built-in Identity behavior is sufficient.
- Keep application dependencies narrow. If wrapping `UserManager`, expose only the methods the application currently uses.
- Always simplify type and namespace usage inside files. Do not use fully qualified type names when a `using` directive makes the code clearer.
- When multiple types have the same name, prefer `using` aliases for the ambiguous types instead of leaving fully qualified names inline throughout the file.
- Keep top-level types in separate files. Do not place an interface and its implementation in the same `.cs` file.
- Enum values should always start from `1` unless an existing enum in the repository already forces a different persisted contract.
- Use `StakeholderId` in observability custom events for actor or subject identification whenever it can be resolved. Only fall back to `UserId`, `Email`, or other identifiers when `StakeholderId` is genuinely unavailable.

## Pull Requests

- When asked to write a PR title and description, always use the repository's PR template from `.github/pull_request_template.md`.
- Do not invent a free-form description when a template exists.
