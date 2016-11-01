//using Microsoft.IdentityModel.Web;
using Orchard.Mvc;
using Orchard.Security;
using Orchard.Users.Events;
using System;
using System.Collections.Generic;
using System.IdentityModel.Services;
using System.Linq;
using System.Web;

namespace Accenture.Orchard.Authentication.Handler
{
    public class LoggedOutRedirect : IUserEventHandler
    {
        private readonly IHttpContextAccessor _httpContext;
        public LoggedOutRedirect(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext;
        }

        public void LoggedOut(IUser user)
        {
            _httpContext.Current().Response.Redirect(FederatedAuthentication.WSFederationAuthenticationModule.Issuer + "?wa=wsignout1.0");
        }

        public void Creating(UserContext context) { }
        public void Created(UserContext context) { }
        public void LoggedIn(IUser user) { }
        public void AccessDenied(IUser user) { }
        public void ChangedPassword(IUser user) { }
        public void SentChallengeEmail(IUser user) { }
        public void ConfirmedEmail(IUser user) { }
        public void Approved(IUser user) { }
        //public void LogInFailed(IUser user) { }
        public void LoggingIn(string userNameOrEmail, string password) { }
        public void LogInFailed(string userNameOrEmail, string password) { }
        
    }
}