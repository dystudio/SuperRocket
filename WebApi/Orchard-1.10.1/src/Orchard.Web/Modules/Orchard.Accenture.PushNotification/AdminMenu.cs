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
            builder.Add(T("Accenture Notifier"), "0", menu =>
            {
                menu.LinkToFirstChild(false);
                menu.Add(T("Send Message"), "5",
                    item => item
                        .Caption(T("Send Message"))
                        .Position("5.0")
                        .Action("PushNotification", "PushNotifyAdmin", new { area = "Orchard.Accenture.PushNotification" }));

            });
        }
    }
}