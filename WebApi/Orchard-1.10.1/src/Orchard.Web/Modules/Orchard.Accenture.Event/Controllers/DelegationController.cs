using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Settings;
using Orchard.UI.Admin;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;
//using Orchard.Core.Contents.ViewModels;
using Orchard.Accenture.Event.ViewModels;
using Orchard.Accenture.Event;
using System.IO;
using Orchard.Accenture.Event.Services;
using System.Text;

namespace Orchard.Accenture.Event.Controllers
{
    [Admin]
    public class DelegationController : Controller
    {
        private readonly IDelegationService _delegationService;


        public DelegationController(IOrchardServices services, IDelegationService delegationService)
        {
            _delegationService = delegationService;

            Services = services;
            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; private set; }
        public Localizer T { get; set; }


        [HttpGet, ActionName("Index")]
        public ActionResult Index()
        {
            ViewBag.IsAdmin = _delegationService.CheckIfCurrentUserInAdminRole();
            return View();
        }

        [HttpPost, ActionName("Index")]
        public ActionResult Index(string orgOwner, string owner)
        {
            ViewBag.OrgOwner = orgOwner;
            ViewBag.Owner = owner;
            ViewBag.IsAdmin = _delegationService.CheckIfCurrentUserInAdminRole();

            var result = _delegationService.Process(orgOwner, owner);

            if (!string.IsNullOrEmpty(result))
            {
                Services.Notifier.Warning(T(result));
            }
            else
            {
                Services.Notifier.Information(T("Delegation was successful."));
            }

            return View();
        }

    }
}