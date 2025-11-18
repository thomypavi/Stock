using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;

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
            return View();
        }

        [HttpPost]
        public IActionResult IniciarSesion(string email, string contraseña)
        {
            var usuario = VerificarUsuario(email, contraseña);

            if (usuario != null)
            {
                if (usuario.TipoUsuario == "Proveedor")
                    return RedirectToAction("Index", "Productos", new { idProveedor = usuario.Id });
                else if (usuario.TipoUsuario == "Administrativo")
                    return RedirectToAction("DashboardAdministrativo", "Dashboard");

            }

            ViewData["ErrorMessage"] = "Usuario o contraseña incorrectos.";
            return View("Index");
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
