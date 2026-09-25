using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.CollectionSections.JellyfinVersionSpecific
{
    public static class UserHelper
    {
        public static IEnumerable<User> GetAllUsers(this IUserManager userManager) => userManager.GetUsers();
    }
}