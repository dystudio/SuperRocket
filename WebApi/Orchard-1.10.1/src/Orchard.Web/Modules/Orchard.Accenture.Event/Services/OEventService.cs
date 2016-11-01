using Orchard.Accenture.Event.Common;
using Orchard.Accenture.Event.Interfaces;
using Orchard.Accenture.Event.Models;
using Orchard.Accenture.Event.ServiceModels;
using Orchard.Accenture.Event.ViewModels;
using Orchard;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Orchard.Data;
using System.Net.Http;
using Orchard.Settings;
using Orchard.Accenture.Event.Orchard.Odata.Services;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Text;
using Orchard.Taxonomies.Services;
using Orchard.Taxonomies.Models;
using System.Collections.ObjectModel;
using Orchard.MediaLibrary.Fields;
using System.IO;
using Orchard.Taxonomies;
using Contrib.MediaFolder.Models;
using Orchard.Services;
using Orchard.Environment.Features;
using Orchard.Environment.Descriptor.Models;
using Orchard.FileSystems.Media;
using Orchard.MediaLibrary.Models;
using CommonModels = Orchard.Core.Common.Models;
using TitleModels = Orchard.Core.Title.Models;

namespace Orchard.Accenture.Event.Services
{
    public class OEventService : IOEventService
    {

        private readonly IContentManager _contentManager;
        private readonly ICacheManager _cacheManager;
        private readonly IOrchardServices _orchardServices;
        private readonly ISiteService _siteService;
        private readonly ITaxonomyService _taxonomyService;
        private readonly ISignals _signals;
        private readonly HttpClient _client = new HttpClient();
        private IClock _clock;
        private OrchardOData _db;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IFeatureManager _featureManager;
        private readonly ShellDescriptor _shellDescriptor;
        private readonly IStorageProvider _storageProvider;
        private readonly IRepository<ParticipantPartRecord> _participantRepository;
        private readonly IRepository<EventPartRecord> _eventRepository;
        private readonly IRepository<SessionPartRecord> _sessionRepository;
        private readonly IRepository<CirclePartRecord> _circleRepository;
        private readonly IRepository<InfoCardPartRecord> _infoCardPartRecord;

        public OEventService(
            IContentManager contentManager,
            ICacheManager cacheManager,
            ISignals signals,
            IOrchardServices orchardServices,
            ISiteService siteService,
            ITaxonomyService taxonomyService,
            IClock clock,
            IWorkContextAccessor workContextAccessor,
            IFeatureManager featureManager,
            ShellDescriptor shellDescriptor,
            IStorageProvider storageProvider,
            IRepository<ParticipantPartRecord> participantRepository,
            IRepository<EventPartRecord> eventRepository,
            IRepository<SessionPartRecord> sessionRepository,
            IRepository<CirclePartRecord> circleRepository,
            IRepository<InfoCardPartRecord> infoCardPartRecord
            )
        {
            _contentManager = contentManager;
            _orchardServices = orchardServices;
            _cacheManager = cacheManager;
            _siteService = siteService;
            _taxonomyService = taxonomyService;
            _clock = clock;
            _workContextAccessor = workContextAccessor;
            _signals = signals;
            _featureManager = featureManager;
            _shellDescriptor = shellDescriptor;
            _storageProvider = storageProvider;
            _participantRepository = participantRepository;
            _eventRepository = eventRepository;
            _sessionRepository = sessionRepository;
            _circleRepository = circleRepository;
            _infoCardPartRecord = infoCardPartRecord;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }

        public ILogger Logger { get; set; }

        public dynamic LoadApp(string app)
        {
            var result = _cacheManager.Get(CacheAndSignals.APP_CACHE + app, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedApp(app);
            });

            return result;
        }

        public dynamic LoadEvents(string app, string eid, IEnumerable<string> adGroups)
        {
            var result = _cacheManager.Get(CacheAndSignals.EVENT_CACHE + app + eid, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedEvents(app, adGroups);
            });

            return result;
        }

        public dynamic LoadEvents(int? id, IEnumerable<string> adGroups)
        {
            _db = new OrchardOData(LoadOdataBaseUrl());
            var result = _eventRepository.Table.Where(i => i.Id == id).FirstOrDefault();
            var baseUrl = GetBaseUrl();
            return MapEventWithCircles(result, baseUrl, adGroups);
        }

        public dynamic LoadSessions(int? id, string eid, IEnumerable<string> adGroups)
        {
            List<object> result = _cacheManager.Get(CacheAndSignals.SESSION_CACHE + id + eid, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedSessions(id, adGroups);
            });

            return result;
        }

        public dynamic LoadParticipants(int? id, int? groupId)
        {

            var result = _cacheManager.Get(CacheAndSignals.PARTICIPANT_CACHE + id + groupId, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedParticipants(id, groupId);
            });

            return result;
        }

        public dynamic LoadParticipantsLayout(int? id)
        {
            var result = _cacheManager.Get(CacheAndSignals.PARTICIPANT_LAYOUT_CACHE + id, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedParticipantsLayout(id);
            });

            return result;
        }

        public dynamic LoadTerms(string taxonomyName)
        {
            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);
            var result = _taxonomyService.GetTerms(taxonomy.Id).ToList();

            return result;
        }

        public dynamic LoadChildren(int id, int level)
        {
            //1.Load parent term
            var parentTerm = _taxonomyService.GetTerm(id);
            //2.Load children terms
            var children = _taxonomyService.GetChildren(parentTerm);

            List<object> result = new List<object>();
            foreach (var item in children)
            {
                var currentLevel = item.Path.Count(x => x == '/');
                if (currentLevel == level)
                {
                    result.Add(new { Id = item.Id, Name = item.Name });
                }
            }

            return result;
        }

        public dynamic LoadParticipantsByGroup(int? id, string name)
        {

            _db = new OrchardOData(LoadOdataBaseUrl());

            var participants = _db.Participants.Where(p => p.ParticipantPart.EventPicker.Ids.Any(l => l == id)).ToList();

            var result = participants.Where(p => p.ParticipantPart.ParticipantLayout.TermPart.Any(t => t.Name == name));

            return result;
        }

        public dynamic LoadParticipants(string eid)
        {
            var participants = _participantRepository.Table.Where(p => p.EnterpriseId.Trim() == eid.Trim()).ToList();
            //search participants by eid
            var result = participants
                .Select(
                 p => new
                 {
                     Id = p.Id,
                     PeopleKey = p.PeopleKey,
                     EnterpriseId = p.EnterpriseId,
                     Name = p.DisplayName,
                     FirstName = p.FirstName,
                     LastName = p.LastName,
                     Role = p.CareerTrack,
                     Level = p.CareerLevel,
                     Country = p.Country,
                     City = p.City,
                     Email = p.Email,
                     Mobile = p.Phone,
                     StandardJobCode = p.StandardJobCode,
                     DomainSpecialty = p.DomainSpecialty,
                     IndustrySpecialty = p.IndustrySpecialty,
                     FirstSecondarySpecialty = p.FirstSecondarySpecialty,
                     SecondSecondarySpecialty = p.SecondSecondarySpecialty,
                     ProfessionalBio = p.ProfessionalBio,
                     JobTitle = p.CareerLevel,
                     WorkPhone = p.WorkPhone,
                     CurrentLocation = p.CurrentLocation,
                     TalentSegment = p.TalentSegment,
                     Timezone = p.Timezone,
                     PictureBase64 = p.Avatar,
                     DTE = p.OrgLevel2Desc,
                     ActiveProjects = p.ActiveProjects,
                     CurrentClient = p.CurrentClient,
                     Avatar = p.Avatar
                 }
                );



            return result.FirstOrDefault();
        }
        public dynamic LoadMultipleParticipants(string eid)
        {
            var eids = eid.Split(',').ToList();

            List<string> trimedEids = new List<string>();
            foreach (var item in eids)
            {
                trimedEids.Add(item.Trim());
            }

            var participants = _participantRepository.Table.Where(p => eid.Contains(p.EnterpriseId)).ToList();

            participants = participants.Where(p => trimedEids.Any(parEid => parEid.Trim() == p.EnterpriseId.Trim())).ToList();

            List<string> existings = new List<string>();
            foreach (var item in participants)
            {
                existings.Add(item.EnterpriseId);
            }

            var notExistings = trimedEids.Except(existings);
            if (notExistings.Any())
            {
                return new
                {
                    Result = "Enterprise ID not found: " + string.Join(",", notExistings.ToArray()),
                    Count = notExistings.Count()
                };
            }
            else
            {
                return new
                {
                    Result = string.Empty,
                    Count = 0
                };
            }

        }

        public dynamic LoadParticipants(int? participantId)
        {
            var participants = _participantRepository.Table.Where(p => p.Id == participantId).ToList();

            //search participants by eid
            var result = participants
                .Select(
                 p => new
                 {
                     Id = p.Id,
                     PeopleKey = p.PeopleKey,
                     EnterpriseId = p.EnterpriseId,
                     Name = p.DisplayName,
                     FirstName = p.FirstName,
                     LastName = p.LastName,
                     Role = p.CareerTrack,
                     Level = p.CareerLevel,
                     Country = p.Country,
                     City = p.City,
                     Email = p.Email,
                     Mobile = p.Phone,
                     StandardJobCode = p.StandardJobCode,
                     DomainSpecialty = p.DomainSpecialty,
                     IndustrySpecialty = p.IndustrySpecialty,
                     FirstSecondarySpecialty = p.FirstSecondarySpecialty,
                     SecondSecondarySpecialty = p.SecondSecondarySpecialty,
                     ProfessionalBio = p.ProfessionalBio,
                     JobTitle = p.CareerLevel,
                     WorkPhone = p.WorkPhone,
                     CurrentLocation = p.CurrentLocation,
                     TalentSegment = p.TalentSegment,
                     Timezone = p.Timezone,
                     PictureBase64 = p.Avatar,
                     DTE = p.OrgLevel2Desc,
                     ActiveProjects = p.ActiveProjects,
                     CurrentClient = p.CurrentClient,
                     Avatar = p.Avatar
                 }
                );

            return result.FirstOrDefault();
        }

        public dynamic LoadParticipants(string eid, int eventId)
        {
            var participants = _participantRepository.Table.Where(p => p.EnterpriseId == eid).ToList();

            participants = participants.Where(
                p => p.EventIds.Split(',').Any(splitedId => splitedId == eventId.ToString())).ToList();

            var result = participants
                .Select(
                 p => new
                 {
                     Id = p.Id,
                     PeopleKey = p.PeopleKey,
                     EnterpriseId = p.EnterpriseId,
                     Name = p.DisplayName,
                     FirstName = p.FirstName,
                     LastName = p.LastName,
                     Role = p.CareerTrack,
                     Level = p.CareerLevel,
                     Country = p.Country,
                     City = p.City,
                     Email = p.Email,
                     Mobile = p.Phone,
                     StandardJobCode = p.StandardJobCode,
                     DomainSpecialty = p.DomainSpecialty,
                     IndustrySpecialty = p.IndustrySpecialty,
                     FirstSecondarySpecialty = p.FirstSecondarySpecialty,
                     SecondSecondarySpecialty = p.SecondSecondarySpecialty,
                     ProfessionalBio = p.ProfessionalBio,
                     JobTitle = p.CareerLevel,
                     WorkPhone = p.WorkPhone,
                     CurrentLocation = p.CurrentLocation,
                     TalentSegment = p.TalentSegment,
                     Timezone = p.Timezone,
                     PictureBase64 = p.Avatar,
                     DTE = p.OrgLevel2Desc,
                     ActiveProjects = p.ActiveProjects,
                     CurrentClient = p.CurrentClient,
                     Avatar = p.Avatar
                 }
                );
            return result.FirstOrDefault();
        }

        public dynamic LoadParticipants()
        {
            var result = _participantRepository.Table.ToList();
            return result;
        }

        public dynamic LoadAvatar(int? id)
        {
            var result = _cacheManager.Get(CacheAndSignals.AVATAR_CACHE + id, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedAvatar(id);
            });
            return result;
        }

        public dynamic LoadInfoCards(int? id)
        {
            var result = _cacheManager.Get(CacheAndSignals.INFORCARDS_CACHE + id, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedInfoCards(id);
            });
            return result;
        }

        public dynamic LoadPolls(int? id)
        {
            var result = _cacheManager.Get(CacheAndSignals.POOLS_CACHE + id, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedPolls(id);
            });
            return result;
        }

        public dynamic LoadEvaluationList(int? id)
        {
            var result = _cacheManager.Get(CacheAndSignals.EVALUATIONS_CACHE + id, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedEvaluationList(id);
            });
            return result;
        }

        public dynamic LoadInfoCards()
        {
            _db = new OrchardOData(LoadOdataBaseUrl());
            var result = _db.InfoCards.ToList();
            return result;
        }

        public dynamic LoadDocuments(int? id, int? groupId, IEnumerable<string> adGroups)
        {
            try
            {
                var files = _cacheManager.Get(CacheAndSignals.DOCUMENT_CACHE + id + groupId, ctx =>
                {
                    ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                    return GetFiles(id);
                });

                files = files.Where(f => f.ADGroup.Any(
                    g => adGroups.Any(
                        a => a.Equals((g.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))))
                    .ToList();

                var baseUrl = GetBaseUrl();
                var filesResult = files.Where(f => f.DocumentLayout.Any(t => t.Id.ToString() == groupId.ToString())).Select(
                    f => new
                    {
                        Name = f.MediaPart == null ? string.Empty : f.MediaPart.FileName,
                        Date = f.Date,
                        Size = GetFileSize(f.MediaPart.MediaUrl),
                        DocUrl = f.MediaPart == null ? string.Empty : baseUrl + f.MediaPart.MediaUrl,
                        ImageUrl = f.CoverImage == null ? string.Empty : f.CoverImage,
                        AutoDownload = f.AutoDownload == null ? false : f.AutoDownload
                    }
                    );

                if (filesResult == null || filesResult.Count() == 0)
                {
                    return null;
                }

                var termPart = files.Where(f => f.DocumentLayout.Any(t => t.Id.ToString() == groupId.ToString())).FirstOrDefault()
                    .DocumentLayout.FirstOrDefault(t => t.Id.ToString() == groupId.ToString());

                var result = new
                {
                    CategoryId = groupId,
                    CategoryName = termPart == null ? string.Empty : termPart.Name,
                    DocumentList = filesResult

                };

                return result;
            }
            catch (Exception ex)
            {
                var groups = string.Empty;
                foreach (var item in adGroups)
                {
                    groups += item + " | ";
                }
                Logger.Error("Error occurs when load documents :" + ex.Message + " | StackTrace: " + ex.StackTrace + "ADGroups " + groups);
            }

            return null;
        }

        public dynamic LoadDocumentsLayout(int? id,string eid, IEnumerable<string> adGroups)
        {

            try
            {
                _db = new OrchardOData(LoadOdataBaseUrl());
                //1.Load Event by id and then get all the participant ids.
                var currentEvent = _eventRepository.Table.Where(i => i.Id == id).FirstOrDefault();

                var files = _cacheManager.Get(CacheAndSignals.DOCUMENT_CACHE + id, ctx =>
                {
                    ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                    return GetFiles(id);
                });


                files = files.Where(f => f.ADGroup.Any(
                    g => adGroups.Any(
                        a => a.Equals((g.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))))
                    .ToList();
                //1.Load Hierarchy by

                var clientLayout = currentEvent.DocumentLayoutFullPath;

                dynamic result = _cacheManager.Get(CacheAndSignals.DOCUMENT_CACHE + id + eid, ctx =>
                {
                    ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                    return this.LoadDocumentsLayout("DocumentLayout", files, clientLayout);
                });

                return result;
            }
            catch (Exception ex)
            {
                var groups = string.Empty;
                foreach (var item in adGroups)
                {
                    groups += item + " | ";
                }
                Logger.Error("Error occurs when load documents layout :" + ex.Message + " | StackTrace: " + ex.StackTrace + "ADGroups " + groups);
            }
            return null;
        }

        #region private
        private List<DocumentEntity> GetFiles(int? id)
        {
            int[] defaultIds = { 0 };

            var images = _contentManager.Query<MediaLibrary.Models.ImagePart>(VersionOptions.Latest, "Image").List()
            .Where(
            part => 
                (((dynamic)part.ContentItem).ImagePart.EventPicker.Ids == null ?
                defaultIds : (((dynamic)part.ContentItem).ImagePart.EventPicker.Ids as int[])).Any(eventId => eventId == id)
            );

            var vectorImages = _contentManager.Query<MediaLibrary.Models.VectorImagePart>(VersionOptions.Latest, "VectorImage").List()
                 .Where(
            part =>
                (((dynamic)part.ContentItem).VectorImagePart.EventPicker.Ids == null ?
                defaultIds : (((dynamic)part.ContentItem).VectorImagePart.EventPicker.Ids as int[])).Any(eventId => eventId == id)
            );

            var videos = _contentManager.Query<MediaLibrary.Models.VideoPart>(VersionOptions.Latest, "Video").List()
                 .Where(
            part =>
                (((dynamic)part.ContentItem).VideoPart.EventPicker.Ids == null ?
                defaultIds : (((dynamic)part.ContentItem).VideoPart.EventPicker.Ids as int[])).Any(eventId => eventId == id)
            );

            var audios = _contentManager.Query<MediaLibrary.Models.AudioPart>(VersionOptions.Latest, "Audio").List()
                 .Where(
            part =>
                (((dynamic)part.ContentItem).AudioPart.EventPicker.Ids == null ?
                defaultIds : (((dynamic)part.ContentItem).AudioPart.EventPicker.Ids as int[])).Any(eventId => eventId == id)
            );

            var documents = _contentManager.Query<MediaLibrary.Models.DocumentPart>(VersionOptions.Latest, "Document").List()
                 .Where(
            part =>
                (((dynamic)part.ContentItem).DocumentPart.EventPicker.Ids == null ?
                defaultIds : (((dynamic)part.ContentItem).DocumentPart.EventPicker.Ids as int[])).Any(eventId => eventId == id)
            );

            var oEmbeds = _contentManager.Query<MediaLibrary.Models.OEmbedPart>(VersionOptions.Latest, "OEmbed").List()
                 .Where(
            part =>
                (((dynamic)part.ContentItem).OEmbedPart.EventPicker.Ids == null ?
                defaultIds : (((dynamic)part.ContentItem).OEmbedPart.EventPicker.Ids as int[])).Any(eventId => eventId == id)
            );

            #region merge
            List<DocumentEntity> files = new List<DocumentEntity>();
            var baseUrl = GetBaseUrl();

            foreach (var doc in documents)
            {
                files.Add(new DocumentEntity
                {
                    Id = doc.Id,
                    Date = doc.ContentItem.As<CommonModels.CommonPart>().CreatedUtc,
                    MediaPart = doc.ContentItem.As<MediaLibrary.Models.MediaPart>(),
                    DocumentLayout = ((dynamic)doc.ContentItem).DocumentPart.DocumentLayout.Terms,
                    ADGroup = ((dynamic)doc.ContentItem).DocumentPart.ADGroup.Terms,
                    Title = doc.ContentItem.As<TitleModels.TitlePart>().Title,
                    CoverImage = ((dynamic)doc.ContentItem).DocumentPart.CoverImage.FirstMediaUrl == null ?
                    string.Empty : baseUrl + ((dynamic)doc.ContentItem).DocumentPart.CoverImage.FirstMediaUrl,
                    AutoDownload = ((dynamic)doc.ContentItem).DocumentPart.AutoDownload.Value

                });
            }
            foreach (var image in images)
            {
                files.Add(new DocumentEntity
                {
                    Id = image.Id,
                    Date = image.ContentItem.As<CommonModels.CommonPart>().CreatedUtc,
                    MediaPart = image.ContentItem.As<MediaLibrary.Models.MediaPart>(),
                    DocumentLayout = ((dynamic)image.ContentItem).ImagePart.DocumentLayout.Terms,
                    ADGroup = ((dynamic)image.ContentItem).ImagePart.ADGroup.Terms,
                    Title = image.ContentItem.As<TitleModels.TitlePart>().Title,
                    CoverImage = ((dynamic)image.ContentItem).ImagePart.CoverImage.FirstMediaUrl == null ?
                    string.Empty : baseUrl + ((dynamic)image.ContentItem).ImagePart.CoverImage.FirstMediaUrl,
                    AutoDownload = ((dynamic)image.ContentItem).ImagePart.AutoDownload.Value
                });
            }
            foreach (var vector in vectorImages)
            {
                files.Add(new DocumentEntity
                {
                    Id = vector.Id,
                    Date = vector.ContentItem.As<CommonModels.CommonPart>().CreatedUtc,
                    MediaPart = vector.ContentItem.As<MediaLibrary.Models.MediaPart>(),
                    DocumentLayout = ((dynamic)vector.ContentItem).DocumentPart.DocumentLayout.Terms,
                    ADGroup = ((dynamic)vector.ContentItem).DocumentPart.ADGroup.Terms,
                    Title = vector.ContentItem.As<TitleModels.TitlePart>().Title,
                    CoverImage = ((dynamic)vector.ContentItem).DocumentPart.CoverImage.FirstMediaUrl == null ?
                   string.Empty : baseUrl + ((dynamic)vector.ContentItem).DocumentPart.CoverImage.FirstMediaUrl,
                    AutoDownload = ((dynamic)vector.ContentItem).DocumentPart.AutoDownload.Value

                });
            }
            foreach (var video in videos)
            {
                files.Add(new DocumentEntity
                {
                    Id = video.Id,
                    Date = video.ContentItem.As<CommonModels.CommonPart>().CreatedUtc,
                    MediaPart = video.ContentItem.As<MediaLibrary.Models.MediaPart>(),
                    DocumentLayout = ((dynamic)video.ContentItem).DocumentPart.DocumentLayout.Terms,
                    ADGroup = ((dynamic)video.ContentItem).DocumentPart.ADGroup.Terms,
                    Title = video.ContentItem.As<TitleModels.TitlePart>().Title,
                    CoverImage = ((dynamic)video.ContentItem).DocumentPart.CoverImage.FirstMediaUrl == null ?
                   string.Empty : baseUrl + ((dynamic)video.ContentItem).DocumentPart.CoverImage.FirstMediaUrl,
                    AutoDownload = ((dynamic)video.ContentItem).DocumentPart.AutoDownload.Value

                });
            }
            foreach (var audio in audios)
            {
                files.Add(new DocumentEntity
                {
                    Id = audio.Id,
                    Date = audio.ContentItem.As<CommonModels.CommonPart>().CreatedUtc,
                    MediaPart = audio.ContentItem.As<MediaLibrary.Models.MediaPart>(),
                    DocumentLayout = ((dynamic)audio.ContentItem).DocumentPart.DocumentLayout.Terms,
                    ADGroup = ((dynamic)audio.ContentItem).DocumentPart.ADGroup.Terms,
                    Title = audio.ContentItem.As<TitleModels.TitlePart>().Title,
                    CoverImage = ((dynamic)audio.ContentItem).DocumentPart.CoverImage.FirstMediaUrl == null ?
                   string.Empty : baseUrl + ((dynamic)audio.ContentItem).DocumentPart.CoverImage.FirstMediaUrl,
                    AutoDownload = ((dynamic)audio.ContentItem).DocumentPart.AutoDownload.Value

                });
            }
            foreach (var oe in oEmbeds)
            {
                files.Add(new DocumentEntity
                {
                    Id = oe.Id,
                    Date = oe.ContentItem.As<CommonModels.CommonPart>().CreatedUtc,
                    MediaPart = oe.ContentItem.As<MediaLibrary.Models.MediaPart>(),
                    DocumentLayout = ((dynamic)oe.ContentItem).DocumentPart.DocumentLayout.Terms,
                    ADGroup = ((dynamic)oe.ContentItem).DocumentPart.ADGroup.Terms,
                    Title = oe.ContentItem.As<TitleModels.TitlePart>().Title,
                    CoverImage = ((dynamic)oe.ContentItem).DocumentPart.CoverImage.FirstMediaUrl == null ?
                   string.Empty : baseUrl + ((dynamic)oe.ContentItem).DocumentPart.CoverImage.FirstMediaUrl,
                    AutoDownload = ((dynamic)oe.ContentItem).DocumentPart.AutoDownload.Value

                });
            }
            #endregion

            return files;
        }

        private dynamic LoadCachedSessions(int? id, IEnumerable<string> adGroups)
        {
            var sessions = _sessionRepository.Table.Where(p => p.AgendaEventPickerIds.Contains(id.ToString())).ToList();

            sessions = sessions.Where(i => i.AgendaADGroups.Split(',').Any(
                group => adGroups.Any(
                    a => a.Equals((group ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
                    && i.AgendaEventPickerIds.Split(',').Any(pickedId => pickedId == id.ToString())
                    && i.SessionIsLatest == true
                    && i.SessionIsPublished == true
                    )
                .ToList();

            List<object> result = new List<object>();

            List<string> presenterIds = new List<string>();
            foreach (var item in sessions)
            {
                foreach (var i in item.AgendaPresenterPickerIds.Split(','))
                {
                    presenterIds.Add(i.Trim());
                }
            }
            presenterIds = presenterIds.Distinct().ToList();

            var presenterList = _participantRepository.Table.Where(p => presenterIds.Contains(p.EnterpriseId.Trim())).ToList();

            presenterList = presenterList.Where(
            p => presenterIds.Any(ids => ids.Split(',').Any(presenter => presenter.Trim() == p.EnterpriseId.Trim()))).ToList();

            foreach (var item in presenterList)
            {
                item.EnterpriseId = item.EnterpriseId.Trim();
            }

            var duplicates = from m in presenterList
                             group m by m.EnterpriseId into g
                             where g.Count() > 1
                             select g;


            if (duplicates.Any())
            {
                List<ParticipantPartRecord> needKeeping = new List<ParticipantPartRecord>();
                List<ParticipantPartRecord> allDuplicateParticipants = new List<ParticipantPartRecord>();
                foreach (var duplicate in duplicates)
                {
                    var ordered = duplicate.OrderByDescending(p => p.Id);
                    foreach (var item in ordered)
                    {
                        allDuplicateParticipants.Add(item);
                    }
                }

                foreach (var duplicate in duplicates)
                {
                    var ordered = duplicate.OrderByDescending(p => p.Id);
                    needKeeping.Add(ordered.FirstOrDefault());
                }

                presenterList = presenterList.Except(allDuplicateParticipants).ToList();
                presenterList.AddRange(needKeeping);

            }

            var baseUrl = GetBaseUrl();

            foreach (var session in sessions)
            {
                var presenterEids = session.AgendaPresenterPickerIds.Split(',');
                var presenters = presenterList.Where(p => presenterEids.Any(presenterEid => presenterEid.Trim() == p.EnterpriseId.Trim())).ToList();

                var eventCategory = session.AgendaCategory;

                result.Add(new
                {
                    EventId = id,
                    StartTime = GetDatetime(Convert.ToDateTime(session.AgendaStartTime), eventCategory),
                    EndTime = GetDatetime(Convert.ToDateTime(session.AgendaEndTime), eventCategory),
                    Title = session.AgendaTitle,
                    Description = session.AgendaFullDescription,
                    EventType = session.AgendaType,
                    EventCategory = eventCategory,
                    Entity = session.AgendaADGroups,
                    Employee = presenters.Select(p => new
                    {
                        Id = p.Id,
                        PeopleKey = p.PeopleKey,
                        EnterpriseId = p.EnterpriseId,
                        Name = p.DisplayName,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        Role = p.CareerTrack,
                        Level = p.CareerLevel,
                        Country = p.Country,
                        City = p.City,
                        Email = p.Email,
                        Mobile = p.Phone,
                        StandardJobCode = p.StandardJobCode,
                        DomainSpecialty = p.DomainSpecialty,
                        IndustrySpecialty = p.IndustrySpecialty,
                        FirstSecondarySpecialty = p.FirstSecondarySpecialty,
                        SecondSecondarySpecialty = p.SecondSecondarySpecialty,
                        ProfessionalBio = p.ProfessionalBio,
                        JobTitle = p.CareerLevel,
                        WorkPhone = p.WorkPhone,
                        CurrentLocation = p.CurrentLocation,
                        TalentSegment = p.TalentSegment,
                        Timezone = p.Timezone,
                        PictureBase64 = p.Avatar,
                        DTE = p.OrgLevel2Desc,
                        ActiveProjects = p.ActiveProjects,
                        CurrentClient = p.CurrentClient,
                        Avatar = p.MediaUrl

                    })
                });
            }

            return result;
        }

        private DateTime GetDatetime(DateTime time, string eventCategory)
        {
            if (eventCategory.ToLower() == "physical")
            {
                return TimeZoneInfo.ConvertTime(time, _workContextAccessor.GetContext().CurrentTimeZone);
            }
            else
            {
                return time;
            }
        }

        private dynamic LoadCachedEvents(string app, IEnumerable<string> adGroups)
        {
            _db = new OrchardOData(LoadOdataBaseUrl());
            var apps = _db.Apps.Where(p => p.TitlePart.Title == app).ToList();

            List<EventPartRecord> list = new List<EventPartRecord>();
            foreach (var application in apps)
            {
                var eventsFiltered = _eventRepository.Table.Where(
               i => i.AppPickerIds.Contains(application.Id.ToString())).ToList();

                eventsFiltered = eventsFiltered.Where(
                    i => i.AppPickerIds.Split(',').Any(splitedId => splitedId == application.Id.ToString())
                    && i.EventIsLatest == true
                    && i.EventIsPublished == true
                    ).ToList();

                list.AddRange(eventsFiltered);
            }

            list = list.Where(i => i.ADGroups.Split(',').Any(
                adGroup => adGroups.Any(
                    a => a.Equals((adGroup).Trim(), StringComparison.OrdinalIgnoreCase))))
                .ToList();
            var baseUrl = GetBaseUrl();
            var result = list.Select(i => MapEvent(i, baseUrl));
            return result;
        }

        private dynamic LoadCachedParticipants(int? id, int? groupId)
        {
            var term = _taxonomyService.GetTerm(Convert.ToInt32(groupId));

            var participantsUnderCurrentEvent = _participantRepository.Table.Where(
                p => p.EventIds.Contains(id.ToString())
                ).ToList();

            participantsUnderCurrentEvent = participantsUnderCurrentEvent.Where(
                p => string.IsNullOrEmpty(p.ParticipantLayoutFullPath) == false
                ).ToList();

            participantsUnderCurrentEvent = participantsUnderCurrentEvent.Where(
                p => p.EventIds.Split(',').Any(splited => splited == id.ToString())
                && p.ParticipantIsPublished == true
                && p.ParticipantIsLatest == true
                && p.ParticipantLayoutFullPath.Split(',').Any(splittedPath => splittedPath == term.FullPath
                )).ToList();

            var duplicates = from m in participantsUnderCurrentEvent
                             group m by m.EnterpriseId into g
                             where g.Count() > 1
                             select g;


            if (duplicates.Any())
            {
                List<ParticipantPartRecord> needKeeping = new List<ParticipantPartRecord>();
                List<ParticipantPartRecord> allDuplicateParticipants = new List<ParticipantPartRecord>();
                foreach (var duplicate in duplicates)
                {
                    var ordered = duplicate.OrderByDescending(p => p.Id);
                    foreach (var item in ordered)
                    {
                        allDuplicateParticipants.Add(item);
                    }
                }

                foreach (var duplicate in duplicates)
                {
                    var ordered = duplicate.OrderByDescending(p => p.Id);
                    needKeeping.Add(ordered.FirstOrDefault());
                }

                participantsUnderCurrentEvent = participantsUnderCurrentEvent.Except(allDuplicateParticipants).ToList();
                participantsUnderCurrentEvent.AddRange(needKeeping);

            }

            var baseUrl = GetBaseUrl();

            var result = participantsUnderCurrentEvent
                .Select(p => new
                {
                    Id = p.Id,
                    PeopleKey = p.PeopleKey,
                    EnterpriseId = p.EnterpriseId,
                    Name = p.DisplayName,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Role = p.CareerTrack,
                    Level = p.CareerLevel,
                    Country = p.Country,
                    City = p.City,
                    Email = p.Email,
                    Mobile = p.Phone,
                    StandardJobCode = p.StandardJobCode,
                    DomainSpecialty = p.DomainSpecialty,
                    IndustrySpecialty = p.IndustrySpecialty,
                    FirstSecondarySpecialty = p.FirstSecondarySpecialty,
                    SecondSecondarySpecialty = p.SecondSecondarySpecialty,
                    ProfessionalBio = p.ProfessionalBio,
                    JobTitle = p.CareerLevel,
                    WorkPhone = p.WorkPhone,
                    CurrentLocation = p.CurrentLocation,
                    TalentSegment = p.TalentSegment,
                    Timezone = p.Timezone,
                    PictureBase64 = p.Avatar,
                    DTE = p.OrgLevel2Desc,
                    ActiveProjects = p.ActiveProjects,
                    CurrentClient = p.CurrentClient,
                    Avatar = p.MediaUrl

                });

            return result;
        }

        private dynamic LoadCachedParticipantsLayout(int? id)
        {
            var currentEvent = _eventRepository.Table.Where(i => i.Id == id).FirstOrDefault();
            var participants = _cacheManager.Get(CacheAndSignals.PARTICIPANS_CACHE_FOR_EVENT + id, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(3)));
                return LoadCachedParticipants(id);
            });
            var clientLayout = currentEvent.ParticipantLayoutFullPath;

            dynamic layout = this.LoadParticipantsLayout("ParticipantLayout", participants.ToList(), clientLayout);
            var result = new
            {

                EventId = currentEvent.Id,
                EventTitle = currentEvent.EventTitle,
                Children = layout
            };
            return result;
        }
        private List<ParticipantPartRecord> LoadCachedParticipants(int? id)
        {
            var participants = _participantRepository.Table.Where(p => p.EventIds.Contains(id.ToString())).ToList();

            participants = participants.Where(p => p.EventIds.Split(',').Any(
                eventId => eventId == id.ToString())
                && p.ParticipantIsPublished == true
                && p.ParticipantIsLatest == true
            ).ToList();

            var duplicates = from m in participants
                             group m by m.EnterpriseId into g
                             where g.Count() > 1
                             select g;


            if (duplicates.Any())
            {
                List<ParticipantPartRecord> needKeeping = new List<ParticipantPartRecord>();
                List<ParticipantPartRecord> allDuplicateParticipants = new List<ParticipantPartRecord>();
                foreach (var duplicate in duplicates)
                {
                    var ordered = duplicate.OrderByDescending(p => p.Id);
                    foreach (var item in ordered)
                    {
                        allDuplicateParticipants.Add(item);
                    }
                }

                foreach (var duplicate in duplicates)
                {
                    var ordered = duplicate.OrderByDescending(p => p.Id);
                    needKeeping.Add(ordered.FirstOrDefault());
                }

                participants = participants.Except(allDuplicateParticipants).ToList();
                participants.AddRange(needKeeping);

            }
            return participants;
        }
        private dynamic LoadCachedApp(string app)
        {
            _db = new OrchardOData(LoadOdataBaseUrl());
            var currentApp = _db.Apps.Where(p => p.TitlePart.Title == app).ToList().OrderBy(i => i.CommonPart.CreatedUtc).FirstOrDefault();

            if (currentApp != null)
            {
                var baseUrl = GetBaseUrl();
                dynamic result = new
                {
                    Title = currentApp.TitlePart.Title,
                    Message = currentApp.AppPart.Message,
                    WelcomeVideoLink = currentApp.AppPart.WelcomeVideoLink,
                    WelcomeVideoCoverImage = currentApp.AppPart.AppWelcomeVideoCoverImage.MediaPart.FirstOrDefault() == null ?
                      string.Empty : baseUrl + currentApp.AppPart.AppWelcomeVideoCoverImage.MediaPart.FirstOrDefault().MediaUrl,
                    AcpetText = currentApp.AppPart.AcceptText,
                    DisagreeText = currentApp.AppPart.DisagreeText,
                    DateFormat = currentApp.AppPart.DateFormat.Value,
                    WelcomeTitle = currentApp.AppPart.WelcomeTitle,
                    DescriptionContext = currentApp.AppPart.DescriptionContext,
                    MachineName = currentApp.AppPart.MachineName,
                    ConsentForm = currentApp.BodyPart.Text
                };

                return result;
            }
            return null;
        }

        private dynamic LoadCachedInfoCards(int? id)
        {

            var currentEvent = _eventRepository.Table.Where(p => p.Id == id).FirstOrDefault();

            var infoCards = _infoCardPartRecord.Table.Where(p => p.EventPickerIds.Contains(id.ToString())).ToList();

            infoCards = infoCards.Where(
                p => p.EventPickerIds.Split(',').Any(eventId => eventId == id.ToString())
                && p.InfoCardIsPublished == true
                && p.InfoCardIsLatest == true
                ).ToList();

            var contactPickerIds = currentEvent.ContactPickerIds;
            //2.Load participants by ids
            var participants = _participantRepository.Table.Where(p => (contactPickerIds ?? string.Empty).Contains(p.EnterpriseId.Trim())).ToList();

            participants = participants.Where(
            p => (contactPickerIds ?? string.Empty).Split(',').Any(eid => eid.Trim() == p.EnterpriseId.Trim())).ToList();

            foreach (var item in participants)
            {
                item.EnterpriseId = item.EnterpriseId.Trim();
            }

            var duplicates = from m in participants
                             group m by m.EnterpriseId into g
                             where g.Count() > 1
                             select g;


            if (duplicates.Any())
            {
                List<ParticipantPartRecord> needKeeping = new List<ParticipantPartRecord>();
                List<ParticipantPartRecord> allDuplicateParticipants = new List<ParticipantPartRecord>();
                foreach (var duplicate in duplicates)
                {
                    var ordered = duplicate.OrderByDescending(p => p.Id);
                    foreach (var item in ordered)
                    {
                        allDuplicateParticipants.Add(item);
                    }
                }

                foreach (var duplicate in duplicates)
                {
                    var ordered = duplicate.OrderByDescending(p => p.Id);
                    needKeeping.Add(ordered.FirstOrDefault());
                }

                participants = participants.Except(allDuplicateParticipants).ToList();
                participants.AddRange(needKeeping);

            }

            var baseUrl = GetBaseUrl();
            var result = new
            {
                IntroduceVideo = new
                {
                    Player = currentEvent.IntroduceVideoPlayer,
                    Subject = currentEvent.IntroduceVideoSubject,
                    Descript = currentEvent.IntroduceVideoDescription,
                    CoverImage = baseUrl + currentEvent.VideoCoverImageUrl
                },
                Map = new
                {
                    DetailAddress = currentEvent.Location,
                    Description = currentEvent.LocationDescription
                },
                Contacts = participants.Select(t => new
                {
                    Id = t.Id,
                    FullName = t.DisplayName,
                    Telphone = t.Phone,
                    ExtNumber = t.ExtendNumber,
                    Avatar = t.Avatar,
                    EnterpriseId = t.EnterpriseId
                }),
                Cards = infoCards.Select(t =>
                {

                    return new
                    {
                        Title = t.Title,
                        StartDate = t.CardStartDate == "1/1/0001" ? string.Empty : t.CardStartDate,
                        EndDate = t.CardEndDate == "1/1/0001" ? string.Empty : t.CardEndDate,
                        CoverImage = t.CardCoverImageUrl,
                        HotelName = t.HotelName,
                        HotelAddress = t.HotelAddress,
                        Website = t.WebSite,
                        Telphone = t.Telphone,
                        ExtNumber = t.ExtNumber
                    };
                })
            };

            return result;
        }

        private dynamic LoadCachedPolls(int? id)
        {
            _db = new OrchardOData(LoadOdataBaseUrl());

            var polls = _db.Polls.Where(p => p.PollPart.EventPicker.Ids.Any(l => l == id)).ToList();
            var result = polls.Select(p => new
            {
                Id = p.Id,
                Title = p.TitlePart.Title,
                Description = p.PollPart.Description.Value,
                AllowAnonymousUser = p.PollPart.AllowAnonymousUser.Value,
                PollyLinked = p.PollPart.PollLinked.Value,
            });

            return result;
        }

        private dynamic LoadCachedEvaluationList(int? id)
        {
            _db = new OrchardOData(LoadOdataBaseUrl());

            var EvaluationList = _db.Evaluations.Where(p => p.EvaluationPart.EventPicker.Ids.Any(l => l == id)).ToList();
            var result = EvaluationList.Select(e => new
            {
                Id = e.Id,
                Title = e.TitlePart.Title,
                Description = e.EvaluationPart.Description.Value,
                QuickSurveyLinked = e.EvaluationPart.QuickSurveyLinked.Value,
            });
            return result;
        }

        private dynamic LoadCachedAvatar(int? id)
        {
            _db = new OrchardOData(LoadOdataBaseUrl());
            var participant = _participantRepository.Table.Where(p => p.Id == id).FirstOrDefault();
            string base64Avatar = participant.Avatar;
            var mediaUrl = participant.MediaUrl;

            if (!string.IsNullOrWhiteSpace(mediaUrl))
            {
                string url = GetBaseUrl() + mediaUrl;
                try
                {
                    var bytes = _client.GetByteArrayAsync(url).Result;
                    base64Avatar = Convert.ToBase64String(bytes).ToString();
                }
                catch (Exception ex)
                {
                    Logger.Error("The person's avatar has some problem, please make sure it is a valid picture." + ex.Message);
                }
            }

            return new
            {
                PeopleKey = participant.PeopleKey,
                EnterpriseId = participant.EnterpriseId,
                Data = base64Avatar
            };
        }

        private long GetFileSize(string path)
        {
            try
            {
                var isAmazonS3Enable = _featureManager.GetAvailableFeatures()
               .Where(f => _shellDescriptor.Features.Any(sf => sf.Name == f.Id))
               .Any(t => "Amba.AmazonS3".Equals(t.Name, StringComparison.OrdinalIgnoreCase));

                if (isAmazonS3Enable)
                {
                    return _storageProvider.GetFile(_storageProvider.GetStoragePath(path)).GetSize();
                }

                string abPath = string.Empty;
                path = HttpUtility.UrlDecode(path, Encoding.UTF8); // fix filename with space cannot found issue

                if (_siteService.GetSiteSettings().As<RemoteStorageSettingsPart>() == null)
                {
                    abPath = System.Web.HttpContext.Current.Server.MapPath(path);
                }
                else
                {
                    var directRouteEnaled = _siteService.GetSiteSettings().As<RemoteStorageSettingsPart>().Record.EnableDirectRoute;
                    if (directRouteEnaled)
                    {
                        if (path.Contains("http"))
                        {
                            Uri networkPath = new Uri(path);
                            path = networkPath.LocalPath;
                        }
                        var mediaLocation = _siteService.GetSiteSettings().As<RemoteStorageSettingsPart>().Record.MediaLocation;
                        //\\CDC100249\media\Default\test\Capture.PNG

                        abPath = mediaLocation + path.Replace('/', '\\').Replace("\\Media", "");
                    }
                }


                FileInfo file = new FileInfo(abPath);
                long size = file.Length;
                return size;
            }
            catch (Exception ex)
            {
                Logger.Error("Can not find the path provided : " + ex.Message);
                return 0;
            }

        }

        public class DocumentEntity
        {
            public int Id { get; set; }
            public DateTime? Date { get; set; }

            public MediaLibrary.Models.MediaPart MediaPart { get; set; }

            public IEnumerable<Taxonomies.Models.TermPart> DocumentLayout { get; set; }
            public IEnumerable<Taxonomies.Models.TermPart> ADGroup { get; set; }
            public string Title { get; set; }
            public string CoverImage { get; set; }
            public bool? AutoDownload { get; set; }

        }

        private Uri LoadOdataBaseUrl()
        {
            var result = _cacheManager.Get(CacheAndSignals.ODATA_BASE_URL_CACHE, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(60)));
                return LoadCachedOdataBaseUrl();
            });

            return result;
        }
        private Uri LoadCachedOdataBaseUrl()
        {
            return new Uri(_siteService.GetSiteSettings().BaseUrl + "/odata");
        }

        private dynamic LoadParticipantsLayout(string taxonomyName, List<ParticipantPartRecord> participants, string clientLayout)
        {
            int level = 1;
            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);
            var terms = _taxonomyService.GetTerms(taxonomy.Id);

            //2.Load all 1 level terms
            List<object> rootGroups = new List<object>();

            foreach (var item in terms)
            {
                var currentLevel = item.Path.Count(x => x == '/');
                if (currentLevel == level)
                {
                    var count = LoadAllChildrenParticipantsForClient(item, participants, clientLayout).Count();
                    if (count > 0)
                    {
                        bool clientContains = clientLayout.Split(',').Any(
                           t => t.Split('/').Any(
                               splitedClientId => splitedClientId == item.Id.ToString()
                              ));
                        if (clientContains)
                        {
                            rootGroups.Add(new
                            {
                                GroupId = item.Id,
                                GroupName = item.Name,
                                GroupParticipantsCount = count,
                                Children = LoadLayoutChildrenForParticipant(item, level, participants, clientLayout)
                            });
                        }
                    }
                }
            }

            return rootGroups;
        }

        private List<ParticipantPartRecord> LoadAllChildrenParticipantsForClient(Taxonomies.Models.TermPart term, List<ParticipantPartRecord> list, string clientLayout)
        {
            //1.Client layout's each term id == term.Id
            //2.Participant's layout's each term's id == term.Id
            var validPartipants = list.Where(p => string.IsNullOrEmpty(p.ParticipantLayoutFullPath) == false);
            var result = validPartipants.Where(
                p => clientLayout.Split(',').Any(
                    clientFullPath => clientFullPath.Split('/').Any(exactId => exactId == term.Id.ToString()))
                    && p.ParticipantLayoutFullPath.Split(',').Any(participantFullPath => participantFullPath.Split('/').Any(
                        participantTermId => participantTermId == term.Id.ToString()))
                ).ToList();
            return result;
        }

        private List<ParticipantPartRecord> LoadParticipantsByFullpath(string fullPath, List<ParticipantPartRecord> list)
        {
            var validPartipants = list.Where(p => string.IsNullOrEmpty(p.ParticipantLayoutFullPath) == false);
            var result = validPartipants.Where(p => p.ParticipantLayoutFullPath.Split(',').Any(splitedFullPath => splitedFullPath == fullPath)).ToList();
            return result;
        }
        private List<Participant> LoadParticipantsByGroup(string name, List<Participant> list)
        {
            var result = list.Where(p => p.ParticipantPart.ParticipantLayout.TermPart.Any(t => t.Name == name)).ToList();
            return result;
        }


        private dynamic LoadDocumentsLayout(string taxonomyName, List<DocumentEntity> list, string clientLayout)
        {
            int level = 1;
            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);
            var terms = _taxonomyService.GetTerms(taxonomy.Id);

            //2.Load all 1 level terms
            List<object> rootGroups = new List<object>();

            foreach (var item in terms)
            {
                var currentLevel = item.Path.Count(x => x == '/');
                if (currentLevel == level)
                {
                    string url = string.Empty;
                    if (item.Fields != null)
                    {
                        var image = item.Fields.Where(f => f.PartFieldDefinition.Name == "CoverImage").FirstOrDefault() as MediaLibraryPickerField;
                        if (image.MediaParts != null && image.MediaParts.Count() > 0)
                        {
                            url = image == null ? string.Empty : GetBaseUrl() + image.MediaParts.FirstOrDefault().MediaUrl;
                        }
                    }
                    var count = LoadAllChildrenDocumentsForClient(item.Id, list, clientLayout).Count();
                    if (count > 0)
                    {
                        bool clientContains = clientLayout.Split(',').Any(
                            t => t.Split('/').Any(
                                splitedClientId => splitedClientId == item.Id.ToString()
                               ));
                        if (clientContains)
                        {
                            rootGroups.Add(new
                            {
                                CategoryId = item.Id,
                                Title = item.Name,
                                Image = url,
                                Count = count,
                                Children = LoadLayoutChildrenForDocument(item, level, list, clientLayout)
                            });
                        }
                    }
                }
            }
            return rootGroups;
        }

        private List<DocumentEntity> LoadAllChildrenDocumentsForClient(int id, List<DocumentEntity> list, string clientLayout)
        {

            //var result = list.Where(p => p.DocumentLayout.Any(
            //t => t.FullPath.Contains(id.ToString()) && clientLayout.Any(c => c.FullPath == t.FullPath))).ToList();
            //return result;
            var result = list.Where(
               p => p.DocumentLayout.Any(t => clientLayout.Split(',').Any(
                       clientFullPath => clientFullPath.Split('/').Any(exactId => exactId == id.ToString()))
                       && t.FullPath.Split('/').Any(docuTermId => docuTermId == id.ToString())
                       )
                   ).ToList();
            return result;
        }
        private List<DocumentEntity> LoadDocumentsByFullpath(string fullPath, List<DocumentEntity> list)
        {
            var result = list.Where(p => p.DocumentLayout.Any(t => t.FullPath == fullPath)).ToList();
            return result;
        }
        private List<DocumentEntity> LoadDocumentsByGroup(string name, List<DocumentEntity> list)
        {
            var result = list.Where(p => p.DocumentLayout.Any(t => t.Name == name)).ToList();
            return result;
        }

        private dynamic LoadLayoutChildrenForParticipant(Taxonomies.Models.TermPart parent, int level, List<ParticipantPartRecord> list, string clientLayout)
        {

            level++;//level 2
            //Get children by current item
            List<object> childrenResult = new List<object>();
            var children = _taxonomyService.GetChildren(parent);

            foreach (var child in children)
            {
                var childLevel = child.Path.Count(x => x == '/');
                if (childLevel == level)
                {
                    if (LoadParticipantsByFullpath(child.FullPath, list).Count() > 0)
                    {
                        bool clientContains = clientLayout.Split(',').Any(
                            t => t.Split('/').Any(clientId => clientId == child.Id.ToString())
                            );
                        if (clientContains)
                        {
                            childrenResult.Add(new
                            {
                                GroupId = child.Id,
                                GroupName = child.Name,
                                GroupParticipantsCount = LoadParticipantsByFullpath(child.FullPath, list).Count,
                                Children = LoadLayoutChildrenForParticipant(child, level, list, clientLayout)
                            });
                        }

                    }
                }
            }


            return childrenResult;


        }

        private dynamic LoadLayoutChildrenForDocument(Taxonomies.Models.TermPart parent, int level, List<DocumentEntity> list, string clientLayout)
        {
            level++;//level 2
            //Get children by current item
            List<object> childrenResult = new List<object>();
            var children = _taxonomyService.GetChildren(parent);

            foreach (var child in children)
            {
                var childLevel = child.Path.Count(x => x == '/');
                if (childLevel == level)
                {
                    if (LoadDocumentsByFullpath(child.FullPath, list).Count() > 0)
                    {
                        string url = string.Empty;
                        if (child.Fields != null)
                        {
                            var image = child.Fields.Where(f => f.PartFieldDefinition.Name == "CoverImage").FirstOrDefault() as MediaLibraryPickerField;
                            if (image.MediaParts != null && image.MediaParts.Count() > 0)
                            {
                                url = image == null ? string.Empty : GetBaseUrl() + image.MediaParts.FirstOrDefault().MediaUrl;
                            }
                        }
                        bool clientLayoutContains = clientLayout.Split(',').Any(t => t == child.FullPath);
                        if (clientLayoutContains)
                        {
                            childrenResult.Add(new
                            {
                                CategoryId = child.Id,
                                Title = child.Name,
                                Image = url,
                                Count = LoadDocumentsByFullpath(child.FullPath, list).Count(),
                                Children = LoadLayoutChildrenForDocument(child, level, list, clientLayout)
                            });
                        }

                    }
                }
            }

            return childrenResult;
        }

        private static StringBuilder BuildQueryOption(string id, List<int> ids)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < ids.Count; i++)
            {
                if (i != ids.Count - 1)
                {
                    builder.Append(string.Format(" ({0} eq {1}) or ", id, ids[i]));
                }
                else
                {
                    builder.Append(string.Format(" ({0} eq {1}) ", id, ids[i]));
                }
            }

            var result = builder.ToString();
            return builder;
        }

        private List<object> MergeMedia<T>(List<object> result, List<T> docs, Func<T> createEntry)
        {
            foreach (var doc in docs)
            {
                result.Add((createEntry()));
            }

            return result;
        }
        private object MapEventWithCircles(EventPartRecord entity, string baseUrl, IEnumerable<string> adGroups)
        {
            var imageUrl = baseUrl + entity.CoverImageUrl;
            var skillCss = baseUrl + entity.SkincssUrl;

            var circles = _circleRepository.Table.Where(p => p.EventPickerIds.Contains(entity.Id.ToString())).ToList();

            var selectedCircles = circles.Where(
                p => p.EventPickerIds.Split(',').Any(eventId => eventId == entity.Id.ToString())
                && p.CircleIsPublished == true
                && p.CircleIsLatest == true
                ).ToList();

            var resultCircles = selectedCircles.Where(
                p => (p.AdGroups ?? string.Empty).Split(',').Any(group => adGroups.Any(g => g.Trim().Equals(group.Trim(), StringComparison.OrdinalIgnoreCase)))).ToList()
                .Select(p => new {
                    Title = p.Title,
                    CircleId = p.AnotherCircleId,
                    CircleGUID = p.AnotherCircleGUID
                });

            var result = new
            {
                Id = entity.Id,
                Code = "",
                Title = entity.EventTitle ?? string.Empty,
                SubTitle = entity.SubTitle ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                Location = entity.Location ?? string.Empty,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Date = entity.EndDate,
                CoverImage = imageUrl,
                Skincss = skillCss,
                CircleID = entity.CircleID,
                CircleGUID = entity.CircleGUID,
                Circles = resultCircles
            };
            return result;
        }
        private object MapEvent(EventPartRecord entity, string baseUrl)
        {
            var imageUrl = baseUrl + entity.CoverImageUrl;
            var skillCss = baseUrl + entity.SkincssUrl;

            var result = new
            {
                Id = entity.Id,
                Code = string.Empty,
                Title = entity.EventTitle ?? string.Empty,
                SubTitle = entity.SubTitle ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                Location = entity.Location ?? string.Empty,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Date = entity.EndDate,
                CoverImage = imageUrl,
                Skincss = skillCss,
                CircleID = entity.CircleID,
                CircleGUID = entity.CircleGUID
            };
            return result;
        }

        private string GetBaseUrl()
        {
            var isAmazonS3Enable = _featureManager.GetAvailableFeatures()
                .Where(f => _shellDescriptor.Features.Any(sf => sf.Name == f.Id))
                .Any(t => "Amba.AmazonS3".Equals(t.Name, StringComparison.OrdinalIgnoreCase));

            if (isAmazonS3Enable)
            {
                return string.Empty;
            }

            if (_siteService.GetSiteSettings().As<RemoteStorageSettingsPart>() == null)
            {
                return _siteService.GetSiteSettings().BaseUrl;
            }

            var directRouteEnaled = _siteService.GetSiteSettings().As<RemoteStorageSettingsPart>().Record.EnableDirectRoute;
            if (directRouteEnaled)
            {
                return string.Empty;
            }
            else
            {
                return _siteService.GetSiteSettings().BaseUrl;
            }

        }
        #endregion
        #region example
        //Keep it as a example for http client usage.

        //private const string EventsUrl = "/odata/Events?$format=json";
        //private const string EventsUrlById = "/odata/Events({0})?$format=json";
        //private const string SessionsUrl = "/odata/Sessions?$filter=startswith(TitlePart/Title,{0})&$format=json";
        //private const string ParticipantsUrlById = "/odata/Participants({0})?$format=json";
        //private const string ParticipantsUrl = "/odata/Participants?$format=json";

        //public dynamic LoadParticipants()
        //{
        //    var url = _siteService.GetSiteSettings().BaseUrl + ParticipantsUrl;
        //    var task = _client.GetAsync(url);
        //    var result = task.Result.Content.ReadAsStringAsync().Result;
        //    return result.ToString();
        //}

        //keep it here as example for some cases
        //public dynamic LoadEvents()
        //{
        //    var url = _siteService.GetSiteSettings().BaseUrl + EventsUrl;

        //    var request = new HttpRequestMessage()
        //    {
        //        RequestUri = new Uri(url),
        //        Method = HttpMethod.Get,
        //    };
        //    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //    var content = _client.SendAsync(request).Result.Content;
        //    return content;
        //}

        //List<Participant> participants = new List<Participant>();
        //foreach (var item in ids)
        //{
        //    participants.Add(_db.Participants.Where(p => p.Id == item).FirstOrDefault());
        //}


        //Below are not supported methods,crying
        //var queryResult = from p in _db.Participants
        //                  from participantId in ids
        //                  where p.Id == participantId
        //                  select p;
        //var result = queryResult.ToList();

        //var queryResult = from p in _db.Participants
        //                  where (new int?[] { 16, 17 }).Contains(p.Id)
        //                  select p;

        //var queryResult = from p in _db.Participants
        //                  where (ids).Any(idsItem => idsItem.ToString() == p.Id.ToString())
        //                  select p;
        //var queryResult = _db.Participants.Where(p => ids.Any(idsItem => idsItem == p.Id)); 
        #endregion
    }
}