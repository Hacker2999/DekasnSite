using DekasnSite.Models;
using Microsoft.Owin;
using Owin;


[assembly: OwinStartupAttribute(typeof(DekasnSite.Startup))]
namespace DekasnSite
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            ApplicationDbContext.Create().Database.Initialize(true);
        }
       
    }
}
