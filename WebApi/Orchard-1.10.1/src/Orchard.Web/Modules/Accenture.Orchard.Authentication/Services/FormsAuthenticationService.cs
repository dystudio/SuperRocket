using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Security;
using Orchard.Security.Providers;
using Orchard.Services;
using Orchard.Users.Models;
using Orchard.Environment.Configuration;
//using Microsoft.IdentityModel.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using System;
using System.IdentityModel.Services;
using System.Security.Claims;
using Orchard.Environment;
using System.IO;
//using Microsoft.IdentityModel.Web;

namespace Accenture.Orchard.Authentication.Services
{
    [OrchardSuppressDependency("Orchard.Security.Providers.FormsAuthenticationService")]
    public class FederatedAuthenticationService : IAuthenticationService
    {
        #region Private Variables
        private readonly IContentManager _contentManager;
        private readonly Work<IMembershipService> _membershipService;
        private readonly IRoleService _roleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<UserRolesPartRecord> _userRolesRepository;
        private readonly FormsAuthenticationService _systemAuthenticationService;
        private readonly ShellSettings _shellSettings;
        private readonly ISslSettingsProvider _sslSettingsProvider;
        #endregion

        public ILogger Logger { get; set; }

        public FederatedAuthenticationService(IClock clock,
            IContentManager contentManager,
            Work<IMembershipService> membershipService,
            IMembershipService amembershipService,
            IRoleService roleService,
            IHttpContextAccessor httpContextAccessor,
            IRepository<UserRolesPartRecord> userRolesRepository,
            ShellSettings shellSettings,
            ISslSettingsProvider sslSettingsProvider)
        {
            this._contentManager = contentManager;
            this._membershipService = membershipService;
            this._roleService = roleService;
            this._httpContextAccessor = httpContextAccessor;
            this._userRolesRepository = userRolesRepository;
            _shellSettings = shellSettings;
            this._sslSettingsProvider = sslSettingsProvider;
            this._systemAuthenticationService =
              new FormsAuthenticationService(shellSettings, clock, amembershipService, httpContextAccessor, sslSettingsProvider,null);
            Logger = NullLogger.Instance;
        }

        public void SignIn(IUser user, bool createPersistentCookie)
        {
            this._systemAuthenticationService.SignIn(user, createPersistentCookie);
        }

        public void SignOut()
        {
            this._systemAuthenticationService.SignOut();
            FederatedAuthentication.SessionAuthenticationModule.SignOut(); 

        }

        public void SetAuthenticatedUserForRequest(IUser user)
        {
            this._systemAuthenticationService.SetAuthenticatedUserForRequest(user);
        }

        public IUser GetAuthenticatedUser()
        {
            IUser user = this._systemAuthenticationService.GetAuthenticatedUser();
            if (user == null)
            {
                HttpContextBase httpContext = this._httpContextAccessor.Current();
                if (httpContext != null && httpContext.Request.IsAuthenticated)
                {
                    int userId = -1;
                    string username = string.Empty;
                    string email = string.Empty;
                    List<string> roles = new List<string>();
                    if (httpContext.User.Identity is WindowsIdentity)
                    {
                        username = HttpContext.Current.User.Identity.Name;
                        try { roles = Roles.GetRolesForUser(username).ToList(); }
                        catch
                        {
                            Logger.Warning("Unable to get roles for user: " +
                                username);
                        }
                    }
                    else if (httpContext.User.Identity is ClaimsIdentity)
                    {
                        //var ESOuser = HttpContext.Current.User;
                        var ESOuser = httpContext.User;
                        var enterprisePrincipal = ESOuser as Accenture.Security.Eso.Web.IEnterprisePrincipal;
                        var enterpriseIdentity = enterprisePrincipal.EnterpriseIdentity;
                        username = enterpriseIdentity.EnterpriseId;
                        email = enterpriseIdentity.EmailAddress;
                        roles = enterpriseIdentity.GetGroups().ToList();

                        if (string.IsNullOrEmpty(username))
                        {
                            throw new SecurityException("Could not determine username from input claims. Ensure that enterprise identity is issued as part of claim from the STS.");
                        }

                        if (!enterpriseIdentity.AllClaims.ContainsKey(System.Security.Claims.ClaimTypes.NameIdentifier))
                        {
                            //such as new Claim(ClaimTypes.NameIdentifier, "Eid")
                            Logger.Error("please raise a itg to request name identifier in claims");
                        }

                        if (roles == null || roles.Count == 0)
                        {
                            Logger.Error("please raise a itg to request ADFS group name in claims");
                        }
                        else
                        {
                            string strAllroles = string.Join(",", roles);

                            //Logger.Error("Roles::{0}:{1}", username, strAllroles);
                        }
                    }

                    // Check if user exists in Membership store
                    user = this._membershipService.Value.GetUser(username);

                    // If user is null, create UserPart on the fly
                    if (user == null)
                        user = this.GetUserPart(userId, username, email, ref roles);

                    this._systemAuthenticationService.SetAuthenticatedUserForRequest(user);

                    this.EnsureUserRoles(roles, user.Id);
                }
            }

            return user;
        }


        private UserPart GetUserPart(int userId, string username, string email,
            ref List<string> roles)
        {
            System.Diagnostics.EventLog.WriteEntry("Application", " in get userpart ln172");
            ContentItem i = new ContentItem()
            {
                VersionRecord = new ContentItemVersionRecord()
                {
                    ContentItemRecord = new ContentItemRecord()
                    {
                        Id = userId
                    }
                },
                ContentManager = this._contentManager
            };
            UserPart userPart = new UserPart();



             var userPart1 = _membershipService.Value.CreateUser(
                                    new CreateUserParams(
                                        username,
                                        "NotUsed!",
                                        email,
                                        null,
                                        null,
                                        true
                                    )
                                );




             userPart = (UserPart)userPart1;
            userPart.Record = new UserPartRecord();
            userPart.Record.Id = userId;
           
            //userPart.UserName = username;
            //userPart.Email = email;
            //userPart.EmailStatus = UserStatus.Approved;




            userPart.NormalizedUserName =
                userPart.UserName.ToLowerInvariant();

            userPart.RegistrationStatus = UserStatus.Approved;
            //var i_userPart = userPart;
            //i.Weld(i_userPart);

           UserRolesPart rolesPart = new UserRolesPart();

            rolesPart.Roles = roles;

            //var i_rolesPart = rolesPart;
            //i.Weld(i_rolesPart);

            return userPart;
        }


        //private UserPart GetUserPart(int userId, string username, string email,
        //    ref List<string> roles)
        //{





       


        //    System.Diagnostics.EventLog.WriteEntry("Application", " in get userpart ln172");




        //    ContentItem i = new ContentItem()
        //    {
        //        VersionRecord = new ContentItemVersionRecord()
        //        {
        //            ContentItemRecord = new ContentItemRecord()
        //            {
        //                Id = userId
        //            }
        //        },
        //        ContentManager = this._contentManager
        //    };
           
            
            
        //    UserPart userPart = new UserPart();

        //    var userPart1 = _membershipService.Value.CreateUser(
        //                            new CreateUserParams(
        //                                username,
        //                                "NotUsed!",
        //                                email,
        //                                null,
        //                                null,
        //                                true
        //                            )
        //                        );



        //    //userPart1.UserName
        //    userPart.Record = new UserPartRecord();


        //    userPart.Record.Id = userId;
        //    userPart.UserName = username;

        //    userPart.Email = email;
        //    userPart.EmailStatus = UserStatus.Approved;
        //    userPart.NormalizedUserName =
        //        userPart.UserName.ToLowerInvariant();

        //    userPart.RegistrationStatus = UserStatus.Approved;

        //    i.Weld(userPart);

        //    UserRolesPart rolesPart = new UserRolesPart();

        //    rolesPart.Roles = roles;
        //    i.Weld(rolesPart);

        //    return userPart;
        //}

        private void EnsureUserRoles(List<string> roles, int userId)
        {

            if (roles.Any())
            {
                IEnumerable<UserRolesPartRecord> currentUserRoleRecords =
                    this._userRolesRepository.Fetch(x => x.UserId == userId);


                IEnumerable<RoleRecord> currentRoleRecords =
                    currentUserRoleRecords.Select(x => x.Role);

                IEnumerable<RoleRecord> targetRoleRecords =
                    this._roleService.GetRoles().Where(x =>
                        roles.Contains(x.Name));


                foreach (RoleRecord addingRole in
                    targetRoleRecords.Where(x => !currentRoleRecords.Contains(x)))
                {
                    this._userRolesRepository.Create(new UserRolesPartRecord
                    {
                        UserId = userId,
                        Role = addingRole
                    });

                }

                foreach (UserRolesPartRecord removingRole in
                    currentUserRoleRecords.Where(x => !targetRoleRecords.Contains(x.Role)))
                {

                    this._userRolesRepository.Delete(removingRole);
                }
            }
        }
    }
}