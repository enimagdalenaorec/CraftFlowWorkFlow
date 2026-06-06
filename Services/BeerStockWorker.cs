using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using CraftFlowWorkFlow.Models;
using CraftFlowWorkFlow.Services;

namespace CraftFlowWorkFlow.Services
{
    public class BeerStockWorker : BackgroundService
    {
        private readonly ICamundaService _camundaService;

        public BeerStockWorker(ICamundaService camundaService)
        {
            _camundaService = camundaService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // POPRAVLJENO: Točan naziv klase je FetchExternalTasks, a ne FetchAndLockTasks
                    var externalTasks = await _camundaService.Client.ExternalTasks
                        .FetchAndLock(new FetchExternalTasks
                        {
                            WorkerId = "beer-worker-1",
                            MaxTasks = 1,
                            // POPRAVLJENO: Točan naziv klase je FetchExternalTaskTopic
                            Topics = new List<FetchExternalTaskTopic>
                            {
                                new FetchExternalTaskTopic("check-beer-stock", 10_000)
                                {
                                    Variables = new List<string> { "stavkeJson" }
                                }
                            }
                        });

                    foreach (var task in externalTasks)
                    {
                        bool isAvailable = true;

                        if (task.Variables.ContainsKey("stavkeJson"))
                        {
                            string json = task.Variables["stavkeJson"].Value.ToString();

                            // POPRAVLJENO: Dodan gornji using System.Text.Json i CraftFlowWorkFlow.Models
                            var stavke = JsonSerializer.Deserialize<List<NarudzbaStavka>>(json);

                            if (stavke != null)
                            {
                                foreach (var stavka in stavke)
                                {
                                    // Simulacija provjere zaliha: Ako netko naruči više od 10 gajbi, odbijamo
                                    if (stavka.Kolicina > 10)
                                    {
                                        isAvailable = false;
                                        break;
                                    }
                                }
                            }
                        }

                        // Pripremamo rezultat za Camundu
                        var resultVariables = new Dictionary<string, VariableValue>
                        {
                            { "available", VariableValue.FromObject(isAvailable) }
                        };

                        // POPRAVLJENO: Točan naziv klase je CompleteExternalTask
                        await _camundaService.Client.ExternalTasks[task.Id].Complete(new CompleteExternalTask
                        {
                            WorkerId = "beer-worker-1",
                            Variables = resultVariables
                        });
                    }
                }
                catch (Exception ex)
                {
                    // U slučaju greške pri spajanju na Camundu (npr. ako Docker još nije pokrenut), samo ispiši u konzolu
                    Console.WriteLine($"[Worker Greška]: {ex.Message}");
                }

                // Radnik provjerava Camundu svake 3 sekunde
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}