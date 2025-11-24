using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Stock.Controllers
{
    public class DashboardController : Controller
    {

       

        [Authorize]
        public IActionResult Index()
        {
            
            var tipoUsuario = User.FindFirst(ClaimTypes.Role)?.Value;

            
            if (tipoUsuario != null)
            {
                if (tipoUsuario.Equals("Proveedor", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("DashboardProveedor");
                }
                else if (tipoUsuario.Equals("Administrativo", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("DashboardAdministrativo");
                }
            }

            
            return RedirectToAction("Index", "Home");
        }


        [Authorize(Roles = "Proveedor")]
        public IActionResult DashboardProveedor()
        {
            
            return RedirectToAction("Index", "Proveedor");
        }

        
        [Authorize(Roles = "Administrativo")]
        public IActionResult DashboardAdministrativo()
        {
            
            return View();
        }
    }
}