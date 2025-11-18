using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;

namespace Stock.Controllers
{
    public class StockController : Controller
    {
        private readonly IConfiguration _configuration;

        public StockController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            List<StockAdmin> stock = new List<StockAdmin>();
            string? cs = _configuration.GetConnectionString("MiConexion");

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                // 🔹 Asegurar que todos los productos existan en StockAdministrador
                string insertMissing = @"
        INSERT INTO StockAdministrador (IdProducto, Cantidad)
        SELECT p.Id, 0
        FROM Productos p
        WHERE NOT EXISTS (
            SELECT 1 FROM StockAdministrador s WHERE s.IdProducto = p.Id
        );";
                using (SqlCommand cmdInsert = new SqlCommand(insertMissing, con))
                {
                    cmdInsert.ExecuteNonQuery();
                }

                // 🔹 Traer la lista con los nombres de productos y cantidades (sin FechaActualizacion)
                string query = @"
        SELECT sa.Id, sa.Cantidad,
               p.Id AS IdProducto, p.Nombre AS NombreProducto, p.Descripcion
        FROM StockAdministrador sa
        INNER JOIN Productos p ON sa.IdProducto = p.Id
        ORDER BY p.Nombre;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stock.Add(new StockAdmin
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            IdProducto = Convert.ToInt32(reader["IdProducto"]),
                            Cantidad = Convert.ToInt32(reader["Cantidad"]),
                            NombreProducto = reader["NombreProducto"].ToString() ?? "",
                            Descripcion = reader["Descripcion"].ToString() ?? ""
                        });
                    }
                }
            }

            return View(stock);
        }



        [HttpPost]
        [HttpPost]
        public IActionResult ActualizarStock(List<StockUpdateModel> Stock)
        {
            string? cs = _configuration.GetConnectionString("MiConexion");

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                foreach (var item in Stock)
                {
                    if (item.CantidadAgregar > 0)
                    {
                        string query = @"
                    UPDATE StockAdministrador
                    SET Cantidad = Cantidad + @Agregar, FechaActualizacion = GETDATE()
                    WHERE IdProducto = @IdProducto";
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@Agregar", item.CantidadAgregar);
                            cmd.Parameters.AddWithValue("@IdProducto", item.IdProducto);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            TempData["Ok"] = "✅ Stock actualizado correctamente.";
            return RedirectToAction("Index");
        }



        [HttpGet]
        public IActionResult Editar(int id)
        {
            StockAdmin item = new StockAdmin();
            string? cs = _configuration.GetConnectionString("MiConexion");

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                string query = @"SELECT sa.Id, sa.Cantidad, sa.IdProducto, p.Nombre, p.Descripcion
                                 FROM StockAdministrador sa
                                 INNER JOIN Productos p ON sa.IdProducto = p.Id
                                 WHERE sa.Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            item.Id = Convert.ToInt32(reader["Id"]);
                            item.IdProducto = Convert.ToInt32(reader["IdProducto"]);
                            item.Cantidad = Convert.ToInt32(reader["Cantidad"]);
                            item.NombreProducto = reader["Nombre"].ToString() ?? "";
                            item.Descripcion = reader["Descripcion"].ToString() ?? "";
                        }
                    }
                }
            }

            return View(item);
        }

        [HttpPost]
        public IActionResult Editar(StockAdmin stock)
        {
            string? cs = _configuration.GetConnectionString("MiConexion");

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                string update = "UPDATE StockAdministrador SET Cantidad=@Cantidad, FechaActualizacion=GETDATE() WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(update, con))
                {
                    cmd.Parameters.AddWithValue("@Cantidad", stock.Cantidad);
                    cmd.Parameters.AddWithValue("@Id", stock.Id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ConfirmarPedido(int idPedido)
        {
            string? cs = _configuration.GetConnectionString("MiConexion");

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                // Obtener los datos del pedido
                int idProducto = 0;
                int cantidad = 0;

                using (SqlCommand cmd = new SqlCommand("SELECT IdProducto, CantidadSolicitada FROM SolicitudesPedido WHERE Id = @Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", idPedido);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idProducto = Convert.ToInt32(reader["IdProducto"]);
                            cantidad = Convert.ToInt32(reader["CantidadSolicitada"]);
                        }
                    }
                }

                // Actualizar el stock del administrador
                using (SqlCommand cmd2 = new SqlCommand(
                    "UPDATE StockAdministrador SET Cantidad = Cantidad + @Cant, FechaActualizacion = GETDATE() WHERE IdProducto = @Prod", con))
                {
                    cmd2.Parameters.AddWithValue("@Cant", cantidad);
                    cmd2.Parameters.AddWithValue("@Prod", idProducto);
                    cmd2.ExecuteNonQuery();
                }

                // Marcar el pedido como completado
                using (SqlCommand cmd3 = new SqlCommand(
                    "UPDATE SolicitudesPedido SET Estado = 'Recibido' WHERE Id = @Id", con))
                {
                    cmd3.Parameters.AddWithValue("@Id", idPedido);
                    cmd3.ExecuteNonQuery();
                }
            }

            TempData["Ok"] = "📦 Pedido confirmado y stock actualizado.";
            return RedirectToAction("Index");
        }

    }
}

