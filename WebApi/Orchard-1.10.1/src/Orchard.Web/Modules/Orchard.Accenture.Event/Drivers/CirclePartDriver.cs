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
using Orchard.Fields.Fields;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Data;

namespace Orchard.Accenture.Event.Drivers {

    public class CirclePartDriver : ContentPartDriver<CirclePart> {
        private readonly IWorkContextAccessor _wca;
        private readonly ITransactionManager _transactionManager;

        public CirclePartDriver(
            IWorkContextAccessor wca,
             ITransactionManager transactionManager) {

            _wca = wca;
            _transactionManager = transactionManager;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        protected override string Prefix {
            get { return "Orchard.Accenture.Event.Circle"; }
        }

        protected override DriverResult Display(
            CirclePart part, string displayType, dynamic shapeHelper)
        {

            var shapes = new List<DriverResult>();

            shapes.Add(ContentShape(
                "Parts_Product",
                () => shapeHelper.Parts_Session(              
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
        protected override DriverResult Editor(CirclePart part, dynamic shapeHelper)
        {

            return ContentShape("Parts_Circle_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts/Circle",
                    Model: part,
                    Prefix: Prefix));
        }

        //POST
        protected override DriverResult Editor(
            CirclePart part, IUpdateModel updater, dynamic shapeHelper)
        {

            //var model = new SessionEditorViewModel
            //    {
            //        Session = part
            //    };
            //if (updater.TryUpdateModel(model, Prefix, null, null)) {

            //}
            return Editor(part, shapeHelper);
        }

    }
}
