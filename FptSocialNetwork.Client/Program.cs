using FptSocialNetwork.Client.Services;

namespace FptSocialNetwork.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
            {
                var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7071/";
                client.BaseAddress = new Uri(apiBaseUrl);
            });
            builder.Services.AddScoped<IConversationApiService, ConversationApiService>();
            builder.Services.AddScoped<IMessageApiService, MessageApiService>();
            builder.Services.AddScoped<IAuthApiService, AuthApiService>();
            builder.Services.AddScoped<IUserApiService, UserApiService>();
            builder.Services.AddScoped<ICloudinaryUploadService, CloudinaryUploadService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();

            app.UseAuthorization();

            app.MapControllers();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
