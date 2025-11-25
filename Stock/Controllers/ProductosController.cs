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

        // **********************************
        // FUNCIÓN AUXILIAR PARA OBTENER PROVEEDORES
        // **********************************
        private List<Usuario> ObtenerListaDeProveedores()
        {
            var proveedores = new List<Usuario>();
            string? connectionString = GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                // Si la cadena de conexión falla, devolvemos una lista vacía para evitar fallos
                return proveedores;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // ASUME QUE LA TABLA SE LLAMA 'Usuarios' Y TIENE EL CAMPO 'TipoUsuario'
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
                                    Email = reader["Email"].ToString() ?? "Proveedor sin Email"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // En un entorno real, registra esta excepción
                System.Diagnostics.Debug.WriteLine("Error al obtener proveedores: " + ex.Message);
            }

            // Siempre retorna una lista (vacía o llena), NUNCA null.
            return proveedores;
        }

        public IActionResult Index(int idProveedor)
        {
            List<Producto> productos = new List<Producto>();
            string? connectionString = GetConnectionString();

            // ... (Resto del código de Index) ...

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT Id, Nombre, Descripcion, Precio, IdProveedor, Cantidad FROM Productos WHERE IdProveedor = @IdProveedor"; // Añadí Cantidad si es parte de tu modelo
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
                                IdProveedor = Convert.ToInt32(reader["IdProveedor"]),
                                // Cantidad, si existe en la BD
                                // Cantidad = Convert.ToInt32(reader["Cantidad"]) 
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
            // **********************************
            // CAMBIO CLAVE: INICIALIZAR ViewBag.Proveedores
            // **********************************
            ViewBag.Proveedores = ObtenerListaDeProveedores();

            var modelo = new Producto { IdProveedor = idProveedor };
            return View(modelo);
        }

        [HttpPost]
        public IActionResult Crear(Producto producto)
        {
            try
            {
                string? connectionString = GetConnectionString();

                // Si la validación del modelo falla (y si tu vista usa Validación de Modelo):
                if (!ModelState.IsValid)
                {
                    // Necesitas volver a cargar ViewBag.Proveedores si hay un error de validación
                    ViewBag.Proveedores = ObtenerListaDeProveedores();
                    return View("Crear", producto);
                }

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // ASUMÍ QUE TAMBIÉN NECESITAS INSERTAR CANTIDAD EN LA DB
                    string query = @"INSERT INTO Productos (Nombre, Descripcion, Precio, IdProveedor, Cantidad)
                                     VALUES (@Nombre, @Descripcion, @Precio, @IdProveedor, @Cantidad)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = producto.Nombre;
                        cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar).Value = producto.Descripcion;
                        cmd.Parameters.Add("@Precio", SqlDbType.Decimal).Value = producto.Precio;
                        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = producto.IdProveedor;
                        cmd.Parameters.Add("@Cantidad", SqlDbType.Int).Value = producto.Cantidad; // Añadido

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Producto creado exitosamente.";
                return RedirectToAction("Index", new { idProveedor = producto.IdProveedor });
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error al guardar el producto: " + ex.Message;
                // Si falla la DB, recarga los proveedores antes de volver a la vista
                ViewBag.Proveedores = ObtenerListaDeProveedores();
                return View("Crear", producto);
            }
        }

        // ... (Resto de acciones: Editar(GET), Editar(POST), Eliminar) ...

        [HttpGet]
        public IActionResult Editar(int id)
        {
            Producto? producto = null;
            string? connectionString = GetConnectionString();


            // BUENA PRÁCTICA: TAMBIÉN CARGAR PROVEEDORES AQUÍ SI LA VISTA LO USA

            ViewBag.Proveedores = ObtenerListaDeProveedores();

            // ... (Resto del código de Editar(GET) ) ...

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT Id, Nombre, Descripcion, Precio, IdProveedor, Cantidad FROM Productos WHERE Id = @Id";
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
                                // Cantidad, si existe en la BD
                                // Cantidad = Convert.ToInt32(reader["Cantidad"])
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

        // ... (Resto del controlador sigue igual) ...
    }
}
