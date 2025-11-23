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

        public IActionResult DashboardAdministrativo(int? filtro)
        {
            List<StockAdmin> stock = new List<StockAdmin>();
            string? cs = _configuration.GetConnectionString("MiConexion");

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
            SELECT sa.IdProducto, sa.Cantidad,
                   p.Nombre AS NombreProducto, p.Descripcion
            FROM StockAdministrador sa
            INNER JOIN Productos p ON sa.IdProducto = p.Id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        stock.Add(new StockAdmin
                        {
                            IdProducto = Convert.ToInt32(rd["IdProducto"]),
                            Cantidad = Convert.ToInt32(rd["Cantidad"]),
                            NombreProducto = rd["NombreProducto"].ToString() ?? "",
                            Descripcion = rd["Descripcion"].ToString() ?? ""
                        });
                    }
                }
            }

            if (filtro.HasValue)
                stock = stock.Where(s => s.Cantidad <= filtro.Value).ToList();

            ViewBag.FiltroActual = filtro;

            return View(stock);
        }

        [HttpPost]
        public IActionResult EnviarPedidos(List<PedidoRequest> pedidos)
        {
            try
            {
                string? cs = _configuration.GetConnectionString("MiConexion");

                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    foreach (var p in pedidos)
                    {
                        // solo los productos marcados
                        if (!p.Seleccionado || p.CantidadSolicitada <= 0)
                            continue;

                        // obtener el proveedor desde Productos
                        int idProveedor = 0;

                        using (var cmdProv = new SqlCommand(
                            "SELECT IdProveedor FROM Productos WHERE Id = @Id", con))
                        {
                            cmdProv.Parameters.AddWithValue("@Id", p.IdProducto);
                            idProveedor = (int)cmdProv.ExecuteScalar();
                        }

                        // insertar solicitud
                        string sql = @"
                    INSERT INTO SolicitudesPedido
                    (IdProducto, IdProveedor, CantidadSolicitada, FechaSolicitud, Estado)
                    VALUES (@IdProducto, @IdProveedor, @Cantidad, GETDATE(), 'Pendiente')";
                        using (var cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("@IdProducto", p.IdProducto);
                            cmd.Parameters.AddWithValue("@IdProveedor", idProveedor);
                            cmd.Parameters.AddWithValue("@Cantidad", p.CantidadSolicitada);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                TempData["Ok"] = "Pedidos enviados correctamente.";
                return RedirectToAction("DashboardAdministrativo");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al enviar pedidos: " + ex.Message;
                return RedirectToAction("DashboardAdministrativo");
            }
        }

    }
}
