using Microsoft.AspNetCore.Mvc;
using SistemaUniversitarioWeb.Models;
using System.Diagnostics;
using System.Data.SQLite;
using Dapper;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace SistemaUniversitarioWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        string connectionString = "Data Source=|DataDirectory|universidad.db";


        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public ActionResult Pregrados()
        {
            return View();
        }

        public IActionResult IngenieriaCivil()
        {
            ViewBag.NombreCarrera = "Ingeniería Civil";
            return View("Carrera");
        }

        public IActionResult Mecatronica()
        {
            ViewBag.NombreCarrera = "Mecatrónica";
            return View("Carrera");
        }

        public IActionResult Derecho()
        {
            ViewBag.NombreCarrera = "Derecho";
            return View("Carrera");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public ActionResult Materias(string nombre, int? semestre, string carrera)
        {

            ViewBag.NombreCarrera = carrera ?? "Todas";
            using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                // Cargar datos filtrados
                var query = new StringBuilder("SELECT * FROM Materias WHERE 1=1");

                if (!string.IsNullOrEmpty(nombre))
                {
                    query.Append(" AND Nombre = @Nombre");
                }

                if (semestre.HasValue)
                {
                    query.Append(" AND Semestre = @Semestre");
                }

                if (!string.IsNullOrEmpty(carrera))
                {
                    query.Append(" AND Carrera = @Carrera");
                }

                var parameters = new { Nombre = nombre, Semestre = semestre, Carrera = carrera };
                var materias = connection.Query<Materia>(query.ToString(), parameters);

                // Cargar opciones para los dropdowns
                ViewBag.NombresMaterias = new SelectList(connection.Query<string>("SELECT DISTINCT Nombre FROM Materias").ToList());
                ViewBag.Semestres = new SelectList(Enumerable.Range(1, 10));
                ViewBag.Carreras = new SelectList(connection.Query<string>("SELECT DISTINCT Carrera FROM Materias").ToList());


                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_TablaMaterias", materias);
                }
                else
                {
                    return View(materias);
                }
            }
        }

        [HttpPost]
        public IActionResult AgregarMateria(string Nombre, string Carrera, int? Semestre, string Codigo, string Horas)
        {
            if (string.IsNullOrEmpty(Nombre) || string.IsNullOrEmpty(Carrera) || !Semestre.HasValue || Semestre < 1 || Semestre > 10 || string.IsNullOrEmpty(Codigo) || string.IsNullOrEmpty(Horas))
            {
                // Puedes mostrar un mensaje de error si quieres
                TempData["Error"] = "Por favor completa todos los campos correctamente.";
                return RedirectToAction("Materias");
            }

            using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                string query = @"
            INSERT INTO Materias (Nombre, Carrera, Semestre, Codigo, Horas)
            VALUES (@Nombre, @Carrera, @Semestre, @Codigo, @Horas)";

                connection.Execute(query, new { Nombre, Carrera, Semestre, Codigo, Horas });
            }

            return RedirectToAction("Materias");
        }

        [HttpPost]
        public IActionResult EditarMateria(int id, string Nombre, string Carrera, int? Semestre, string Codigo, string Horas)
        {
            if (string.IsNullOrEmpty(Nombre) || string.IsNullOrEmpty(Carrera) || !Semestre.HasValue || Semestre < 1 || Semestre > 10 || string.IsNullOrEmpty(Codigo) || string.IsNullOrEmpty(Horas))
            {
                TempData["Error"] = "Por favor completa todos los campos correctamente.";
                return RedirectToAction("Materias");
            }

            using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                string query = @"
            UPDATE Materias
            SET Nombre = @Nombre,
                Carrera = @Carrera,
                Semestre = @Semestre,
                Codigo = @Codigo,
                Horas = @Horas
            WHERE Id = @Id";

                connection.Execute(query, new { Id = id, Nombre, Carrera, Semestre, Codigo, Horas });
            }

            TempData["Exito"] = "Materia actualizada exitosamente.";
            return RedirectToAction("Materias");
        }

        [HttpPost]
        public IActionResult EliminarMateria(int id)
        {
            using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                string query = "DELETE FROM Materias WHERE Id = @Id";
                connection.Execute(query, new { Id = id });
            }

            TempData["Exito"] = "Materia eliminada exitosamente.";
            return RedirectToAction("Materias");
        }

        public IActionResult Horarios(string carrera = null, int? semestre = null, string materia = null)
        {
            using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                var query = new StringBuilder(@"
            SELECT 
                m.Nombre AS Materia,
                m.Codigo,
                m.Carrera,
                m.Semestre,
                h.Grupo,
                h.CantEstudiantes,
                d.Nombre || ' ' || d.Apellido AS Docente,
                dm.Id AS DocenteMateriaId,
                h.Dia,
                h.HoraInicio,
                h.HoraFin,
                h.Id
            FROM Horarios h
            INNER JOIN Materias m ON h.MateriaId = m.Id
            INNER JOIN DocenteMateria dm ON m.Id = dm.MateriaId
            INNER JOIN Docente d ON dm.DocenteId = d.Id
            WHERE 1=1");

                if (!string.IsNullOrEmpty(carrera))
                {
                    query.Append(" AND m.Carrera = @Carrera");
                }

                if (semestre.HasValue)
                {
                    query.Append(" AND m.Semestre = @Semestre");
                }

                if (!string.IsNullOrEmpty(materia))
                {
                    query.Append(" AND m.Nombre = @Materia");
                }

                var parameters = new { Carrera = carrera, Semestre = semestre, Materia = materia };
                var horarios = connection.Query<HorarioVM>(query.ToString(), parameters);

                // Cargar listas para dropdowns
                ViewBag.Materias = new SelectList(connection.Query<string>("SELECT DISTINCT Nombre FROM Materias").ToList());
                ViewBag.Carreras = new SelectList(connection.Query<string>("SELECT DISTINCT Carrera FROM Materias").ToList());
                ViewBag.Codigos = new SelectList(connection.Query<string>("SELECT DISTINCT Codigo FROM Materias").ToList());
                ViewBag.Docentes = new SelectList(connection.Query<string>("SELECT DISTINCT Nombre || ' ' || Apellido AS NombreCompleto FROM Docente").ToList());
                ViewBag.Semestres = new SelectList(Enumerable.Range(1, 10));

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_TablaHorarios", horarios);
                }
                else
                {
                    return View(horarios);
                }
            }
        }


        [HttpPost]
        public IActionResult EditarHorario(int Id, int Grupo, int CantEstudiantes, string Dia, string HoraInicio, string HoraFin, int DocenteId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();

                    // Obtener MateriaId desde el horario
                    var materiaId = connection.QueryFirstOrDefault<int>(
                        "SELECT MateriaId FROM Horarios WHERE Id = @Id",
                        new { Id });

                    if (materiaId == 0)
                        throw new Exception("No se encontró la materia asociada.");

                    // 1. Actualizar Horario
                    var queryHorario = @"
                UPDATE Horarios
                SET Grupo = @Grupo,
                    CantEstudiantes = @CantEstudiantes,
                    Dia = @Dia,
                    HoraInicio = @HoraInicio,
                    HoraFin = @HoraFin
                WHERE Id = @Id";

                    connection.Execute(queryHorario, new { Id, Grupo, CantEstudiantes, Dia, HoraInicio, HoraFin });

                    // 2. Actualizar DocenteMateria si se cambió el docente
                    var docenteActual = connection.QueryFirstOrDefault<int>(
                        "SELECT DocenteId FROM DocenteMateria WHERE MateriaId = @MateriaId",
                        new { MateriaId = materiaId });

                    if (DocenteId > 0 && DocenteId != docenteActual)
                    {
                        var queryDocenteMateria = @"
                    UPDATE DocenteMateria
                    SET DocenteId = @DocenteId
                    WHERE MateriaId = @MateriaId";

                        connection.Execute(queryDocenteMateria, new { MateriaId = materiaId, DocenteId });
                    }

                    return Json(new { success = true, message = "Horario y docente actualizados exitosamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AgregarHorario(int MateriaId, int Grupo, int CantEstudiantes, int numDias,
    string Dia1, string HoraInicio1, string HoraFin1,
    string Dia2, string HoraInicio2, string HoraFin2,
    string Dia3, string HoraInicio3, string HoraFin3)
        {
            if (MateriaId <= 0 || Grupo <= 0 || CantEstudiantes <= 0 || numDias <= 0)
            {
                TempData["Error"] = "Por favor completa correctamente los campos.";
                return RedirectToAction("Horarios");
            }

            using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                // Insertar horario base
                string query = @"
            INSERT INTO Horarios (MateriaId, Grupo, CantEstudiantes, Dia, HoraInicio, HoraFin)
            VALUES (@MateriaId, @Grupo, @CantEstudiantes, @Dia, @HoraInicio, @HoraFin)";

                // Insertar cada día
                if (numDias >= 1 && !string.IsNullOrEmpty(Dia1) && !string.IsNullOrEmpty(HoraInicio1) && !string.IsNullOrEmpty(HoraFin1))
                {
                    connection.Execute(query, new { MateriaId, Grupo, CantEstudiantes, Dia = Dia1, HoraInicio = HoraInicio1, HoraFin = HoraFin1 });
                }

                if (numDias >= 2 && !string.IsNullOrEmpty(Dia2) && !string.IsNullOrEmpty(HoraInicio2) && !string.IsNullOrEmpty(HoraFin2))
                {
                    connection.Execute(query, new { MateriaId, Grupo, CantEstudiantes, Dia = Dia2, HoraInicio = HoraInicio2, HoraFin = HoraFin2 });
                }

                if (numDias == 3 && !string.IsNullOrEmpty(Dia3) && !string.IsNullOrEmpty(HoraInicio3) && !string.IsNullOrEmpty(HoraFin3))
                {
                    connection.Execute(query, new { MateriaId, Grupo, CantEstudiantes, Dia = Dia3, HoraInicio = HoraInicio3, HoraFin = HoraFin3 });
                }
            }

            TempData["Exito"] = "Horario agregado exitosamente.";
            return RedirectToAction("Horarios");
        }

        [HttpPost]
        public IActionResult EliminarHorario(int id)
        {
            using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                string query = "DELETE FROM Horarios WHERE Id = @Id";
                connection.Execute(query, new { Id = id });
            }

            TempData["Exito"] = "Horario eliminado exitosamente.";
            return RedirectToAction("Horarios");
        }

    }


}
