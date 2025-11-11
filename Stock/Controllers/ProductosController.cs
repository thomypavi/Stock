using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;

namespace Stock.Controllers
{
    public class ProductosController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProductosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 📦 LISTADO DE PRODUCTOS DE UN PROVEEDOR
        public IActionResult Index(int idProveedor)
        {
            List<Producto> productos = new List<Producto>();

            string? connectionString = _configuration.GetConnectionString("MiConexion");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT * FROM Productos WHERE IdProveedor = @IdProveedor";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            productos.Add(new Producto
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"].ToString() ?? "",
                                Descripcion = reader["Descripcion"].ToString() ?? "",
                                Precio = Convert.ToDecimal(reader["Precio"]),
                                IdProveedor = Convert.ToInt32(reader["IdProveedor"])
                            });
                        }
                    }
                }
            }

            ViewBag.IdProveedor = idProveedor;
            return View(productos);
        }

        // 🧾 FORMULARIO DE CREACIÓN DE PRODUCTO CON DESPLEGABLE DE PROVEEDORES
        [HttpGet]
        public IActionResult Crear()
        {
            // Obtener todos los proveedores (TipoUsuario = 'Proveedor')
            List<Usuario> proveedores = new List<Usuario>();

            string? connectionString = _configuration.GetConnectionString("MiConexion");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT Id, Email FROM Usuarios WHERE TipoUsuario = 'Proveedor'";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            proveedores.Add(new Usuario
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Email = reader["Email"].ToString() ?? ""
                            });
                        }
                    }
                }
            }

            // Pasar la lista a la vista
            ViewBag.Proveedores = proveedores;
            return View();
        }

        // 💾 GUARDAR PRODUCTO EN LA BASE DE DATOS
        [HttpPost]
        public IActionResult Crear(Producto producto)
        {
            try
            {
                Console.WriteLine("🟢 ID recibido: " + producto.IdProveedor);

                string? connectionString = _configuration.GetConnectionString("MiConexion");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 🔹 Aseguramos que el campo Cantidad también se guarde
                    string query = @"INSERT INTO Productos (Nombre, Descripcion, Precio, IdProveedor, Cantidad)
                             VALUES (@Nombre, @Descripcion, @Precio, @IdProveedor, @Cantidad)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = producto.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = producto.Descripcion;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = producto.Precio;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = producto.IdProveedor;
                        cmd.Parameters.Add("@Cantidad", SqlDbType.Int).Value = producto.Cantidad; // 🔸 nuevo

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Ok"] = "Producto agregado correctamente.";
                return RedirectToAction("Index", new { idProveedor = producto.IdProveedor });
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error al guardar el producto: " + ex.Message;
                return View("Crear", producto);
            }
        }

    }
}


