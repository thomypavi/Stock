using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;
using System.Security.Claims; // <--- NECESARIO
using Microsoft.AspNetCore.Authentication; // <--- NECESARIO (Para SignIn/SignOut)
using Microsoft.AspNetCore.Authentication.Cookies; // <--- NECESARIO (Para CookieAuthenticationDefaults)

namespace Stock.Controllers
{
    // Hacemos el controlador 'async' para poder usar await en las acciones de autenticación
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // Si el usuario ya está autenticado, redirige automáticamente
            if (User.Identity.IsAuthenticated)
            {
                // Podrías redirigir a un dashboard genérico o verificar el rol aquí
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IniciarSesion(string email, string contraseña) // <--- CAMBIO CRUCIAL: AGREGADO async y Task
        {
            var usuario = VerificarUsuario(email, contraseña);

            if (usuario != null)
            {
                // ***************************************************************
                // 1. CREAR LA IDENTIDAD Y LA COOKIE DE SESIÓN
                // ***************************************************************
                var claims = new List<Claim>
                {
                    // CRUCIAL: Almacena el ID que se usará en ProveedorController (NameIdentifier)
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.TipoUsuario) // Almacena el rol si lo necesitas
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                // Firma la cookie de autenticación e inicia la sesión
                await HttpContext.SignInAsync( // <--- CAMBIO CRUCIAL: AGREGADO await
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));
                // ***************************************************************

                // Redirección basada en el tipo de usuario
                if (usuario.TipoUsuario == "Proveedor")
                {
                    // En el _Layout, usamos ProveedorController, así que vamos a su acción principal
                    return RedirectToAction("OrdenesRecibidas", "Proveedor");
                }
                else if (usuario.TipoUsuario == "Administrativo")
                {
                    return RedirectToAction("DashboardAdministrativo", "Dashboard");
                }
            }

            ViewData["ErrorMessage"] = "Usuario o contraseña incorrectos.";
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CerrarSesion() // <--- NECESARIO PARA EL BOTÓN DE LOGOUT
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }

        // --- Resto del código (Register y VerificarUsuario) permanece igual ---

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Usuario nuevoUsuario)
        {
            // ... (código de registro) ...
            string? connectionString = _configuration.GetConnectionString("MiConexion")
                 ?? throw new Exception("Cadena de conexión no encontrada");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                string query = @"INSERT INTO Usuarios (Email, Contraseña, TipoUsuario)
                                 VALUES (@Email, @Contraseña, @TipoUsuario)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = nuevoUsuario.Email;
                    cmd.Parameters.Add("@Contraseña", SqlDbType.VarChar).Value = nuevoUsuario.Contraseña;
                    cmd.Parameters.Add("@TipoUsuario", SqlDbType.VarChar).Value = nuevoUsuario.TipoUsuario;

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index", "Login");
        }

        private Usuario? VerificarUsuario(string email, string contraseña)
        {
            Usuario? usuario = null;

            string? connectionString = _configuration.GetConnectionString("MiConexion")
                 ?? throw new Exception("Cadena de conexión no encontrada");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                string query = @"SELECT Id, Email, Contraseña, TipoUsuario 
                                 FROM Usuarios 
                                 WHERE Email = @Email AND Contraseña = @Contraseña";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = email;
                    cmd.Parameters.Add("@Contraseña", SqlDbType.VarChar).Value = contraseña;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario = new Usuario
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Email = reader["Email"].ToString() ?? "",
                                Contraseña = reader["Contraseña"].ToString() ?? "",
                                TipoUsuario = reader["TipoUsuario"].ToString() ?? ""
                            };
                        }
                    }
                }
            }

            return usuario;
        }
    }
}