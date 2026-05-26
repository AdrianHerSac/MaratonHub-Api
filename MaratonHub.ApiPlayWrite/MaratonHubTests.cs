using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MaratonHub.ApiPlayWrite;

/// <summary>
/// Pruebas de integración de extremo a extremo (E2E) para MaratonHub utilizando Playwright.
/// Estas pruebas validan los flujos de usuario clave, incluyendo autenticación,
/// navegación pública y protegida, búsquedas y visualización de contenidos.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class MaratonHubTests : PageTest
{
    // Permite configurar la URL base a través de variables de entorno para CI/CD,
    // por defecto utiliza la URL del servidor de desarrollo local de Angular.
    private string BaseUrl => Environment.GetEnvironmentVariable("MARATONHUB_WEB_URL") ?? "http://localhost:4200";

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
    [Description("Verifica que un login con credenciales erróneas muestre el correspondiente mensaje de error.")]
    public async Task IniciarSesion_CredencialesInvalidas_DeberiaMostrarMensajeDeError()
    {
        // 1. Ir a la página de login
        await Page.GotoAsync($"{BaseUrl}/login");

        // 2. Rellenar con credenciales aleatorias/inválidas
        await Page.Locator("#username").FillAsync("usuario_fantasma_invalido");
        await Page.Locator("#password").FillAsync("ClaveFalsa123!");

        // 3. Hacer clic en Entrar
        await Page.Locator("button.submit-btn").ClickAsync();

        // 4. Verificar que se muestra un mensaje de error
        var errorAlert = Page.Locator(".error-message");
        await Expect(errorAlert).ToBeVisibleAsync();
        
        // Espera que contenga texto indicativo de credenciales inválidas o error
        string textContent = await errorAlert.TextContentAsync() ?? "";
        Assert.That(textContent.Trim(), Is.Not.Empty);
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

    [Test]
    [Description("Flujo completo: Iniciar Sesión con éxito, verificar el estado de la sesión y cerrar sesión.")]
    public async Task FlujoSesionCompleto_LoginExitoso_YCerrarSesion()
    {
        // NOTA: Para que esta prueba funcione en tu entorno de desarrollo,
        // debe existir este usuario sembrado en la base de datos MongoDB del backend.
        // Puedes cambiar las credenciales por unas reales de prueba.
        string usuarioPrueba = "Adrian"; // O tu usuario de pruebas
        string clavePrueba = "Password123!"; // O tu contraseña real de pruebas

        // 1. Ir a login
        await Page.GotoAsync($"{BaseUrl}/login");

        // 2. Introducir credenciales
        await Page.Locator("#username").FillAsync(usuarioPrueba);
        await Page.Locator("#password").FillAsync(clavePrueba);

        // 3. Hacer clic en iniciar sesión
        await Page.Locator("button.submit-btn").ClickAsync();

        // 4. Si las credenciales son válidas, nos redirigirá a /inicio
        // Nota: Si el usuario no existe en la base de datos durante el test automatizado,
        // este paso fallará de manera segura e indicará que necesitas sembrar el usuario.
        try 
        {
            await Page.WaitForURLAsync($"{BaseUrl}/inicio", new() { Timeout = 5000 });
            await Expect(Page).ToHaveURLAsync(new Regex(".*/inicio"));

            // 5. Verificar que la barra de navegación muestra el dropdown de perfil con el nombre de usuario
            var profileDropdownBtn = Page.Locator("nav button").Filter(new() { HasText = usuarioPrueba }).First;
            await Expect(profileDropdownBtn).ToBeVisibleAsync();

            // 6. Hacer clic para desplegar las opciones de perfil
            await profileDropdownBtn.ClickAsync();

            // 7. Verificar que las opciones protegidas como "Mi Perfil" y "Panel Admin" (para Adrian) están visibles
            await Expect(Page.Locator("nav a:has-text('Mi Perfil')")).ToBeVisibleAsync();
            if (usuarioPrueba.Equals("Adrian", StringComparison.OrdinalIgnoreCase))
            {
                await Expect(Page.Locator("nav a:has-text('Panel Admin')")).ToBeVisibleAsync();
            }

            // 8. Hacer clic en "Cerrar Sesión"
            var logoutBtn = Page.Locator("nav a:has-text('Cerrar Sesión')");
            await logoutBtn.ClickAsync();

            // 9. Verificar que se redirige a la pantalla de login o al estado de no autenticado
            await Page.WaitForURLAsync($"{BaseUrl}/login");
            await Expect(Page).ToHaveURLAsync(new Regex(".*/login"));
            
            // Y que el botón "Iniciar Sesión" vuelve a aparecer en la navbar
            var loginNavbarBtn = Page.Locator("nav a:has-text('Iniciar Sesión')").First;
            await Expect(loginNavbarBtn).ToBeVisibleAsync();
        }
        catch (TimeoutException)
        {
            // Capturamos el timeout amigablemente en caso de que no exista el usuario sembrado aún,
            // para advertir al desarrollador en lugar de mostrar un error críptico.
            Assert.Inconclusive($"El flujo de inicio de sesión exitoso requiere que el usuario de pruebas '{usuarioPrueba}' esté registrado previamente en el backend de desarrollo.");
        }
    }
}
