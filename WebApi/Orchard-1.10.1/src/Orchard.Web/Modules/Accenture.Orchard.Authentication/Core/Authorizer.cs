using Orchard;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Security.Permissions;
using Orchard.UI.Notify;
using Orchard.Users.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;

namespace Accenture.Orchard.Authentication.Core
{
    [OrchardSuppressDependency("Orchard.Security.Authorizer")]
    public class Authorizer : IAuthorizer
    {
        private const string AdminPanelAccess = "AccessAdminPanel";

        private readonly IAuthorizationService _authorizationService;
        private readonly INotifier _notifier;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IContentManager _contentManager;

        public Localizer T { get; set; }

        public Authorizer(IAuthorizationService authorizationService,
            INotifier notifier, IWorkContextAccessor workContextAccessor,
            IContentManager contentManager)
        {
            this._authorizationService = authorizationService;
            this._notifier = notifier;
            this._workContextAccessor = workContextAccessor;
            this._contentManager = contentManager;
            T = NullLocalizer.Instance;
        }

        public bool Authorize(Permission permission)
        {
            return Authorize(permission, null, null);
        }

        public bool Authorize(Permission permission, LocalizedString message)
        {
            return Authorize(permission, null, message);
        }

        public bool Authorize(Permission permission, IContent content)
        {
            return Authorize(permission, content, null);
        }

        public bool Authorize(Permission permission, IContent content, LocalizedString message)
        {
            IUser user = this._workContextAccessor.GetContext().CurrentUser;
            if (_authorizationService.TryCheckAccess(permission, user, content))
            {
                // if the user is attempting to access the admin area then they must have 
                // a UserPartRecord associated to their username.
                if (permission.Name == AdminPanelAccess)
                    this.CreateUserIfNotExists(ref user);

                return true;
            }

            if (message != null)
            {
                if (user == null)
                {
                    this._notifier.Error(T("{0}. Anonymous users do not have {1} permission.",
                        message, permission.Name));
                }
                else
                {
                    this._notifier.Error(T("{0}. Current user, {2}, does not have {1} permission.",
                        message, permission.Name, user.UserName));
                }
            }

            return false;
        }

        /// <summary>
        /// Check to see if there is a UserPart for the user. If there is not
        /// then create one from the username.
        /// </summary>
        /// <param name="user">Currently logged in user.</param>
        private void CreateUserIfNotExists(ref IUser user)
        {
            string lowerName = user.UserName == null ? "" : user.UserName.ToLowerInvariant();
            IUser systemUser = this._contentManager.Query<UserPart, UserPartRecord>().Where(u =>
                u.NormalizedUserName == lowerName).List().FirstOrDefault();

            if (systemUser == null && !string.IsNullOrEmpty(user.UserName))
            {
                UserPart userPart = this._contentManager.New<UserPart>("User");

                userPart.Record.UserName = user.UserName;
                userPart.Record.Email = user.Email;
                userPart.Record.NormalizedUserName =
                    user.UserName.ToLowerInvariant();
                userPart.Record.HashAlgorithm = "SHA1";
                userPart.Record.RegistrationStatus = UserStatus.Approved;
                userPart.Record.EmailStatus = UserStatus.Approved;
                SetPassword(userPart.Record, Guid.NewGuid().ToString());

                this._contentManager.Create(userPart);
            }
        }

        /// <summary>
        /// Sets a fake password on the User. This password will never be used
        /// as the users will automatically be logged in.
        /// </summary>
        /// <param name="partRecord"></param>
        /// <param name="password"></param>
        private static void SetPassword(UserPartRecord partRecord, string password)
        {
            byte[] saltBytes = new byte[0x10];
            using (RNGCryptoServiceProvider random = new RNGCryptoServiceProvider())
            {
                random.GetBytes(saltBytes);
            }

            byte[] passwordBytes = Encoding.Unicode.GetBytes(password);

            byte[] combinedBytes = saltBytes.Concat(passwordBytes).ToArray();

            byte[] hashBytes;
            using (HashAlgorithm hashAlgorithm =
                HashAlgorithm.Create(partRecord.HashAlgorithm))
            {
                hashBytes = hashAlgorithm.ComputeHash(combinedBytes);
            }

            partRecord.PasswordFormat = MembershipPasswordFormat.Hashed;
            partRecord.Password = Convert.ToBase64String(hashBytes);
            partRecord.PasswordSalt = Convert.ToBase64String(saltBytes);
        }
    }
}