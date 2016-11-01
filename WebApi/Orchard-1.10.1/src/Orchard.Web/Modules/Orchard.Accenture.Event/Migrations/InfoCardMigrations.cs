using System;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using Orchard.Core.Contents.Extensions;
using Orchard.Indexing;
using System.Data;

namespace Orchard.Accenture.Event.Migrations
{
    public class InfoCardMigrations : DataMigrationImpl
    {
        private readonly IContentManager _contentManager;

        public InfoCardMigrations(IContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public int Create()
        {
            SchemaBuilder.CreateTable("InfoCardPartRecord", table => table
                .ContentPartRecord()
                .Column("HotelName", DbType.String)
                .Column("HotelAddress", DbType.String)
                .Column("WebSite", DbType.String)
                .Column("Telphone", DbType.String)
                .Column("ExtNumber", DbType.String)
            );

            ContentDefinitionManager.AlterPartDefinition("InfoCardPart",
                  builder => builder.Attachable());

            ContentDefinitionManager.AlterTypeDefinition("InfoCard", cfg => cfg
              .WithPart("InfoCard")
              .WithPart("CommonPart")
              .WithPart("InfoCardPart")
              .WithPart("TitlePart")
              .Creatable()
              .Listable()
              .Indexed());

            ContentDefinitionManager.AlterPartDefinition("InfoCardPart", part => part
                   .WithField("StartDate", cfg => cfg
                   .OfType("DateTimeField")
                   .WithDisplayName("Start Date")
                   .WithSetting("DateTimeFieldSettings.Display", "DateOnly")
                       .WithSetting("DateTimeFieldSettings.Required", "false")));

            ContentDefinitionManager.AlterPartDefinition("InfoCardPart", part => part
               .WithField("EndDate", cfg => cfg
               .OfType("DateTimeField")
               .WithDisplayName("End Date")
               .WithSetting("DateTimeFieldSettings.Display", "DateOnly")
                   .WithSetting("DateTimeFieldSettings.Required", "false")));

            ContentDefinitionManager.AlterPartDefinition("InfoCardPart",
                   builder => builder
                       .WithField("CoverImage",
                           fieldBuilder => fieldBuilder
                               .OfType("MediaLibraryPickerField")
                               .WithDisplayName("Cover Image")));

            ContentDefinitionManager.AlterPartDefinition("InfoCardPart", part => part
                   .WithField("EventPicker", cfg => cfg    // add event picker for infocard module
                   .OfType("SmartContentPickerField")
                   .WithSetting("SmartContentPickerFieldSettings.Multiple", "true")
                       .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                       .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                       .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                       .WithDescription("Select a Event."));
            return 1;
        }

        public int UpdateFrom1()
        {

            SchemaBuilder.AlterTable("InfoCardPartRecord", table => table
             .AddColumn<string>("Title"));
            SchemaBuilder.AlterTable("InfoCardPartRecord", table => table
             .AddColumn<string>("CardStartDate"));
            SchemaBuilder.AlterTable("InfoCardPartRecord", table => table
             .AddColumn<string>("CardEndDate"));
            SchemaBuilder.AlterTable("InfoCardPartRecord", table => table
             .AddColumn<string>("CardCoverImageUrl"));
            SchemaBuilder.AlterTable("InfoCardPartRecord", table => table
             .AddColumn<string>("EventPickerIds"));

            return 2;
        }

        public int UpdateFrom2()
        {

            SchemaBuilder.AlterTable("InfoCardPartRecord", table => table
            .AddColumn<bool>("InfoCardIsPublished"));
            SchemaBuilder.AlterTable("InfoCardPartRecord", table => table
              .AddColumn<bool>("InfoCardIsLatest"));

            return 3;
        }
    }
}