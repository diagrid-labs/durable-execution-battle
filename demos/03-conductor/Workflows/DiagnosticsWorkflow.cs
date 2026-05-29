using Conductor.Client.Models;
using Conductor.Definition;
using Conductor.Definition.TaskType;
using EnterpriseDiagnostics.Conductor.Models;
using ConductorTask = Conductor.Definition.TaskType.Task;

namespace EnterpriseDiagnostics.Conductor.Workflows;

public static class DiagnosticsWorkflow
{
    public const string Name    = "diagnostics_workflow";
    public const int    Version = 1;

    public static ConductorWorkflow Build()
    {
        // ── Fan-out: 5 parallel subsystem scans ──
        var warpCoreScan    = ScanBranch("scan_warp_core",    "warp_core");
        var shieldsScan     = ScanBranch("scan_shields",      "shields");
        var lifeSupportScan = ScanBranch("scan_life_support", "life_support");
        var sensorsScan     = ScanBranch("scan_sensors",      "sensors");
        var weaponsScan     = ScanBranch("scan_weapons",      "weapons");

        var scanFork = new ForkJoinTask("scan_fork",
            BranchTasks(warpCoreScan),
            BranchTasks(shieldsScan),
            BranchTasks(lifeSupportScan),
            BranchTasks(sensorsScan),
            BranchTasks(weaponsScan));

        // Conductor requires an explicit JOIN task immediately after a FORK task.
        var scanJoin = new JoinTask("scan_join",
            warpCoreScan, shieldsScan, lifeSupportScan, sensorsScan, weaponsScan);

        // ── ~60s durable WAIT — ⚡ KILL POINT ⚡ ──
        // The conductor-csharp 1.1.4 WaitTask ctor reads duration.Seconds (the 0..59
        // component) not duration.TotalSeconds — see SDK source
        // ~/.nuget/packages/conductor-csharp/1.1.4/src/Conductor/Definition/TaskType/WaitTask.cs
        // line 25. FromSeconds(60).Seconds is 0, which Conductor treats as "no wait".
        // FromSeconds(59) is the largest single-component value and gives the demo
        // a ~60s window for the kill+restart choreography.
        var engineeringReview = new WaitTask("engineering_review", TimeSpan.FromSeconds(59));
        // SwitchTask gated on ${workflow.input.testing}. The "false" branch runs
        // the durable WaitTask; when testing=true no case matches and the switch
        // falls through immediately. Matches the in-code conditional used by the
        // other four engines.
        var engineeringReviewSwitch = new SwitchTask("engineering_review_switch", "${workflow.input.testing}")
            .WithDecisionCase("false", BranchTasks(engineeringReview));

        // ── Prioritization ──
        // SimpleTask signature is (taskName, taskReferenceName).
        // The Conductor task type is "prioritize_issues"; the reference name within
        // this workflow is "prioritize" (so other tasks can read ${prioritize.output.*}).
        var prioritize = new SimpleTask("prioritize_issues", "prioritize")
            .WithInput("ship_name", "${workflow.input.ship_name}")
            .WithInput("scans", new[]
            {
                "${scan_warp_core.output}",
                "${scan_shields.output}",
                "${scan_life_support.output}",
                "${scan_sensors.output}",
                "${scan_weapons.output}"
            });

        // ── Conditional bridge notification ──
        // SwitchTask 2nd arg is the case expression bound to "${prioritize.output.severity}".
        // value-param evaluator is set internally by the SDK.
        var notifySwitch = new SwitchTask("notify_switch", "${prioritize.output.severity}")
            .WithDecisionCase(Severity.RedAlert,    BranchTasks(NotifyBranch("notify_red")))
            .WithDecisionCase(Severity.YellowAlert, BranchTasks(NotifyBranch("notify_yellow")));

        var wf = new ConductorWorkflow()
            .WithName(Name)
            .WithVersion(Version)
            .WithDescription("USS Enterprise diagnostics — fan-out scans, 60s review, conditional bridge alert")
            .WithOwner("marc@diagrid.io")
            .WithTask(scanFork)
            .WithTask(scanJoin)
            .WithTask(engineeringReviewSwitch)
            .WithTask(prioritize)
            .WithTask(notifySwitch);

        // ConductorWorkflow does not initialize OutputParameters in its constructor;
        // initialise it before calling WithOutputParameter to avoid NullReferenceException.
        wf.OutputParameters = new Dictionary<string, object>();

        return wf
            .WithOutputParameter("ship_name",           "${workflow.input.ship_name}")
            .WithOutputParameter("diagnostics_date",    "${workflow.input.diagnostics_date}")
            .WithOutputParameter("engineer_name",       "${workflow.input.engineer_name}")
            .WithOutputParameter("priority",            "${prioritize.output}")
            .WithOutputParameter("bridge_notification", "${notify_red.output.message}");
    }

    // Note: SimpleTask signature is (taskName, taskReferenceName). For scan tasks the
    // Conductor task type name and the reference name within the workflow are
    // intentionally identical (e.g. both "scan_warp_core") so the scan output
    // references ${scan_warp_core.output} read naturally.
    // Note: WithInput is defined on the base Task class and returns Task, not SimpleTask.
    // Return type uses ConductorTask alias (= Conductor.Definition.TaskType.Task) to avoid
    // CS0266 implicit conversion error AND the namespace collision with
    // EnterpriseDiagnostics.Conductor which makes bare "Conductor.Definition..." ambiguous.
    private static ConductorTask ScanBranch(string refName, string subsystem) =>
        new SimpleTask(refName, refName)
            .WithInput("ship_name", "${workflow.input.ship_name}")
            .WithInput("subsystem", subsystem);

    private static ConductorTask NotifyBranch(string refName) =>
        new SimpleTask("notify_bridge", refName)
            .WithInput("ship_name",     "${workflow.input.ship_name}")
            .WithInput("engineer_name", "${workflow.input.engineer_name}")
            .WithInput("priority",      "${prioritize.output}");

    // Helper: convert one or more fluent builder tasks into a WorkflowTask[]
    // suitable for ForkJoinTask (params WorkflowTask[][]) and SwitchTask.WithDecisionCase
    // (params WorkflowTask[]).
    // Task.GetWorkflowDefTasks() (defined on the base Task class) collects parent tasks,
    // the task itself, and any child tasks into a List<WorkflowTask>.
    private static WorkflowTask[] BranchTasks(params ConductorTask[] tasks) =>
        tasks.SelectMany(t => t.GetWorkflowDefTasks()).ToArray();
}
