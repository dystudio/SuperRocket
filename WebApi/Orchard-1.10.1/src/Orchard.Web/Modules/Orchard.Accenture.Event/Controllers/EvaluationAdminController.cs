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
using Orchard.Core.Contents.ViewModels;
using Orchard.Accenture.Event.Models;
using Orchard.Security;
using Orchard.Accenture.Event.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Users.ViewModels;
using Orchard.Users.Models;

namespace Orchard.Accenture.Event.Controllers
{
    [Admin]
    public class EvaluationAdminController : Controller
    {

        private readonly IContentManager _contentManager;
        private readonly ISiteService _siteService;
        private readonly ITransactionManager _transactionManager;
        private readonly IOrchardServices _orchardServices;

        private dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public EvaluationAdminController(
            IContentManager contentManager,
            IShapeFactory shapeFactory,
            ISiteService siteService,
            ITransactionManager transactionManager,
            IOrchardServices orchardServices)
        {

            _contentManager = contentManager;
            Shape = shapeFactory;
            _siteService = siteService;
            _transactionManager = transactionManager;
            _orchardServices = orchardServices;
            T = NullLocalizer.Instance;
        }

        public ActionResult List(ListViewModel model, PagerParameters pagerParameters)
        {
            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageEvaluation, T("Not allowed to manage Evaluation")))
                return new HttpUnauthorizedResult();

            var pager = new Pager(_siteService.GetSiteSettings(), pagerParameters);

            var query = _contentManager.Query(VersionOptions.Latest,"Evaluation");
            if (!_orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner))
                query.Where<CommonPartRecord>(cr => cr.OwnerId == _orchardServices.WorkContext.CurrentUser.Id);
            else
            {
                model.Options.IsSiteOwner = true;
                model.Users = _orchardServices.ContentManager
                     .Query<UserPart, UserPartRecord>()
                     .List()
                     .OrderBy(x => x.UserName)
                     .Select(x => new UserEntry { User = x.Record });

                if (model.Options.Owner != null)
                {
                    query.Where<CommonPartRecord>(cr => cr.OwnerId == Convert.ToInt32(model.Options.Owner));
                }

            }
            if (!string.IsNullOrEmpty(model.Options.Search))
            {
                query.Join<TitlePartRecord>().Where(cr => cr.Title.Contains(model.Options.Search.Trim()));
            }
            switch (model.Options.OrderBy)
            {
                case ViewModels.ContentsOrder.Modified:
                    query.OrderByDescending<CommonPartRecord>(cr => cr.ModifiedUtc);
                    break;
                case ViewModels.ContentsOrder.Created:
                    query.OrderByDescending<CommonPartRecord>(cr => cr.CreatedUtc);
                    break;
            }

            var pagerShape = Shape.Pager(pager).TotalItemCount(query.Count());
            var pageOfContentItems = query.Slice(pager.GetStartIndex(), pager.PageSize).ToList();

            var list = Shape.List();
            list.AddRange(pageOfContentItems.Select(ci => _contentManager.BuildDisplay(ci, "SummaryAdmin")));

            dynamic viewModel = Shape.ViewModel()
                .ContentItems(list)
                .Pager(pagerShape)
                .Options(model.Options)
                .Users(model.Users)
                .IsSiteOwner(model.Options.IsSiteOwner);

            // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
            return View((object)viewModel);
        }

        [HttpPost]
        [ActionName("List")]
        [Mvc.FormValueRequired("submit.Filter")]
        public ActionResult ListFilterPost(ViewModels.ContentOptions options, string searchText)
        {
            var routeValues = ControllerContext.RouteData.Values;
            if (options != null)
            {
                routeValues["Options.OrderBy"] = options.OrderBy;

                if (String.IsNullOrWhiteSpace(searchText))
                {
                    routeValues.Remove("Options.Search");
                }
                else
                {
                    routeValues["Options.Search"] = searchText;
                }

                routeValues["Options.Owner"] = options.Owner;
            }

            return RedirectToAction("List", routeValues);
        }
    }
}