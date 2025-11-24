using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stock.Models;
using System.Data;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Stock.Controllers
{
    public class ProductosController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProductosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string? GetConnectionString() => _configuration.GetConnectionString("MiConexion");

        public IActionResult Index(int idProveedor)
        {
            List<Producto> productos = new List<Producto>();
            string? connectionString = GetConnectionString();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT Id, Nombre, Descripcion, Precio, IdProveedor FROM Productos WHERE IdProveedor = @IdProveedor";
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


            ViewData["IdProveedorActual"] = idProveedor;

            return View(productos);
        }

        [HttpGet]
        public IActionResult Crear(int idProveedor)
        {

            var modelo = new Producto { IdProveedor = idProveedor };
            return View(modelo);
        }

        [HttpPost]
        public IActionResult Crear(Producto producto)
        {
            try
            {
                string? connectionString = GetConnectionString();

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

                TempData["MensajeExito"] = "Producto creado exitosamente.";
                return RedirectToAction("Index", new { idProveedor = producto.IdProveedor });
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error al guardar el producto: " + ex.Message;
                return View("Crear", producto);
            }
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            Producto? producto = null;
            string? connectionString = GetConnectionString();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT Id, Nombre, Descripcion, Precio, IdProveedor FROM Productos WHERE Id = @Id";
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
                                IdProveedor = Convert.ToInt32(reader["IdProveedor"])
                            };
                        }
                    }
                }
            }

            if (producto == null)
            {
                return NotFound();
            }
            return View(producto);
        }

        [HttpPost]
        public IActionResult Editar(Producto productoModificado)
        {
            try
            {
                string? connectionString = GetConnectionString();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"UPDATE Productos 
                                     SET Nombre = @Nombre, Descripcion = @Descripcion, Precio = @Precio 
                                     WHERE Id = @Id AND IdProveedor = @IdProveedor";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = productoModificado.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = productoModificado.Descripcion;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = productoModificado.Precio;
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = productoModificado.Id;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = productoModificado.IdProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = $"Producto '{productoModificado.Nombre}' actualizado exitosamente.";
                return RedirectToAction("Index", new { idProveedor = productoModificado.IdProveedor });
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error al actualizar el producto: " + ex.Message;
                return View("Editar", productoModificado);
            }
        }

        [HttpPost]
        public IActionResult Eliminar(int id, int idProveedor)
        {
            try
            {
                string? connectionString = GetConnectionString();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = "DELETE FROM Productos WHERE Id = @Id AND IdProveedor = @IdProveedor";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Producto eliminado exitosamente.";
                return RedirectToAction("Index", new { idProveedor });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar el producto: " + ex.Message;
                return RedirectToAction("Index", new { idProveedor });
            }
        }
    }
}