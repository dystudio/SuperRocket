using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Security;
using Orchard;
using Orchard.Logging;
using Orchard.Localization;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.Fields;

using Newtonsoft.Json.Linq;
using System.Text;

namespace Orchard.SuperRocket.Services
{
    public class HtmlModuleService : IHtmlModuleService
    {
        private readonly IOrchardServices _orchardServices;
        private readonly IContentManager _contentManager;

        public HtmlModuleService(
            IOrchardServices orchardServices,
            IContentManager contentManager) {

            _orchardServices = orchardServices;
            _contentManager = contentManager;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public dynamic GetAvailableHtmlModules()
        {
            var test = _contentManager.Query("HtmlModule").List().Count();

            var modules = _contentManager.Query("HtmlModule").List().Select(contentItem => new
            {
                Title = ((dynamic)contentItem).HtmlModulePart.Title.Value
            });
            return modules;
        }
    }
}