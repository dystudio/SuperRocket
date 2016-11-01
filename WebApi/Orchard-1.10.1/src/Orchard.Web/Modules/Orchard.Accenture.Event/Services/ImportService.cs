using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Orchard.Accenture.Event.Handlers;
using Orchard.Accenture.Event.Models;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Data;
using Orchard.Fields.Fields;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using Orchard.Tasks.Scheduling;
using Orchard.Taxonomies.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using ParticipantPart = Orchard.Accenture.Event.Models.ParticipantPart;
using SessionPart = Orchard.Accenture.Event.Models.SessionPart;
using TermPart = Orchard.Taxonomies.Models.TermPart;

namespace Orchard.Accenture.Event.Services
{
    public class ImportService : IImportService, IScheduledTaskHandler
    {
        private const string AdminAdGroup = "CIO.EmployeeExperience.Ops";
        private const string TaskType = "UpdatePhase";
        private const string DefaultChildGroup = "Global";
        private const int MinutesToCheck = -30;

        private readonly IScheduledTaskManager _taskManager;

        private readonly IContentManager _contentManager;
        private readonly IOrchardServices _orchardServices;
        private readonly ITaxonomyService _taxonomyService;
        private readonly IPeopleService _peopleService;
        private readonly ITransactionManager _transactionManager;
        private readonly IRepository<ParticipantPartRecord> _repository;

        public ImportService(
            IContentManager contentManager,
            IOrchardServices orchardServices,
            ITaxonomyService taxonomyService,
            IPeopleService peopleService,
            IRepository<ParticipantPartRecord> repository,
            ITransactionManager transactionManager,
            IScheduledTaskManager taskManager
            )
        {
            _taskManager = taskManager;

            _contentManager = contentManager;
            _orchardServices = orchardServices;
            _taxonomyService = taxonomyService;
            _peopleService = peopleService;
            _transactionManager = transactionManager;

            _repository = repository;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public dynamic DeleteParticipant(int eventId, HttpPostedFileBase file)
        {
            IUser owner = _orchardServices.WorkContext.CurrentUser;
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            SetTransactionLevelToReadUncommitted();

            List<ValidationResult> results = new List<ValidationResult>();

            #region Get eid collection from sheet
            var sheet = LoadSheet(file, "Participant");

            List<ParticipantModelForExcel> participantsModelFromExcel = new List<ParticipantModelForExcel>();
            if (sheet != null)
            {
                var rowCount = sheet.LastRowNum;
                var cellCount = sheet.GetRow(0) == null ? 0 : sheet.GetRow(0).LastCellNum;
                for (var i = 1; i <= rowCount; i++)
                {
                    IRow currentRow = sheet.GetRow(i);
                    List<ICell> cells = new List<ICell>();
                    if (currentRow != null)
                    {
                        for (var j = 0; j < cellCount; j++)
                        {
                            var cell = currentRow.GetCell(j, MissingCellPolicy.RETURN_NULL_AND_BLANK);
                            if (cell != null)
                            {
                                cell.SetCellType(CellType.String);
                            }
                            cells.Add(cell);
                        }

                        var enterpriseId = cells[0] == null ? string.Empty : cells[0].StringCellValue.Trim();

                        if (!(string.IsNullOrEmpty(enterpriseId)))
                        {
                            participantsModelFromExcel.Add(new ParticipantModelForExcel
                            {
                                EnterpriseId = enterpriseId,
                                UserGroup = string.Empty,
                                RowNumber = i.ToString()
                            });
                        }
                    }
                }
            }
            #endregion

            #region Validation no data | row count | missing fields | duplicates
            if (!participantsModelFromExcel.Any())
            {
                return "There is no data found in the Excel. Please populate prior to removing participants.";
            }

            if (participantsModelFromExcel.Count > 150)
            {
                return "Please limit bulk delete to a maximum of 150 records in one transaction.";
            }

            foreach (var model in participantsModelFromExcel)
            {
                if (string.IsNullOrEmpty(model.EnterpriseId))
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = "Enterprise ID is missing.";
                    validation.RowNumber = model.RowNumber;
                    results.Add(validation);
                }
            }

            var duplicates = from m in participantsModelFromExcel
                             group m by m.EnterpriseId into g
                             where g.Count() > 1
                             select g;

            if (duplicates.Any())
            {
                foreach (var item in duplicates)
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = String.Format("Duplicates found in Excel {0}", item.Key);
                    validation.RowNumber = item.FirstOrDefault().RowNumber;
                    results.Add(validation);
                }
            }

            if (results.Any())
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in results)
                {
                    builder.Append(String.Format("Error found in Excel row {0}, Error detail: {1}\r\n"
                        , (Convert.ToInt32(item.RowNumber) + 1), item.Error));
                }
                return builder.ToString();
            }
            #endregion

            #region Get existing participants
            // get participant content
            var query = _contentManager.Query(VersionOptions.Published, "Participant");

            // filter by owner
            var ownedParticipants = query
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id);

            // filter by event
            var eventParticipants = ownedParticipants
                .Where<ParticipantPartRecord>(p =>
                    p.EventIds.StartsWith(eventId.ToString() + ",")
                    || p.EventIds.Contains("," + eventId.ToString() + ",")
                    || p.EventIds.EndsWith("," + eventId.ToString())
                    || (!p.EventIds.Contains(",") && p.EventIds == eventId.ToString()));

            // execute query
            var ownedEventParticipants = eventParticipants.List();

            // filter by participants to delete
            var forDeleteParticipants = ownedEventParticipants
                .Where(i => participantsModelFromExcel.Select(m => m.EnterpriseId).Contains(i.As<ParticipantPart>().EnterpriseId));

            // Check for missing EIDs in excel
            if (participantsModelFromExcel.Count() > forDeleteParticipants.Count() || forDeleteParticipants.Count() == 0)
            {
                List<ParticipantModelForExcel> missingParticipants = participantsModelFromExcel
                .Where(a => !forDeleteParticipants
                    .Select(b => b.As<ParticipantPart>().EnterpriseId)
                    .ToList()
                    .Contains(a.EnterpriseId))
                .ToList();

                if (missingParticipants.Count() > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    missingParticipants.Sort((ParticipantModelForExcel x, ParticipantModelForExcel y) => Convert.ToInt32(x.RowNumber).CompareTo(Convert.ToInt32(y.RowNumber)));
                    foreach (var item in missingParticipants)
                    {
                        sb.Append(String.Format("Error found in Excel row {0}, Error detail: {1} not existing in event or participant not owned or not existing in Orchard. \r\n"
                            , (Convert.ToInt32(item.RowNumber) + 1), item.EnterpriseId));
                    }
                    return sb.ToString();
                }
            }
            #endregion

            #region Update
            try
            {
                foreach (var participantModel in participantsModelFromExcel)
                {
                    // Select participant content to update by EID
                    var participant = forDeleteParticipants
                        .Where(i => i.As<ParticipantPart>().EnterpriseId == participantModel.EnterpriseId)
                        .FirstOrDefault();

                    if (participant != null)
                    {
                        // Get events of Participant
                        var ids = ((dynamic)participant).ParticipantPart.EventPicker.Ids as int[];

                        if (ids.Count() > 1)
                        {
                            // Remove event from participant
                            ((dynamic)participant).ParticipantPart.EventPicker.Ids = ids.Where(x => x != eventId).ToArray();

                            participant.As<ParticipantPart>().EventIds = String.Join(",", ids.Where(x => x != eventId).ToArray());
                            participant.As<CommonPart>().PublishedUtc = DateTime.UtcNow;
                            participant.As<CommonPart>().ModifiedUtc = DateTime.UtcNow;

                            _contentManager.Publish(participant);
                        }
                        else
                        {
                            // Delete participant if tagged to only this event
                            _contentManager.Destroy(participant);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _transactionManager.Cancel();
                Logger.Error("Error occurs when execute the update for import:" + ex.Message);
            }
            #endregion

            return string.Empty;
        }

        public dynamic ImportParticipant(int id, HttpPostedFileBase file, IUser owner)
        {
            SetTransactionLevelToReadUncommitted();

            List<ValidationResult> results = new List<ValidationResult>();

            #region Get eid collection from sheet
            var sheet = LoadSheet(file, "Participant");

            List<ParticipantModelForExcel> participantsModelFromExcel = new List<ParticipantModelForExcel>();
            if (sheet != null)
            {
                var rowCount = sheet.LastRowNum;
                var cellCount = sheet.GetRow(0) == null ? 0 : sheet.GetRow(0).LastCellNum;
                for (var i = 1; i <= rowCount; i++)
                {
                    IRow currentRow = sheet.GetRow(i);
                    List<ICell> cells = new List<ICell>();
                    if (currentRow != null)
                    {
                        for (var j = 0; j < cellCount; j++)
                        {
                            var cell = currentRow.GetCell(j, MissingCellPolicy.RETURN_NULL_AND_BLANK);
                            if (cell != null)
                            {
                                cell.SetCellType(CellType.String);
                            }
                            cells.Add(cell);
                        }

                        var enterpriseId = cells[0] == null ? string.Empty : cells[0].StringCellValue.Trim();
                        var userGroup = cells[1] == null ? string.Empty : cells[1].StringCellValue.Trim();

                        if (!(string.IsNullOrEmpty(enterpriseId) && string.IsNullOrEmpty(userGroup)))
                        {
                            participantsModelFromExcel.Add(new ParticipantModelForExcel
                            {
                                EnterpriseId = enterpriseId,
                                UserGroup = userGroup,
                                RowNumber = i.ToString()
                            });
                        }
                    }
                }
            }
            #endregion

            #region Validation no data | row count | missing required fields | duplicates
            if (!participantsModelFromExcel.Any())
            {
                return "There is no data found in the Excel. Please populate prior to importing.";
            }

            if (participantsModelFromExcel.Count > 150)
            {
                return "Please limit bulk import to a maximum of 150 records in one transaction.";
            }

            foreach (var model in participantsModelFromExcel)
            {
                if (string.IsNullOrEmpty(model.EnterpriseId))
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = "Enterprise ID is missing.";
                    validation.RowNumber = model.RowNumber;
                    results.Add(validation);
                }
                if (string.IsNullOrEmpty(model.UserGroup))
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = "Participant Layout is missing.";
                    validation.RowNumber = model.RowNumber;
                    results.Add(validation);
                }
            }

            var duplicates = from m in participantsModelFromExcel
                             group m by m.EnterpriseId into g
                             where g.Count() > 1
                             select g;

            if (duplicates.Any())
            {
                foreach (var item in duplicates)
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = "Duplicates found in Excel " + item.Key;
                    validation.RowNumber = item.FirstOrDefault().RowNumber;
                    results.Add(validation);
                }
            }

            if (results.Any())
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in results)
                {
                    builder.Append("Error found in Excel row " + (Convert.ToInt32(item.RowNumber) + 1).ToString() + ", Error detail: " + item.Error + "\r\n");
                }
                return builder.ToString();
            }
            #endregion

            #region  Split to new and existing participants

            List<ParticipantModelForExcel> newParticipantsModelExcel = new List<ParticipantModelForExcel>();
            List<ParticipantModelForExcel> existingParticipantsModelExcel = new List<ParticipantModelForExcel>();

            #region Option 1: Get all owned participants then filter (Current)
            //// query owned event participants content and filter by owner
            //var ownedEventParticipants = _contentManager.Query(VersionOptions.Latest, "Participant")
            //    .Where<CommonPartRecord>(cr => cr.OwnerId == owner.Id)
            //    .List();

            //// existing participants to update
            //existingParticipantsModelExcel = participantsModelFromExcel
            //    .Where(i => ownedEventParticipants
            //        .Select(s => s.As<ParticipantPart>().EnterpriseId)
            //        .Contains(i.EnterpriseId))
            //    .ToList();

            //// new participants to create
            //newParticipantsModelExcel = participantsModelFromExcel.Except(existingParticipantsModelExcel).ToList();
            #endregion

            #region Option 2: Get each item from database
            foreach (var excelItem in participantsModelFromExcel)
            {
                var participant = _contentManager.Query(VersionOptions.Latest, "Participant")
                    .Where<CommonPartRecord>(cr => cr.OwnerId == owner.Id)
                    .Where<ParticipantPartRecord>(p => p.EnterpriseId == excelItem.EnterpriseId)
                    .Count();
                if (participant != 0)
                {
                    existingParticipantsModelExcel.Add(excelItem);
                }
                else
                {
                    newParticipantsModelExcel.Add(excelItem);
                }
            }
            #endregion

            #endregion

            #region Get all data from service
            string[] eids = participantsModelFromExcel.Select(s => s.EnterpriseId.Trim()).ToArray();

            var profiles = _peopleService.GetBulkProfile(eids);
            #endregion

            #region Validation profile count
            if (((List<dynamic>)profiles).Count() != eids.Count())
            {
                _transactionManager.Cancel();
                var message = "Error retrieving profiles: The total count returned from the People Service is less than the requested number of EIDs.";
                Logger.Error(message);
                //return message;
            }
            #endregion

            #region Validation of missing profiles and list all participants with no profiles
            List<ParticipantModelForExcel> noProfileParticipantsModelExcel = new List<ParticipantModelForExcel>();

            foreach (var item in (List<dynamic>)profiles)
            {
                if (string.IsNullOrEmpty(item.PeopleKey))
                {
                    ValidationResult result = new ValidationResult();
                    result.Error = String.Format("EnterpriseID {0} is not found in People Service.", item.EnterpriseId);
                    result.RowNumber = participantsModelFromExcel.FirstOrDefault(f => f.EnterpriseId == item.EnterpriseId).RowNumber;
                    results.Add(result);

                    ParticipantModelForExcel notExisting = new ParticipantModelForExcel();
                    notExisting.EnterpriseId = item.EnterpriseId;
                    noProfileParticipantsModelExcel.Add(notExisting);
                }
            }
            #endregion

            #region List all participants model lists with profiles
            // list all participants with profiles
            List<ParticipantModelForExcel> participantsModelExcelWithProfile = newParticipantsModelExcel
                .Union(existingParticipantsModelExcel)
                .Except(noProfileParticipantsModelExcel)
                .ToList();

            List<ParticipantModel> participantsModels = new List<ParticipantModel>();
            foreach (var item in participantsModelExcelWithProfile)
            {
                var profile = ((List<dynamic>)profiles).Where(p => p.EnterpriseId == item.EnterpriseId.Trim()).FirstOrDefault();

                if (profile != null)
                {
                    participantsModels.Add(new ParticipantModel
                    {
                        EnterpriseId = profile.EnterpriseId,
                        PeopleKey = profile.PeopleKey,
                        DisplayName = profile.DisplayName,
                        Avatar = profile.Avatar,
                        Email = profile.Email,
                        WorkEmail = profile.WorkEmail,
                        Phone = profile.Phone,
                        WorkPhone = profile.WorkPhone,
                        Mobile = profile.Mobile,
                        Country = profile.Country,
                        CountryHome = profile.CountryHome,
                        City = profile.City,
                        HomeCity = profile.HomeCity,
                        Location = profile.Location,
                        CurrentLocation = profile.CurrentLocation,
                        TalentSegment = profile.TalentSegment,
                        JobTitle = profile.JobTitle,
                        CareerTrack = profile.CareerTrack,
                        CareerLevel = profile.CareerLevel,
                        DomainSpecialty = profile.DomainSpecialty,
                        IndustrySpecialty = profile.IndustrySpecialty,
                        FirstSecondarySpecialty = profile.FirstSecondarySpecialty,
                        SecondSecondarySpecialty = profile.SecondSecondarySpecialty,
                        StandardJobCode = profile.StandardJobCode,
                        Timezone = profile.Timezone,
                        Bio = profile.Bio,
                        Orglevel2desc = profile.Orglevel2desc,
                        CurrentProjects = profile.CurrentProjects,
                        CurrentClient = profile.CurrentClient,
                        UserGroup = item.UserGroup,
                        RowNumber = item.RowNumber
                    });
                }
                else
                {
                    ValidationResult result = new ValidationResult();
                    result.Error = String.Format(@"{0} is not found in People Service.", item.EnterpriseId);
                    result.RowNumber = item.RowNumber;
                    if (!results.Any(r => (r.Error == result.Error && r.RowNumber == result.RowNumber)))
                    {
                        results.Add(result);
                    }
                }
            }
            #endregion

            #region Validate ParticipantLayout
            foreach (var item in participantsModels)
            {
                dynamic term = null;
                bool notExistParentGroup = false;
                bool notExistChildGroup = false;
                List<String> validateParentGroupList = new List<String>();
                List<String> validateChildGroupList = new List<String>();
                List<UserGroup> groupListExcel = GetUserGroups(item.UserGroup).ToList();

                foreach (var groupExcel in groupListExcel)
                {
                    // assign childGroup if specified in excel (>)
                    string childGroup = String.IsNullOrEmpty(groupExcel.ChildGroup) ? item.Orglevel2desc : groupExcel.ChildGroup;

                    // validate participant with no org
                    if (string.IsNullOrEmpty(childGroup) && !string.IsNullOrEmpty(item.PeopleKey))
                    {
                        ValidationResult result = new ValidationResult();
                        result.Error = String.Format(@"{0}'s Participant Layout """" is empty in People Service. Please manually add this user in Orchard.", item.EnterpriseId);
                        result.RowNumber = item.RowNumber;
                        if (!results.Any(r => (r.Error == result.Error && r.RowNumber == result.RowNumber)))
                        {
                            results.Add(result);
                        }
                        notExistChildGroup = true;
                        continue;
                    }

                    // validate ParticipantLayout term
                    var isValidGroup = IsValidTaxonomyTerm("ParticipantLayout", groupExcel.ParentGroup);

                    // not existing parent group
                    notExistParentGroup = !isValidGroup;

                    // assign new child group and validate if exists
                    if (isValidGroup)
                    {
                        term = AssignChildTermForTaxonomyField("ParticipantLayout", childGroup, groupExcel.ParentGroup);
                        if ((term == null || term.Count == 0) && !string.IsNullOrEmpty(childGroup))
                        {
                            notExistChildGroup = true;
                        }
                    }

                    // not existing parent group with existing child group
                    if (!notExistChildGroup && notExistParentGroup)
                    {
                        string validatedUserGroup = String.Format("{0}>\"{1}\"", childGroup, groupExcel.ParentGroup);
                        validateParentGroupList.Add(validatedUserGroup);
                    }

                    // not existing child group
                    if (notExistChildGroup)
                    {
                        string parent = notExistParentGroup ? String.Format("\"{0}\"", groupExcel.ParentGroup) : groupExcel.ParentGroup;
                        string validatedOrglevel2 = String.Format("\"{0}\">{1}", childGroup, parent);
                        validateChildGroupList.Add(validatedOrglevel2);
                    }
                }

                // list results for parent group
                if (validateParentGroupList.Count > 0)
                {
                    ValidationResult result = new ValidationResult();
                    result.Error = String.Format(
                        "{0}'s Participant Layout {1} is not found in Orchard CMS. Please use a valid Participant Layout value"
                        , item.EnterpriseId, String.Join("; ", validateParentGroupList));
                    result.RowNumber = item.RowNumber;
                    results.Add(result);
                }

                // list results for child group
                if (validateChildGroupList.Count > 0)
                {
                    ValidationResult result = new ValidationResult();
                    result.Error = String.Format(
                        "{0}'s Participant Layout {1} is not found in Orchard CMS. Please request an update to the Participant Layout taxonomy, or manually add this user in Orchard."
                        , item.EnterpriseId, String.Join("; ", validateChildGroupList));
                    result.RowNumber = item.RowNumber;
                    results.Add(result);
                }
            }
            #endregion

            #region Return if errors found
            // Check for validation errors
            if (results.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                results.Sort((ValidationResult x, ValidationResult y) => Convert.ToInt32(x.RowNumber).CompareTo(Convert.ToInt32(y.RowNumber)));
                foreach (var item in results)
                {
                    sb.Append(String.Format("Error found in Excel row {0} , Error detail: {1} \r\n", Convert.ToInt32(item.RowNumber) + 1, item.Error));
                }
                string result = sb.ToString();

                return result;
            }
            #endregion

            #region Create the new participants
            // list new participants to create with profiles
            List<ParticipantModelForExcel> newParticipantsWithProfile = newParticipantsModelExcel.Except(noProfileParticipantsModelExcel).ToList();
            List<ParticipantModel> participantsModelToCreate = new List<ParticipantModel>();
            foreach (var item in newParticipantsWithProfile)
            {
                ParticipantModel model = participantsModels.Where(m => m.EnterpriseId == item.EnterpriseId).FirstOrDefault();
                participantsModelToCreate.Add(model);
            }

            // create content from model
            try
            {
                foreach (var participant in participantsModelToCreate)
                {
                    var part = _contentManager.New<ParticipantPart>("Participant");

                    ModifyParticipantPart(part, participant, owner, id, true);
                }
            }
            catch (Exception ex)
            {
                _transactionManager.Cancel();
                Logger.Error("Error occurs when create participant in import:" + ex.Message);
            }
            #endregion

            #region Update owned participants
            // list existing participants in event to update with profiles            
            List<ParticipantModelForExcel> existingParticipantsWithProfile = existingParticipantsModelExcel.Except(noProfileParticipantsModelExcel).ToList();
            List<ParticipantModel> participantsModelToUpdate = new List<ParticipantModel>();
            foreach (var item in existingParticipantsWithProfile)
            {
                ParticipantModel model = participantsModels.Where(m => m.EnterpriseId == item.EnterpriseId).FirstOrDefault();
                participantsModelToUpdate.Add(model);
            }

            // update content from model
            try
            {
                foreach (var participant in participantsModelToUpdate)
                {
                    var currentItem = _contentManager.Query(VersionOptions.Published, "Participant")
                        .Where<CommonPartRecord>(cr => cr.OwnerId == owner.Id)
                        .Where<ParticipantPartRecord>(i => i.EnterpriseId == participant.EnterpriseId)
                        .List()
                        .FirstOrDefault();

                    if (currentItem != null)
                    {
                        var part = currentItem.As<ParticipantPart>();

                        ModifyParticipantPart(part, participant, owner, id, false);
                    }
                }
            }
            catch (Exception ex)
            {
                _transactionManager.Cancel();
                Logger.Error("Error occurs when create participant in import:" + ex.Message);
            }
            #endregion

            return string.Empty;
        }

        public dynamic ImportSessionFile(int id, HttpPostedFileBase file, IUser owner)
        {
            SetTransactionLevelToReadUncommitted();

            List<ValidationResult> results = new List<ValidationResult>();

            #region Get the sheet
            var sheet = LoadSheet(file, "Agenda");
            #endregion

            #region Get the session dto collection from excel
            //2.Get the session dto collection from excel

            List<SessionModelForExcel> sessions = new List<SessionModelForExcel>();
            if (sheet != null)
            {
                var rowCount = sheet.LastRowNum;
                var cellCount = sheet.GetRow(0) == null ? 0 : sheet.GetRow(0).LastCellNum;
                SessionModelForExcel entity;
                for (var i = 1; i <= rowCount; i++)
                {
                    IRow currentRow = sheet.GetRow(i);
                    if (currentRow != null)
                    {
                        List<ICell> cells = new List<ICell>();
                        for (var j = 0; j < cellCount; j++)
                        {
                            var cell = currentRow.GetCell(j, MissingCellPolicy.RETURN_NULL_AND_BLANK);
                            if (j != 1 && j != 2)
                            {
                                if (cell != null)
                                {
                                    cell.SetCellType(CellType.String);
                                }
                            }

                            cells.Add(cell);
                        }
                        if (String.IsNullOrEmpty(string.Concat(cells)))
                        {
                            //ValidationResult validation = new ValidationResult();
                            //validation.Error = "There is an empty row in the Excel.";
                            //validation.RowNumber = i.ToString();
                            //results.Add(validation);
                            continue;
                        };
                        entity = new SessionModelForExcel();
                        entity.Title = cells[0] == null ? string.Empty : cells[0].StringCellValue.Trim();
                        if (cells[1] == null)
                        {
                            entity.StartTime = DateTime.MinValue;
                        }
                        else
                        {
                            entity.StartTime = cells[1].CellType == CellType.String ? DateTime.MaxValue : cells[1].DateCellValue;
                        }
                        if (cells[2] == null)
                        {
                            entity.EndTime = DateTime.MinValue;
                        }
                        else
                        {
                            entity.EndTime = cells[2].CellType == CellType.String ? DateTime.MaxValue : cells[2].DateCellValue;
                        }
                        entity.SessionType = cells[3] == null ? string.Empty : cells[3].StringCellValue.Trim();
                        entity.SessionCategory = string.Empty;
                        entity.AdGroup = cells[4] == null ? string.Empty : cells[4].StringCellValue.Trim();
                        entity.Description = cells[5] == null ? string.Empty : cells[5].StringCellValue.Trim();
                        entity.Presenter = cells[6] == null ? string.Empty : cells[6].StringCellValue.Trim();

                        entity.RowNumber = i.ToString();
                        sessions.Add(entity);
                    }
                    else
                    {
                        //ValidationResult validation = new ValidationResult();
                        //validation.Error = "There is an empty row in the Excel.";
                        //validation.RowNumber = i.ToString();
                        //results.Add(validation);
                    }
                }
            }
            #endregion

            #region Validations
            //3.Validations
            if (!sessions.Any())
            {
                return "There is no data found in the Excel. Please populate prior to importing.";
            }
            if (sessions.Count > 150)
            {
                return "Please limit bulk import to a maximum of 150 records in one transaction.";
            }

            ValidationResult result;
            for (int i = 0; i < sessions.Count(); i++)
            {
                if (string.IsNullOrEmpty(sessions[i].Title))
                {
                    result = new ValidationResult();
                    result.RowNumber = sessions[i].RowNumber;
                    result.Error = "Title is required.";
                    results.Add(result);
                }

                if (sessions[i].StartTime == DateTime.MinValue)
                {
                    result = new ValidationResult();
                    result.RowNumber = sessions[i].RowNumber;
                    result.Error = "Start Time is required.";
                    results.Add(result);

                }

                if (sessions[i].StartTime == DateTime.MaxValue)
                {
                    result = new ValidationResult();
                    result.RowNumber = sessions[i].RowNumber;
                    result.Error = "Start Time is invalid.";
                    results.Add(result);

                }

                if (sessions[i].EndTime == DateTime.MinValue)
                {
                    result = new ValidationResult();
                    result.RowNumber = sessions[i].RowNumber;
                    result.Error = "End Time is required.";
                    results.Add(result);
                }

                if (sessions[i].EndTime == DateTime.MaxValue)
                {
                    result = new ValidationResult();
                    result.RowNumber = sessions[i].RowNumber;
                    result.Error = "End Time is invalid.";
                    results.Add(result);

                }

                if ((sessions[i].StartTime != DateTime.MaxValue) && (sessions[i].StartTime > sessions[i].EndTime))
                {
                    result = new ValidationResult();
                    result.RowNumber = sessions[i].RowNumber;
                    result.Error = "Start Time cannot be later than End Time.";
                    results.Add(result);
                }

                if (string.IsNullOrEmpty(sessions[i].AdGroup))
                {
                    result = new ValidationResult();
                    result.RowNumber = sessions[i].RowNumber;
                    result.Error = "AD Group is required.";
                    results.Add(result);
                }
                else
                {
                    var adGroup = sessions[i].AdGroup;
                    var adGroups = adGroup.Split(';');

                    // validate including Admin AD Group if not existing in all imported sessions
                    List<string> listAdGroups = adGroups.ToList();
                    if (!listAdGroups.Contains(AdminAdGroup))
                    {
                        listAdGroups.Add(AdminAdGroup);
                    }

                    // validate taxonomy terms
                    var terms = FindExistingTermsForSession("ADGroup", listAdGroups.ToArray());

                    if (adGroups.Length > terms.Count)
                    {
                        List<string> existings = new List<string>();

                        foreach (var term in terms)
                        {
                            existings.Add(term.Name);
                        }

                        var notExistings = adGroups.ToList().Except(existings);
                        result = new ValidationResult();
                        result.RowNumber = sessions[i].RowNumber;
                        result.Error = "AD Group " + string.Join(";", notExistings) + " is not found in Orchard CMS.";
                        results.Add(result);
                    }
                }

                if (!string.IsNullOrEmpty(sessions[i].Presenter))
                {
                    // Get presenter Ids
                    List<string> presenterIds = GetNotExistingEids(sessions[i].Presenter, owner);
                    if (presenterIds.Any())
                    {
                        result = new ValidationResult();
                        result.RowNumber = sessions[i].RowNumber;
                        result.Error = "Presenter " + string.Join(",", presenterIds) + " cannot be found in Orchard CMS.";
                        results.Add(result);
                    }

                }

                if (!string.IsNullOrEmpty(sessions[i].SessionType))
                {
                    string[] splited = { sessions[i].SessionType };
                    var terms = FindExistingTermsForSession("SessionType", splited);
                    if (!terms.Any())
                    {
                        result = new ValidationResult();
                        result.RowNumber = sessions[i].RowNumber;
                        result.Error = "Session Type cannot be found.";
                        results.Add(result);
                    }
                }

                if (!string.IsNullOrEmpty(sessions[i].SessionCategory))
                {
                    string[] splited = { sessions[i].SessionCategory };
                    var terms = FindExistingTermsForSession("SessionCategory", splited);
                    if (!terms.Any())
                    {
                        result = new ValidationResult();
                        result.RowNumber = sessions[i].RowNumber;
                        result.Error = "Session Category cannot be found.";
                        results.Add(result);
                    }
                }
            }

            if (results.Any())
            {
                results.Sort((ValidationResult x, ValidationResult y) => Convert.ToInt32(x.RowNumber).CompareTo(Convert.ToInt32(y.RowNumber)));
                StringBuilder builder = new StringBuilder();
                foreach (var error in results)
                {
                    builder.Append("Error found in Excel row " + (Convert.ToInt32(error.RowNumber) + 1).ToString() + ", Error detail: " + error.Error + " \r\n");
                }
                return builder.ToString();
            }
            #endregion

            #region Create session
            try
            {
                foreach (var item in sessions)
                {
                    var session = _contentManager.New<SessionPart>("Session");

                    ((dynamic)session.ContentItem).SessionPart.TitlePart.Title = item.Title;

                    _contentManager.Create(session, VersionOptions.Draft);

                    ((DateTimeField)((dynamic)session.ContentItem).SessionPart.StartTime).DateTime = item.StartTime.ToUniversalTime();
                    ((DateTimeField)((dynamic)session.ContentItem).SessionPart.EndTime).DateTime = item.EndTime.ToUniversalTime();

                    string[] splitedSessionType = { item.SessionType };
                    var sessionTypeTerms = FindExistingTermsForSession("SessionType", splitedSessionType);
                    _taxonomyService.UpdateTerms(session.ContentItem, sessionTypeTerms, "SessionType");

                    string[] splitedSessionCategory = { item.SessionCategory };
                    var sessionCategoryTerms = FindExistingTermsForSession("SessionCategory", splitedSessionCategory);
                    _taxonomyService.UpdateTerms(session.ContentItem, sessionCategoryTerms, "SessionCategory");

                    // include Admin AD Group if not existing in all created sessions
                    List<string> listAdGroups = item.AdGroup.Split(';').ToList();
                    if (!listAdGroups.Contains(AdminAdGroup))
                    {
                        listAdGroups.Add(AdminAdGroup);
                    }

                    string[] splitedAdGroups = listAdGroups.ToArray();
                    var adGroupTerms = FindExistingTermsForSession("ADGroup", splitedAdGroups);
                    _taxonomyService.UpdateTerms(session.ContentItem, adGroupTerms, "ADGroup");

                    ((dynamic)session.ContentItem).SessionPart.BodyPart.Text = item.Description;

                    ((dynamic)session.ContentItem).SessionPart.AgendaPresenterPickerIds = item.Presenter;

                    var ids = new[] { id };
                    ((dynamic)session.ContentItem).SessionPart.EventPicker.Ids = ids;

                    // Owner of content item
                    ((dynamic)session.ContentItem).CommonPart.Owner = owner;

                    _contentManager.Publish(session.ContentItem);
                }
            }
            catch (Exception ex)
            {
                _transactionManager.Cancel();
                Logger.Error("Error occurs when create session in importing:" + ex.Message);
            }

            #endregion

            return string.Empty;
        }

        private void SetTransactionLevelToReadUncommitted()
        {
            TransactionManager transacManager = (TransactionManager)_transactionManager;
            transacManager.IsolationLevel = System.Data.IsolationLevel.ReadUncommitted;
        }

        public dynamic GetEvents()
        {
            var query = _contentManager.Query(VersionOptions.Published, "Event");
            if (!_orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner))
            {
                var result = query.Where<CommonPartRecord>(cr => cr.OwnerId == _orchardServices.WorkContext.CurrentUser.Id)
                    .List()
                    .OrderBy(p => ((dynamic)p).TitlePart.Title);
                return result;
            }
            else
            {
                var result = query.List().OrderBy(p => ((dynamic)p).TitlePart.Title);
                return result;
            }
        }

        public IUser GetEventOwner(int id)
        {
            IUser owner = null;
            EventPart part = _contentManager.Query<EventPart, EventPartRecord>(VersionOptions.Latest)
                    .Where<EventPartRecord>(p => p.Id == id)
                    .List()
                    .FirstOrDefault();
            if (part != null)
            {
                owner = part.As<CommonPart>().Owner;
            }
            return owner;
        }

        public dynamic GetParticipants(int eventId)
        {
            var query = _contentManager.Query(VersionOptions.Published, "Participant");
            var result = query.List().Where(w => ((string[])((dynamic)w).ParticipantPart.EventIds.Split(',')).Contains(eventId.ToString()));
            return result;
        }

        private List<string> ConvertToStringList(List<TermPart> terms)
        {
            List<string> groups = new List<string>();
            foreach (var item in terms)
            {
                groups.Add(item.Name);
            }
            return groups;
        }

        private List<string> GetNotExistingEids(string eids, IUser owner)
        {
            List<string> eidList = eids.Split(',').Select(s => s.Trim()).ToList();
            List<string> notExistingEids = new List<string>();

            foreach (var eidItem in eidList)
            {
                var idCount = _contentManager.Query(VersionOptions.Latest, "Participant")
                    .Where<CommonPartRecord>(cr => cr.OwnerId == owner.Id)
                    .Where<ParticipantPartRecord>(pr => pr.EnterpriseId == eidItem)
                    .Count();
                if (idCount == 0)
                {
                    notExistingEids.Add(eidItem);
                }
            }
            return notExistingEids;
        }

        private bool IsValidTaxonomyTerm(string taxonomyName, string rootGroup)
        {
            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);
            int termCount = _taxonomyService.GetTerms(taxonomy.Id).Where(t => t.Name == rootGroup).Count();
            return termCount == 0 ? false : true;
        }

        private List<TermPart> AssignChildTermForTaxonomyField(string taxonomyName, string name, string rootGroup)
        {
            var termResult = new List<TermPart>();
            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);
            var terms = _taxonomyService.GetTerms(taxonomy.Id).Where(t => t.Name == name);

            foreach (var term in terms)
            {
                var parent = _taxonomyService.GetParents(term).FirstOrDefault();
                if (parent != null && parent.Name == rootGroup)
                {
                    termResult.Add(term);
                }
            }
            return termResult;
        }

        private List<TermPart> FindExistingTermsForSession(string taxonomyName, string[] splitedTerms)
        {
            var termResult = new List<TermPart>();
            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);
            var terms = _taxonomyService.GetTerms(taxonomy.Id);
            foreach (var item in terms)
            {
                if (splitedTerms.Any(t => t.Trim() == item.Name))
                {
                    termResult.Add(item);
                }
            }
            return termResult;
        }

        private ISheet LoadSheet(HttpPostedFileBase file, String sheetName)
        {
            var extension = Path.GetExtension(file.FileName);

            IWorkbook workbook;
            ISheet sheet = null;

            if (extension == ".xlsx")
                workbook = new XSSFWorkbook(file.InputStream);
            else
                workbook = new HSSFWorkbook(file.InputStream);

            if (workbook.NumberOfSheets > 0)
            {
                sheet = workbook.GetSheet(sheetName);
            }
            return sheet;
        }

        private List<UserGroup> GetUserGroups(string userGroupField)
        {
            List<UserGroup> userGroups = new List<UserGroup>();
            List<string> groupText = userGroupField.Split(';').Select(s => s.Trim()).ToList();
            foreach (var text in groupText)
            {
                UserGroup ug = new UserGroup();
                ug.Text = text;
                ug.Levels = text.Split('>').Select(s => s.Trim()).ToList();
                ug.ParentGroup = ug.Levels[0];
                ug.ChildGroup = ug.Levels.Count > 1 ? ug.Levels[1] : string.Empty;
                userGroups.Add(ug);
            }
            return userGroups;
        }

        public class SessionModelForExcel
        {
            public string Title { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string SessionType { get; set; }
            public string SessionCategory { get; set; }
            public string AdGroup { get; set; }
            public string Description { get; set; }
            public string Presenter { get; set; }
            public string RowNumber { get; set; }
        }

        public class ValidationResult
        {
            public string Error { get; set; }
            public string RowNumber { get; set; }
        }

        public class ParticipantModel
        {
            public string EnterpriseId { get; set; }
            public string PeopleKey { get; set; }
            public string DisplayName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Avatar { get; set; }
            public string Email { get; set; }
            public string WorkEmail { get; set; }
            public string Phone { get; set; }
            public string WorkPhone { get; set; }
            public string ExtendNumber { get; set; }
            public string Mobile { get; set; }
            public string Country { get; set; }
            public string CountryHome { get; set; }
            public string City { get; set; }
            public string HomeCity { get; set; }
            public string Location { get; set; }
            public string CurrentLocation { get; set; }
            public string TalentSegment { get; set; }
            public string JobTitle { get; set; }
            public string CareerTrack { get; set; }
            public string CareerLevel { get; set; }
            public string DomainSpecialty { get; set; }
            public string IndustrySpecialty { get; set; }
            public string FirstSecondarySpecialty { get; set; }
            public string SecondSecondarySpecialty { get; set; }
            public string StandardJobCode { get; set; }
            public string Timezone { get; set; }
            public string Bio { get; set; }
            public string Orglevel2desc { get; set; }
            public string CurrentProjects { get; set; }
            public string CurrentClient { get; set; }
            public string UserGroup { get; set; }
            public string RowNumber { get; set; }
        }

        public class UserGroup
        {
            public string Text { get; set; }
            public string ParentGroup { get; set; }
            public string ChildGroup { get; set; }
            public List<string> Levels { get; set; }
        }

        private void ModifyParticipantPart(ParticipantPart part, ParticipantModel participant, IUser owner, int id, bool isCreate)
        {
            //1.Title
            ((dynamic)part.ContentItem).ParticipantPart.TitlePart.Title = participant.EnterpriseId;
            //2.ParticipantPart's fields
            part.EnterpriseId = participant.EnterpriseId;
            part.PeopleKey = participant.PeopleKey;
            part.DisplayName = participant.DisplayName;
            part.Avatar = participant.Avatar;
            part.Email = participant.Email;
            //part.WorkEmail = participant.WorkEmail;
            part.Phone = participant.Phone;
            part.WorkPhone = participant.WorkPhone;
            //part.Mobile = participant.Mobile;
            part.Country = participant.Country;
            //part.CountryHome = participant.CountryHome;
            part.City = participant.City;
            //part.HomeCity = participant.HomeCity;
            //part.Location = participant.Location;
            part.CurrentLocation = participant.CurrentLocation;
            part.TalentSegment = participant.TalentSegment;
            //part.JobTitle = participant.JobTitle;
            part.CareerTrack = participant.CareerTrack;
            part.CareerLevel = participant.CareerLevel;
            part.DomainSpecialty = participant.DomainSpecialty;
            part.IndustrySpecialty = participant.IndustrySpecialty;
            part.FirstSecondarySpecialty = participant.FirstSecondarySpecialty;
            part.SecondSecondarySpecialty = participant.SecondSecondarySpecialty;
            part.StandardJobCode = participant.StandardJobCode;
            part.Timezone = participant.Timezone;
            part.OrgLevel2Desc = participant.Orglevel2desc;
            part.ActiveProjects = participant.CurrentProjects;
            part.CurrentClient = participant.CurrentClient;

            ((dynamic)part.ContentItem).ParticipantPart.BodyPart.Text = participant.Bio;
            part.ProfessionalBio = participant.Bio;

            #region Participant Layout
            var userGroupList = GetUserGroups(participant.UserGroup).ToList();

            List<TermPart> allTerms = new List<TermPart>();
            foreach (var userGroupItem in userGroupList)
            {
                string childGroup = String.IsNullOrEmpty(userGroupItem.ChildGroup) ? participant.Orglevel2desc : userGroupItem.ChildGroup;
                var term = AssignChildTermForTaxonomyField("ParticipantLayout", childGroup, userGroupItem.ParentGroup);
                allTerms.AddRange(term);
            }
            part.MediaUrl = string.Empty;

            _contentManager.Create(part, VersionOptions.Draft);

            if (allTerms != null)
            {
                _taxonomyService.UpdateTerms(part.ContentItem, allTerms, "ParticipantLayout");
            }
            #endregion

            #region Event Ids
            List<int> idList = new List<int>();
            if (isCreate)
            {
                int[] ids = new[] { id };
                idList = ids.ToList();
            }
            else
            {
                int[] ids = ((dynamic)part.ContentItem).ParticipantPart.EventPicker.Ids as int[];
                idList = ids.ToList();
                idList.Add(id);
            }

            ((dynamic)part.ContentItem).ParticipantPart.EventPicker.Ids = idList.Distinct().ToArray();
            part.EventIds = String.Join(",", idList.Distinct().ToArray());
            #endregion

            ((dynamic)part.ContentItem).CommonPart.CreatedUtc = DateTime.UtcNow;
            ((dynamic)part.ContentItem).CommonPart.PublishedUtc = DateTime.UtcNow;
            ((dynamic)part.ContentItem).CommonPart.ModifiedUtc = DateTime.UtcNow;

            // Owner of content item
            ((dynamic)part.ContentItem).CommonPart.Owner = owner;

            _contentManager.Publish(part.ContentItem);
        }

        public dynamic BulkImportParticipant(int id, HttpPostedFileBase file, IUser owner)
        {
            #region [ VALIDATION ]

            List<ValidationResult> results = new List<ValidationResult>();
            List<ParticipantModelForExcel> participantsModelFromExcel = new List<ParticipantModelForExcel>();

            #region [ Upload file ]
            var sheet = LoadSheet(file, "Participant");
            #endregion

            #region [ Place into model ]
            if (sheet != null)
            {
                var rowCount = sheet.LastRowNum;
                var cellCount = sheet.GetRow(0) == null ? 0 : sheet.GetRow(0).LastCellNum;
                for (var i = 1; i <= rowCount; i++)
                {
                    IRow currentRow = sheet.GetRow(i);
                    List<ICell> cells = new List<ICell>();
                    if (currentRow != null)
                    {
                        for (var j = 0; j < cellCount; j++)
                        {
                            var cell = currentRow.GetCell(j, MissingCellPolicy.RETURN_NULL_AND_BLANK);
                            if (cell != null)
                            {
                                cell.SetCellType(CellType.String);
                            }
                            cells.Add(cell);
                        }

                        var enterpriseId = cells[0] == null ? string.Empty : cells[0].StringCellValue.Trim();
                        var userGroup = cells[1] == null ? string.Empty : cells[1].StringCellValue.Trim();

                        if (!(string.IsNullOrEmpty(enterpriseId) && string.IsNullOrEmpty(userGroup)))
                        {
                            participantsModelFromExcel.Add(new ParticipantModelForExcel
                            {
                                EnterpriseId = enterpriseId,
                                UserGroup = userGroup,
                                RowNumber = i.ToString()
                            });
                        }
                    }
                }
            }
            #endregion

            #region [ Validation for no data ]
            if (!participantsModelFromExcel.Any())
            {
                return "There is no data found in the Excel. Please populate prior to importing.";
            }
            #endregion

            #region [ Validation for missing fields ]
            foreach (var model in participantsModelFromExcel)
            {
                if (string.IsNullOrEmpty(model.EnterpriseId))
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = "Enterprise ID is missing.";
                    validation.RowNumber = model.RowNumber;
                    results.Add(validation);
                }
                if (string.IsNullOrEmpty(model.UserGroup))
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = "Participant Layout is missing.";
                    validation.RowNumber = model.RowNumber;
                    results.Add(validation);
                }
            }
            #endregion

            #region [ Validation for duplicates]
            var duplicates = from m in participantsModelFromExcel
                             group m by m.EnterpriseId into g
                             where g.Count() > 1
                             select g;

            if (duplicates.Any())
            {
                foreach (var item in duplicates)
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = "Duplicates found in Excel " + item.Key;
                    validation.RowNumber = item.FirstOrDefault().RowNumber;
                    results.Add(validation);
                }
            }
            #endregion

            #region [ Validate Participant Layout terms ]
            // Get valid terms
            var taxonomy = _taxonomyService.GetTaxonomyByName("ParticipantLayout");
            var terms = _taxonomyService.GetTerms(taxonomy.Id).ToList();

            foreach (var item in participantsModelFromExcel)
            {
                List<UserGroup> groups = GetUserGroups(item.UserGroup).ToList();

                foreach (var group in groups)
                {
                    // Check parent group
                    bool isValid = terms.Where(w => w.Name == group.ParentGroup).Count() != 0 ? true : false;
                    if (!isValid)
                    {
                        ValidationResult validation = new ValidationResult();
                        validation.Error = String.Format("Participant Layout '{0}' not valid.", group.ParentGroup);
                        validation.RowNumber = item.RowNumber;
                        results.Add(validation);
                    }

                    // Check child group
                    if (!String.IsNullOrEmpty(group.ChildGroup) && isValid)
                    {
                        isValid = false;
                        var childTerms = terms.Where(t => t.Name == group.ChildGroup);
                        foreach (var childTerm in childTerms)
                        {
                            var parent = _taxonomyService.GetParents(childTerm).FirstOrDefault();
                            if (parent != null && parent.Name == group.ParentGroup)
                            {
                                isValid = true;
                                break;
                            }
                        }
                        if (!isValid)
                        {
                            ValidationResult validation = new ValidationResult();
                            validation.Error = String.Format("Participant Layout '{0}>{1}' not valid.", group.ParentGroup, group.ChildGroup);
                            validation.RowNumber = item.RowNumber;
                            results.Add(validation);
                        }
                    }
                }
            }
            #endregion

            #region [ Validation results ]
            if (results.Any())
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in results)
                {
                    builder.Append("Error found in Excel row " + (Convert.ToInt32(item.RowNumber) + 1).ToString() + ", Error detail: " + item.Error + "\r\n");
                }
                return builder.ToString();
            }
            #endregion

            #endregion

            #region [ IMPORTING ]

            ImportPhaseProcess(participantsModelFromExcel, id, owner);

            #endregion

            #region [ UPDATING ]
            try
            {
                DateTime date = DateTime.UtcNow.AddMinutes(1);
                this.Logger.Error("Update Schedule: " + date.ToString());

                var eventPart = _contentManager.Query<EventPart, EventPartRecord>(VersionOptions.Latest)
                    .Where<EventPartRecord>(e => e.Id == id)
                    .List()
                    .FirstOrDefault();

                //UpdatePhaseProcess(eventPart);

                if (date > DateTime.UtcNow)
                {
                    this._taskManager.CreateTask(TaskType, date, eventPart.ContentItem);
                }
            }
            catch (Exception e)
            {
                this.Logger.Error(e, e.Message);
            }
            #endregion

            return string.Empty;
        }

        public void ImportPhaseProcess(List<ParticipantModelForExcel> participantsModelFromExcel, int id, IUser owner)
        {
            const int kMaxCommit = 100;
            var externalParticipants = participantsModelFromExcel;

            Dictionary<string, ParticipantPart> localParticipants = new Dictionary<string, ParticipantPart>();

            //localParticipants = _contentManager.Query<ParticipantPart, ParticipantPartRecord>(VersionOptions.Latest)
            //    .Where<CommonPartRecord>(cr => cr.OwnerId == owner.Id)
            //    .List()
            //    .ToDictionary(p => p.EnterpriseId);

            // query each participant
            foreach (var item in externalParticipants)
            {
                ParticipantPart participant = _contentManager.Query<ParticipantPart, ParticipantPartRecord>(VersionOptions.Latest)
                    .Where<CommonPartRecord>(cr => cr.OwnerId == owner.Id)
                    .Where<ParticipantPartRecord>(p => p.EnterpriseId == item.EnterpriseId)
                    .List().FirstOrDefault();
                if (participant != null)
                {
                    localParticipants.Add(participant.EnterpriseId, participant);
                }
            }

            var count = 0;
            foreach (var part in externalParticipants)
            {
                ParticipantPart p = null;

                //check if exists
                if (!localParticipants.TryGetValue(part.EnterpriseId, out p))
                {
                    var newPart = _contentManager.New<ParticipantPart>("Participant");
                    p = newPart;

                    ((dynamic)p.ContentItem).ParticipantPart.TitlePart.Title = part.EnterpriseId;
                    p.EnterpriseId = part.EnterpriseId;
                    p.As<CommonPart>().CreatedUtc = DateTime.UtcNow;

                    _contentManager.Create(newPart, VersionOptions.Draft);
                }

                //update ParticipantLayout
                List<UserGroup> userGroups = GetUserGroups(part.UserGroup).ToList();
                List<TermPart> oldTerms = _taxonomyService.GetTermsForContentItem(p.Id, "ParticipantLayout").ToList();
                List<TermPart> newTerms = new List<TermPart>();
                foreach (var userGroup in userGroups)
                {
                    var childGroup = String.IsNullOrEmpty(userGroup.ChildGroup) ? DefaultChildGroup : userGroup.ChildGroup;
                    var term = AssignChildTermForTaxonomyField("ParticipantLayout", childGroup, userGroup.ParentGroup);
                    if (term.Count != 0)
                    {
                        newTerms.AddRange(term);
                    }

                    var globalTerm = AssignChildTermForTaxonomyField("ParticipantLayout", DefaultChildGroup, userGroup.ParentGroup);
                    if (globalTerm.Count != 0)
                    {
                        newTerms.AddRange(globalTerm);
                    }
                }
                if (newTerms.Distinct().Except(oldTerms).Count() != 0)
                {
                    oldTerms.AddRange(newTerms.Distinct().Except(oldTerms));
                    _taxonomyService.UpdateTerms(p.ContentItem, oldTerms, "ParticipantLayout");
                }

                //update Event
                int[] ids = ((dynamic)p.ContentItem).ParticipantPart.EventPicker.Ids as int[];
                List<int> idList = ids.ToList();
                if (!idList.Contains(id))
                {
                    idList.Add(id);
                    ((dynamic)p.ContentItem).ParticipantPart.EventPicker.Ids = idList.Distinct().ToArray();
                    p.EventIds = String.Join(",", idList.Distinct().ToArray());
                }

                //publish
                p.As<CommonPart>().Owner = owner;
                p.As<CommonPart>().PublishedUtc = DateTime.UtcNow;
                p.As<CommonPart>().ModifiedUtc = DateTime.UtcNow;

                _contentManager.Publish(p.ContentItem);

                if (++count % kMaxCommit == 0)
                {
                    //commit and begin new transaction and clear content manager
                    _transactionManager.RequireNew();
                    _contentManager.Clear();
                }
            }
        }

        public void UpdatePhaseProcess(EventPart eventPart)
        {
            List<ValidationResult> results = new List<ValidationResult>();
            
            DateTime timeCheck = DateTime.UtcNow.AddMinutes(MinutesToCheck);
            int id = eventPart.Id;
            string eventName = eventPart.EventTitle;
            const int kMaxCommit = 100;

            // get event participants
            var localParticipants = _contentManager.Query<ParticipantPart, ParticipantPartRecord>(VersionOptions.Published)
                .Where<ParticipantPartRecord>(p =>
                    p.EventIds.StartsWith(id.ToString() + ",")
                    || p.EventIds.Contains("," + id.ToString() + ",")
                    || p.EventIds.EndsWith("," + id.ToString())
                    || (!p.EventIds.Contains(",") && p.EventIds == id.ToString()))
                .Where<CommonPartRecord>(c =>
                    c.PublishedUtc > timeCheck
                    || c.CreatedUtc > timeCheck
                    || c.ModifiedUtc > timeCheck)
                .List()
                .ToDictionary(p => p.EnterpriseId);

            // get profiles
            string[] eids = localParticipants.Select(s => s.Key.Trim()).ToArray();
            //var profiles = _peopleService.GetBulkProfile(eids);

            //this.Logger.Error(String.Format("Running Update Process for Event Id: {0}, Event Name: {1}, Range: {2}, EID Count: {3}, Profile Count: {4}"
            //    , id.ToString()
            //    , eventName
            //    , timeCheck
            //    , eids.Count()
            //    , ((List<dynamic>)profiles).Count()));

            this.Logger.Error(String.Format("Running Update Process for Event Id: {0}, Event Name: {1}, Range: {2}, EID Count: {3}"
                , id.ToString()
                , eventName
                , timeCheck
                , eids.Count()));
            
            var count = 0;

            foreach (var eid in eids)
            {
                //var profiles = _peopleService.GetBulkProfile(eids);
                
                string[] eidArr = eids.Where(w => w == eid).ToArray();
                var profiles = _peopleService.GetBulkProfile(eidArr);

                dynamic profile = ((List<dynamic>)profiles).Where(p => p.EnterpriseId == eid).FirstOrDefault();

                if (profile != null)
                {
                    ParticipantPart p = null;

                    // get local participant part
                    localParticipants.TryGetValue(profile.EnterpriseId, out p);

                    bool hasChanged = false;

                    #region Profile Data
                    // PeopleKey
                    if (p.PeopleKey != profile.PeopleKey)
                    {
                        p.PeopleKey = profile.PeopleKey;
                        hasChanged = true;
                    }

                    // DisplayName
                    if (p.DisplayName != profile.DisplayName)
                    {
                        p.DisplayName = profile.DisplayName;
                        hasChanged = true;
                    }

                    // Avatar
                    if (p.Avatar != profile.Avatar)
                    {
                        p.Avatar = profile.Avatar;
                        hasChanged = true;
                    }

                    // Email
                    if (p.Email != profile.Email)
                    {
                        p.Email = profile.Email;
                        hasChanged = true;
                    }

                    // Phone
                    if (p.Phone != profile.Phone)
                    {
                        p.Phone = profile.Phone;
                        hasChanged = true;
                    }

                    // WorkPhone
                    if (p.WorkPhone != profile.WorkPhone)
                    {
                        p.WorkPhone = profile.WorkPhone;
                        hasChanged = true;
                    }

                    // Country
                    if (p.Country != profile.Country)
                    {
                        p.Country = profile.Country;
                        hasChanged = true;
                    }

                    // City
                    if (p.City != profile.City)
                    {
                        p.City = profile.City;
                        hasChanged = true;
                    }

                    // CurrentLocation
                    if (p.CurrentLocation != profile.CurrentLocation)
                    {
                        p.CurrentLocation = profile.CurrentLocation;
                        hasChanged = true;
                    }

                    // TalentSegment
                    if (p.TalentSegment != profile.TalentSegment)
                    {
                        p.TalentSegment = profile.TalentSegment;
                        hasChanged = true;
                    }

                    // CareerTrack
                    if (p.CareerTrack != profile.CareerTrack)
                    {
                        p.CareerTrack = profile.CareerTrack;
                        hasChanged = true;
                    }

                    // CareerLevel
                    if (p.CareerLevel != profile.CareerLevel)
                    {
                        p.CareerLevel = profile.CareerLevel;
                        hasChanged = true;
                    }

                    // DomainSpecialty
                    if (p.DomainSpecialty != profile.DomainSpecialty)
                    {
                        p.DomainSpecialty = profile.DomainSpecialty;
                        hasChanged = true;
                    }

                    // IndustrySpecialty
                    if (p.IndustrySpecialty != profile.IndustrySpecialty)
                    {
                        p.IndustrySpecialty = profile.IndustrySpecialty;
                        hasChanged = true;
                    }

                    // FirstSecondarySpecialty
                    if (p.FirstSecondarySpecialty != profile.FirstSecondarySpecialty)
                    {
                        p.FirstSecondarySpecialty = profile.FirstSecondarySpecialty;
                        hasChanged = true;
                    }

                    // SecondSecondarySpecialty
                    if (p.SecondSecondarySpecialty != profile.SecondSecondarySpecialty)
                    {
                        p.SecondSecondarySpecialty = profile.SecondSecondarySpecialty;
                        hasChanged = true;
                    }

                    // StandardJobCode
                    if (p.StandardJobCode != profile.StandardJobCode)
                    {
                        p.StandardJobCode = profile.StandardJobCode;
                        hasChanged = true;
                    }

                    // Timezone
                    if (p.Timezone != profile.Timezone)
                    {
                        p.Timezone = profile.Timezone;
                        hasChanged = true;
                    }

                    // OrgLevel2Desc
                    if (p.OrgLevel2Desc != profile.Orglevel2desc)
                    {
                        p.OrgLevel2Desc = profile.Orglevel2desc;
                        hasChanged = true;
                    }

                    // ActiveProjects
                    if (p.ActiveProjects != profile.CurrentProjects)
                    {
                        p.ActiveProjects = profile.CurrentProjects;
                        hasChanged = true;
                    }

                    // CurrentClient
                    if (p.CurrentClient != profile.CurrentClient)
                    {
                        p.CurrentClient = profile.CurrentClient;
                        hasChanged = true;
                    }

                    // ProfessionalBio
                    if (p.ProfessionalBio != profile.Bio || ((dynamic)p.ContentItem).ParticipantPart.BodyPart.Text != profile.Bio)
                    {
                        ((dynamic)p.ContentItem).ParticipantPart.BodyPart.Text = profile.Bio;
                        p.ProfessionalBio = profile.Bio;
                        hasChanged = true;
                    }
                    #endregion

                    #region ParticipantLayout
                    // ParticipantLayout
                    List<TermPart> oldTerms = _taxonomyService.GetTermsForContentItem(p.Id, "ParticipantLayout").ToList();
                    List<TermPart> newTerms = new List<TermPart>();
                    foreach (var oldTerm in oldTerms)
                    {
                        if (oldTerm.Name == DefaultChildGroup)
                        {
                            TermPart parentTerm = _taxonomyService.GetParents(oldTerm).FirstOrDefault();
                            var term = AssignChildTermForTaxonomyField("ParticipantLayout", profile.Orglevel2desc, parentTerm.Name);
                            if (term.Count != 0)
                            {
                                newTerms.AddRange(term);
                            }
                        }
                    }
                    if (newTerms.Distinct().Except(oldTerms).Count() != 0)
                    {
                        oldTerms.AddRange(newTerms.Distinct().Except(oldTerms));
                        _taxonomyService.UpdateTerms(p.ContentItem, oldTerms, "ParticipantLayout");
                        hasChanged = true;
                    }
                    #endregion

                    if (hasChanged)
                    {
                        //publish
                        p.As<CommonPart>().PublishedUtc = DateTime.UtcNow;
                        p.As<CommonPart>().ModifiedUtc = DateTime.UtcNow;
                        _contentManager.Publish(p.ContentItem);
                        
                        ValidationResult validation = new ValidationResult();
                        validation.Error = String.Format("Updated: {0}", p.EnterpriseId);
                        validation.RowNumber = count.ToString();
                        results.Add(validation);
                    }
                    else
                    {
                        ValidationResult validation = new ValidationResult();
                        validation.Error = String.Format("Unchanged: {0}", p.EnterpriseId);
                        validation.RowNumber = count.ToString();
                        results.Add(validation);
                    }

                    if (++count % kMaxCommit == 0)
                    {
                        //commit and begin new transaction and clear content manager
                        _transactionManager.RequireNew();
                        _contentManager.Clear();
                    }
                }
                else
                {
                    ValidationResult validation = new ValidationResult();
                    validation.Error = String.Format("Error: {0}", eid);
                    validation.RowNumber = count.ToString();
                    results.Add(validation);
                }
            }

            if (results.Any())
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in results)
                {
                    builder.Append(item.Error + ",");
                }
                this.Logger.Error(builder.ToString());
            }
        }

        public void Process(ScheduledTaskContext context)
        {
            if (context.Task.TaskType == TaskType)
            {
                try
                {
                    UpdatePhaseProcess(context.Task.ContentItem.As<EventPart>());
                }
                catch (Exception e)
                {
                    this.Logger.Error(e, "Update Task Error: " + e.Message);
                }
                finally
                {
                    this.Logger.Error("Update Task Completed.");
                }
            }
        }

    }
}