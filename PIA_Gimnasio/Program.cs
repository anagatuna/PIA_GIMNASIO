using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Logica;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Inicio", "");   // La raíz del sitio abre el inicio
});

// Sesión: guarda el usuario que inició sesión (ver Login.cshtml.cs)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Clases que se inyectan en las páginas
builder.Services.AddScoped<bdConexion>();
builder.Services.AddScoped<logicaUsuario>();
builder.Services.AddScoped<logicaCatalogo>();
builder.Services.AddScoped<logicaRutina>();
builder.Services.AddScoped<logicaSucursal>();
builder.Services.AddScoped<logicaEmpleado>();
builder.Services.AddScoped<logicaSocio>();
builder.Services.AddScoped<logicaExpediente>();
builder.Services.AddScoped<logicaSuscripcion>();
builder.Services.AddScoped<logicaControlAcceso>();
builder.Services.AddScoped<logicaClase>();
builder.Services.AddScoped<logicaReserva>();
builder.Services.AddScoped<logicaPaseLista>();
builder.Services.AddScoped<logicaReporte>();
builder.Services.AddScoped<logicaInicio>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapRazorPages();

app.Run();
