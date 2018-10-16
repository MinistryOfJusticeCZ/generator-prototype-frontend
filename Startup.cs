using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MSPGeneratorWeb.Startup))]
namespace MSPGeneratorWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
