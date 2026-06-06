using Camunda.Api.Client;

namespace CraftFlowWorkFlow.Services
{
    public class CamundaService : ICamundaService
    {
        public CamundaClient Client { get; }

        public CamundaService()
        {
            Client = CamundaClient.Create("http://localhost:8080/engine-rest");
        }
    }
}
