using System.Collections.Generic;
using Orchard.Environment.Extensions.Models;
using Orchard.Security.Permissions;

namespace Orchard.Test
{
    public class Permissions : IPermissionProvider
    {

        public virtual Feature Feature { get; set; }

        public IEnumerable<Permission> GetPermissions()
        {
            return new Permission[] {
            };
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[] {
                new PermissionStereotype {
                    Name = "Administrator",
                    Permissions = new Permission[] {
                    }
                },
                new PermissionStereotype {
                    Name = "Editor",
                    Permissions = new Permission[] {
                    }
                },
                new PermissionStereotype {
                    Name = "Moderator",
                },
                new PermissionStereotype {
                    Name = "Author",
                    Permissions = new Permission[] {
                    }
                },
                new PermissionStereotype {
                    Name = "Contributor",
                    Permissions = new Permission[] {
                    }
                },
            };
        }
    }
}

