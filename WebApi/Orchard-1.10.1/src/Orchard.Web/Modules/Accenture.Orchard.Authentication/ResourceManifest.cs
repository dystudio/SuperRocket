using Orchard.UI.Resources;

namespace Accenture.Orchard.Authentication
{
    public class ResourceManifest : IResourceManifestProvider
    {
        public void BuildManifests(ResourceManifestBuilder builder)
        {
            var manifest = builder.Add();
            manifest.DefineScript("PeopleLogin").SetUrl("PeopleLogin.js").SetDependencies("jQuery");
            manifest.DefineStyle("Authentication").SetUrl("Authentication.css");
        }
    }
}