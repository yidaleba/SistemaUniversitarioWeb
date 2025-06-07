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
        public IActionResult EditarHorario(int Id, int Semestre, int Grupo, int CantEstudiantes, string Dia, string HoraInicio, string HoraFin)
        {
            try
            {
                using (var connection = new SQLiteConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();

                    // Mensaje para ver qué datos estamos recibiendo
                    System.Diagnostics.Debug.WriteLine($"Editando Horario ID={Id}, Datos: {Dia}, {HoraInicio}, {HoraFin}");

                    var query = @"
                UPDATE Horarios
                SET 
                    Dia = @Dia,
                    Semestre = @Semestre,
                    Grupo = @Grupo,
                    CantEstudiantes = @CantEstudiantes,
                    HoraInicio = @HoraInicio,
                    HoraFin = @HoraFin
                WHERE Id = @Id";

                    var rowsAffected = connection.Execute(query, new { Id, Semestre, Grupo, CantEstudiantes, Dia, HoraInicio, HoraFin });

                    if (rowsAffected == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("?? No se encontró ningún horario con ese Id");
                        TempData["Error"] = "No se encontró el horario.";
                    }
                    else
                    {
                        TempData["Exito"] = "Horario actualizado exitosamente.";
                    }

                    return Json(new { success = rowsAffected > 0 });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("? Error: " + ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
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
