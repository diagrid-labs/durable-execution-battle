# conductor-csharp SDK notes (pinned)

**Package version:** `conductor-csharp 1.1.4`
**Verified on:** 2026-05-17
**Target framework used for probe:** `net10.0`

No 2.x or 3.x stable release exists. The entire published history of `conductor-csharp` tops out at `1.1.4` (latest stable). The spec's `.csproj` placeholder `Version="3.x.x"` is wrong — use `1.1.4`.

---

## Type and method bindings

| Plan/spec name                          | Actual symbol in this version                                                                                                          |
|-----------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| `ConductorWorkflow` (namespace)         | `Conductor.Definition.ConductorWorkflow` — inherits `WorkflowDef`                                                                     |
| `.WithName(string)`                     | `public ConductorWorkflow WithName(string name)` — exists, same name                                                                  |
| `.WithVersion(int)`                     | `public ConductorWorkflow WithVersion(int version)` — exists, same name                                                               |
| `.WithTask(...)`                        | `public ConductorWorkflow WithTask(params WorkflowTask[] tasks)` — exists, same name                                                  |
| `.WithDescription(string)`              | `public ConductorWorkflow WithDescription(string description)` — exists, same name                                                    |
| `.WithOwnerEmail(string)`               | **Does NOT exist.** Actual method: `public ConductorWorkflow WithOwner(string ownerEmail)` — the parameter is `ownerEmail` but the method is `WithOwner`, not `WithOwnerEmail` |
| `.WithOutput(string, string)`           | **Does NOT exist.** Actual method: `public ConductorWorkflow WithOutputParameter(string key, object value)` — rename everywhere in spec |
| `ForkJoinTask` ctor                     | `public ForkJoinTask(string taskReferenceName, params WorkflowTask[][] forkTasks)` — note: param name is `taskReferenceName` (not `refName`), branches typed `WorkflowTask[][]` |
| `WaitTask` ctor + duration setter       | **No `.WithDuration()` method.** Duration is passed directly in the constructor: `new WaitTask(string taskReferenceName, TimeSpan duration)`. A second ctor takes `DateTime until`. No separate setter exists. **Bug note:** internally uses `duration.Seconds` (not `duration.TotalSeconds`) — a `TimeSpan.FromSeconds(60)` works fine; a `TimeSpan.FromMinutes(1)` would produce `"0s"`. Use `TimeSpan.FromSeconds(60)` as the spec shows. |
| `SimpleTask` ctor + input setter        | `public SimpleTask(string taskName, string taskReferenceName)` — note **reversed arg order** vs spec examples: first arg is `taskName` (the Conductor task type name), second is `taskReferenceName`. Input setter: `public Task WithInput(string key, object value)` on the base `Task` class — method exists as shown in spec. |
| `SwitchTask` ctor + case + expression   | Constructor: `public SwitchTask(string taskReferenceName, string caseExpression, bool useJavascript = false)` — second arg is the value-param expression (e.g. `"${workflow.input.ship_name}"`), **not** the evaluator-type string `"value-param"`. `.WithDecisionCase(string key, params WorkflowTask[] tasks)` — spec says `.SwitchCase(...)` which **does NOT exist**. `.WithExpression(...)` **does NOT exist** — `Expression` is a property set internally by the constructor. `.WithInput(...)` (inherited) works for binding the switch parameter value. |
| `[WorkerTask]` attribute                | `Conductor.Client.Worker.WorkerTask` — attribute name is `WorkerTask`, namespace `Conductor.Client.Worker`. Constructor: `WorkerTask(string taskType = default, int batchSize = default, string domain = default, int pollIntervalMs = default, string workerId = default)` — named args `taskType:` and `workerId:` match the spec's usage. Applied at method level (class-level also supported but coordinator uses method-level discovery). |
| `AddConductorWorkflowTaskCoordinator`   | **Does NOT exist.** Actual DI extension: `AddConductorWorker(Configuration configuration = null, ...)` in `Conductor.Client.Extensions.DependencyInjectionExtensions`. No `.WithWorker<T>()` chaining method exists. |
| `.WithWorker<T>()`                      | **Does NOT exist.** Workers are registered individually via `AddConductorWorkflowTask<T>(IServiceCollection, T worker)` passing an instance, or discovered automatically at startup by `WorkflowTaskCoordinator.DiscoverWorkers()` which scans all loaded assemblies for `[WorkerTask]`-decorated methods and instantiates them via `Activator.CreateInstance`. |
| `WorkflowResourceApi` ctor              | Three overloads: `WorkflowResourceApi()`, `WorkflowResourceApi(string basePath)`, `WorkflowResourceApi(Conductor.Client.Configuration configuration = null)` — no `ApiClient` overload. |
| `WorkflowResourceApi.StartWorkflow`     | `public string StartWorkflow(StartWorkflowRequest body)` — returns `string` (the workflow execution id). Also overloaded as `StartWorkflow(string name, Dictionary<string, Object> body, int? version, ...)`. |
| `WorkflowResourceApi.GetExecutionStatus`| `public Workflow GetExecutionStatus(string workflowId, bool? includeTasks = null, bool? summarize = null)` — returns `Conductor.Client.Models.Workflow`. The spec's call `GetExecutionStatus(id, includeTasks: false)` compiles correctly (`bool?` accepts `false`). |
| `WorkflowResourceApi.Terminate`         | `public void Terminate(string workflowId, string reason = null, bool? triggerFailureWorkflow = null)` — first param is `workflowId`, not `id`. Spec usage `api.Terminate(id, "manual termination")` is positionally correct. |
| `MetadataResourceApi` register workflow | **No `Update(...)` method.** Actual method: `public Object UpdateWorkflowDefinitions(List<WorkflowDef> body, bool? overwrite = null)`. There is also `public Object Create(WorkflowDef body, bool? overwrite = null)` (single def, not a list). `WorkflowExecutor.RegisterWorkflow` wraps these two for convenience. |
| `MetadataResourceApi` register task     | `public Object RegisterTaskDef(List<TaskDef> body)` — takes a `List<TaskDef>`, matches spec. |
| `MetadataResourceApi` update task       | `public Object UpdateTaskDef(TaskDef body)` — takes a single `TaskDef`, matches spec. |
| `ConductorWorkflow.ToWorkflowDef()`     | **Does NOT exist.** `ConductorWorkflow` already **is** a `WorkflowDef` (it subclasses it directly). Pass the `ConductorWorkflow` instance directly wherever a `WorkflowDef` is expected — e.g. `metadataApi.UpdateWorkflowDefinitions(new List<WorkflowDef> { workflow })`. |

---

## SWITCH evaluator type string

- The `SwitchTask` source hardcodes `private const string VALUE_PARAM_NAME = "value-param"` and sets `EvaluatorType = VALUE_PARAM_NAME` when `useJavascript = false`.
- The second constructor argument is the **value expression** (the input key to switch on), not the evaluator type string. The evaluator type is always set internally.
- **Verified value:** `"value-param"` (hyphenated). This is set by the SDK's constructor — the application never needs to pass it explicitly.
- To switch on `${prioritize.output.severity}`, construct: `new SwitchTask("notify_switch", "${prioritize.output.severity}")` — the constructor sets `Expression = "switchCaseValue"` and `WithInput("switchCaseValue", "${prioritize.output.severity}")` automatically. Then add cases with `.WithDecisionCase("red_alert", ...)` etc.

---

## Wire format defaults

- The SDK uses **Newtonsoft.Json** (RestSharp + `NewtonsoftJson` serializer).
- Model properties are annotated with `[DataMember(Name = "camelCase")]` — e.g. `timeoutPolicy`, `ownerEmail`, `schemaVersion`. The wire format the SDK sends to Conductor is **camelCase** (Conductor's own REST API expects camelCase JSON for its metadata — `name`, `ownerEmail`, `tasks`, `inputParameters`, etc.).
- Worker input/output deserialization (`ObjectMapperProvider`) uses plain `JsonSerializerSettings` with no naming strategy — property names round-trip by their C# name or `[DataMember]` name.
- The `JsonNamingPolicy.SnakeCaseLower` in `Program.cs` applies **only to the ASP.NET minimal API** (request/response bodies on `POST /start`, `GET /status`, etc.) — it does **not** affect what the SDK sends to Conductor.
- **Recommendation:** Keep the `JsonNamingPolicy.SnakeCaseLower` override on `JsonOptions` for the ASP.NET endpoints (so the `.http` file can use `ship_name` etc.). Do **not** apply it to the SDK's own serialization path. The SDK's `ApiClient` uses its own `JsonSerializerSettings` independently.

---

## Notable findings / divergences from the plan

1. **No 3.x version exists.** The spec's `.csproj` shows `Version="3.x.x"`. The actual latest stable is `1.1.4`. Pin `1.1.4` in the `.csproj`.

2. **`WaitTask` has no `.WithDuration()` method.** The spec's code `new WaitTask("engineering_review").WithDuration(TimeSpan.FromSeconds(60))` does not compile. The correct call is `new WaitTask("engineering_review", TimeSpan.FromSeconds(60))` — duration goes in the constructor. Additionally, the internal implementation uses `duration.Seconds` (the seconds component), not `duration.TotalSeconds`, which is fine for values under 60s but is a subtle bug for durations ≥ 1 minute that should be noted.

3. **`SwitchTask` constructor semantics are different from the spec.** The spec writes `new SwitchTask("notify_switch", "value-param")` (passing the evaluator type as the second arg). The actual second arg is the **case expression value** (e.g. `"${prioritize.output.severity}"`). The evaluator type is always set to `"value-param"` internally. Methods `.WithExpression(...)` and `.SwitchCase(...)` referenced in the spec do not exist; use `.WithDecisionCase(string key, params WorkflowTask[] tasks)` instead.

4. **`AddConductorWorkflowTaskCoordinator(...).WithWorker<T>()` chain does not exist.** The spec's `Program.cs` wiring pattern is not how the SDK works. The actual pattern is: `services.AddConductorWorker(configuration)` registers the coordinator; workers are either registered as `IWorkflowTask` instances via `services.AddConductorWorkflowTask(workerInstance)`, or discovered automatically at startup via assembly scanning (the coordinator calls `DiscoverWorkers()` which finds all `[WorkerTask]`-decorated methods across loaded assemblies). The `services.WithHostedService()` call registers `WorkflowTaskService` as a `BackgroundService`.

5. **`WithOwnerEmail(string)` does not exist; use `WithOwner(string ownerEmail)`.** The method name is `WithOwner`, not `WithOwnerEmail`.

6. **`WithOutput(string, string)` does not exist; use `WithOutputParameter(string key, object value)`.** The method name is `WithOutputParameter`.

7. **`ConductorWorkflow.ToWorkflowDef()` does not exist.** `ConductorWorkflow` is already a `WorkflowDef` subclass — pass it directly as `WorkflowDef`. The spec line `metadataApi.Update(new List<WorkflowDef> { workflow.ToWorkflowDef() })` should become `metadataApi.UpdateWorkflowDefinitions(new List<WorkflowDef> { workflow })`. There is no `Update(...)` method — the correct method is `UpdateWorkflowDefinitions(List<WorkflowDef>)`.

8. **`SimpleTask` constructor arg order is `(taskName, taskReferenceName)` — first arg is the Conductor task type name, second is the reference name.** The spec's `ScanBranch` helper uses `new SimpleTask(refName, refName)` which happens to work when both are the same string, but `new SimpleTask("prioritize", "prioritize_issues")` in the spec reverses the intent — should be `new SimpleTask("prioritize_issues", "prioritize")` if `"prioritize_issues"` is the Conductor task type and `"prioritize"` is the reference name.
