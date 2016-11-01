using Orchard.Accenture.Event.Services;
using Orchard.Accenture.Event.ViewModels;
using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using System;
using System.IO;
using System.Web.Mvc;

namespace Orchard.Accenture.Event.Controllers
{
    [Admin]
    public class ImportSessionAdmin : Controller
    {
        private readonly IImportService _importService;
        private readonly IMembershipService _membershipService;

        public ImportSessionAdmin(
            IOrchardServices services,
            IImportService importService,
            IMembershipService membershipService)
        {
            _importService = importService;
            _membershipService = membershipService;
            Services = services;
            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; private set; }
        public Localizer T { get; set; }
        [HttpGet, ActionName("Import")]
        public ActionResult Import()
        {
            var viewModel = new ImportSessionViewModel();
            viewModel.Events = _importService.GetEvents();
            return View(viewModel);
        }

        [HttpPost, ActionName("Import")]
        public ActionResult ImportPOST()
        {
            int eventId = 0;
            eventId = Convert.ToInt32(Request.Form["event-picker"]);
            var file = Request.Files.Get(0);
            if (!Services.Authorizer.Authorize(Permissions.ManageSession, T("Not allowed to import.")))
                return new HttpUnauthorizedResult();

            IUser user = Services.WorkContext.CurrentUser;
            string owner = Convert.ToString(Request.Form["owner"]);
            if (!String.IsNullOrEmpty(owner))
            {
                user = _membershipService.GetUser(owner);
                if (user == null)
                {
                    ModelState.AddModelError("SessionFile", T("Delegation user not found.").Text);
                }
            }

            if (String.IsNullOrEmpty(file.FileName))
            {
                ModelState.AddModelError("SessionFile", T("Please choose a file to import.").Text);
            }
            else
            {
                var extension = Path.GetExtension(file.FileName);
                if (extension != ".xlsx" && extension != ".xls")
                    ModelState.AddModelError("ParticipantFile", T("Please choose a valid file.").Text);
            }

            if (ModelState.IsValid)
            {
                string result = _importService.ImportSessionFile(eventId, file, user);
                if (!string.IsNullOrEmpty(result))
                {
                    Services.Notifier.Warning(T(result));
                }
                else
                {
                    Services.Notifier.Information(T("Your data has been imported successfully."));
                }
            }

            var viewModel = new ImportSessionViewModel();
            viewModel.Events = _importService.GetEvents();
            viewModel.CurrentEventId = eventId;
            viewModel.Owner = owner;
            return View(viewModel);
        }

        //public ActionResult ImportResult(string executionId)
        //{
        //    var journal = _recipeJournal.GetRecipeJournal(executionId);
        //    return View(journal);
        //}
    }
}