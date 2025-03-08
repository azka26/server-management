using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using ServerManagementApi.Models;

namespace ServerManagementApi.Attributes
{
    public class DeploymentKeyAttribute : ActionFilterAttribute
    {
        public DeploymentKeyAttribute()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var appsettings = httpContext.RequestServices.GetService<AppSettings>()!;
            var expectedKey = appsettings.DeploymentConfiguration.DeploymentKey;

            if (string.IsNullOrEmpty(expectedKey))
            {
                context.Result = new ForbidResult();
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue("DeploymentKey", out var extractedKey))
            {
                context.Result = new ForbidResult();
                return;
            }

            if (!string.Equals(extractedKey, expectedKey, StringComparison.Ordinal))
            {
                context.Result = new ForbidResult();
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
