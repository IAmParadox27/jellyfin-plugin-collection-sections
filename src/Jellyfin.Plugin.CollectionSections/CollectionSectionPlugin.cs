using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.CollectionSections.Configuration;
using Jellyfin.Plugin.HomeScreenSections.HomeScreen;
using Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CollectionSections;

public class CollectionSectionPlugin : BasePlugin<PluginConfiguration>, IHasPluginConfiguration, IHasWebPages
{
    [ModuleInitializer]
	public static void Init()
    {
        // This is annoyingly necessary at the moment. Looking to find a solution to this.
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            return AssemblyLoadContext.All.FirstOrDefault(x => x.Name?.Contains("Referenceable") ?? false)?.Assemblies?.FirstOrDefault(x => x.FullName == args.Name);
        };
    }

    public override Guid Id => Guid.Parse("043b2c48-b3e0-4610-b398-8217b146d1a4");

    public override string Name => "Collection Sections";

    private readonly ICollectionManager m_collectionManager;
    private readonly IPlaylistManager m_playlistManager;
    private readonly ILibraryManager m_libraryManager;
    private readonly IDtoService m_dtoService;
    private readonly IUserManager m_userManager;
    private readonly IServiceProvider m_serviceProvider;

    public CollectionSectionPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, 
        ICollectionManager collectionManager, IPlaylistManager playlistManager, ILibraryManager libraryManager, 
        IUserManager userManager, IDtoService dtoService, IServiceProvider serviceProvider) : base(applicationPaths, xmlSerializer)
    {
        m_collectionManager = collectionManager;
        m_libraryManager = libraryManager;
        m_playlistManager = playlistManager;
        m_dtoService = dtoService;
        m_userManager = userManager;
        m_serviceProvider = serviceProvider;
        
        ConfigurationChanged += OnConfigurationChanged;
        
        OnConfigurationChanged(null, Configuration);
    }

    private void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
    {
        if (e is PluginConfiguration pluginConfiguration)
        {
            IHomeScreenManager homeScreenManager = m_serviceProvider.GetRequiredService<IHomeScreenManager>();

            foreach (SectionsConfig section in pluginConfiguration.Sections)
            {
                if (homeScreenManager.GetSectionTypes().Any(x => x.Section == section.UniqueId))
                {
                    IHomeScreenSection sectionInstance = homeScreenManager.GetSectionTypes().First(x => x.Section == section.UniqueId);

                    if (sectionInstance is PluginDefinedSection pluginDefinedSection)
                    {
                        pluginDefinedSection.DisplayText = section.DisplayText;
                        pluginDefinedSection.AdditionalData = section.CollectionName;

                        if (section.SectionType == SectionType.Collection)
                        {
                            pluginDefinedSection.OnGetResults = GetCollectionResults;
                        }
                        else if (section.SectionType == SectionType.Playlist)
                        {
                            pluginDefinedSection.OnGetResults = GetPlaylistResults;
                        }
                    }
                }
                else
                {
                    PluginDefinedSection pluginDefinedSection = new PluginDefinedSection(section.UniqueId, section.DisplayText)
                    {
                        AdditionalData = section.CollectionName,
                        OnGetResults = section.SectionType == SectionType.Collection
                            ? GetCollectionResults
                            : GetPlaylistResults
                    };
                    
                    homeScreenManager.RegisterResultsDelegate(pluginDefinedSection);
                }
            }
        }
    }

    private QueryResult<BaseItemDto> GetCollectionResults(HomeScreenSectionPayload payload)
    {
        DtoOptions dtoOptions = new DtoOptions
        {
            Fields = new[]
            {
                ItemFields.PrimaryImageAspectRatio,
                ItemFields.MediaSourceCount
            },
            ImageTypes = new[]
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Banner,
                ImageType.Thumb
            },
            ImageTypeLimit = 1
        };

        User? user = m_userManager.GetUserById(payload.UserId);
        BoxSet collection = m_serviceProvider.GetRequiredService<CollectionManagerProxy>().GetCollections(user)
            .FirstOrDefault(x => x.Name == payload.AdditionalData);
        
        List<BaseItem> items =  collection.GetChildren(user, true);
            
        items = items.Take(Math.Min(items.Count, 32)).ToList();
        
        return new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(items, dtoOptions));
    }

    private QueryResult<BaseItemDto> GetPlaylistResults(HomeScreenSectionPayload payload)
    {
        DtoOptions dtoOptions = new DtoOptions
        {
            Fields = new[]
            {
                ItemFields.PrimaryImageAspectRatio,
                ItemFields.MediaSourceCount
            },
            ImageTypes = new[]
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Banner,
                ImageType.Thumb
            },
            ImageTypeLimit = 1
        };

        var playlist = m_playlistManager.GetPlaylists(payload.UserId)
            .FirstOrDefault(x => x.Name == payload.AdditionalData);

        var itemsRaw = playlist.GetManageableItems()
            .Where(i => i.Item2.IsVisible(m_userManager.GetUserById(payload.UserId)));

        var groupedItems = itemsRaw.GroupBy(x =>
        {
            if (x.Item2 is Episode episode)
            {
                return episode.Series;
            }

            return x.Item2;
        });
        
        var items = groupedItems.Take(Math.Min(groupedItems.Count(), 32)).ToArray();
        
        return new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(items.Select(x => x.Key).ToList(), dtoOptions));
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        var prefix = GetType().Namespace;

        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = $"{prefix}.Configuration.config.html"
        };
    }
}