package io.diagrid.enterprisediagnostics.config;

import com.uber.cadence.client.WorkflowClient;
import com.uber.cadence.client.WorkflowClientOptions;
import com.uber.cadence.serviceclient.ClientOptions;
import com.uber.cadence.serviceclient.IWorkflowService;
import com.uber.cadence.serviceclient.WorkflowServiceTChannel;
import com.uber.cadence.worker.Worker;
import com.uber.cadence.worker.WorkerFactory;
import io.diagrid.enterprisediagnostics.activity.NotifyBridgeActivityImpl;
import io.diagrid.enterprisediagnostics.activity.PrioritizeIssuesActivityImpl;
import io.diagrid.enterprisediagnostics.activity.ScanActivity;
import io.diagrid.enterprisediagnostics.workflow.DiagnosticsWorkflowImpl;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.ArrayList;
import java.util.List;

@Configuration
public class CadenceConfig {

    private final CadenceProperties props;

    public CadenceConfig(CadenceProperties props) {
        this.props = props;
    }

    @Bean(destroyMethod = "close")
    public IWorkflowService workflowService() {
        return new WorkflowServiceTChannel(
            ClientOptions.newBuilder()
                .setHost(props.host())
                .setPort(props.port())
                .setServiceName(props.serviceName())
                .build());
    }

    @Bean
    public WorkflowClient workflowClient(IWorkflowService service) {
        return WorkflowClient.newInstance(service,
            WorkflowClientOptions.newBuilder()
                .setDomain(props.domain())
                .build());
    }

    /**
     * Workflow implementations must NOT be Spring singletons — Cadence instantiates them
     * fresh per workflow execution via reflection. We register the class (not an instance)
     * so Cadence controls the lifecycle.  Activity implementations are regular Spring beans
     * and can be registered as instances.
     */
    @Bean(destroyMethod = "shutdownNow")
    public WorkerFactory workerFactory(
            WorkflowClient client,
            List<ScanActivity> scanActivities,
            PrioritizeIssuesActivityImpl prioritize,
            NotifyBridgeActivityImpl notify) {

        WorkerFactory factory = WorkerFactory.newInstance(client);
        Worker worker = factory.newWorker(props.taskList());

        // Register workflow by class — Cadence instantiates it inside the workflow thread
        worker.registerWorkflowImplementationTypes(DiagnosticsWorkflowImpl.class);

        List<Object> activityImpls = new ArrayList<>(scanActivities);
        activityImpls.add(prioritize);
        activityImpls.add(notify);
        worker.registerActivitiesImplementations(activityImpls.toArray());

        factory.start();
        return factory;
    }
}
