using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComercioController : ControllerBase
    {
        private readonly string _filePathList;
        private readonly string _uploadsPath;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public ComercioController(IWebHostEnvironment env)
        {
            _filePathList = Path.Combine(env.WebRootPath, "data", "comercios.json");
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        }

        private List<Comercio> LoadComercios()
        {
            if (!System.IO.File.Exists(_filePathList))
                return new List<Comercio>();
            var json = System.IO.File.ReadAllText(_filePathList);
            if (string.IsNullOrWhiteSpace(json))
                return new List<Comercio>();
            var lista = JsonSerializer.Deserialize<List<Comercio>>(json, JsonOptions);
            return lista ?? new List<Comercio>();
        }

        private void SaveComercios(List<Comercio> comercios)
        {
            var folder = Path.GetDirectoryName(_filePathList);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            var json = JsonSerializer.Serialize(comercios ?? new List<Comercio>(), JsonOptions);
            System.IO.File.WriteAllText(_filePathList, json);
        }

        private Comercio? GetComercioPrincipal(List<Comercio> lista)
        {
            if (lista == null || lista.Count == 0) return null;
            var c1 = lista.FirstOrDefault(x => x.Id == 1);
            return c1 ?? lista[0];
        }

        private static int NextId(List<Comercio> lista)
        {
            if (lista == null || lista.Count == 0) return 1;
            return lista.Max(x => x.Id) + 1;
        }

        private static (double lat, double lng) ExtraerCoordenadas(string ubicacion)
        {
            if (string.IsNullOrWhiteSpace(ubicacion))
                return (0, 0);
            var match = System.Text.RegularExpressions.Regex.Match(
                ubicacion, @"(-?\d+\.\d+),(-?\d+\.\d+)");
            if (match.Success &&
                double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                return (lat, lng);
            }
            return (0, 0);
        }

        [HttpGet]
        public IActionResult Get()
        {
            var lista = LoadComercios();
            var comercio = GetComercioPrincipal(lista);
            if (comercio == null) return Ok(new Comercio());
            return Ok(comercio);
        }

        [HttpGet("todos")]
        public IActionResult GetTodos()
        {
            var lista = LoadComercios();
            return Ok(lista);
        }

        [HttpGet("rubro/{rubro}")]
        public IActionResult GetPorRubro(string rubro)
        {
            var lista = LoadComercios();
            var filtrados = lista
                .Where(x => (x.Rubro ?? "").ToLower() == rubro.ToLower())
                .ToList();
            return Ok(filtrados);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var lista = LoadComercios();
            var comercio = lista.FirstOrDefault(x => x.Id == id);
            if (comercio == null) return NotFound("No existe el comercio.");
            return Ok(comercio);
        }

        [HttpGet("token/{token}")]
        public IActionResult GetPorToken(string token)
        {
            var lista = LoadComercios();
            var comercio = lista.FirstOrDefault(x => x.Token == token);
            if (comercio == null) return NotFound("Token inválido");
            return Ok(comercio);
        }

        [HttpPut]
        public IActionResult Put([FromBody] Comercio comercioNuevo)
        {
            if (comercioNuevo == null) return BadRequest();
            var lista = LoadComercios();
            if (lista.Count == 0)
            {
                comercioNuevo.Id = comercioNuevo.Id > 0 ? comercioNuevo.Id : 1;
                lista.Add(comercioNuevo);
                SaveComercios(lista);
                return Ok(comercioNuevo);
            }
            var principal = GetComercioPrincipal(lista)!;
            var idx = lista.FindIndex(x => x.Id == principal.Id);
            if (idx < 0) idx = 0;
            principal.Nombre = comercioNuevo.Nombre;
            principal.Descripcion = comercioNuevo.Descripcion;
            principal.WhatsApp = new string((comercioNuevo.WhatsApp ?? "").Where(char.IsDigit).ToArray());
            principal.Instagram = string.IsNullOrWhiteSpace(comercioNuevo.Instagram)
                ? comercioNuevo.Instagram
                : comercioNuevo.Instagram.StartsWith("http")
                    ? comercioNuevo.Instagram
                    : "https://instagram.com/" + comercioNuevo.Instagram.Replace("@", "");
            principal.Ubicacion = comercioNuevo.Ubicacion;
            var coords = ExtraerCoordenadas(comercioNuevo.Ubicacion);
            principal.Latitud = coords.lat;
            principal.Longitud = coords.lng;
            principal.Correo = comercioNuevo.Correo;
            principal.Horarios = comercioNuevo.Horarios;
            principal.Rubro = comercioNuevo.Rubro;
            principal.Estado = comercioNuevo.Estado;
            lista[idx] = principal;
            SaveComercios(lista);
            return Ok(principal);
        }

        [HttpPut("{id:int}")]
        public IActionResult PutById(int id, [FromBody] Comercio comercioNuevo)
        {
            if (comercioNuevo == null) return BadRequest();
            var lista = LoadComercios();
            var comercio = lista.FirstOrDefault(x => x.Id == id);
            if (comercio == null) return NotFound("No existe el comercio.");
            comercio.Nombre = comercioNuevo.Nombre;
            comercio.Descripcion = comercioNuevo.Descripcion;
            comercio.WhatsApp = new string((comercioNuevo.WhatsApp ?? "").Where(char.IsDigit).ToArray());
            comercio.Instagram = comercioNuevo.Instagram;
            comercio.Ubicacion = comercioNuevo.Ubicacion;
            var coords = ExtraerCoordenadas(comercioNuevo.Ubicacion);
            comercio.Latitud = coords.lat;
            comercio.Longitud = coords.lng;
            comercio.Correo = comercioNuevo.Correo;
            comercio.Horarios = comercioNuevo.Horarios;
            comercio.Rubro = comercioNuevo.Rubro;
            comercio.Estado = comercioNuevo.Estado;
            SaveComercios(lista); comercio.Rubro = comercioNuevo.Rubro;
            comercio.Estado = comercioNuevo.Estado;
            comercio.Latitud = comercioNuevo.Latitud;
            comercio.Longitud = comercioNuevo.Longitud;
            SaveComercios(lista);
            return Ok(comercio);
            
        }

        [HttpPost("crear")]
        public IActionResult Crear([FromBody] Comercio nuevo)
        {
            if (nuevo == null) return BadRequest();
            var lista = LoadComercios();
            if (nuevo.Id <= 0)
                nuevo.Id = NextId(lista);
            if (lista.Any(x => x.Id == nuevo.Id))
                return BadRequest("Ya existe un comercio con ese Id.");
            nuevo.WhatsApp = new string((nuevo.WhatsApp ?? "").Where(char.IsDigit).ToArray());
            if (!string.IsNullOrWhiteSpace(nuevo.Instagram) && !nuevo.Instagram.StartsWith("http"))
                nuevo.Instagram = "https://instagram.com/" + nuevo.Instagram.Replace("@", "");
            nuevo.Ubicacion = nuevo.Ubicacion;
            var coords = ExtraerCoordenadas(nuevo.Ubicacion);
            nuevo.Latitud = coords.lat;
            nuevo.Longitud = coords.lng;
            lista.Add(nuevo);
            SaveComercios(lista);
            return Ok(nuevo);
        }

        [HttpPost("{id:int}/portada")]
        public async Task<IActionResult> SubirPortadaPorId(int id, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Archivo inválido");
            if (!Directory.Exists(_uploadsPath))
                Directory.CreateDirectory(_uploadsPath);
            var extension = Path.GetExtension(archivo.FileName).ToLower();
            var esImagen = extension == ".jpg" || extension == ".jpeg" ||
                extension == ".png" || extension == ".webp" || extension == ".jfif";
            var esVideo = extension == ".mp4";
            if (!esImagen && !esVideo)
                return BadRequest("Solo se permiten imágenes o videos MP4");
            var lista = LoadComercios();
            var comercio = lista.FirstOrDefault(x => x.Id == id);
            if (comercio == null) return NotFound("No existe el comercio.");
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaCompleta = Path.Combine(_uploadsPath, nombreArchivo);
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                await archivo.CopyToAsync(stream);
            comercio.PortadaUrl = $"/uploads/{nombreArchivo}";
            comercio.PortadaTipo = esImagen ? "imagen" : "video";
            SaveComercios(lista);
            return Ok(comercio);
        }

        [HttpPost("{id:int}/logo")]
        public async Task<IActionResult> SubirLogoPorId(int id, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Archivo inválido");
            if (!Directory.Exists(_uploadsPath))
                Directory.CreateDirectory(_uploadsPath);
            var extension = Path.GetExtension(archivo.FileName).ToLower();
            var esImagen = extension == ".jpg" || extension == ".jpeg" ||
               extension == ".png" || extension == ".webp" || extension == ".jfif";
            if (!esImagen)
                return BadRequest("El logo debe ser una imagen");
            var lista = LoadComercios();
            var comercio = lista.FirstOrDefault(x => x.Id == id);
            if (comercio == null) return NotFound("No existe el comercio.");
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaCompleta = Path.Combine(_uploadsPath, nombreArchivo);
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                await archivo.CopyToAsync(stream);
            comercio.LogoUrl = $"/uploads/{nombreArchivo}";
            SaveComercios(lista);
            return Ok(comercio);
        }
        [HttpPut("{id:int}/destacado")]
        public IActionResult ToggleDestacado(int id, [FromBody] bool esDestacado)
        {
            var lista = LoadComercios();
            var comercio = lista.FirstOrDefault(x => x.Id == id);
            if (comercio == null) return NotFound();
            comercio.EsDestacado = esDestacado;
            SaveComercios(lista);
            return Ok(comercio);
        }

        [HttpGet("destacados")]
        public IActionResult GetDestacados()
        {
            var lista = LoadComercios();
            var destacados = lista.Where(x => x.EsDestacado).ToList();
            return Ok(destacados);
        }
    }

}