using Jellyfin.Plugin.CollectionSections.JellyfinVersionSpecific;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.CollectionSections.Services
{
    public class StartupService : IScheduledTask
    {
        public string Name => "CollectionSections Startup";

        public string Key => "Jellyfin.Plugin.CollectionSections.Startup";
        
        public string Description => "Startup Service for CollectionSections";
        
        public string Category => "Startup Services";
        
        private readonly IServerApplicationHost m_serverApplicationHost;
        private readonly IApplicationPaths m_applicationPaths;

        public StartupService(IServerApplicationHost serverApplicationHost, IApplicationPaths applicationPaths)
        {
            m_serverApplicationHost = serverApplicationHost;
            m_applicationPaths = applicationPaths;
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            
            CollectionSectionPlugin.Instance.OnConfigurationChanged(this, CollectionSectionPlugin.Instance.Configuration);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => StartupServiceHelper.GetDefaultTriggers();
    }
}