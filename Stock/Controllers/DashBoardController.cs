using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;

namespace Stock.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IConfiguration _configuration;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 🔹 Dashboard del proveedor (ya lo tenés, lo dejamos vacío por ahora)
        public IActionResult DashboardProveedor()
        {
            return View();
        }

        // 🔹 Dashboard del administrador con proveedores + productos
        public IActionResult DashboardAdministrativo()
        {
            List<Producto> productos = new List<Producto>();

            string? connectionString = _configuration.GetConnectionString("MiConexion");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT * FROM Productos";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
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
                                IdProveedor = Convert.ToInt32(reader["IdProveedor"]),
                                Cantidad = Convert.ToInt32(reader["Cantidad"]) // ⚠️ importante
                            });
                        }
                    }
                }
            }

            ViewBag.Productos = productos;
            return View();
        }

        [HttpGet]
        public IActionResult AgregarMercaderia(int? idProveedor)
        {
            string? cs = _configuration.GetConnectionString("MiConexion");
            var proveedores = new List<Usuario>();
            var productos = new List<Producto>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                // 1️⃣ Obtener proveedores
                string q1 = "SELECT Id, Email FROM Usuarios WHERE TipoUsuario = 'Proveedor'";
                using (SqlCommand cmd = new SqlCommand(q1, con))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        proveedores.Add(new Usuario
                        {
                            Id = Convert.ToInt32(rd["Id"]),
                            Email = rd["Email"].ToString() ?? ""
                        });
                    }
                }

                // 2️⃣ Si seleccionaste un proveedor, traer sus productos
                if (idProveedor.HasValue)
                {
                    string q2 = "SELECT * FROM Productos WHERE IdProveedor = @Id";
                    using (SqlCommand cmd = new SqlCommand(q2, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", idProveedor.Value);
                        using (SqlDataReader rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                productos.Add(new Producto
                                {
                                    Id = Convert.ToInt32(rd["Id"]),
                                    Nombre = rd["Nombre"].ToString() ?? "",
                                    Descripcion = rd["Descripcion"].ToString() ?? "",
                                    Precio = Convert.ToDecimal(rd["Precio"]),
                                    IdProveedor = Convert.ToInt32(rd["IdProveedor"]),
                                    Cantidad = Convert.ToInt32(rd["Cantidad"])
                                });
                            }
                        }
                    }
                }
            }

            ViewBag.Proveedores = proveedores;
            ViewBag.IdProveedorSeleccionado = idProveedor;
            return View(productos);
        }



        [HttpPost]
        public IActionResult ConfirmarMercaderia(List<StockUpdateModel> Stock)
        {
            string? cs = _configuration.GetConnectionString("MiConexion");

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                foreach (var item in Stock)
                {
                    if (item.CantidadAgregar > 0)
                    {
                        // 1️⃣ Intentamos actualizar el stock del administrador
                        string query = @"
                    UPDATE StockAdministrador
                    SET Cantidad = Cantidad + @Agregar, FechaActualizacion = GETDATE()
                    WHERE IdProducto = @IdProducto";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@Agregar", item.CantidadAgregar);
                            cmd.Parameters.AddWithValue("@IdProducto", item.IdProducto);
                            int filasAfectadas = cmd.ExecuteNonQuery();

                            // 2️⃣ Si no existía ese producto en stock del admin, lo insertamos
                            if (filasAfectadas == 0)
                            {
                                string insert = @"
                            INSERT INTO StockAdministrador (IdProducto, Cantidad, FechaActualizacion)
                            VALUES (@IdProducto, @Cantidad, GETDATE())";

                                using (SqlCommand insertCmd = new SqlCommand(insert, con))
                                {
                                    insertCmd.Parameters.AddWithValue("@IdProducto", item.IdProducto);
                                    insertCmd.Parameters.AddWithValue("@Cantidad", item.CantidadAgregar);
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }

            TempData["Ok"] = "✅ Mercadería agregada correctamente al stock del administrador.";
            return RedirectToAction("Index", "Stock");
        }



    }
}
