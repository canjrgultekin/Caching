using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Caching.Metrics;

public static class MetricsProvider
{
    public static MeterProvider ConfigureMetrics()
    {
        return Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Caching"))
            .AddPrometheusExporter() // Prometheus Exporter
            .Build();
    }
}
