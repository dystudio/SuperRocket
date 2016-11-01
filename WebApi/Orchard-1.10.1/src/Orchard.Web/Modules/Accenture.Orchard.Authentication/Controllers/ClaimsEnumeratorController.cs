using Accenture.Orchard.Authentication.Models;
using Microsoft.IdentityModel.Claims;
using Orchard.Mvc;
using System.Web;
using System.Web.Mvc;

namespace Accenture.Orchard.Authentication.Controllers
{
    public class ClaimsEnumeratorController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimsEnumeratorController(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public ActionResult Index()
        {
            HttpContextBase httpContext = this._httpContextAccessor.Current();
            ClaimsEnumerator claimsEnumerator = new ClaimsEnumerator();
            if (httpContext != null && httpContext.Request.IsAuthenticated)
            {
                if (httpContext.User.Identity is ClaimsIdentity)
                {
                    ClaimsIdentity claimsIdentity =
                        (ClaimsIdentity)httpContext.User.Identity;
                    claimsEnumerator.Claims = claimsIdentity.Claims;
                }
            }

            return View(claimsEnumerator);
        }
    }
}