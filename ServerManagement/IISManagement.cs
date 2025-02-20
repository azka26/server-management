using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Linq;
using System.Configuration;

namespace ServerManagement
{
    public class IISManagement
    {   
        public void CreatePoolIfNotExists(string poolName)
        {
            using var serverManager = new ServerManager();
            var pool = serverManager.ApplicationPools.FirstOrDefault(f => f.Name == poolName);
            if (pool != null)
            {
                pool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                pool.ManagedRuntimeVersion = "";
                pool.AutoStart = true;
                serverManager.CommitChanges();
                return;
            }

            pool = serverManager.ApplicationPools.Add(poolName);
            pool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
            pool.ManagedRuntimeVersion = "";
            pool.AutoStart = true;
            serverManager.CommitChanges();
        }

        public void StartPool(string poolName)
        {
            using var serverManager = new ServerManager();
            var pool = serverManager.ApplicationPools.FirstOrDefault(f => f.Name == poolName);
            if (pool != null)
            {
                if (pool.State != ObjectState.Started)
                {
                    pool.Start();
                    serverManager.CommitChanges();
                }

                return;
            }
            
            throw new Exception($"Pool with name = {poolName} not found.");
        }

        public void StopPool(string poolName)
        {
            using var serverManager = new ServerManager();
            var pool = serverManager.ApplicationPools.FirstOrDefault(f => f.Name == poolName);
            if (pool != null)
            {
                if (pool.State != ObjectState.Stopped)
                {
                    pool.Stop();
                    serverManager.CommitChanges();
                }

                return;
            }

            throw new Exception($"Pool with name = {poolName} not found.");
        }

        public void DeleteSiteIfExist(string siteUrl)
        {
            using var serverManager = new ServerManager();
            Site defaultSite = serverManager.Sites["Default Web Site"];
            if (defaultSite != null)
            {
                if (defaultSite.Applications.Any(a => a.Path == siteUrl))
                {
                    defaultSite.Applications.Remove(defaultSite.Applications[siteUrl]);
                    serverManager.CommitChanges();
                }

                return;
            }
        }

        public void CreateSite(string poolName, string siteUrl, string physicalPath)
        {
            CreatePoolIfNotExists(poolName);

            using ServerManager serverManager = new ServerManager();
            var appPool = serverManager.ApplicationPools.FirstOrDefault(f => f.Name == poolName);
            Site defaultSite = serverManager.Sites["Default Web Site"];
            if (defaultSite != null)
            {
                if (defaultSite.Applications.Any(a => a.Path == siteUrl))
                {
                    throw new Exception($"Site Url = {siteUrl} already exists.");
                }

                Application newApplication = defaultSite.Applications.Add(siteUrl, physicalPath);
                newApplication.ApplicationPoolName = poolName;
                serverManager.CommitChanges();
            }
            else
            {
                throw new Exception("Default Web Site not found.");
            }
        }

        public void DeployApps(string appName, string physicalPath)
        {
            var siteUrlFrontend = $"/{appName}";
            var siteUrlBackend = $"/{appName}/backend";

            var physicalPathFrontend = Path.Combine(physicalPath, appName, "frontend");
            var physicalPathBackend = Path.Combine(physicalPath, appName, "backend");

            if (!Directory.Exists(physicalPathFrontend))
            {
                Directory.CreateDirectory(physicalPathFrontend);
            }

            if (!Directory.Exists(physicalPathBackend))
            {
                Directory.CreateDirectory(physicalPathBackend);
            }

            CreatePoolIfNotExists(appName);
            StopPool(appName);

            DeleteSiteIfExist(siteUrlBackend);
            DeleteSiteIfExist(siteUrlFrontend);

            CreateSite(appName, siteUrlFrontend, physicalPathFrontend);
            CreateSite(appName, siteUrlBackend, physicalPathBackend);

            StartPool(appName);
        }
    }
}
