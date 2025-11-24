using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;

namespace Stock.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        
        public IActionResult Index()
        {
            
            if (User.Identity.IsAuthenticated)
            {
                
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IniciarSesion(string email, string contraseña)
        {
            var usuario = VerificarUsuario(email, contraseña);

            if (usuario != null)
            {
                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.TipoUsuario)
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

               
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ErrorMessage"] = "Usuario o contraseña incorrectos.";
            return View("Index");
        }

        
        private IActionResult RedireccionarSegunRol(string tipoUsuario, int idProveedor)
        {
            if (tipoUsuario == "Proveedor")
                return RedirectToAction("Index", "Proveedor");
            else if (tipoUsuario == "Administrativo")
                return RedirectToAction("DashboardAdministrativo", "Dashboard");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CerrarSesion()
        {
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }

       
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Usuario nuevoUsuario)
        {
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
                    
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Contraseña", contraseña);

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