package io.diagrid.enterprisediagnostics;

import io.diagrid.enterprisediagnostics.config.CadenceProperties;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;

@SpringBootApplication
@EnableConfigurationProperties(CadenceProperties.class)
public class EnterpriseDiagnosticsApplication {
    public static void main(String[] args) {
        SpringApplication.run(EnterpriseDiagnosticsApplication.class, args);
    }
}
