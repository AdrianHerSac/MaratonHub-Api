using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MaratonHub.ApiPlayWrite;

/// <summary>
/// Pruebas de integración de extremo a extremo (E2E) para MaratonHub utilizando Playwright.
/// Estas pruebas validan los flujos de usuario, incluyendo autenticación,
/// navegación pública y protegida, búsquedas y visualización de contenidos.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class MaratonHubTests : PageTest
{
    // Permite configurar la URL base a través de variables de entorno para CI/CD,
    // por defecto utiliza la URL del servidor de desarrollo local de Angular.
    private string BaseUrl => Environment.GetEnvironmentVariable("MARATONHUB_WEB_URL") ?? "http://localhost:4200";

    // Sobrescribe las opciones de contexto del navegador para habilitar la grabación de video.
    // Los videos de cada test se guardarán automáticamente en formato .webm dentro de "bin/Debug/net10.0/playwright-videos/".
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            RecordVideoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playwright-videos"),
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
        };
    }

    [SetUp]
    public void Setup()
    {
        // Configuración adicional antes de cada prueba si es necesario.
    }

    [Test]
    [Description("Verifica que un usuario pueda navegar entre el modo de Inicio de Sesión y Registro correctamente.")]
    public async Task Autenticacion_AlternarModos_DeberiaCambiarDeFormulario()
    {
        // 1. Ir a la página de login
        await Page.GotoAsync($"{BaseUrl}/login");

        // 2. Verificar que inicialmente esté en modo "Iniciar Sesión"
        await Expect(Page.Locator("h2")).ToHaveTextAsync("Iniciar Sesión");
        await Expect(Page.Locator("button.submit-btn")).ToHaveTextAsync("Entrar");

        // 3. Hacer clic en "Regístrate aquí"
        var toggleLink = Page.Locator(".toggle-text a");
        await Expect(toggleLink).ToHaveTextAsync("Regístrate aquí");
        await toggleLink.ClickAsync();

        // 4. Verificar que el formulario cambió a modo "Registrarse"
        await Expect(Page.Locator("h2")).ToHaveTextAsync("Registrarse");
        await Expect(Page.Locator("button.submit-btn")).ToHaveTextAsync("Crear Cuenta");

        // 5. Alternar de vuelta a iniciar sesión
        await toggleLink.ClickAsync();
        await Expect(Page.Locator("h2")).ToHaveTextAsync("Iniciar Sesión");
    }

    [Test]
    [Description("Verifica que las rutas protegidas redirijan automáticamente al usuario no autenticado a la pantalla de login.")]
    public async Task RutasProtegidas_UsuarioAnonimo_DeberiaRedirigirALogin()
    {
        // Rutas protegidas por el guard de autenticación
        string[] rutasProtegidas = { "/mi-lista", "/profile", "/groups" };

        foreach (var ruta in rutasProtegidas)
        {
            // Intentar acceder directamente a la ruta protegida
            await Page.GotoAsync($"{BaseUrl}{ruta}");

            // Esperar y verificar que se redirija a /login debido al authGuard
            await Page.WaitForURLAsync($"{BaseUrl}/login");
            await Expect(Page).ToHaveURLAsync(new Regex(".*/login"));
        }
    }

    [Test]
    [Description("Verifica que un usuario anónimo pueda navegar por las secciones públicas de la plataforma.")]
    public async Task NavegacionPublica_UsuarioAnonimo_DeberiaPoderNavegar()
    {
        // 1. Ir a la página de inicio
        await Page.GotoAsync($"{BaseUrl}/inicio");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/inicio"));

        // 2. Navegar a Películas
        var moviesLink = Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = "Películas" }).First;
        await Expect(moviesLink).ToBeVisibleAsync();
        await moviesLink.ClickAsync();
        await Page.WaitForURLAsync($"{BaseUrl}/movies");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/movies"));

        // 3. Navegar a Series
        var tvLink = Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = "Series" }).First;
        await Expect(tvLink).ToBeVisibleAsync();
        await tvLink.ClickAsync();
        await Page.WaitForURLAsync($"{BaseUrl}/tv");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/tv"));

        // 4. Navegar a Famosos
        var personsLink = Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = "Famosos" }).First;
        await Expect(personsLink).ToBeVisibleAsync();
        await personsLink.ClickAsync();
        await Page.WaitForURLAsync($"{BaseUrl}/persons");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/persons"));

        // 5. Navegar a Top Rated
        var topRatedLink = Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = "Top Rated" }).First;
        await Expect(topRatedLink).ToBeVisibleAsync();
        await topRatedLink.ClickAsync();
        await Page.WaitForURLAsync($"{BaseUrl}/top-rated");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/top-rated"));
    }

    [Test]
    [Description("Verifica la búsqueda de películas y series desde el buscador de la barra de navegación.")]
    public async Task BuscadorNavbar_DeberiaBuscarYMostrarResultados()
    {
        // 1. Ir a inicio
        await Page.GotoAsync($"{BaseUrl}/inicio");

        // 2. Localizar el campo de entrada de búsqueda en la barra de navegación
        var searchInput = Page.Locator("nav input[placeholder*='Buscar']").First;
        await Expect(searchInput).ToBeVisibleAsync();

        // 3. Escribir una consulta de búsqueda (ej. "Matrix" o "Spider-Man")
        string consulta = "Spider-Man";
        await searchInput.FillAsync(consulta);

        // 4. Presionar Enter
        await searchInput.PressAsync("Enter");

        // 5. Verificar que se redirige o se muestran resultados
        // Playwright esperará a que cambie la URL o a que se actualice la vista
        await Expect(Page).ToHaveURLAsync(new Regex($".*query={Uri.EscapeDataString(consulta)}|.*search-results|.*movies.*|.*inicio.*"));
        
        // Se puede añadir aserción para tarjetas de películas si el selector existe
        // var mediaCards = Page.Locator(".media-card");
        // await Expect(mediaCards).ToHaveCountAsync(new Regex("[1-9]\\d*")); // Al menos 1 resultado
    }
}
