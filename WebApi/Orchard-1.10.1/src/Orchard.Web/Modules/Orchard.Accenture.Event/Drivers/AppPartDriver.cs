using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Orchard.Accenture.Event.Models;
using Orchard.Accenture.Event.Services;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Environment.Extensions;
using Orchard.Accenture.Event.ViewModels;
using Orchard.Data;
using Orchard.Localization;
using Orchard.Logging;

namespace Orchard.Accenture.Event.Drivers {

    public class AppPartDriver : ContentPartDriver<AppPart> {

        private readonly IWorkContextAccessor _wca;
        private readonly ITransactionManager _transactionManager;

        public AppPartDriver(
            ITransactionManager transactionManager,
            IWorkContextAccessor wca) {
            _transactionManager = transactionManager;
            _wca = wca;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        protected override string Prefix {
            get { return "Orchard.Accenture.Event.App"; }
        }

        protected override DriverResult Display(
            AppPart part, string displayType, dynamic shapeHelper)
        {

            var shapes = new List<DriverResult>();

            shapes.Add(ContentShape(
                "Parts_Product",
                () => shapeHelper.Parts_Event(              
                    ContentPart: part
                    )
                ));

            if (part != null)
            {
                shapes.Add(ContentShape(
                        "Parts_Product_PriceTiers",
                        () => {
                            return shapeHelper.Parts_Product_PriceTiers(
                                ContentPart: part
                                );
                        })
                    );
            }
            return Combined(shapes.ToArray());
        }

       

        //GET
        protected override DriverResult Editor(AppPart part, dynamic shapeHelper)
        {

            return ContentShape("Parts_App_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts/App",
                    Model: new AppViewModel
                    {
                        App = part
                    },
                    Prefix: Prefix));
        }

        //POST
        protected override DriverResult Editor(
            AppPart part, IUpdateModel updater, dynamic shapeHelper)
        {

            var model = new AppViewModel
                {
                    App = part
                };
            if (updater.TryUpdateModel(model, Prefix, null, null)) {
                var title = ((dynamic)part.ContentItem).TitlePart.Title;
                if (title.IndexOf(" ") >= 0)
                {
                    updater.AddModelError(Prefix + T("Space is not allowed in Title"), T("Space is not allowed in App Title"));
                    _transactionManager.Cancel();
                }
            }
            return Editor(part, shapeHelper);
        }

    }
}
