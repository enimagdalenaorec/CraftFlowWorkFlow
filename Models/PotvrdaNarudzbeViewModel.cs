namespace CraftFlowWorkFlow.Models
{
    public class PotvrdaNarudzbeViewModel
    {
        public string TaskId { get; set; } = string.Empty; 
        public string ProcessInstanceId { get; set; } = string.Empty;
        public string ImeKupca { get; set; } = string.Empty;

        public List<NarudzbaStavka> Stavke { get; set; } = new List<NarudzbaStavka>();

        public bool Approved { get; set; }
    }
}
