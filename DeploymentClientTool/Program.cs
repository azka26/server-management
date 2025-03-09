namespace DeploymentClientTool;
public partial class Main
{

    class Program
    {
        /// <summary>
        /// Parameters:
        /// -baseUrl="http://localhost/deployment-tools"
        /// -frontEndPackage="D:\package\packageName.zip"
        /// -backEndPackage="D:\package\packageName.zip"
        /// -applicationName="appName"
        /// -backgroundServiceName="serviceName"
        /// -backgroundServiceDescription="serviceDescription"
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var parameter = BindParameter(args);
            if (!ValidateParameter(parameter))
            {
                return;
            }

            int backendId = 0;
            if (parameter.BackendPackage != null)
            {
                Console.WriteLine("Deploying BackendPackage...");
                var fileInfo = new FileInfo(parameter.BackendPackage);
                var url = $"{parameter.BaseUrl!.TrimEnd('/')}/api/ServerManagement/UploadPackage";
                backendId = FileUploader.UploadFileAsync(url, fileInfo).Result;
            }

            int frontendId = 0;
            if (parameter.FrontendPackage != null)
            {
                Console.WriteLine("Deploying FrontendPackage...");
                var fileInfo = new FileInfo(parameter.FrontendPackage);
                var url = $"{parameter.BaseUrl!.TrimEnd('/')}/api/ServerManagement/UploadPackage";
                frontendId = FileUploader.UploadFileAsync(url, fileInfo).Result;
            }

            var deployPackageRequest = new DeployPackageRequest
            {
                ApplicationName = parameter.ApplicationName,
                FrontendPackageId = frontendId,
                BackendPackageId = backendId,
                BackgroundServiceName = parameter.BackgroundServiceName,
                BackgroundServiceDescription = parameter.BackgroundServiceDescription
            };
        }

        static DeploymentParameter BindParameter(string[] args)
        {
            var parameter = new DeploymentParameter();
            foreach (var arg in args)
            {
                var argLower = arg.ToLower();
                if (argLower.StartsWith("-baseUrl=".ToLower()))
                {
                    parameter.BaseUrl = arg.Substring("-baseUrl=".Length).Trim('\"').Trim('\'');
                }
                else if (argLower.StartsWith("-frontEndPackage=".ToLower()))
                {
                    parameter.FrontendPackage = arg.Substring("-frontEndPackage=".Length).Trim('\"').Trim('\'');
                }
                else if (argLower.StartsWith("-backEndPackage=".ToLower()))
                {
                    parameter.BackendPackage = arg.Substring("-backEndPackage=".Length).Trim('\"').Trim('\'');
                }
                else if (argLower.StartsWith("-applicationName=".ToLower()))
                {
                    parameter.ApplicationName = arg.Substring("-applicationName=".Length).Trim('\"').Trim('\'');
                }
                else if (argLower.StartsWith("-backgroundServiceName=".ToLower()))
                {
                    parameter.BackgroundServiceName = arg.Substring("-backgroundServiceName=".Length).Trim('\"').Trim('\'');
                }
                else if (argLower.StartsWith("-backgroundServiceDescription=".ToLower()))
                {
                    parameter.BackgroundServiceDescription = arg.Substring("-backgroundServiceDescription=".Length).Trim('\"').Trim('\'');
                }
            }
            return parameter;
        }

        static bool ValidateParameter(DeploymentParameter parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter.BaseUrl))
            {
                Console.WriteLine("BaseUrl is required.");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(parameter.FrontendPackage) && string.IsNullOrWhiteSpace(parameter.BackendPackage))
            {
                Console.WriteLine("FrontendPackage or BackendPackage is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(parameter.ApplicationName))
            {
                Console.WriteLine("ApplicationName is required.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(parameter.BackgroundServiceName))
            {
                if (string.IsNullOrWhiteSpace(parameter.BackendPackage))
                {
                    Console.WriteLine("BackendPackage is required for deploy Background Service.");
                    return false;
                }
            }

            return true;
        }
    }
}
