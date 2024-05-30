using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Public.Web.Models;

namespace Public.Web.Controllers
{
    public class AgentController : Controller
    {
        // GET: AgentController
        public ActionResult Index()
        {
            var model = new AgentInfoModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult Index(AgentInfoModel model)
        {
            // Optionally, you can process the value here
            // For simplicity, we just pass it back to the view
            model.Response = "Sike";
            return View(model);
        }

    }
}
