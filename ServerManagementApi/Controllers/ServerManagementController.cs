using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerManagement;
using ServerManagementApi.Attributes;
using ServerManagementApi.Models.Configurations;
using ServerManagementApi.Models.Entities;

namespace ServerManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [DeploymentKey()]
    public class ServerManagementController : ControllerBase
    {
        private readonly IISManagement _iisManagement;
        private readonly WindowServiceManagement _windowServiceManagement;
        private readonly AppSettings _appSettings;
        private readonly string _basePath;
        private readonly AppDbContext _appDbContext;
        public ServerManagementController(AppSettings appSettings, AppDbContext appDbContext)
        {
            _iisManagement = new IISManagement();
            _windowServiceManagement = new WindowServiceManagement();
            _appSettings = appSettings;
            _basePath = _appSettings.DeploymentConfiguration.BasePath;
            _appDbContext = appDbContext;
        }

        [HttpPost()]
        [Route("DeployAppOnly/{siteName}")]
        public async Task<IActionResult> DeployAppOnly([FromRoute] string siteName, IFormFile frontend, IFormFile backend)
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

                var siteBackend = $"/{siteName}/backend";
                var siteFrontend = $"/{siteName}";

                var physicalPathBackend = $"{_basePath}\\{siteName}\\backend";
                var physicalPathFrontend = $"{_basePath}\\{siteName}\\frontend";
                var tempPathFrontend = $"{_basePath}\\{siteName}\\temp-frontend";
                var tempPathBackend = $"{_basePath}\\{siteName}\\temp-backend";

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
                #endregion

                CleanDirectory(tempPathFrontend);
                CleanDirectory(tempPathBackend);

                #region Register IIS
                _iisManagement.CreateSite(siteName, siteFrontend, physicalPathFrontend);
                _iisManagement.CreateSite(siteName, siteBackend, physicalPathBackend);

                await _iisManagement.StopPool(siteName);

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

                await _iisManagement.StartPool(siteName);
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

                var siteBackend = $"/{siteName}/backend";
                var siteFrontend = $"/{siteName}";

                var physicalPathBackend = $"{_basePath}\\{siteName}\\backend";
                var physicalPathFrontend = $"{_basePath}\\{siteName}\\frontend";
                var tempPathFrontend = $"{_basePath}\\{siteName}\\temp-frontend";
                var tempPathBackend = $"{_basePath}\\{siteName}\\temp-backend";
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

                await _iisManagement.StopPool(siteName);

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

                await _iisManagement.StartPool(siteName);
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



        #region NEW FUNCTIONALITY
        [HttpPost()]
        [Route("UploadPackage")]
        public async Task<IActionResult> UploadPackageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                if (file == null)
                {
                    throw new InvalidDataException("File is required");
                }

                var allowedContentTypes = new[]
                {
                    "application/x-zip-compressed",
                    "application/zip"
                };

                if (!allowedContentTypes.Any(f => f == file.ContentType))
                {
                    throw new InvalidDataException("File must be a zip file");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".zip")
                {
                    throw new InvalidDataException("File must be a zip file");
                }

                var uploadPath = _appSettings.DeploymentConfiguration.UploadPath;
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var package = new PackageDeployment()
                {
                    OriginalPackageName = file.FileName,
                    PackageSize = file.Length,
                    ServerFileName = Guid.NewGuid().ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                var saveTo = Path.Combine(uploadPath, package.ServerFileName);
                using var fs = new FileStream(saveTo, FileMode.Create, FileAccess.ReadWrite);
                await file.CopyToAsync(fs, cancellationToken);

                _appDbContext.PackageDeployment.Add(package);
                await _appDbContext.SaveChangesAsync(cancellationToken);

                return Ok(package.Id);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("exception", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost()]
        [Route("DeployPackage")]
        public async Task<IActionResult> DeployPackageAsync([FromBody] DeployPackage model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.ApplicationName))
                {
                    throw new InvalidDataException("ApplicationName is required.");
                }

                if (!IsValidApplicationName(model.ApplicationName))
                {
                    throw new InvalidDataException("Invalid ApplicationName");
                }

                if (model.FrontendPackageId == null && model.BackendPackageId == null)
                {
                    throw new InvalidDataException("Both frontend and backend package is null.");
                }

                _iisManagement.CreatePoolIfNotExists(model.ApplicationName);
                await _iisManagement.StopPool(model.ApplicationName, cancellationToken: cancellationToken);

                await DeployFrontend(model, cancellationToken);
                await DeployBackend(model, cancellationToken);
                RegisterService(model, cancellationToken);
                
                await _iisManagement.StartPool(model.ApplicationName, cancellationToken: cancellationToken);

                return Ok(true);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("exception", e.Message);
                return BadRequest(ModelState);
            }
        }
        #endregion

        #region PRIVATE METHOD
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

        private bool IsValidApplicationName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false; // Input cannot be null or empty
            }

            // Regular expression to match a-z (case-insensitive) without spaces
            var regex = new Regex("^[a-z\\-]+$");
            return regex.IsMatch(input);
        }

        private void ExtractPackage(PackageDeployment package, string targetDir)
        {
            if (Directory.Exists(targetDir)) 
            {
                Directory.Delete(targetDir, true);
            }
            Directory.CreateDirectory(targetDir);

            var serverFile = Path.Combine(_appSettings.DeploymentConfiguration.UploadPath, package.ServerFileName);
            var fs = new FileStream(serverFile, FileMode.Open, FileAccess.Read);
            ZipFile.ExtractToDirectory(fs, targetDir);
        }

        private async Task DeployFrontend(DeployPackage model, CancellationToken cancellationToken = default)
        {
            if (!model.FrontendPackageId.HasValue || string.IsNullOrWhiteSpace(model.ApplicationName)) return;
            var package = await _appDbContext.PackageDeployment.FirstOrDefaultAsync(f => f.Id == model.FrontendPackageId, cancellationToken);
            if (package == null) throw new Exception("Frontend Package not found.");

            var applicationPath = Path.Combine(_appSettings.DeploymentConfiguration.BasePath, model.ApplicationName, "frontend");
            if (!Directory.Exists(applicationPath))
            {
                Directory.CreateDirectory(applicationPath);
            }

            var tempApplicationPath = Path.Combine(_appSettings.DeploymentConfiguration.BasePath, model.ApplicationName, "temp-frontend");
            ExtractPackage(package, tempApplicationPath);
            MoveAll(tempApplicationPath, applicationPath);

            var siteUrl = $"/{model.ApplicationName}";
            _iisManagement.DeleteSiteIfExist(siteUrl);
            _iisManagement.CreateSite(model.ApplicationName, siteUrl, applicationPath);
        }

        private async Task DeployBackend(DeployPackage model, CancellationToken cancellationToken = default)
        {
            if (!model.BackendPackageId.HasValue || string.IsNullOrWhiteSpace(model.ApplicationName)) return;
            var package = await _appDbContext.PackageDeployment.FirstOrDefaultAsync(f => f.Id == model.BackendPackageId, cancellationToken);
            if (package == null) throw new Exception("Backend Package not found.");

            var applicationPath = Path.Combine(_appSettings.DeploymentConfiguration.BasePath, model.ApplicationName, "backend");
            if (!Directory.Exists(applicationPath))
            {
                Directory.CreateDirectory(applicationPath);
            }

            var tempApplicationPath = Path.Combine(_appSettings.DeploymentConfiguration.BasePath, model.ApplicationName, "temp-backend");
            ExtractPackage(package, tempApplicationPath);
            MoveAll(tempApplicationPath, applicationPath);

            var siteUrl = $"/{model.ApplicationName}/backend";
            _iisManagement.DeleteSiteIfExist(siteUrl);
            _iisManagement.CreateSite(model.ApplicationName, siteUrl, applicationPath);
        }

        private void RegisterService(DeployPackage model, CancellationToken cancellationToken = default)
        {
            if (!model.BackendPackageId.HasValue || string.IsNullOrWhiteSpace(model.ApplicationName) || string.IsNullOrEmpty(model.BackgroundServiceName)) return;
            var applicationPath = Path.Combine(_appSettings.DeploymentConfiguration.BasePath, model.ApplicationName, "backend");
            var bgServicePath = Path.Combine(applicationPath, model.BackgroundServiceName);
            var description = model.BackgroundServiceName + " Description";
            if (!string.IsNullOrEmpty(model.BackgroundServiceDescription)) 
            {
                description = model.BackgroundServiceDescription;
            }

            _windowServiceManagement.UninstallService(model.ApplicationName);
            _windowServiceManagement.InstallService(bgServicePath, model.ApplicationName, description);
            _windowServiceManagement.StartService(model.ApplicationName);
        }
        #endregion
    }
}
