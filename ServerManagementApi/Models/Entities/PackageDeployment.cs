namespace ServerManagementApi.Models.Entities
{
    public class PackageDeployment : BaseModel
    {
        public int Id { get; set; }
        public string OriginalPackageName { get; set; }
        public string ServerFileName { get; set; }
        public long PackageSize { get; set; }
        public string IpAddress { get; set; }
    }
}
