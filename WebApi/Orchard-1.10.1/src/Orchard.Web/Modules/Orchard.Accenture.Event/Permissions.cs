using System.Collections.Generic;
using Orchard.Environment.Extensions.Models;
using Orchard.Security.Permissions;

namespace Orchard.Accenture.Event
{
    public class Permissions : IPermissionProvider
    {
        public static readonly Permission ManageEvent = new Permission { Description = "Manage Event", Name = "ManageEvent" };
        public static readonly Permission ManageApp = new Permission { Description = "Manage App", Name = "ManageApp" };
        public static readonly Permission ManageParticipant = new Permission { Description = "Manage Participant", Name = "ManageParticipant" };
        public static readonly Permission ManageSession = new Permission { Description = "Manage Session", Name = "ManageSession" };
        public static readonly Permission ManageInfoCard = new Permission { Description = "Manage InfoCard", Name = "ManageInfoCard" };
        public static readonly Permission ManagePoll = new Permission { Description = "Manage Poll", Name = "ManagePoll" };
        public static readonly Permission ManageEvaluation = new Permission { Description = "Manage Evaluation", Name = "ManageEvaluation" };
        public static readonly Permission ManageCircle = new Permission { Description = "Manage Circle", Name = "ManageCircle" };
        public static readonly Permission ManageTaxonomy = new Permission { Description = "Manage Taxonomy", Name = "ManageTaxonomy" };
        public static readonly Permission ImportParticipant = new Permission { Description = "Import Participant", Name = "ImportParticipant" };
        public static readonly Permission ImportSession = new Permission { Description = "Import Session", Name = "ImportSession" };
        public static readonly Permission ManageDelegation = new Permission { Description = "Manage Delegation", Name = "ManageDelegation" };
        public virtual Feature Feature { get; set; }

        public IEnumerable<Permission> GetPermissions()
        {
            return new Permission[] {
                  ManageEvent,
                  ManageApp,
                  ManageParticipant,
                  ManageSession,
                  ManageInfoCard,
                  ManagePoll,
                  ManageEvaluation,
                  ManageCircle,
                  ManageTaxonomy,
                  ImportParticipant,
                  ImportSession,
                  ManageDelegation

            };
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[] {
                new PermissionStereotype {
                    Name = "Administrator",
                    Permissions = new Permission[] {
                          ManageEvent,
                    }
                },
                new PermissionStereotype {
                    Name = "Editor",
                    Permissions = new Permission[] {
                          ManageEvent,
                    }
                },
                new PermissionStereotype {
                    Name = "Moderator",
                },
                new PermissionStereotype {
                    Name = "Author",
                    Permissions = new Permission[] {
                          ManageEvent,
                    }
                },
                new PermissionStereotype {
                    Name = "Contributor",
                    Permissions = new Permission[] {
                          ManageEvent,
                    }
                },
            };
        }
    }
}

