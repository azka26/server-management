using System.ServiceProcess;

namespace ServerManagement
{
    public class WindowServiceManagement {
        public void InstallService(string exePath, string serviceName, string serviceDescription)
        {
            try {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "sc";
                    process.StartInfo.Arguments = $"create \"{serviceName}\" binPath= \"{exePath}\"";
                    process.Start();
                    process.WaitForExit();
                }

                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "sc";
                    process.StartInfo.Arguments = $"description \"{serviceName}\" \"{serviceDescription}\"";
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch 
            {
                // Service is already installed
            }
        }

        public void UninstallService(string serviceName)
        {
            try {
                using var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "sc";
                process.StartInfo.Arguments = $"delete \"{serviceName}\"";
                process.Start();
                process.WaitForExit();
            } catch {
                // Service is not installed
            }
        }

        public void StartService(string serviceName)
        {
            try {
                using var serviceController = new ServiceController(serviceName);
                if (serviceController.Status != ServiceControllerStatus.Running)
                {
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running);
                }
            } catch {
                // Service is not installed
            }
        }

        public void StopService(string serviceName)
        {
            try {
                using var serviceController = new ServiceController(serviceName);
                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            } catch {
                // Service is not installed
            }
        }
    }
}
