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
    public class CircleMigrations : DataMigrationImpl
    {
       
        public CircleMigrations()
        {
        }

        public int Create()
        {
            SchemaBuilder.CreateTable("CirclePartRecord", table => table
                .ContentPartRecord()
                .Column("Title", DbType.String)
                .Column("CircleId", DbType.String)
                .Column("CircleGUID", DbType.String)
                .Column("EventPickerIds", DbType.String)
            );

            ContentDefinitionManager.AlterPartDefinition("CirclePart",
                  builder => builder.Attachable());

            ContentDefinitionManager.AlterTypeDefinition("Circle", cfg => cfg
              .WithPart("Circle")
              .WithPart("CommonPart")
              .WithPart("CirclePart")
              .WithPart("TitlePart")
              .Creatable()
              .Listable()
              .Indexed());

            ContentDefinitionManager.AlterPartDefinition("CirclePart", part => part
                   .WithField("EventPicker", cfg => cfg  
                   .OfType("SmartContentPickerField")
                   .WithSetting("SmartContentPickerFieldSettings.Multiple", "false")
                       .WithSetting("SmartContentPickerFieldSettings.Hint", "Select a Event.")
                       .WithSetting("SmartContentPickerFieldSettings.Required", "true")
                       .WithSetting("SmartContentPickerFieldSettings.DisplayedContentTypes", "Event"))
                       .WithDescription("Select a Event."));
            return 1;
        }
        public int UpdateFrom1()
        {

            ContentDefinitionManager.AlterPartDefinition("CirclePart", partBuilder => partBuilder
                   .WithField("WorkCircleId", fieldBuilder => fieldBuilder.OfType("TextField")
                       .WithDisplayName("Circle Id")
                       .WithSetting("TextFieldSettings.Required", "false")
                       .WithSetting("TextFieldSettings.Flavor", "Wide"))
                  .WithField("WorkCircleGUID", fieldBuilder => fieldBuilder.OfType("TextField")
                       .WithDisplayName("Circle GUID")
                       .WithSetting("TextFieldSettings.Required", "false")
                       .WithSetting("TextFieldSettings.Flavor", "Wide")));

            return 2;
        }
        public int UpdateFrom2()
        {

            ContentDefinitionManager.AlterPartDefinition("CirclePart", part => part
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
            SchemaBuilder.AlterTable("CirclePartRecord", table => table
              .AddColumn<string>("AdGroups"));

            SchemaBuilder.AlterTable("CirclePartRecord", table => table
               .AlterColumn("AdGroups", column => column.WithType(DbType.String).Unlimited()));

            return 4;
        }

        public int UpdateFrom4()
        { 
            SchemaBuilder.AlterTable("CirclePartRecord", table => table
              .DropColumn("CircleId"));
            SchemaBuilder.AlterTable("CirclePartRecord", table => table
             .DropColumn("CircleGUID"));

            SchemaBuilder.AlterTable("CirclePartRecord", table => table
              .AddColumn<string>("AnotherCircleId"));
            SchemaBuilder.AlterTable("CirclePartRecord", table => table
              .AddColumn<string>("AnotherCircleGUID"));

            return 5;
        }

        public int UpdateFrom5()
        {
            SchemaBuilder.AlterTable("CirclePartRecord", table => table
            .AddColumn<bool>("CircleIsPublished"));
            SchemaBuilder.AlterTable("CirclePartRecord", table => table
              .AddColumn<bool>("CircleIsLatest"));

            return 6;
        }
    }
}