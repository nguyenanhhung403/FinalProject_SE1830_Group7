namespace EVWarrantyManagement.Configuration;

public class N8nSettings
{
    public string? BaseUrl { get; set; }

    public string? WorkflowPath { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>
    /// Override hostname for image URLs when sending to n8n.
    /// Useful when n8n runs in Docker and needs to access host machine.
    /// Examples: "host.docker.internal" (Docker Desktop), "172.17.0.1" (Linux Docker), or your machine's IP.
    /// </summary>
    public string? ImageHostOverride { get; set; }
}

