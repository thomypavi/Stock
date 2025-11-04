using Microsoft.AspNetCore.Mvc;

namespace Stock.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult DashboardProveedor()
        {
            return View();
        }

        public IActionResult DashboardAdministrativo()
        {
            return View();
        }
    }
}
