using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MyTested.AspNetCore.Mvc;

namespace Lexvor.Tests {
    public class TestStartup : Startup {
        public TestStartup(IHostingEnvironment env)
            : base(env) {
        }

        public void ConfigureTestServices(IServiceCollection services) {
            base.ConfigureServices(services);
        }
    }
}
