using HelloClassLibrary;
using Microsoft.AspNet.Builder;

namespace HelloWeb
{
    public class Startup
    {
        [MyAspect]
        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseWelcomePage();
            var foo = new Foo();
            foo.Bar();
        }
    }
 
    
   
}