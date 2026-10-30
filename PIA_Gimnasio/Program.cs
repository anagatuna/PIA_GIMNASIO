using Microsoft.AspNetCore.Authentication.Cookies;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// Clases que se inyectan en las páginas
builder.Services.AddScoped<bdConexion>();
builder.Services.AddScoped<logicaUsuario>();
builder.Services.AddScoped<logicaEjercicio>();
builder.Services.AddScoped<logicaRutina>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccesoDenegado";   // Si el rol no tiene permiso para la página
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Permisos por rol: cada página revisa el catálogo de Seguridad/Modulos.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PorModulo", politica => politica
        .RequireAuthenticatedUser()
        .RequireAssertion(contexto =>
            contexto.Resource is HttpContext http
            && Modulos.PuedeEntrar(contexto.User, http.GetRouteValue("page")?.ToString())));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Inicio", "");   // La raíz del sitio abre el panel de inicio
    options.Conventions.AuthorizeFolder("/", "PorModulo");
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