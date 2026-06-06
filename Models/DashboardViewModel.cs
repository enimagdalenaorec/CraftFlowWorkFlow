using System.Collections.Generic;
using Camunda.Api.Client.UserTask;

namespace CraftFlowWorkFlow.Models
{
    public class DashboardViewModel
    {
        public List<UserTaskInfo> AktivniZadaci { get; set; } = new List<UserTaskInfo>();
    }
}