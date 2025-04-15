using System.Reflection;
using LibADO.Login;
using Microsoft.AspNetCore.Http;
using UserMPA.Pages;
var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton(connectionString);

builder.Services.AddRazorPages();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<LoginRepository>(provider =>
    new LoginRepositoryADO(connectionString));
builder.Services.AddScoped(provider => new RequisitarModel(provider.GetRequiredService<string>()));
builder.Services.AddScoped(provider => new RequisitionsModel(provider.GetRequiredService<string>()));
builder.Services.AddScoped(provider => new CancelarADModel(provider.GetRequiredService<string>()));
builder.Services.AddScoped(provider => new SearchModel(provider.GetRequiredService<string>()));


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LibDB.dll");
    return File.Exists(path) ? Assembly.LoadFile(path) : null;
};

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.Urls.Add("http://0.0.0.0:5100");


app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
