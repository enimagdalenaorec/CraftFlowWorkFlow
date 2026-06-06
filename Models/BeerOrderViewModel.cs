namespace CraftFlowWorkFlow.Models
{
    public class BeerOrderViewModel
    {
        public string IdProcesa { get; set; } = string.Empty; 
        public string ImeKupca { get; set; } = string.Empty;
        public List<NarudzbaStavka> Stavke { get; set; } = new List<NarudzbaStavka>();
    }
}
