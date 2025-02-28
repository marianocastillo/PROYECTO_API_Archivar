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

    }
}
