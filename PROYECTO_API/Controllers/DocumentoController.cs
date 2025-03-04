using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using PROYECTO_API.Models;
using Dapper;


namespace PROYECTO_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentoController : ControllerBase
    {
        private readonly string _rutaServidor;
        private readonly string _cadenaSQL;
        private readonly IConfiguration _configuration;
        private IConfiguration configuration;

        public DocumentoController (IConfiguration config){
            _rutaServidor = config.GetSection("Configuracion").GetSection("RutaServidor").Value;
            _cadenaSQL = config.GetConnectionString("CadenaSQL");
            _configuration = configuration;
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




        [HttpGet("ObtenerPorDescripcion")]
        public IActionResult ObtenerDocumentosPorDescripcion(string descripcion)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_cadenaSQL))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_obtener_documentos_por_descripcion", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@descripcion", descripcion);


                        SqlDataReader reader = cmd.ExecuteReader();
                        List<object> documentos = new List<object>();

                        while (reader.Read())
                        {
                            documentos.Add(new
                            {
                                Descripcion = reader["Descripcion"].ToString(),
                                Ruta = reader["Ruta"].ToString()
                            });
                        }

                        return Ok(documentos);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }




        [HttpGet("archivos/{nombreArchivo}")]
        public IActionResult ObtenerArchivo(string nombreArchivo)
        {
            try
            {
                string rutaCarpeta = @"C:\ProgramasVisual\Archivos\"; // Ruta donde guardas los archivos
                string rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

                if (!System.IO.File.Exists(rutaCompleta))
                {
                    return NotFound(new { mensaje = "Archivo no encontrado" });
                }

                string tipoMime = "application/octet-stream"; // Tipo MIME por defecto
                string extension = Path.GetExtension(rutaCompleta).ToLower();

                // Asignar tipos MIME comunes
                var tiposMime = new Dictionary<string, string>
        {
            { ".pdf", "application/pdf" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".jpg", "image/jpeg" },
            { ".png", "image/png" }
        };

                if (tiposMime.ContainsKey(extension))
                {
                    tipoMime = tiposMime[extension];
                }

                var archivoBytes = System.IO.File.ReadAllBytes(rutaCompleta);
                return File(archivoBytes, tipoMime);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }







    }
}
