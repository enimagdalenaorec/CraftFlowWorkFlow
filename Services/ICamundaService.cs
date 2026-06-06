using Camunda.Api.Client;

namespace CraftFlowWorkFlow.Services
{
    public interface ICamundaService
    {
        CamundaClient Client { get; }
    }
}
