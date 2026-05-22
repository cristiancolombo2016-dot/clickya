using Microsoft.AspNetCore.Mvc;

namespace ClickYa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery] int ciudadId = 1)
    {
        // MOCK mínimo solo para probar
        var data = new
        {
            locales = new[]
            {
               new { id = 1, ciudadId = 1, nombre = "Local Demo SN", categoria = "pizzas" },
                new { id = 2, ciudadId = 2, nombre = "Local Demo Rosario", categoria = "servicios" }
            },
            banners = new[]
{
    new { id = 1, ciudadId = 1, local = 1, imagen = "banner1.png" }
},
            destacados = new[]
{
    new { id = 1, ciudadId = 1, local = 1, nombre = "Promo", precio = "$999", imagen = "promo.png" }
}

        };

        // filtrado por ciudad
        var result = new
        {
            locales = data.locales.Where(x => x.ciudadId == ciudadId),
            banners = data.banners.Where(x => x.ciudadId == ciudadId),
            destacados = data.destacados.Where(x => x.ciudadId == ciudadId)
        };

        return Ok(result);
    }
}
