using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Stock.Controllers
{
    public class ProveedorController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProveedorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string? GetConnectionString() => _configuration.GetConnectionString("MiConexion");


        private int GetCurrentProveedorId() => 5;


        public IActionResult OrdenesRecibidas()
        {
            List<OrdenDeCompra> ordenes = new List<OrdenDeCompra>();
            int idProveedorActual = GetCurrentProveedorId();

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Consulta todas las órdenes para el proveedor, idealmente esto se filtraría en la vista
                    // para mostrar solo 'Pendientes' o 'Nuevas'.
                    string query = "SELECT IdOrden, Fecha, Estado, IdProveedor FROM Ordenes WHERE IdProveedor = @IdProveedor";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ordenes.Add(new OrdenDeCompra
                                {
                                    IdOrden = Convert.ToInt32(reader["IdOrden"]),
                                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                                    Estado = reader["Estado"].ToString() ?? "",
                                    IdProveedor = Convert.ToInt32(reader["IdProveedor"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR DE DB en OrdenesRecibidas: " + ex.Message);
            }

            return View(ordenes);
        }

        public IActionResult HistorialDeOrdenes()
        {
            List<OrdenDeCompra> ordenes = new List<OrdenDeCompra>();
            int idProveedorActual = GetCurrentProveedorId();

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Consulta solo órdenes que ya fueron enviadas (o el estado que defina como historial)
                    string query = "SELECT IdOrden, Fecha, Estado, IdProveedor FROM Ordenes WHERE IdProveedor = @IdProveedor AND Estado = 'Enviada'";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ordenes.Add(new OrdenDeCompra
                                {
                                    IdOrden = Convert.ToInt32(reader["IdOrden"]),
                                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                                    Estado = reader["Estado"].ToString() ?? "",
                                    IdProveedor = Convert.ToInt32(reader["IdProveedor"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR DE DB en HistorialDeOrdenes: " + ex.Message);
            }

            return View(ordenes);
        }

        public IActionResult Detalle(int idOrden)
        {
            // Inicializa el ViewModel y su lista de ítems
            OrdenDetalleViewModel model = new OrdenDetalleViewModel
            {
                Items = new List<OrdenItem>()
            };

            string? connectionString = GetConnectionString();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // OBTENER CABECERA DE LA ORDEN
                    string queryCabecera = "SELECT Fecha, Estado, IdProveedor FROM Ordenes WHERE IdOrden = @IdOrden";
                    using (SqlCommand cmd = new SqlCommand(queryCabecera, con))
                    {
                        cmd.Parameters.Add("@IdOrden", SqlDbType.Int).Value = idOrden;
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.IdOrden = idOrden;
                                model.Fecha = Convert.ToDateTime(reader["Fecha"]);
                                model.Estado = reader["Estado"].ToString() ?? "";
                                model.IdProveedor = Convert.ToInt32(reader["IdProveedor"]);
                            }
                            else
                            {
                                return NotFound(); // Orden no encontrada
                            }
                        }
                    }

                    // OBTENER DETALLE (ÍTEMS) DE LA ORDEN
                    string queryDetalle = @"
                        SELECT OI.IdProducto, OI.Cantidad, OI.PrecioUnitario, P.Nombre 
                        FROM OrdenItems OI 
                        INNER JOIN Productos P ON OI.IdProducto = P.Id
                        WHERE OI.IdOrden = @IdOrden";

                    using (SqlCommand cmdDetalle = new SqlCommand(queryDetalle, con))
                    {
                        cmdDetalle.Parameters.Add("@IdOrden", SqlDbType.Int).Value = idOrden;
                        using (SqlDataReader readerDetalle = cmdDetalle.ExecuteReader())
                        {
                            while (readerDetalle.Read())
                            {
                                model.Items.Add(new OrdenItem
                                {
                                    IdProducto = Convert.ToInt32(readerDetalle["IdProducto"]),
                                    NombreProducto = readerDetalle["Nombre"].ToString() ?? "Producto Desconocido",
                                    Cantidad = Convert.ToInt32(readerDetalle["Cantidad"]),
                                    PrecioUnitario = Convert.ToDouble(readerDetalle["PrecioUnitario"])
                                });
                            }
                        }
                    }
                }

                // CALCULAR EL TOTAL
                model.Total = model.Items.Sum(i => i.Cantidad * i.PrecioUnitario);
            }
            catch (Exception ex)
            {
                return Content("ERROR en Detalle de Orden: " + ex.Message);
            }

            return View(model);
        }


        public IActionResult ConfirmarEnvio(int idOrden)
        {
            string? connectionString = GetConnectionString();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = "UPDATE Ordenes SET Estado = 'Enviada' WHERE IdOrden = @IdOrden";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdOrden", SqlDbType.Int).Value = idOrden;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR al confirmar envío: " + ex.Message);
            }

            return RedirectToAction("HistorialDeOrdenes");
        }

        public IActionResult ProductosAsignados()
        {
            List<Producto> productos = new List<Producto>();
            int idProveedorActual = GetCurrentProveedorId();

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Consulta simplificada, eliminando el JOIN a ProductosProveedor.
                    string query = @"
                        SELECT Id, Nombre, Descripcion, Precio, IdProveedor 
                        FROM Productos
                        WHERE IdProveedor = @IdProveedor";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;
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
            }
            catch (Exception ex)
            {
                return Content("ERROR DE DB en ProductosAsignados: " + ex.Message);
            }

            return View(productos);
        }


        public IActionResult EditarInformacion()
        {
            int idProveedorActual = GetCurrentProveedorId();
            Proveedor proveedorActual = new Proveedor();

            string? connectionString = GetConnectionString();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                string query = "SELECT IdProveedor, Nombre, Telefono, Direccion FROM Proveedores WHERE IdProveedor = @IdProveedor";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            proveedorActual.IdProveedor = Convert.ToInt32(reader["IdProveedor"]);
                            proveedorActual.Nombre = reader["Nombre"].ToString() ?? "";
                            proveedorActual.Telefono = reader["Telefono"].ToString() ?? "";
                            proveedorActual.Direccion = reader["Direccion"].ToString() ?? "";
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
            }
            return View(proveedorActual);
        }

        [HttpPost]
        public IActionResult GuardarCambios(Proveedor proveedorModificado)
        {
            if (ModelState.IsValid)
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = @"UPDATE Proveedores SET Telefono = @Telefono, Direccion = @Direccion WHERE IdProveedor = @IdProveedor";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Telefono", SqlDbType.VarChar).Value = proveedorModificado.Telefono;
                        cmd.Parameters.Add("@Direccion", SqlDbType.VarChar).Value = proveedorModificado.Direccion;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = proveedorModificado.IdProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "La información se actualizó correctamente.";
                return RedirectToAction("EditarInformacion");
            }

            return View("EditarInformacion", proveedorModificado);
        }

        public IActionResult Index()
        {
            // Redirige al listado principal del proveedor, que son los Productos Asignados o las Órdenes Recibidas.
            return RedirectToAction("OrdenesRecibidas");
        }
    }
}