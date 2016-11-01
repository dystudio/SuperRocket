using Orchard.Data.Migration;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using System.Data;
using Orchard.Indexing;
using Orchard.Environment.Extensions;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Services;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;

namespace Orchard.Accenture.Event
{
    public class ParticipantMigrations : DataMigrationImpl
    {
        private readonly ITaxonomyService _taxonomyService;
        private readonly IOrchardServices _orchardServices;
        private readonly IContentManager _contentManager;
        private readonly string[] sampleTerms = { "Nice", "Beautiful", "Handsome" };
        private readonly string[] documentTerms = { "Seating", "Hotel", "Location" };
        private readonly string[] sessionType = { "talk", "group", "video", "meal" };
        private readonly string[] sessionCategory = { "Virtual", "Physical" };
        private readonly string[] ADGroup = { "Global", "CIO", "AccentureLeadership" };
        private readonly string dateFormat = "MM/DD/YYYY HH:mm:ss\r\nMM/DD/YYYY hh:mm:ss A\r\nDD/MM/YYYY HH:mm:ss\r\nDD/MM/YYYY hh:mm:ss A";
        private readonly string localTimeZone = System.TimeZoneInfo.Local.DisplayName;
        public ParticipantMigrations(
            ITaxonomyService taxonomyService,
            IOrchardServices orchardServices,
            IContentManager contentManager)
        {
            _taxonomyService = taxonomyService;
            _orchardServices = orchardServices;
            _contentManager = contentManager;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }
        public int Create()
        {
            CreateTaxonomy("ADGroup", ADGroup);

            #region Particpant
            {
                SchemaBuilder.CreateTable("ParticipantPartRecord", table => table
                    .ContentPartRecord()
                    .Column("EnterpriseId", DbType.String) // add peopleKey(get from people service)
                    .Column("PeopleKey", DbType.String)
                    .Column("DisplayName", DbType.String)
                    .Column("FirstName", DbType.String) // FisrtName(keep null)
                    .Column("LastName", DbType.String) // LastName(keep null)
                    .Column("Avatar", DbType.String, column => column.Unlimited())
                    .Column("Email", DbType.String)
                    .Column("Phone", DbType.String)
                    .Column("WorkPhone", DbType.String) // new request from profile
                    .Column("ExtendNumber", DbType.String)
                    .Column("Country", DbType.String)
                    .Column("City", DbType.String)
                    .Column("TalentSegment", DbType.String)
                    .Column("CareerTrack", DbType.String)
                    .Column("CareerLevel", DbType.Int16)
                    .Column("DomainSpecialty", DbType.String)
                    .Column("IndustrySpecialty", DbType.String)
                    .Column("FirstSecondarySpecialty", DbType.String)
                    .Column("SecondSecondarySpecialty", DbType.String)
                    .Column("StandardJobCode", DbType.String)  // new request from profile
                    .Column("CurrentLocation", DbType.String)  // new request from profile
                    .Column("Timezone", DbType.String)  //new request from profile

               );

                ContentDefinitionManager.AlterPartDefinition("ParticipantPart",
                  builder => builder.Attachable());

                ContentDefinitionManager.AlterTypeDefinition("Participant", cfg => cfg
                  .WithPart("Participant")
                  .WithPart("CommonPart")
                  .WithPart("TitlePart")
                  .WithPart("AutoroutePart", builder => builder
                      .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                      .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                      .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: '{Content.Slug}', Description: 'some-participant'}]")
                      .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                  .WithPart("BodyPart")
                  .WithPart("ParticipantPart")
                  .WithPart("TagsPart")
                  .Creatable()
                  .Listable()
                  .Indexed());

                ContentDefinitionManager.AlterPartDefinition("Participant",
                    builder => builder
                        .WithField("ParticipantAvatar",
                            fieldBuilder => fieldBuilder
                                .OfType("MediaLibraryPickerField")
                                .WithDisplayName("Avatar")));

                CreateTaxonomy("ParticipantLayout", sampleTerms);

                ContentDefinitionManager.AlterPartDefinition("ParticipantPart", part => part
                   .WithField("ParticipantLayout", cfg => cfg
                   .OfType("TaxonomyField")
                   .WithDisplayName("Participant Layout")
                   .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                       .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                       .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                       .WithSetting("TaxonomyFieldSettings.Required", "true")
                       .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                       .WithSetting("TaxonomyFieldSettings.Taxonomy", "ParticipantLayout"))
                       .WithDescription("Participant Layout."));

                ContentDefinitionManager.AlterPartDefinition("ParticipantPart", part => part
                   .WithField("EventPicker", cfg => cfg    // add event picker for participant module
                   .OfType("SmartContentPickerField")
                   .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                   .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                   .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                   .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                   .WithDescription("Select a Event."));
            }



            #endregion

            #region Session
            {
                SchemaBuilder.CreateTable("SessionPartRecord", table => table
                   .ContentPartRecord()
               );

                ContentDefinitionManager.AlterPartDefinition("SessionPart",
                  builder => builder.Attachable());

                ContentDefinitionManager.AlterTypeDefinition("Session", cfg => cfg
                  .WithPart("Session")
                  .WithPart("CommonPart")
                  .WithPart("TitlePart")
                  .WithPart("AutoroutePart", builder => builder
                      .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                      .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                      .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: '{Content.Slug}', Description: 'some-Session'}]")
                      .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                  .WithPart("BodyPart")
                  .WithPart("SessionPart")
                  .WithPart("TagsPart")
                  .Creatable()
                  .Listable()
                  .Indexed());

                ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                   .WithField("StartTime", cfg => cfg
                   .OfType("DateTimeField")
                   .WithDisplayName("Start Time")
                   .WithSetting("DateTimeFieldSettings.Display", "DateAndTime")
                       .WithSetting("DateTimeFieldSettings.Hint", "Choose the start time for session. And please note the timezone is " + localTimeZone)
                       .WithSetting("DateTimeFieldSettings.Required", "true"))
                       .WithDescription("Session start time."));

                ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                   .WithField("EndTime", cfg => cfg
                   .OfType("DateTimeField")
                   .WithDisplayName("End Time")
                   .WithSetting("DateTimeFieldSettings.Display", "DateAndTime")
                       .WithSetting("DateTimeFieldSettings.Hint", "Choose the end time for session. And please note the timezone is " + localTimeZone)
                       .WithSetting("DateTimeFieldSettings.Required", "true"))
                       .WithDescription("Session end time."));

                ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                  .WithField("PresenterPicker", cfg => cfg
                  .OfType("SmartContentPickerField")
                      .WithSetting("SmartContentPickerFieldSettings.Multiple", "false")
                      .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a presenter.")
                      .WithSetting("SmartContentPickerFieldSettings.Required", "false")
                      .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Participant"))
                      .WithDescription("Select a presenter."));

                ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                   .WithField("EventPicker", cfg => cfg    // add event picker for session module
                   .OfType("SmartContentPickerField")
                   .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                       .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                       .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                       .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                       .WithDescription("Select a Event."));

                CreateTaxonomy("SessionType", sessionType);

                ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                   .WithField("SessionType", cfg => cfg
                   .OfType("TaxonomyField")
                   .WithDisplayName("Session Type")
                   .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                       .WithSetting("TaxonomyFieldSettings.SingleChoice", "true")
                       .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                       .WithSetting("TaxonomyFieldSettings.Required", "false")
                       .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                       .WithSetting("TaxonomyFieldSettings.Taxonomy", "SessionType"))
                       .WithDescription("Type."));

                CreateTaxonomy("SessionCategory", sessionCategory);

                ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                   .WithField("SessionCategory", cfg => cfg
                   .OfType("TaxonomyField")
                   .WithDisplayName("Session Category")
                   .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                       .WithSetting("TaxonomyFieldSettings.SingleChoice", "true")
                       .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                       .WithSetting("TaxonomyFieldSettings.Required", "false")
                       .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                       .WithSetting("TaxonomyFieldSettings.Taxonomy", "SessionCategory"))
                       .WithDescription("Session Category."));

                ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                   .WithField("ADGroup", cfg => cfg
                   .OfType("TaxonomyField")
                   .WithDisplayName("AD Group")
                   .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                       .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                       .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                       .WithSetting("TaxonomyFieldSettings.Required", "true")
                       .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                       .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                       .WithDescription("AD Group."));

            }
            #endregion

            #region Event
            {
                SchemaBuilder.CreateTable("EventPartRecord", table => table
                   .ContentPartRecord()
                   .Column("StartDate", DbType.String)
                   .Column("EndDate", DbType.String)
                   .Column("Location", DbType.String)
                   .Column("LocationDescription", DbType.String)
                   .Column("IntroduceVideoPlayer", DbType.String)
                   .Column("IntroduceVideoSubject", DbType.String)
                   .Column("IntroduceVideoDescription", DbType.String)
                   .Column("SubTitle", DbType.String)
                   .Column("Description", DbType.String)
                   .Column("CircleID", DbType.String)
                   .Column("CircleGUID", DbType.String)
               );

                ContentDefinitionManager.AlterPartDefinition("EventPart",
                  builder => builder.Attachable());

                ContentDefinitionManager.AlterTypeDefinition("Event", cfg => cfg
                  .WithPart("Event")
                  .WithPart("CommonPart")
                  .WithPart("TitlePart")
                  .WithPart("AutoroutePart", builder => builder
                      .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                      .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                      .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: '{Content.Slug}', Description: 'some-Event'}]")
                      .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                  .WithPart("EventPart")
                  .WithPart("TagsPart")
                  .WithPart("PublishLaterPart")
                  .Draftable()
                  .Creatable()
                  .Listable()
                  .Indexed());

                ContentDefinitionManager.AlterPartDefinition("EventPart",
                   builder => builder
                       .WithField("EventSkinFile",
                           fieldBuilder => fieldBuilder
                               .OfType("MediaLibraryPickerField")
                               .WithDisplayName("Skin File")));

                ContentDefinitionManager.AlterPartDefinition("EventPart",
                 builder => builder
                     .WithField("VideoCoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Video Cover Image")));

                ContentDefinitionManager.AlterPartDefinition("EventPart",
                  builder => builder
                      .WithField("CoverImage",
                          fieldBuilder => fieldBuilder
                              .OfType("MediaLibraryPickerField")
                              .WithDisplayName("Cover Image")));

                ContentDefinitionManager.AlterPartDefinition("EventPart", part => part
                  .WithField("AppPicker", cfg => cfg
                  .OfType("SmartContentPickerField")
                      .WithSetting("SmartContentPickerFieldSettings.Multiple", "false")
                      .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a App.")
                      .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                      .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "App"))
                      .WithDescription("Select a App."));

                ContentDefinitionManager.AlterPartDefinition("EventPart", part => part
                  .WithField("ContactPicker", cfg => cfg
                  .OfType("SmartContentPickerField")
                      .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                      .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a participant as Contact.")
                      .WithSetting("SmartContentPickerFieldSettings.Required", "false")
                      .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Participant"))
                      .WithDescription("Select a participant as Contact."));


                ContentDefinitionManager.AlterPartDefinition("EventPart", part => part
                  .WithField("ADGroup", cfg => cfg
                   .OfType("TaxonomyField")
                   .WithDisplayName("AD Group")
                   .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                       .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                       .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                       .WithSetting("TaxonomyFieldSettings.Required", "true")
                       .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                       .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                       .WithDescription("AD Group."));

                ContentDefinitionManager.AlterPartDefinition("EventPart", part => part
                  .WithField("ParticipantLayout", cfg => cfg
                  .OfType("TaxonomyField")
                  .WithDisplayName("Participant Layout")
                  .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                      .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                      .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                      .WithSetting("TaxonomyFieldSettings.Required", "true")
                      .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                      .WithSetting("TaxonomyFieldSettings.Taxonomy", "ParticipantLayout"))
                      .WithDescription("Participant Layout."));

                ContentDefinitionManager.AlterPartDefinition("EventPart", part => part
                .WithField("DocumentLayout", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("Document Layout")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "DocumentLayout"))
                   .WithDescription("Document Layout."));
            }
            #endregion

            #region App
            {
                SchemaBuilder.CreateTable("AppPartRecord", table => table
                   .ContentPartRecord()
                   .Column("WelcomeVideoLink", DbType.String, column => column.Unlimited())
                   .Column("WelcomeTitle", DbType.String)
                   .Column("DescriptionContext", DbType.String, column => column.Unlimited())
                   .Column("MachineName", DbType.String)
                   .Column("Message", DbType.String)
                   .Column("AcceptText", DbType.String)
                   .Column("DisagreeText", DbType.String)
               );

                ContentDefinitionManager.AlterPartDefinition("AppPart", part => part
                    .WithField("AppWelcomeVideoCoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Video Cover Image"))
                    .WithField("DateFormat", fieldBuilder => fieldBuilder.OfType("EnumerationField")
                       .WithDisplayName("Date Format")
                       .WithSetting("EnumerationFieldSettings.Hint", "Please provide the date format.")
                       .WithSetting("EnumerationFieldSettings.Options", dateFormat)
                       .WithSetting("EnumerationFieldSettings.ListMode", "Dropdown")));

                ContentDefinitionManager.AlterPartDefinition("AppPart",
                  builder => builder.Attachable());

                ContentDefinitionManager.AlterTypeDefinition("App", cfg => cfg
                  .WithPart("App")
                  .WithPart("CommonPart")
                  .WithPart("TitlePart")
                  .WithPart("AutoroutePart", builder => builder
                      .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                      .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                      .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: '{Content.Slug}', Description: 'some-Event'}]")
                      .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                  .WithPart("AppPart")
                  .WithPart("BodyPart")
                  .WithPart("TagsPart")
                  .Draftable()
                  .Creatable()
                  .Listable()
                  .Indexed());

            }
            #endregion

            #region Poll
            {
                ContentDefinitionManager.AlterPartDefinition("PollPart", builder => builder.Attachable());
                ContentDefinitionManager.AlterTypeDefinition("Poll", cfg => cfg
                    .WithPart("CommonPart")
                    .WithPart("PollPart")
                    .Creatable()
                    .Listable()
                    .Indexed()
                    );

                ContentDefinitionManager.AlterPartDefinition("PollPart", partBuilder => partBuilder
                    .WithField("Title", fieldBuilder => fieldBuilder.OfType("TextField")
                        .WithDisplayName("Poll Title")
                        .WithSetting("TextFieldSettings.Required", "true")
                        .WithSetting("TextFieldSettings.Flavor", "Wide"))
                   .WithField("Description", fieldBuilder => fieldBuilder.OfType("TextField")
                        .WithDisplayName("Poll Description")
                        .WithSetting("TextFieldSettings.Required", "true")
                        .WithSetting("TextFieldSettings.Flavor", "Wide"))
                   .WithField("AllowAnonymousUser", fieldBuilder => fieldBuilder.OfType("TextField")
                        .WithDisplayName("Max Count of Allowed Anonymous Users")
                        .WithSetting("TextFieldSettings.Required", "true")
                        .WithSetting("TextFieldSettings.Flavor", "Wide"))
                   .WithField("PollLinked", fieldBuilder => fieldBuilder.OfType("TextField")
                        .WithDisplayName("Input the Poll Link")
                        .WithSetting("TextFieldSettings.Required", "true")
                        .WithSetting("TextFieldSettings.Flavor", "Wide"))
                        );
                ContentDefinitionManager.AlterPartDefinition("PollPart", part => part
                   .WithField("EventPicker", cfg => cfg    // add event picker for Poll module
                   .OfType("SmartContentPickerField")
                   .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")  // change to Multi-event picker
                   .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                   .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                   .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                   .WithDescription("Select a Event."));
            }
            #endregion

            #region Evaluation
            {
                ContentDefinitionManager.AlterPartDefinition("EvaluationPart", builder => builder.Attachable());
                ContentDefinitionManager.AlterTypeDefinition("Evaluation", cfg => cfg
                    .WithPart("CommonPart")
                    .WithPart("EvaluationPart")
                    .Creatable()
                    .Listable()
                    .Indexed()
                    );

                ContentDefinitionManager.AlterPartDefinition("EvaluationPart", partBuilder => partBuilder
                    .WithField("Title", fieldBuilder => fieldBuilder.OfType("TextField")
                        .WithDisplayName("Evaluation Title")
                        .WithSetting("TextFieldSettings.Required", "true")
                        .WithSetting("TextFieldSettings.Flavor", "Wide"))
                   .WithField("Description", fieldBuilder => fieldBuilder.OfType("TextField")
                        .WithDisplayName("Evaluation Description")
                        .WithSetting("TextFieldSettings.Required", "true")
                        .WithSetting("TextFieldSettings.Flavor", "Wide"))
                   .WithField("QuickSurveyLinked", fieldBuilder => fieldBuilder.OfType("TextField")
                        .WithDisplayName("Input the QuickSurvey Link")
                        .WithSetting("TextFieldSettings.Required", "true")
                        .WithSetting("TextFieldSettings.Flavor", "Wide"))
                        );
                ContentDefinitionManager.AlterPartDefinition("EvaluationPart", part => part
                   .WithField("EventPicker", cfg => cfg    // add event picker for Evaluation module
                   .OfType("SmartContentPickerField")
                   .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                   .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                   .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                   .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                   .WithDescription("Select a Event."));
            }
            #endregion

            #region media
            CreateTaxonomy("DocumentLayout", documentTerms);

            ContentDefinitionManager.AlterPartDefinition("TermPart",
                 builder => builder
                     .WithField("CoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Cover Image")));

            ContentDefinitionManager.AlterPartDefinition("ImagePart", part => part
               .WithField("DocumentLayout", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("Document Layout")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "DocumentLayout"))
                   .WithDescription("Document Layout.")
               .WithField("EventPicker", cfg => cfg    // add event picker for media module
               .OfType("SmartContentPickerField")
               .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                    .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                    .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                    .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                       .WithDescription("Select a Event.")
               .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group.")
               .WithField("CoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Cover Image"))
               .WithField("AutoDownload", fieldBuilder => fieldBuilder.OfType("BooleanField")
                        .WithDisplayName("Auto Download")
                        .WithSetting("BooleanFieldSettings.SelectionMode", "Dropdown"))
            );

            ContentDefinitionManager.AlterPartDefinition("VectorImagePart", part => part
               .WithField("DocumentLayout", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("Document Layout")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "DocumentLayout"))
                   .WithDescription("Document Layout.")
               .WithField("EventPicker", cfg => cfg    // add event picker for media module
               .OfType("SmartContentPickerField")
               .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                   .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                   .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                   .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                   .WithDescription("Select a Event.")
               .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group.")
                .WithField("CoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Cover Image"))
               .WithField("AutoDownload", fieldBuilder => fieldBuilder.OfType("BooleanField")
                        .WithDisplayName("Auto Download")
                        .WithSetting("BooleanFieldSettings.SelectionMode", "Dropdown"))
            );

            ContentDefinitionManager.AlterPartDefinition("VideoPart", part => part
               .WithField("DocumentLayout", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("Document Layout")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "DocumentLayout"))
                   .WithDescription("Document Layout.")
               .WithField("EventPicker", cfg => cfg    // add event picker for media module
               .OfType("SmartContentPickerField")
               .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                    .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                    .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                    .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                    .WithDescription("Select a Event.")
              .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group.")
                .WithField("CoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Cover Image"))
               .WithField("AutoDownload", fieldBuilder => fieldBuilder.OfType("BooleanField")
                        .WithDisplayName("Auto Download")
                        .WithSetting("BooleanFieldSettings.SelectionMode", "Dropdown"))
            );

            ContentDefinitionManager.AlterPartDefinition("AudioPart", part => part
               .WithField("DocumentLayout", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("Document Layout")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "DocumentLayout"))
                   .WithDescription("Document Layout.")
               .WithField("EventPicker", cfg => cfg    // add event picker for media module
               .OfType("SmartContentPickerField")
               .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                    .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                    .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                    .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                    .WithDescription("Select a Event.")
              .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group.")
                .WithField("CoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Cover Image"))
               .WithField("AutoDownload", fieldBuilder => fieldBuilder.OfType("BooleanField")
                        .WithDisplayName("Auto Download")
                        .WithSetting("BooleanFieldSettings.SelectionMode", "Dropdown"))
            );

            ContentDefinitionManager.AlterPartDefinition("DocumentPart", part => part
               .WithField("DocumentLayout", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("Document Layout")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "DocumentLayout"))
                   .WithDescription("Document Layout.")
               .WithField("EventPicker", cfg => cfg    // add event picker for media module
               .OfType("SmartContentPickerField")
               .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                    .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                    .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                    .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                    .WithDescription("Select a Event.")
               .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group.")
                .WithField("CoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Cover Image"))
               .WithField("AutoDownload", fieldBuilder => fieldBuilder.OfType("BooleanField")
                        .WithDisplayName("Auto Download")
                        .WithSetting("BooleanFieldSettings.SelectionMode", "Dropdown"))
            );

            ContentDefinitionManager.AlterPartDefinition("OEmbedPart", part => part
                .WithField("DocumentLayout", cfg => cfg
                .OfType("TaxonomyField")
                .WithDisplayName("Document Layout")
                .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                    .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                    .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                    .WithSetting("TaxonomyFieldSettings.Required", "true")
                    .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                    .WithSetting("TaxonomyFieldSettings.Taxonomy", "DocumentLayout"))
                    .WithDescription("Document Layout.")
               .WithField("EventPicker", cfg => cfg    // add event picker for media module
               .OfType("SmartContentPickerField")
               .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                    .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                    .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                    .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                    .WithDescription("Select a Event.")
               .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "true")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group.")
                .WithField("CoverImage",
                         fieldBuilder => fieldBuilder
                             .OfType("MediaLibraryPickerField")
                             .WithDisplayName("Cover Image"))
               .WithField("AutoDownload", fieldBuilder => fieldBuilder.OfType("BooleanField")
                        .WithDisplayName("Auto Download")
                        .WithSetting("BooleanFieldSettings.SelectionMode", "Dropdown"))
            );



            ContentDefinitionManager.AlterTypeDefinition("Image", td => td
                .Listable()
            );

            ContentDefinitionManager.AlterTypeDefinition("VectorImage", td => td
                .Listable()
            );

            ContentDefinitionManager.AlterTypeDefinition("Video", td => td
                .Listable()
            );

            ContentDefinitionManager.AlterTypeDefinition("Audio", td => td
                .Listable()
            );

            ContentDefinitionManager.AlterTypeDefinition("Document", td => td
                .Listable()
            );

            ContentDefinitionManager.AlterTypeDefinition("OEmbed", td => td
               .Listable()
           );


            //alter content type for DocumentGroup with image field
            ContentDefinitionManager.AlterPartDefinition("TermPart",
                  builder => builder
                      .WithField("CoverImage",
                          fieldBuilder => fieldBuilder
                              .OfType("MediaLibraryPickerField")
                              .WithDisplayName("Cover Image")));
            #endregion

            return 1;
        }

        public int UpdateFrom1()
        {

            ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                  .WithField("SessionType", cfg => cfg
                  .OfType("TaxonomyField")
                  .WithDisplayName("Session Type")
                  .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                      .WithSetting("TaxonomyFieldSettings.SingleChoice", "true")
                      .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                      .WithSetting("TaxonomyFieldSettings.Required", "false")
                      .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                      .WithSetting("TaxonomyFieldSettings.Taxonomy", "SessionType"))
                      .WithDescription("Type."));

            ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                  .WithField("SessionCategory", cfg => cfg
                  .OfType("TaxonomyField")
                  .WithDisplayName("Session Category")
                  .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                      .WithSetting("TaxonomyFieldSettings.SingleChoice", "true")
                      .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                      .WithSetting("TaxonomyFieldSettings.Required", "false")
                      .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                      .WithSetting("TaxonomyFieldSettings.Taxonomy", "SessionCategory"))
                      .WithDescription("Session Category."));

            ContentDefinitionManager.AlterPartDefinition("EventPart", part => part
                 .WithField("ADGroup", cfg => cfg
                  .OfType("TaxonomyField")
                  .WithDisplayName("AD Group")
                  .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                      .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                      .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                      .WithSetting("TaxonomyFieldSettings.Required", "true")
                      .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                      .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                      .WithDescription("AD Group."));


            #region media
            ContentDefinitionManager.AlterPartDefinition("ImagePart", part => part
                .WithField("ADGroup", cfg => cfg
                .OfType("TaxonomyField")
                .WithDisplayName("AD Group")
                .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                    .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                    .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                    .WithSetting("TaxonomyFieldSettings.Required", "true")
                    .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                    .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                    .WithDescription("AD Group."));

            ContentDefinitionManager.AlterPartDefinition("VectorImagePart", part => part
               .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group."));

            ContentDefinitionManager.AlterPartDefinition("VideoPart", part => part
              .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group."));

            ContentDefinitionManager.AlterPartDefinition("AudioPart", part => part
              .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group."));

            ContentDefinitionManager.AlterPartDefinition("DocumentPart", part => part

               .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group.")
                );

            ContentDefinitionManager.AlterPartDefinition("OEmbedPart", part => part
               .WithField("ADGroup", cfg => cfg
               .OfType("TaxonomyField")
               .WithDisplayName("AD Group")
               .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                   .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                   .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                   .WithSetting("TaxonomyFieldSettings.Required", "true")
                   .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                   .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                   .WithDescription("AD Group.")
                );
            #endregion

            return 2;
        }

        public int UpdateFrom2()
        {
            ContentDefinitionManager.AlterPartDefinition("SessionPart", part => part
                   .WithField("ADGroup", cfg => cfg
                   .OfType("TaxonomyField")
                   .WithDisplayName("AD Group")
                   .WithSetting("TaxonomyFieldSettings.AllowCustomTerms", "false")
                       .WithSetting("TaxonomyFieldSettings.SingleChoice", "false")
                       .WithSetting("TaxonomyFieldSettings.LeavesOnly", "true")
                       .WithSetting("TaxonomyFieldSettings.Required", "true")
                       .WithSetting("TaxonomyFieldSettings.Autocomplete", "false")
                       .WithSetting("TaxonomyFieldSettings.Taxonomy", "ADGroup"))
                       .WithDescription("AD Group."));
            return 3;
        }

        public int UpdateFrom3()
        {
            ContentDefinitionManager.AlterTypeDefinition("Evaluation", cfg => cfg
                .WithPart("TitlePart")
                );
            ContentDefinitionManager.AlterTypeDefinition("Poll", cfg => cfg
                .WithPart("TitlePart")
                );
            ContentDefinitionManager.AlterPartDefinition("EvaluationPart", partBuilder => partBuilder
                .RemoveField("Title"));
            ContentDefinitionManager.AlterPartDefinition("PollPart", partBuilder => partBuilder
                .RemoveField("Title")
                .WithField("AllowAnonymousUser", fieldBuilder => fieldBuilder.OfType("TextField")
                     .WithDisplayName("Max Count of Allowed Anonymous Users")
                     .WithSetting("TextFieldSettings.Required", "false")
                     .WithSetting("TextFieldSettings.Flavor", "Wide")));
            return 4;
        }

        public int UpdateFrom4()
        {
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
                .AlterColumn("CareerLevel", c => c.WithType(DbType.String)));
            return 5;
        }
        public int UpdateFrom5()
        {
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AddColumn<string>("ActiveProjects"));
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AddColumn<string>("CurrentClient"));
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AddColumn<string>("OrgLevel2Desc"));
            return 6;
        }
        public int UpdateFrom6()
        {
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AddColumn<string>("EventIds"));
            return 7;
        }
        public int UpdateFrom7()
        {
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AddColumn<string>("ParticipantLayoutFullPath"));
            return 8;
        }
        public int UpdateFrom8()
        {
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AddColumn<string>("ProfessionalBio"));
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AddColumn<string>("MediaUrl"));
            return 9;
        }
        public int UpdateFrom9()
        {
            ContentDefinitionManager.AlterPartDefinition("ParticipantPart",
                   builder => builder
                       .WithField("ParticipantAvatar",
                           fieldBuilder => fieldBuilder
                               .OfType("MediaLibraryPickerField")
                               .WithDisplayName("Avatar")));
            return 10;
        }
        public int UpdateFrom10()
        {
            ContentDefinitionManager.AlterPartDefinition("Participant", partBuilder => partBuilder
            .RemoveField("ParticipantAvatar"));
            return 11;
        }
        public int UpdateFrom11()
        {
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AlterColumn("ProfessionalBio", column => column.WithType(DbType.String).Unlimited()));
            return 12;
        }

        public int UpdateFrom12()
        {
            SchemaBuilder.AlterTable("EventPartRecord", table => table
               .AddColumn<string>("EventTitle"));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
               .AddColumn<string>("AppPickerIds"));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
               .AddColumn<string>("ADGroups"));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
               .AddColumn<string>("CoverImageUrl"));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
               .AddColumn<string>("SkincssUrl"));

            SchemaBuilder.AlterTable("EventPartRecord", table => table
              .AlterColumn("ADGroups", column => column.WithType(DbType.String).Unlimited()));

            return 13;
        }
        public int UpdateFrom13()
        {
            SchemaBuilder.AlterTable("EventPartRecord", table => table
              .AddColumn<string>("ParticipantLayoutFullPath"));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
               .AddColumn<string>("DocumentLayoutFullPath"));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
               .AddColumn<string>("ContactPickerIds"));

            SchemaBuilder.AlterTable("EventPartRecord", table => table
              .AlterColumn("ParticipantLayoutFullPath", column => column.WithType(DbType.String).Unlimited()));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
              .AlterColumn("DocumentLayoutFullPath", column => column.WithType(DbType.String).Unlimited()));

            return 14;
        }

        public int UpdateFrom14()
        {
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaTitle"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaStartTime"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaEndTime"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaType"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaCategory"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaADGroups"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaPresenterPickerIds"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaEventPickerIds"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<string>("AgendaFullDescription"));

            SchemaBuilder.AlterTable("SessionPartRecord", table => table
               .AlterColumn("AgendaADGroups", column => column.WithType(DbType.String).Unlimited()));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
               .AlterColumn("AgendaFullDescription", column => column.WithType(DbType.String).Unlimited()));
            return 15;
        }

        public int UpdateFrom15()
        {
            ContentDefinitionManager.AlterPartDefinition("SessionPart", partBuilder => partBuilder
               .RemoveField("PresenterPicker"));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
              .AddColumn<string>("VideoCoverImageUrl"));
            ContentDefinitionManager.AlterPartDefinition("EventPart", partBuilder => partBuilder
               .RemoveField("ContactPicker"));

            return 16;
        }
        public int UpdateFrom16()
        {
            SchemaBuilder.AlterTable("EventPartRecord", table => table
             .AddColumn<bool>("EventIsPublished"));
            SchemaBuilder.AlterTable("EventPartRecord", table => table
              .AddColumn<bool>("EventIsLatest"));

            return 17;
        }

        public int UpdateFrom17()
        {
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
               .AlterColumn("ActiveProjects", column => column.WithType(DbType.String).Unlimited()));
            return 18;
        }

        public int UpdateFrom18()
        {
            SchemaBuilder.AlterTable("EventPartRecord", table => table
               .AlterColumn("IntroduceVideoPlayer", column => column.WithType(DbType.String).Unlimited()));

            return 19;
        }

        public int UpdateFrom19()
        {
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
             .AddColumn<bool>("ParticipantIsPublished"));
            SchemaBuilder.AlterTable("ParticipantPartRecord", table => table
              .AddColumn<bool>("ParticipantIsLatest"));

            return 20;
        }

        public int UpdateFrom20()
        {
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
             .AddColumn<bool>("SessionIsPublished"));
            SchemaBuilder.AlterTable("SessionPartRecord", table => table
              .AddColumn<bool>("SessionIsLatest"));

            return 21;
        }

        public int UpdateFrom21()
        {
            ContentDefinitionManager.AlterTypeDefinition("App", cfg => cfg
                 .WithPart("App")
                 .WithPart("CommonPart")
                 .WithPart("TitlePart")
                 .WithPart("AutoroutePart", builder => builder
                     .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                     .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                     .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: '{Content.Slug}', Description: 'some-Event'}]")
                     .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                 .WithPart("AppPart")
                 .WithPart("BodyPart")
                 .WithPart("TagsPart")
                 .WithPart("ContentPermissionsPart")
                 .Draftable()
                 .Creatable(false)
                 .Listable()
                 .Indexed());

            return 22;
        }

        private void CreateTaxonomy(string taxonomyName, string[] terms)
        {
            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);

            if (taxonomy == null)
            {
                try
                {
                    taxonomy = _orchardServices.ContentManager.New("Taxonomy").As<TaxonomyPart>();
                    taxonomy.Name = taxonomyName;
                    taxonomy.ContentItem.As<TitlePart>().Title = taxonomyName;
                    _taxonomyService.CreateTermContentType(taxonomy);
                    _orchardServices.ContentManager.Create(taxonomy);
                    _orchardServices.ContentManager.Publish(taxonomy.ContentItem);

                    foreach (var term in terms)
                    {
                        CreateTerm(taxonomy, term);
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.Error("Error occurs when create terms with the migration :" + ex.Message);
                }
            }
        }

        private void CreateTerm(TaxonomyPart tax, string termName)
        {
            var term = _taxonomyService.NewTerm(tax);
            term.Name = termName;
            term.Selectable = true;
            _contentManager.Create(term, VersionOptions.Published);
        }

    }
}