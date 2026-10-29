using Microsoft.AspNetCore.Authentication.Cookies;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Logica;

var builder = WebApplication.CreateBuilder(args);

// Clases que se inyectan en las páginas
builder.Services.AddScoped<bdConexion>();
builder.Services.AddScoped<logicaUsuario>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Index";   // Si el rol no tiene permiso, regresa al inicio
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();