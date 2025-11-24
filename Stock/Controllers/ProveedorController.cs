using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace Stock.Controllers
{
    
    [Authorize(Roles = "Proveedor")]
    public class ProveedorController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProveedorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        
        private string? GetConnectionString() => _configuration.GetConnectionString("MiConexion");

        
        private int GetCurrentProveedorId()
        {
            
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            
            if (idClaim != null && int.TryParse(idClaim.Value, out int idProveedor))
            {
                return idProveedor;
            }
            
            return 0;
        }

       
        public IActionResult Index()
        {
            return RedirectToAction("OrdenesRecibidas");
        }

        
        public IActionResult OrdenesRecibidas()
        {
            List<OrdenDeCompra> ordenes = new List<OrdenDeCompra>();
            int idProveedorActual = GetCurrentProveedorId();

            if (idProveedorActual == 0) return Unauthorized();

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    string query = @"
                        SELECT IdOrden, Fecha, Estado, IdProveedor 
                        FROM Ordenes 
                        WHERE IdProveedor = @IdProveedor AND Estado != 'Enviada' 
                        ORDER BY Fecha DESC";

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
                return Content($"ERROR DE DB en OrdenesRecibidas: {ex.Message}");
            }

            return View(ordenes);
        }

        
        public IActionResult HistorialDeOrdenes()
        {
            List<OrdenDeCompra> ordenes = new List<OrdenDeCompra>();
            int idProveedorActual = GetCurrentProveedorId();

            if (idProveedorActual == 0) return Unauthorized();

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    string query = @"
                        SELECT IdOrden, Fecha, Estado, IdProveedor 
                        FROM Ordenes 
                        WHERE IdProveedor = @IdProveedor AND Estado = 'Enviada'
                        ORDER BY Fecha DESC";

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
                return Content($"ERROR DE DB en HistorialDeOrdenes: {ex.Message}");
            }

            return View(ordenes);
        }

        
        public IActionResult Detalle(int idOrden)
        {
            int idProveedorActual = GetCurrentProveedorId();
            if (idProveedorActual == 0) return Unauthorized();

            
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

                    
                    string queryCabecera = "SELECT Fecha, Estado, IdProveedor FROM Ordenes WHERE IdOrden = @IdOrden AND IdProveedor = @IdProveedor";
                    using (SqlCommand cmd = new SqlCommand(queryCabecera, con))
                    {
                        cmd.Parameters.Add("@IdOrden", SqlDbType.Int).Value = idOrden;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;

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
                               
                                return NotFound();
                            }
                        }
                    }

                    
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

                model.Total = model.Items.Sum(i => i.Cantidad * i.PrecioUnitario);
            }
            catch (Exception ex)
            {
                return Content("ERROR en Detalle de Orden: " + ex.Message);
            }

            return View(model);
        }

        
        [HttpPost]
        public IActionResult ConfirmarEnvio(int idOrden)
        {
            int idProveedorActual = GetCurrentProveedorId();
            if (idProveedorActual == 0) return Unauthorized();

            string? connectionString = GetConnectionString();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    string query = "UPDATE Ordenes SET Estado = 'Enviada' WHERE IdOrden = @IdOrden AND IdProveedor = @IdProveedor";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdOrden", SqlDbType.Int).Value = idOrden;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["MensajeExito"] = $"Orden #{idOrden} marcada como 'Enviada'.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "ERROR al confirmar envío: " + ex.Message;
            }

            return RedirectToAction("HistorialDeOrdenes");
        }

        
        [HttpGet]
        public IActionResult CargarProducto()
        {
           
            return View(new CargarProductoViewModel());
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CargarProducto(CargarProductoViewModel viewModel)
        {
            int idProveedor = GetCurrentProveedorId();

            if (idProveedor == 0) return Unauthorized();


            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = @"INSERT INTO Productos 
                                     (Nombre, Descripcion, Precio, IdProveedor, StockActual, StockMinimo, CantidadReposicion)
                                     VALUES 
                                     (@Nombre, @Descripcion, @Precio, @IdProveedor, 0, 0, 0)"; 

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = viewModel.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = viewModel.Descripcion ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = viewModel.Precio;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["MensajeExito"] = "Producto cargado exitosamente. Stock pendiente de asignación por el Administrador.";
                return RedirectToAction("MisProductos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al cargar el producto: " + ex.Message);
                
                return View(viewModel);
            }
        }


        
        [HttpGet]
        public IActionResult EditarProducto(int id)
        {
            int idProveedorActual = GetCurrentProveedorId();
            if (idProveedorActual == 0) return Unauthorized();

            Producto producto = new Producto();

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = "SELECT Id, Nombre, Descripcion, Precio, IdProveedor FROM Productos WHERE Id = @IdProducto AND IdProveedor = @IdProveedor";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                producto.Id = Convert.ToInt32(reader["Id"]);
                                producto.Nombre = reader["Nombre"].ToString() ?? "";
                                producto.Descripcion = reader["Descripcion"].ToString() ?? "";
                                producto.Precio = Convert.ToDecimal(reader["Precio"]);
                                producto.IdProveedor = Convert.ToInt32(reader["IdProveedor"]);
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al cargar datos del producto: {ex.Message}";
                return RedirectToAction("MisProductos");
            }

            return View(producto);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarProducto(Producto productoModificado)
        {
            int idProveedorActual = GetCurrentProveedorId();

            if (idProveedorActual == 0 || idProveedorActual != productoModificado.IdProveedor) return Unauthorized();

            if (ModelState.IsValid)
            {
                try
                {
                    string? connectionString = GetConnectionString();
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();

                        string query = @"UPDATE Productos 
                                         SET Nombre = @Nombre, Descripcion = @Descripcion, Precio = @Precio 
                                         WHERE Id = @IdProducto AND IdProveedor = @IdProveedor";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = productoModificado.Nombre;
                            cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = productoModificado.Descripcion ?? (object)DBNull.Value;
                            cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = productoModificado.Precio;
                            cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = productoModificado.Id;
                            cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;

                            cmd.ExecuteNonQuery();
                        }
                    }
                    TempData["MensajeExito"] = $"El producto '{productoModificado.Nombre}' se actualizó correctamente.";
                    return RedirectToAction("MisProductos");
                }
                catch (Exception ex)
                {
                    TempData["MensajeError"] = $"Error al guardar cambios del producto: {ex.Message}";
                    return View(productoModificado);
                }
            }

            return View(productoModificado);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarProducto(int id)
        {
            int idProveedorActual = GetCurrentProveedorId();
            if (idProveedorActual == 0) return Unauthorized();

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = "DELETE FROM Productos WHERE Id = @IdProducto AND IdProveedor = @IdProveedor";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;

                        int filasAfectadas = cmd.ExecuteNonQuery();

                        if (filasAfectadas > 0)
                        {
                            TempData["MensajeExito"] = "Producto eliminado correctamente.";
                        }
                        else
                        {
                            TempData["MensajeError"] = "No se pudo eliminar el producto o no se encontró para este proveedor.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al eliminar el producto: {ex.Message}";
            }

            return RedirectToAction("MisProductos");
        }

       
        public IActionResult MisProductos()
        {
            List<Producto> productos = new List<Producto>();
            int idProveedorActual = GetCurrentProveedorId();

            if (idProveedorActual == 0) return Unauthorized();

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

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
                return Content($"ERROR DE DB en MisProductos: {ex.Message}");
            }

            return View(productos);
        }

        
        [HttpGet]
        public IActionResult EditarInformacion()
        {
            int idProveedorActual = GetCurrentProveedorId();
            if (idProveedorActual == 0) return Unauthorized();

            Proveedor proveedorActual = new Proveedor();
            string? connectionString = GetConnectionString();

            try
            {
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
            }
            catch (Exception ex)
            {
                return Content($"ERROR al cargar datos de proveedor: {ex.Message}");
            }

            return View(proveedorActual);
        }


       
        [HttpPost]
        public IActionResult GuardarCambios(Proveedor proveedorModificado)
        {
            int idProveedorActual = GetCurrentProveedorId();

            if (idProveedorActual == 0 || idProveedorActual != proveedorModificado.IdProveedor) return Unauthorized();

            if (ModelState.IsValid)
            {
                string? connectionString = GetConnectionString();
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();

                        string query = @"UPDATE Proveedores SET Telefono = @Telefono, Direccion = @Direccion WHERE IdProveedor = @IdProveedor";
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.Add("@Telefono", SqlDbType.VarChar).Value = proveedorModificado.Telefono;
                            cmd.Parameters.Add("@Direccion", SqlDbType.VarChar).Value = proveedorModificado.Direccion;
                            cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedorActual;

                            cmd.ExecuteNonQuery();
                        }
                    }
                    TempData["MensajeExito"] = "La información se actualizó correctamente.";
                    return RedirectToAction("EditarInformacion");
                }
                catch (Exception ex)
                {
                    TempData["MensajeError"] = $"Error al guardar cambios: {ex.Message}";
                    return View("EditarInformacion", proveedorModificado);
                }
            }
            
            return View("EditarInformacion", proveedorModificado);
        }
    }
}