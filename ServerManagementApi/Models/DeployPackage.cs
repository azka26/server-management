namespace ServerManagementApi.Models
{
    public class DeployPackage
    {
        public int Id { get; set; }
        public string? ApplicationName { get; set; }
        public int? FrontendPackageId { get; set; }
        public int? BackendPackageId { get; set; }
        public string? BackgroundServiceName { get; set; }
        public string? BackgroundServiceDescription { get; set; }
    }
}
