using System.Linq;
using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Navigation;
using Orchard;

namespace Orchard.Accenture.Event
{
    public class AdminMenu : INavigationProvider {
        private readonly IAuthorizationService _authorizationService;
        private readonly IWorkContextAccessor _workContextAccessor;

        public AdminMenu(IAuthorizationService authorizationService, IWorkContextAccessor workContextAccessor) {
            _authorizationService = authorizationService;
            _workContextAccessor = workContextAccessor;
        }

        public Localizer T { get; set; }

        public string MenuName { get { return "admin"; } }

        public void GetNavigation(NavigationBuilder builder)
        {
            builder.Add(T("Accenture Event"), "0", menu =>
            {
                menu.LinkToFirstChild(false);
                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Apps"))
                        .Position("5.0")
                        .Action("List", "AppAdmin", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManageApp));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Events"))
                        .Position("5.1")
                        .Action("List", "Event", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManageEvent));

                menu.Add(T("Events"), "5", 
                    item => item
                        .Caption(T("Participants"))
                        .Position("5.2")
                        .Action("List", "ParticipantAdmin", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManageParticipant));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Sessions"))
                        .Position("5.3")
                        .Action("List", "SessionAdmin", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManageSession));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("InfoCards"))
                        .Position("5.4")
                        .Action("List", "InfoCardAdmin", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManageInfoCard));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Polls"))
                        .Position("5.5")
                        .Action("List", "PollAdmin", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManagePoll));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Evaluations"))
                        .Position("5.6")
                        .Action("List", "EvaluationAdmin", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManageEvaluation));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Circles"))
                        .Position("5.7")
                        .Action("List", "CircleAdmin", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManageCircle));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Documents"))
                        .Position("5.8")
                        .Action("Index", "Admin", new { area = "Orchard.MediaLibrary" })
                        .Permission(Permissions.ManageEvent));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Taxonomies"))
                        .Position("5.9")
                        .Action("Index", "Admin", new { area = "Orchard.Taxonomies" })
                        .Permission(Permissions.ManageTaxonomy));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Bulk Import Participant"))
                        .Position("6.0")
                        .Action("Import", "ImportParticipantAdmin", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ImportParticipant));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Bulk Delete Participant"))
                        .Position("6.1")
                        .Action("Delete", "DeleteParticipant", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ImportParticipant));
                
                menu.Add(T("Events"), "5",
                   item => item
                       .Caption(T("Bulk Import Session"))
                       .Position("6.1")
                       .Action("Import", "ImportSessionAdmin", new { area = "Orchard.Accenture.Event" })
                       .Permission(Permissions.ImportSession));

                menu.Add(T("Events"), "5",
                    item => item
                        .Caption(T("Delegation"))
                        .Position("6.1")
                        .Action("Index", "Delegation", new { area = "Orchard.Accenture.Event" })
                        .Permission(Permissions.ManageDelegation));

            });
        }
    }
}