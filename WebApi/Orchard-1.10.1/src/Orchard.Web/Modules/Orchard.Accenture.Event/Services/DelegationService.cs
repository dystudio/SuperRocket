using Orchard.Accenture.Event.Models;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Title.Models;
using Orchard.Data;
using Orchard.Environment.Configuration;
using Orchard.FileSystems.Media;
using Orchard.MediaLibrary.Models;
using Orchard.MediaLibrary.Services;
using Orchard.Roles.Models;
using Orchard.Security;
using Orchard.Taxonomies.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using Orchard.Logging;

namespace Orchard.Accenture.Event.Services
{
    public class DelegationService : IDelegationService
    {
        private readonly IContentManager _contentManager;
        private readonly IOrchardServices _orchardServices;
        private readonly ITransactionManager _transactionManager;
        private readonly IMembershipService _membershipService;
        private IMediaLibraryService _mediaLibraryService;
        private readonly IStorageProvider _storageProvider;
        private readonly IRepository<UserRolesPartRecord> _userRolesRepository;
        private readonly ShellSettings _shellSettings;
        public ILogger Logger { get; set; }

        public DelegationService(IContentManager contentManager,
            IOrchardServices orchardServices,
            ITransactionManager transactionManager,
            IMembershipService membershipService,
            IMediaLibraryService mediaLibraryService,
            IStorageProvider storageProvider,
            IRepository<UserRolesPartRecord> userRolesRepository,
            ShellSettings shellSettings)
        {
            _contentManager = contentManager;
            _orchardServices = orchardServices;
            _transactionManager = transactionManager;
            _membershipService = membershipService;
            _mediaLibraryService = mediaLibraryService;
            _storageProvider = storageProvider;
            _userRolesRepository = userRolesRepository;
            _shellSettings = shellSettings;
            Logger = NullLogger.Instance;
        }

        public string Process(string originalOwner, string owner)
        {
            string result = string.Empty;
            int originalUserId = _orchardServices.WorkContext.CurrentUser.Id;
            IUser originalUser = _membershipService.GetUser(originalOwner);
            IUser deleagationUser = _membershipService.GetUser(owner);

            if (deleagationUser == null)
            {
                result = Constancts.DelegationUserDoesNotExist;
                return result;
            }

            if (CheckIfCurrentUserInAdminRole())
            {
                if (originalUser == null)
                {
                    result = Constancts.OriginalUserDoesNotExist;
                    return result;
                }
                else
                {
                    originalUserId = originalUser.Id;
                    //bool originalUserInAdminRole = CheckIfUserInAdminRole(originalUserId);

                    //if (originalUserInAdminRole)
                    //{
                    //    result = Constancts.OriginalUserShouldNotBeAdminRole;
                    //    return result;
                    //}
                }
            }

            if ("SqlCe".Equals(_shellSettings.DataProvider, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Error("Data Provider is SqlCe, it may cost more time to delegate.");

                var query = _contentManager.Query(VersionOptions.Latest)
                    .Where<CommonPartRecord>(cr => cr.OwnerId == originalUserId).List();

                foreach (var item in query)
                {
                    var commonPart = item.Get<CommonPart>();

                    commonPart.Owner = deleagationUser;
                    commonPart.PublishedUtc = DateTime.UtcNow;
                    commonPart.ModifiedUtc = DateTime.UtcNow;

                    _contentManager.Publish(item);
                }
            }
            else
            {
                executeDelegate(originalUserId, deleagationUser.Id);
            }

            //move the media files to the delegated user.
            //var rootMediaFolder = _mediaLibraryService.GetRootMediaFolder();
            //var newRootMediaFolderPath = rootMediaFolder.MediaPath.Replace(rootMediaFolder.Name, owner);
            //newRootMediaFolderPath += "\\" + rootMediaFolder.Name;

            //copyFolders(rootMediaFolder.MediaPath, rootMediaFolder.MediaPath, newRootMediaFolderPath);


            return result;
        }

        public bool CheckIfCurrentUserInAdminRole()
        {
            int currentUserId = _orchardServices.WorkContext.CurrentUser.Id;
            return CheckIfUserInAdminRole(currentUserId);
        }

        public bool CheckIfUserInAdminRole(int userId)
        {
            var currentUserRoleRecords = _userRolesRepository.Fetch(x => x.UserId == userId).ToArray();
            var permissions = currentUserRoleRecords.SelectMany(t => t.Role.RolesPermissions);
            return permissions.Any(t => StandardPermissions.SiteOwner.Name
                .Equals(t.Permission.Name, StringComparison.OrdinalIgnoreCase));
        }

        public bool checkIfUserInRole(int userId, string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            var currentUserRoleRecords = _userRolesRepository.Fetch(x => x.UserId == userId).ToArray();
            var currentRoleRecords = currentUserRoleRecords.Select(x => x.Role);
            return currentRoleRecords.Any(t => roleName.Equals(t.Name, StringComparison.OrdinalIgnoreCase));
        }

        private int executeDelegate(int originalUserId, int delegationUserId)
        {
            using (SqlConnection connection = new SqlConnection(_shellSettings.DataConnectionString))
            {
                string commandString = string.Format("UPDATE Common_CommonPartRecord SET OwnerId = {1} WHERE OwnerId = {0}",
                    originalUserId, delegationUserId);
                var command = new SqlCommand(commandString, connection);

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        private void copyFolders(string folderPath, string rootName, string newRootName)
        {
            copyFiles(folderPath, rootName, newRootName);

            var folders = _storageProvider.ListFolders(folderPath);

            if (folders != null)
            {
                foreach (var folder in folders)
                {
                    string oldFolderPath = folder.GetPath();
                    string newFolderPath = oldFolderPath.Replace(rootName, newRootName);

                    if (!_storageProvider.FolderExists(newFolderPath))
                    {
                        //_storageProvider.RenameFolder(oldFolderPath, newFolderPath);
                        _storageProvider.CreateFolder(newFolderPath);
                    }

                    copyFolders(oldFolderPath, rootName, newRootName);
                }
            }
        }

        private void copyFiles(string folderPath, string rootName, string newRootName)
        {
            var files = _storageProvider.ListFiles(folderPath);

            if (files != null)
            {
                foreach (var file in files)
                {
                    string oldPath = file.GetPath();
                    string newPath = oldPath.Replace(rootName, newRootName);
                    string newFolderPath = Path.GetDirectoryName(newPath);

                    //if (_storageProvider.FileExists(newPath))
                    //{
                    //    _storageProvider.DeleteFile(newPath);
                    //}
                    //_storageProvider.CopyFile(oldPath, newPath);


                    //var stream = file.OpenRead();
                    //MemoryStream ms = new MemoryStream();
                    //using (StreamReader sr = new StreamReader(stream))
                    //{
                    //    string str = sr.ReadToEnd();
                    //    byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
                    //    ms.Write(bytes, 0, bytes.Length);
                    //}
                    //ms.Position = 0;
                    //_mediaLibraryService.ImportMedia(ms, relativePath, file.GetName());

                    var mediaParts = buildGetMediaContentItemsQuery(_orchardServices.ContentManager, folderPath, false).List();

                    foreach (var media in mediaParts)
                    {
                        var clonedContentItem = _orchardServices.ContentManager.Clone(media.ContentItem);
                        var clonedMediaPart = clonedContentItem.As<MediaPart>();
                        var clonedTitlePart = clonedContentItem.As<TitlePart>();

                        clonedMediaPart.FileName = media.FileName;
                        clonedTitlePart.Title = clonedTitlePart.Title;

                        _orchardServices.ContentManager.Publish(clonedContentItem);
                        _mediaLibraryService.CopyFile(media.FolderPath, media.FileName, newFolderPath, media.FileName);
                        //media.FolderPath = newFolderPath + media.FolderPath.Substring(folderPath.Length);
                    }

                }
            }
        }

        private IContentQuery<MediaPart> buildGetMediaContentItemsQuery(
    IContentManager contentManager, string folderPath = null, bool recursive = false, string mediaType = null, VersionOptions versionOptions = null)
        {

            var query = contentManager.Query<MediaPart>(versionOptions);

            query = query.Join<MediaPartRecord>();

            if (!String.IsNullOrEmpty(mediaType))
            {
                query = query.ForType(new[] { mediaType });
            }

            if (!String.IsNullOrEmpty(folderPath))
            {
                if (recursive)
                {
                    query = query.Join<MediaPartRecord>().Where(m => m.FolderPath.StartsWith(folderPath));
                }
                else
                {
                    query = query.Join<MediaPartRecord>().Where(m => m.FolderPath == folderPath);
                }
            }

            query = query.Join<MediaPartRecord>();

            return query;
        }

        private static class Constancts
        {
            public const string OriginalUserDoesNotExist = "Original user doesn't exist in Orchard.";
            public const string DelegationUserDoesNotExist = "Delegation user doesn't exist in Orchard.";
            public const string OriginalUserShouldNotBeAdminRole = "Original user shouldn't be in Administrator role.";

            public static class RoleName
            {
                public const string Administrator = "Administrator";
            }
        }

    }
}