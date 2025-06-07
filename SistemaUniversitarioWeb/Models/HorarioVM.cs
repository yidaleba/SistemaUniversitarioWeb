namespace SistemaUniversitarioWeb.Models
{
    public class HorarioVM
    {
        public int Id { get; set; }
        public string Materia { get; set; }
        public string Codigo { get; set; }
        public string Carrera { get; set; }
        public int Semestre { get; set; }
        public int Grupo { get; set; }
        public int CantEstudiantes { get; set; }
        public string Docente { get; set; }
        public string Dia { get; set; }
        public string HoraInicio { get; set; }
        public string HoraFin { get; set; }
        public int DocenteMateriaId { get; set; }

        public List<string> MateriasDisponibles { get; set; } = new List<string>();
        public List<string> CarrerasDisponibles { get; set; } = new List<string>();
        public List<string> DocentesDisponibles { get; set; } = new List<string>();
    }
}