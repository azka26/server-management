namespace DeploymentClientTool;
public class DeploymentParameter
{
    public string? BaseUrl { get; set; }
    public string? FrontendPackage { get; set; }
    public string? BackendPackage { get; set; }
    public string? ApplicationName { get; set; }
    public string? BackgroundServiceName { get; set; }
    public string? BackgroundServiceDescription { get; set; }
    public string? DeploymentKey { get; set; }
}
