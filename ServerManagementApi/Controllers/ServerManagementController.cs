using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using ServerManagement;
using ServerManagementApi.Attributes;

namespace ServerManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerManagementController : ControllerBase
    {
        private readonly IISManagement _iisManagement;
        private readonly WindowServiceManagement _windowServiceManagement;
        public ServerManagementController()
        {
            _iisManagement = new IISManagement();
            _windowServiceManagement = new WindowServiceManagement();
        }

        [DeploymentKey()]
        [HttpPost()]
        [Route("DeployApp/{windowServiceName}/{siteName}")]
        public async Task<IActionResult> DeployApp([FromRoute] string siteName, [FromRoute] string windowServiceName, IFormFile frontend, IFormFile backend)
        {
            try
            {
                if (frontend == null || backend == null)
                {
                    return BadRequest("Both frontend and backend files must be uploaded");
                }

                if (frontend.ContentType != "application/zip" || backend.ContentType != "application/zip")
                {
                    return BadRequest("Both frontend and backend files must be zip files");
                }

                var basePath = $"C:\\inetpub\\wwwroot";

                var siteBackend = $"/{siteName}/backend";
                var siteFrontend = $"/{siteName}";

                var physicalPathBackend = $"{basePath}\\{siteName}\\backend";
                var physicalPathFrontend = $"{basePath}\\{siteName}\\frontend";
                var tempPathFrontend = $"{basePath}\\{siteName}\\temp-frontend";
                var tempPathBackend = $"{basePath}\\{siteName}\\temp-backend";
                var bgService = $"{physicalPathBackend}\\{windowServiceName}.exe";

                if (!Directory.Exists(physicalPathBackend))
                {
                    Directory.CreateDirectory(physicalPathBackend);
                }

                if (!Directory.Exists(physicalPathFrontend))
                {
                    Directory.CreateDirectory(physicalPathFrontend);
                }

                if (!Directory.Exists(tempPathBackend))
                {
                    Directory.CreateDirectory(tempPathBackend);
                }

                if (!Directory.Exists(tempPathFrontend))
                {
                    Directory.CreateDirectory(tempPathFrontend);
                }

                #region Cleanup
                _iisManagement.DeleteSiteIfExist(siteBackend);
                _iisManagement.DeleteSiteIfExist(siteFrontend);
                _windowServiceManagement.StopService(siteName);
                _windowServiceManagement.UninstallService(siteName);
                #endregion

                CleanDirectory(tempPathFrontend);
                CleanDirectory(tempPathBackend);

                #region Register IIS
                _iisManagement.CreateSite(siteName, siteFrontend, physicalPathFrontend);
                _iisManagement.CreateSite(siteName, siteBackend, physicalPathBackend);

                _iisManagement.StopPool(siteName);

                #region DEPLOY FILES
                using (var frontendStream = frontend.OpenReadStream())
                {
                    ZipFile.ExtractToDirectory(frontendStream, tempPathFrontend);
                }
                MoveAll(tempPathFrontend, physicalPathFrontend);

                using (var backendStream = backend.OpenReadStream())
                {
                    ZipFile.ExtractToDirectory(backendStream, tempPathBackend);
                }
                MoveAll(tempPathBackend, physicalPathBackend);
                #endregion

                _iisManagement.StartPool(siteName);
                _windowServiceManagement.InstallService(bgService, siteName, "Background Service for " + siteName);
                _windowServiceManagement.StartService(siteName);
                #endregion

                if (Directory.Exists(tempPathFrontend))
                {
                    Directory.Delete(tempPathFrontend, true);
                }
                
                if (Directory.Exists(tempPathBackend))
                {
                    Directory.Delete(tempPathBackend, true);
                }
                
                return Ok(true);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("InnerException", ex.InnerException.Message);
                }
                return BadRequest(ModelState);
            }
        }

        private void MoveAll(string source, string target)
        {
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }

            var sourceDir = new DirectoryInfo(source);
            var targetDir = new DirectoryInfo(target);

            foreach (var file in sourceDir.GetFiles())
            {
                file.MoveTo(Path.Combine(targetDir.FullName, file.Name), true);
            }

            foreach (var dir in sourceDir.GetDirectories())
            {
                MoveAll(dir.FullName, Path.Combine(targetDir.FullName, dir.Name));
            }
        }

        private void CleanDirectory(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }
    }
}
