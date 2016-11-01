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
using Orchard.UI.Notify;
using Orchard.Localization;
using Orchard.Logging;
using System.Web.Mvc;
using Orchard.Core.Common.Models;

namespace Orchard.Accenture.Event.Drivers {

    public class ParticipantPartDriver : ContentPartDriver<ParticipantPart> {
        private readonly IWorkContextAccessor _wca;
        private readonly IRepository<ParticipantPartRecord> _repository;
        private readonly ITransactionManager _transactionManager;
        private readonly IContentManager _contentManager;

        public ParticipantPartDriver(
            IWorkContextAccessor wca,
             IRepository<ParticipantPartRecord> repository,
             IOrchardServices orchardServices,
             ITransactionManager transactionManager,
             IContentManager contentManager) {

            _wca = wca;
            _repository = repository;
            _transactionManager = transactionManager;
            _contentManager = contentManager;


            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
            Services = orchardServices; 
        }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }
        public IOrchardServices Services { get; private set; }

        protected override string Prefix {
            get { return "Orchard.Accenture.Event.Participant"; }
        }

        protected override DriverResult Display(
            ParticipantPart part, string displayType, dynamic shapeHelper)
        {

            var shapes = new List<DriverResult>();

            shapes.Add(ContentShape(
                "Parts_Product",
                () => shapeHelper.Parts_Participant(              
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
        protected override DriverResult Editor(ParticipantPart part, dynamic shapeHelper) {

            return ContentShape("Parts_Participant_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts/Participant",
                    Model: new ParticipantEditorViewModel {
                        Participant = part
                    },
                    Prefix: Prefix));
        }

        //POST
        protected override DriverResult Editor(
            ParticipantPart part, IUpdateModel updater, dynamic shapeHelper) {

                var model = new ParticipantEditorViewModel
                {
                    Participant = part
                };

           
           
                if (updater.TryUpdateModel(model, Prefix, null, null))
                {
                var enterpriseId = model.Participant.EnterpriseId;
                if (string.IsNullOrEmpty(enterpriseId))
                {
                    updater.AddModelError(Prefix + T("Enterprise ID cannot leave blank"), T("Enterprise ID cannot be empty"));
                    _transactionManager.Cancel();
                }
                var query = _contentManager.Query(VersionOptions.Latest, "Participant")
                    .Where<CommonPartRecord>(cr => cr.OwnerId == Services.WorkContext.CurrentUser.Id);

                var participants = query.Where<ParticipantPartRecord>(cr => cr.EnterpriseId == model.Participant.EnterpriseId);

                if (participants != null && participants.Count() > 1)
                {
                    updater.AddModelError(Prefix + T("This Enterprise ID already exists."), T("This Enterprise ID already exists."));
                    _transactionManager.Cancel();

                }
            }
            
            
            return Editor(part, shapeHelper);
        }

    }
}
