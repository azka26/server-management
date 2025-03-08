namespace ServerManagementApi.Models.Entities
{
    public class DeployPackage : BaseModel
    {
        public int Id { get; set; }
        public string? ApplicationName { get; set; }
        public int? FrontendPackageId { get; set; }
        public int? BackendPackageId { get; set; }
        public string? BackgroundServiceName { get; set; }
        public string? BackgroundServiceDescription { get; set; }
        public string? IpAddress { get; set; }
    }
}
