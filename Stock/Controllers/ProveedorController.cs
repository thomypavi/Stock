using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

        private string GetConnectionString()
            => _configuration.GetConnectionString("MiConexion")!;

        // ------------------------------------------------------------
        // OBTENER ID DEL PROVEEDOR LOGUEADO
        // ------------------------------------------------------------
        private int GetCurrentProveedorId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out int idProveedor))
                return idProveedor;

            return 0;
        }


        // ------------------------------------------------------------
        // PANTALLA PRINCIPAL
        // ------------------------------------------------------------
        public IActionResult Index()
        {
            return RedirectToAction("OrdenesRecibidas");
        }


        // ------------------------------------------------------------
        // ÓRDENES PENDIENTES
        // ------------------------------------------------------------
        public IActionResult OrdenesRecibidas()
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            List<OrdenDeCompra> ordenes = new List<OrdenDeCompra>();

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();
                    string query = @"
                        SELECT IdOrden, Fecha, Estado, IdProveedor
                        FROM OrdenDeCompra
                        WHERE IdProveedor = @IdProveedor 
                          AND Estado != 'Enviada'
                        ORDER BY Fecha DESC";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ordenes.Add(new OrdenDeCompra
                                {
                                    IdOrden = reader.GetInt32(0),
                                    Fecha = reader.GetDateTime(1),
                                    Estado = reader.GetString(2),
                                    IdProveedor = reader.GetInt32(3)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR en Órdenes Recibidas: " + ex.Message);
            }

            return View(ordenes);
        }


        // ------------------------------------------------------------
        // HISTORIAL DE ÓRDENES ENVIADAS
        // ------------------------------------------------------------
        public IActionResult HistorialDeOrdenes()
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            List<OrdenDeCompra> ordenes = new();

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        SELECT IdOrden, Fecha, Estado, IdProveedor
                        FROM OrdenDeCompra
                        WHERE IdProveedor = @IdProveedor 
                          AND Estado = 'Enviada'
                        ORDER BY Fecha DESC";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ordenes.Add(new OrdenDeCompra
                                {
                                    IdOrden = reader.GetInt32(0),
                                    Fecha = reader.GetDateTime(1),
                                    Estado = reader.GetString(2),
                                    IdProveedor = reader.GetInt32(3)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR en Historial de Órdenes: " + ex.Message);
            }

            return View(ordenes);
        }


        // ------------------------------------------------------------
        // DETALLE DE UNA ORDEN
        // ------------------------------------------------------------
        public IActionResult Detalle(int idOrden)
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            var model = new OrdenDetalleViewModel { Items = new List<OrdenItem>() };

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string queryCabecera = @"
                        SELECT Fecha, Estado, IdProveedor
                        FROM OrdenDeCompra
                        WHERE IdOrden = @IdOrden AND IdProveedor = @IdProveedor";

                    using (var cmd = new SqlCommand(queryCabecera, con))
                    {
                        cmd.Parameters.Add("@IdOrden", SqlDbType.Int).Value = idOrden;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return NotFound();

                            model.IdOrden = idOrden;
                            model.Fecha = r.GetDateTime(0);
                            model.Estado = r.GetString(1);
                            model.IdProveedor = r.GetInt32(2);
                        }
                    }

                    string queryDetalle = @"
                        SELECT OI.IdProducto, OI.Cantidad, OI.PrecioUnitario, P.Nombre
                        FROM OrdenItems OI
                        JOIN Productos P ON OI.IdProducto = P.Id
                        WHERE OI.IdOrden = @IdOrden";

                    using (var cmd = new SqlCommand(queryDetalle, con))
                    {
                        cmd.Parameters.Add("@IdOrden", SqlDbType.Int).Value = idOrden;

                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                model.Items.Add(new OrdenItem
                                {
                                    IdProducto = rd.GetInt32(0),
                                    Cantidad = rd.GetInt32(1),
                                    PrecioUnitario = rd.GetDouble(2),
                                    NombreProducto = rd.GetString(3)
                                });
                            }
                        }
                    }

                    model.Total = model.Items.Sum(x => x.Cantidad * x.PrecioUnitario);
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR en detalle: " + ex.Message);
            }

            return View(model);
        }


        // ------------------------------------------------------------
        // CONFIRMAR ENVÍO
        // ------------------------------------------------------------
        [HttpPost]
        public IActionResult ConfirmarEnvio(int idOrden)
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        UPDATE OrdenDeCompra 
                        SET Estado = 'Enviada'
                        WHERE IdOrden = @IdOrden AND IdProveedor = @IdProveedor";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdOrden", SqlDbType.Int).Value = idOrden;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = $"Orden #{idOrden} enviada.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "ERROR al confirmar envío: " + ex.Message;
            }

            return RedirectToAction("HistorialDeOrdenes");
        }


        // ------------------------------------------------------------
        // MIS PRODUCTOS
        // ------------------------------------------------------------
        public IActionResult MisProductos()
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            List<Producto> productos = new();

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        SELECT Id, Nombre, Descripcion, Precio, IdProveedor
                        FROM Productos
                        WHERE IdProveedor = @IdProveedor";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productos.Add(new Producto
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Descripcion = reader.GetString(2),
                                    Precio = reader.GetDecimal(3),
                                    IdProveedor = reader.GetInt32(4)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR en Mis Productos: " + ex.Message);
            }

            return View(productos);
        }


        // ------------------------------------------------------------
        // CARGAR PRODUCTO
        // ------------------------------------------------------------
        [HttpGet]
        public IActionResult CargarProducto()
        {
            return View(new CargarProductoViewModel());
        }

        [HttpPost]
        public IActionResult CargarProducto(CargarProductoViewModel vm)
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        INSERT INTO Productos (Nombre, Descripcion, Precio, IdProveedor)
                        VALUES (@Nombre, @Descripcion, @Precio, @IdProveedor)";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = vm.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = vm.Descripcion ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = vm.Precio;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Producto cargado correctamente.";
                return RedirectToAction("MisProductos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(vm);
            }
        }


        // ------------------------------------------------------------
        // EDITAR PRODUCTO
        // ------------------------------------------------------------
        [HttpGet]
        public IActionResult EditarProducto(int id)
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            Producto? producto = null;

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        SELECT Id, Nombre, Descripcion, Precio, IdProveedor
                        FROM Productos
                        WHERE Id = @IdProducto AND IdProveedor = @IdProveedor";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                producto = new Producto
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Descripcion = reader.GetString(2),
                                    Precio = reader.GetDecimal(3),
                                    IdProveedor = reader.GetInt32(4)
                                };
                            }
                            else
                                return NotFound();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "ERROR al cargar producto: " + ex.Message;
                return RedirectToAction("MisProductos");
            }

            return View(producto);
        }


        [HttpPost]
        public IActionResult EditarProducto(Producto producto)
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0 || idProveedor != producto.IdProveedor)
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(producto);

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        UPDATE Productos 
                        SET Nombre = @Nombre, Descripcion = @Descripcion, Precio = @Precio
                        WHERE Id = @IdProducto AND IdProveedor = @IdProveedor";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = producto.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = producto.Descripcion ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = producto.Precio;
                        cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = producto.Id;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Producto actualizado.";
                return RedirectToAction("MisProductos");
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "ERROR al guardar cambios: " + ex.Message;
                return View(producto);
            }
        }


        // ------------------------------------------------------------
        // ELIMINAR PRODUCTO
        // ------------------------------------------------------------
        [HttpPost]
        public IActionResult EliminarProducto(int id)
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        DELETE FROM Productos 
                        WHERE Id = @IdProducto AND IdProveedor = @IdProveedor";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Producto eliminado.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "ERROR al eliminar: " + ex.Message;
            }

            return RedirectToAction("MisProductos");
        }


        // ------------------------------------------------------------
        // EDITAR INFORMACIÓN DEL PROVEEDOR
        // ------------------------------------------------------------
        [HttpGet]
        public IActionResult EditarInformacion()
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0) return Unauthorized();

            Proveedor proveedor = new();

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        SELECT IdProveedor, Nombre, Telefono, Direccion
                        FROM Proveedores
                        WHERE IdProveedor = @IdProveedor";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                proveedor.IdProveedor = reader.GetInt32(0);
                                proveedor.Nombre = reader.GetString(1);
                                proveedor.Telefono = reader.GetString(2);
                                proveedor.Direccion = reader.GetString(3);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR al cargar información del proveedor: " + ex.Message);
            }

            return View(proveedor);
        }


        [HttpPost]
        public IActionResult GuardarCambios(Proveedor proveedor)
        {
            int idProveedor = GetCurrentProveedorId();
            if (idProveedor == 0 || idProveedor != proveedor.IdProveedor)
                return Unauthorized();

            if (!ModelState.IsValid)
                return View("EditarInformacion", proveedor);

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"
                        UPDATE Proveedores
                        SET Telefono = @Telefono, Direccion = @Direccion
                        WHERE IdProveedor = @IdProveedor";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Telefono", SqlDbType.VarChar).Value = proveedor.Telefono;
                        cmd.Parameters.Add("@Direccion", SqlDbType.VarChar).Value = proveedor.Direccion;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Información actualizada.";
                return RedirectToAction("EditarInformacion");
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "ERROR al guardar cambios: " + ex.Message;
                return View("EditarInformacion", proveedor);
            }
        }
    }
}