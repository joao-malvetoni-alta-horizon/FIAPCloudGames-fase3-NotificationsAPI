namespace Notifications.Functions;

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Configura o pipeline de traces do OpenTelemetry, exportando via OTLP para o New Relic.
/// Instanciado uma vez por ambiente de execução (cold start), reaproveitado entre invocações.
/// </summary>
public static class Telemetry
{
    public static TracerProvider BuildTracerProvider(string serviceName)
    {
        string licenseKey = Environment.GetEnvironmentVariable("NEW_RELIC_LICENSE_KEY")
            ?? throw new InvalidOperationException("NEW_RELIC_LICENSE_KEY não configurada.");

        return Sdk.CreateTracerProviderBuilder()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .AddAWSInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAWSLambdaConfigurations(options => options.DisableAwsXRayContextExtraction = true)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri("https://otlp.nr-data.net:4318/v1/traces");
                otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                otlp.Headers = $"api-key={licenseKey}";
            })
            .Build();
    }
}
