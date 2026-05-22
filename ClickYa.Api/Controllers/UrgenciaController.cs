using Microsoft.AspNetCore.Mvc;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrgenciaController : ControllerBase
    {
        [HttpPost("foto")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> SubirFoto(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Sin archivo");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await archivo.CopyToAsync(stream);

            return Ok(new { url = $"/uploads/{fileName}" });
        }
    }
}