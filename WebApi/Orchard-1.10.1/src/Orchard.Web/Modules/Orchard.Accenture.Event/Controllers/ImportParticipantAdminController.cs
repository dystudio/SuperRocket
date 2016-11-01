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
    public class ImportParticipantAdminController : Controller
    {
        private readonly IImportService _importService;
        private readonly IMembershipService _membershipService;

        public ImportParticipantAdminController(
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
            var viewModel = new ImportParticipantViewModel();
            viewModel.Events = _importService.GetEvents();

            return View(viewModel);
        }

        [HttpPost, ActionName("Import")]
        public ActionResult ImportPOST()
        {
            
            int eventId = 0;
            eventId = Convert.ToInt32(Request.Form["event-picker"]);

            IUser user = Services.WorkContext.CurrentUser;
            string owner = Convert.ToString(Request.Form["owner"]);

            #region validate            
            if (!Services.Authorizer.Authorize(Permissions.ManageParticipant, T("Not allowed to import.")))
            { 
                return new HttpUnauthorizedResult();
            }
            
            if (!String.IsNullOrEmpty(owner))
            {
                user = _membershipService.GetUser(owner);
                if (user == null)
                {
                    ModelState.AddModelError("ParticipantFile", T("Delegation user not found.").Text);
                }
                else
                {
                    IUser eventOwner = _importService.GetEventOwner(eventId);
                    if (user != eventOwner)
                    {
                        ModelState.AddModelError("ParticipantFile", T("Delegation user not owner of event.").Text);
                    }
                }
            }

            if (String.IsNullOrEmpty(Request.Files["ParticipantFile"].FileName))
            {
                ModelState.AddModelError("ParticipantFile", T("Please choose a file to import.").Text);
            }
            else
            {
                string[] allowedExtension = { ".xlsx", ".xls" };
                var extension = Path.GetExtension(Request.Files["ParticipantFile"].FileName);
                bool allowed = false;
                foreach (var item in allowedExtension)
                {
                    if (item == extension)
                    {
                        allowed = true;
                    }
                }

                if (!allowed)
                {
                    ModelState.AddModelError("ParticipantFile", T("Please choose a valid file.").Text);
                }
            }
            #endregion

            if (ModelState.IsValid)
            {
                // Get the file
                var file = Request.Files.Get(0);

                // Import
                string result = _importService.ImportParticipant(eventId, file, user);
                //string result = _importService.BulkImportParticipant(eventId, file, user);

                if (!string.IsNullOrEmpty(result))
                {
                    Services.Notifier.Warning(T(result));
                }
                else
                {
                    Services.Notifier.Information(T("Your data has been imported successfully."));
                }
            }

            var viewModel = new ImportParticipantViewModel();
            viewModel.Events = _importService.GetEvents();
            viewModel.CurrentEventId = eventId;
            viewModel.Owner = owner;
            return View(viewModel);
        }
    }
}