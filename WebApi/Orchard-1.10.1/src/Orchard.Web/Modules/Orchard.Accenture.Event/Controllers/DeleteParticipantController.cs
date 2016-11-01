using Orchard.Accenture.Event.Services;
using Orchard.Accenture.Event.ViewModels;
using Orchard.Localization;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using System;
using System.IO;
using System.Web.Mvc;

namespace Orchard.Accenture.Event.Controllers
{
    [Admin]
    public class DeleteParticipantController : Controller
    {
        private readonly IImportService _importService;
        public IOrchardServices Services { get; private set; }
        public Localizer T { get; set; }

        public DeleteParticipantController(
            IOrchardServices services,
            IImportService importService)
        {
            _importService = importService;
            Services = services;
            T = NullLocalizer.Instance;
        }

        // GET: DeleteParticipant
        [HttpGet, ActionName("Delete")]
        public ActionResult Delete()
        {
            var viewModel = new DeleteParticipantViewModel();
            viewModel.Events = _importService.GetEvents();
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeletePOST()
        {
            int eventId = 0;
            eventId = Convert.ToInt32(Request.Form["event-picker"]);

            if (!Services.Authorizer.Authorize(Permissions.ManageParticipant, T("Not allowed to remove participants.")))
                return new HttpUnauthorizedResult();

            if (String.IsNullOrEmpty(Request.Files["ParticipantFile"].FileName))
            {
                ModelState.AddModelError("ParticipantFile", T("Please choose a file to remove participants.").Text);
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

            if (ModelState.IsValid)
            {
                // Get the file
                var file = Request.Files.Get(0);

                // Import list of to be deleted
                string result = _importService.DeleteParticipant(eventId, file);
                if (!string.IsNullOrEmpty(result))
                {
                    Services.Notifier.Warning(T(result));
                }
                else
                {
                    Services.Notifier.Information(T("Participants has been removed from the event successfully."));
                }
            }

            var viewModel = new DeleteParticipantViewModel();
            viewModel.Events = _importService.GetEvents();
            viewModel.CurrentEventId = eventId;
            return View(viewModel);
        }

    }
}