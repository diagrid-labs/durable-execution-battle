package io.diagrid.enterprisediagnostics.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "cadence")
public record CadenceProperties(
    String serviceName,
    String host,
    int port,
    String domain,
    String taskList) {}
