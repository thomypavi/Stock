using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Stock.Controllers
{

    [Authorize(Roles = "Administrativo")]
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string? GetConnectionString() => _configuration.GetConnectionString("MiConexion");





        private List<Stock.Models.Proveedor> ObtenerProveedores()
        {
            List<Stock.Models.Proveedor> proveedores = new List<Stock.Models.Proveedor>();
            string? connectionString = GetConnectionString();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();


                    string query = "SELECT IdProveedor, Nombre FROM Proveedores";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            proveedores.Add(new Stock.Models.Proveedor
                            {
                                IdProveedor = Convert.ToInt32(reader["IdProveedor"]),
                                Nombre = reader["Nombre"].ToString() ?? "N/A"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                ViewData["ErrorDB"] = "Error al cargar proveedores: " + ex.Message;
            }
            return proveedores;
        }


        public IActionResult GestionProductos()
        {
            List<Producto> productos = new List<Producto>();
            string? connectionString = GetConnectionString();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();


                    string query = @"SELECT Id, Nombre, Descripcion, Precio, IdProveedor, 
                                            StockActual, StockMinimo, CantidadReposicion 
                                     FROM Productos";
                    using (SqlCommand cmd = new SqlCommand(query, con))
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
                                StockActual = Convert.ToInt32(reader["StockActual"]),
                                StockMinimo = Convert.ToInt32(reader["StockMinimo"]),
                                CantidadReposicion = Convert.ToInt32(reader["CantidadReposicion"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"ERROR DE DB al listar productos: {ex.Message}";
                return View("~/Views/Productos/Index.cshtml", new List<Producto>());
            }


            ViewData["Proveedores"] = ObtenerProveedores();

            return View("~/Views/Productos/Index.cshtml", productos);
        }


        [HttpGet]
        public IActionResult CrearProducto()
        {

            ViewBag.Proveedores = new SelectList(ObtenerProveedores(), "IdProveedor", "Nombre");


            return View("~/Views/Productos/Crear.cshtml", new Producto());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearProducto(Producto producto)
        {


            if (!ModelState.IsValid)
            {

                ViewBag.Proveedores = new SelectList(ObtenerProveedores(), "IdProveedor", "Nombre");
                return View("~/Views/Productos/Crear.cshtml", producto);
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
                                     (@Nombre, @Descripcion, @Precio, @IdProveedor, @StockActual, @StockMinimo, @CantidadReposicion)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = producto.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = producto.Descripcion ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = producto.Precio;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = producto.IdProveedor;

                        cmd.Parameters.Add("@StockActual", SqlDbType.Int).Value = producto.StockActual;
                        cmd.Parameters.Add("@StockMinimo", SqlDbType.Int).Value = producto.StockMinimo;
                        cmd.Parameters.Add("@CantidadReposicion", SqlDbType.Int).Value = producto.CantidadReposicion;

                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["MensajeExito"] = "Producto creado exitosamente por el Administrador.";
                return RedirectToAction("GestionProductos");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error al guardar el producto: " + ex.Message;
                ViewBag.Proveedores = new SelectList(ObtenerProveedores(), "IdProveedor", "Nombre");
                return View("~/Views/Productos/Crear.cshtml", producto);
            }
        }


        [HttpGet]
        public IActionResult EditarProducto(int id)
        {
            Producto? producto = null;
            string? connectionString = GetConnectionString();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();


                    string query = @"SELECT Id, Nombre, Descripcion, Precio, IdProveedor, 
                                            StockActual, StockMinimo, CantidadReposicion 
                                     FROM Productos WHERE Id = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                producto = new Producto
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Nombre = reader["Nombre"].ToString() ?? "",
                                    Descripcion = reader["Descripcion"].ToString() ?? "",
                                    Precio = Convert.ToDecimal(reader["Precio"]),
                                    IdProveedor = Convert.ToInt32(reader["IdProveedor"]),

                                    StockActual = Convert.ToInt32(reader["StockActual"]),
                                    StockMinimo = Convert.ToInt32(reader["StockMinimo"]),
                                    CantidadReposicion = Convert.ToInt32(reader["CantidadReposicion"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error al cargar el producto: " + ex.Message;
            }

            if (producto == null)
            {
                TempData["ErrorMessage"] = "Producto no encontrado.";
                return RedirectToAction("GestionProductos");
            }


            ViewBag.Proveedores = new SelectList(ObtenerProveedores(), "IdProveedor", "Nombre", producto.IdProveedor);


            return View("~/Views/Productos/Editar.cshtml", producto);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarProducto(Producto productoModificado)
        {

            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = new SelectList(ObtenerProveedores(), "IdProveedor", "Nombre", productoModificado.IdProveedor);
                return View("~/Views/Productos/Editar.cshtml", productoModificado);
            }

            try
            {
                string? connectionString = GetConnectionString();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();


                    string query = @"UPDATE Productos
                                     SET Nombre = @Nombre, 
                                         Descripcion = @Descripcion, 
                                         Precio = @Precio, 
                                         IdProveedor = @IdProveedor,
                                         StockActual = @StockActual, 
                                         StockMinimo = @StockMinimo, 
                                         CantidadReposicion = @CantidadReposicion 
                                     WHERE Id = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = productoModificado.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = productoModificado.Descripcion ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = productoModificado.Precio;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = productoModificado.IdProveedor;
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = productoModificado.Id;


                        cmd.Parameters.Add("@StockActual", SqlDbType.Int).Value = productoModificado.StockActual;
                        cmd.Parameters.Add("@StockMinimo", SqlDbType.Int).Value = productoModificado.StockMinimo;
                        cmd.Parameters.Add("@CantidadReposicion", SqlDbType.Int).Value = productoModificado.CantidadReposicion;


                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = $"Producto '{productoModificado.Nombre}' actualizado exitosamente por el Administrador.";
                return RedirectToAction("GestionProductos");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error al actualizar el producto: " + ex.Message;
                ViewBag.Proveedores = new SelectList(ObtenerProveedores(), "IdProveedor", "Nombre", productoModificado.IdProveedor);
                return View("~/Views/Productos/Editar.cshtml", productoModificado);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarProducto(int id)
        {

            try
            {
                string? connectionString = GetConnectionString();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();


                    string query = "DELETE FROM Productos WHERE Id = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Producto eliminado exitosamente por el Administrador.";
                return RedirectToAction("GestionProductos");
            }
            catch (Exception ex)
            {

                TempData["ErrorMessage"] = "Error al eliminar el producto: " + ex.Message;
                return RedirectToAction("GestionProductos");
            }
        }
    }
}
