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

            return View(productos);
        }

        [HttpGet]
        public IActionResult Crear(int idProveedor)
        {
            // Pasar el modelo con IdProveedor para que el formulario lo envíe correctamente
            var modelo = new Producto { IdProveedor = idProveedor };
            return View(modelo);
        }

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
                    string query = @"INSERT INTO Productos (Nombre, Descripcion, Precio, IdProveedor)
                             VALUES (@Nombre, @Descripcion, @Precio, @IdProveedor)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = producto.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = producto.Descripcion;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = producto.Precio;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = producto.IdProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }

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

