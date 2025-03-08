using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Threading.Tasks;
using System.Threading;

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
                pool.ProcessModel.IdentityType = ProcessModelIdentityType.ApplicationPoolIdentity;
                serverManager.CommitChanges();
                return;
            }

            pool = serverManager.ApplicationPools.Add(poolName);
            pool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
            pool.ManagedRuntimeVersion = "";
            pool.AutoStart = true;
            serverManager.CommitChanges();
        }

        public async Task StartPool(string poolName, int maxWaitingTimeMiliseconds = 1000, CancellationToken cancellationToken = default)
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

                await WaitUntil(poolName, ObjectState.Started, maxWaitingTimeMiliseconds, cancellationToken);
                return;
            }

            throw new Exception($"Pool with name = {poolName} not found.");
        }

        public async Task StopPool(string poolName, int maxWaitingTimeMiliseconds = 1000, CancellationToken cancellationToken = default)
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

                await WaitUntil(poolName, ObjectState.Stopped, maxWaitingTimeMiliseconds, cancellationToken);
                return;
            }

            throw new Exception($"Pool with name = {poolName} not found.");
        }


        private ObjectState GetPoolState(string poolName) {
            using var serverManager = new ServerManager();
            var pool = serverManager.ApplicationPools.FirstOrDefault(f => f.Name == poolName);
            if (pool == null) throw new Exception($"Pool with name = {poolName} not found.");
            return pool.State;
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

        private async Task WaitUntil(string poolName, ObjectState targetPoolState, int maxWaitingTimeMiliseconds = 1000, CancellationToken cancellationToken = default)
        {
            var taskDelay = Task.Delay(maxWaitingTimeMiliseconds, cancellationToken);
            var task = Task.Run(async () =>
            {
                var poolState = GetPoolState(poolName);
                var retryCount = 0;
                for (; poolState != targetPoolState;)
                {
                    poolState = GetPoolState(poolName);
                    await Task.Delay(500);
                    retryCount++;
                }
            }, cancellationToken);
            var completed = await Task.WhenAny(taskDelay, task);
            if (completed == taskDelay)
            {
                throw new Exception($"Failed Waiting Change State {poolName} to {targetPoolState.ToString()}");
            }
        }
    }
}
