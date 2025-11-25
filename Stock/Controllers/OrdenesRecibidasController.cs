using Microsoft.AspNetCore.Mvc;

namespace Stock.Controllers
{
    public class OrdenesRecibidasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
