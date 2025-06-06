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

namespace SistemaUniversitarioWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        string connectionString = "Data Source=|DataDirectory|universidad.db";


        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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

        public ActionResult IngenieriaCivil()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public ActionResult Materias()
        {
            var materias = new List<Materia>();
            string connectionString = "Data Source=|DataDirectory|universidad.db;Version=3;";



            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Materias WHERE Carrera = 'Ingenieria civil'";

                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        materias.Add(new Materia
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString(),
                            Carrera = reader["Carrera"].ToString(),
                            Semestre = Convert.ToInt32(reader["Semestre"]),
                            Codigo = reader["Codigo"].ToString(),
                            Horas = reader["Horas"].ToString()
                        });
                    }
                }
            }

            return View(materias);
        }




    }


}
