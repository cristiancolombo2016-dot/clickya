using Microsoft.AspNetCore.Mvc;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/ciudades")]
    public class CiudadesController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var ciudades = new[]
            {
                new { Id = 1, Nombre = "San Nicolás de los Arroyos", Activa = true },
                new { Id = 2, Nombre = "Rosario", Activa = false },
                new { Id = 3, Nombre = "Buenos Aires", Activa = false }
            };

            return Ok(ciudades);
        }
    }
}
