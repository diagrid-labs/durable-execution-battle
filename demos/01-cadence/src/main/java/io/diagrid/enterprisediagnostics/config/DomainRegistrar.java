package io.diagrid.enterprisediagnostics.config;

import com.uber.cadence.DomainAlreadyExistsError;
import com.uber.cadence.RegisterDomainRequest;
import com.uber.cadence.serviceclient.IWorkflowService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Component;

@Component
public class DomainRegistrar implements ApplicationRunner {

    private static final Logger log = LoggerFactory.getLogger(DomainRegistrar.class);

    private final IWorkflowService service;
    private final CadenceProperties props;

    public DomainRegistrar(IWorkflowService service, CadenceProperties props) {
        this.service = service;
        this.props = props;
    }

    @Override
    public void run(ApplicationArguments args) {
        RegisterDomainRequest req = new RegisterDomainRequest()
            .setName(props.domain())
            .setWorkflowExecutionRetentionPeriodInDays(1);
        try {
            service.RegisterDomain(req);
            log.info("registered Cadence domain '{}'", props.domain());
        } catch (DomainAlreadyExistsError ignored) {
            log.info("Cadence domain '{}' already exists", props.domain());
        } catch (Exception e) {
            log.error("failed to register Cadence domain '{}'", props.domain(), e);
            throw new IllegalStateException("Cadence domain registration failed", e);
        }
    }
}
