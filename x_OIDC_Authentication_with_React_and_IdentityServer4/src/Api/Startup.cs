using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // accepts any access token issued by identity server
            services
                .AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    // URL of our identity server
                    options.Authority = "https://localhost:5001";
                    // HTTPS required for the authority (defaults to true but disabled for development).
                    options.RequireHttpsMetadata = false;
                    // the name of this API - note: matches the API resource name configured above
                    options.Audience = "doughnutapi";
                });

            services.AddMvc(options =>
                options.EnableEndpointRouting = false);
            
            //// adds an authorization policy to make sure the token is for scope 'api1'
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("ApiScope", policy =>
            //    {
            //        policy.RequireAuthenticatedUser();
            //        policy.RequireClaim("scope", "api1");
            //    });
            //});
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCors(builder =>
              builder
                .WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials() );

            app.UseAuthentication();
            app.UseMvc();

            //if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Doughnut API is running!");
            });



            //app.UseRouting();

            //app.UseAuthentication();
            //app.UseAuthorization();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers()
            //        .RequireAuthorization("ApiScope");
            //});
        }
    }
}
