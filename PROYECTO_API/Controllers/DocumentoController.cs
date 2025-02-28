using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using PROYECTO_API.Models;


namespace PROYECTO_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentoController : ControllerBase
    {
        private readonly string _rutaServidor;
        private readonly string _cadenaSQL;

        public DocumentoController (IConfiguration config){
            _rutaServidor = config.GetSection("Configuracion").GetSection("RutaServidor").Value;
            _cadenaSQL = config.GetConnectionString("CadenaSQL");
        }

        [HttpPost]
        [Route("Subir")]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueCountLimit = int.MaxValue)]
        public IActionResult Subir([FromForm] Documento request)
        {
            string rutaDocumento = Path.Combine(_rutaServidor, request.Archivo.FileName);

            try
            {
                using (FileStream newfile = System.IO.File.Create(rutaDocumento))
                {
                    request.Archivo.CopyTo(newfile);
                    newfile.Flush();
                }
                using (var conexion = new SqlConnection(_cadenaSQL))
                {
                    conexion.Open();
                    var cmd = new SqlCommand("sp_guardar_documento", conexion);
                    cmd.Parameters.AddWithValue("descripcion", request.Descripcion);
                    cmd.Parameters.AddWithValue("ruta", rutaDocumento);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }

                return StatusCode(StatusCodes.Status200OK, new { mensaje = "documento guardado" });
            }
            catch (Exception error)
            {
                return StatusCode(StatusCodes.Status200OK, new { mensaje = error.Message });
            }
        }




        [HttpGet]
        [Route("Obtener/{id}")]
        public IActionResult Obtener(int id)
        {
            try
            {
                using (var conexion = new SqlConnection(_cadenaSQL))
                {
                    conexion.Open();
                    var cmd = new SqlCommand("sp_obtener_documento_por_id", conexion);
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.CommandType = CommandType.StoredProcedure;

                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var urlArchivo = reader["Ruta"]?.ToString(); // Suponiendo que la ruta está en la columna "Ruta"
                        if (urlArchivo != null)
                        {
                            return Ok(new { url = urlArchivo });
                        }
                        else
                        {
                            return NotFound(new { mensaje = "Documento no encontrado" });
                        }
                    }

                    return NotFound(new { mensaje = "Documento no encontrado" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }


    }
}
