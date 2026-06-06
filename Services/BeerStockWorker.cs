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
                    var externalTasks = await _camundaService.Client.ExternalTasks
                        .FetchAndLock(new FetchExternalTasks
                        {
                            WorkerId = "beer-worker-1",
                            MaxTasks = 5, // Povećano na 5 da može pokupiti više različitih zadataka odjednom
                            Topics = new List<FetchExternalTaskTopic>
                            {
                                // 1. Tvoj postojeći topic za zalihe
                                new FetchExternalTaskTopic("check-beer-stock", 10_000)
                                {
                                    Variables = new List<string> { "stavkeJson" }
                                },
                                
                                // 2. NOVI TOPIC: Sada radnik sluša i tvoj drugi Service Task!
                                new FetchExternalTaskTopic("notify-customer", 10_000)
                            }
                        });

                    foreach (var task in externalTasks)
                    {
                        // --- AKO JE STIGAO ZADATAK ZA PROVJERU ZALIHA ---
                        if (task.TopicName == "check-beer-stock")
                        {
                            bool isAvailable = true;

                            if (task.Variables.ContainsKey("stavkeJson"))
                            {
                                string json = task.Variables["stavkeJson"].Value.ToString();
                                var stavke = JsonSerializer.Deserialize<List<NarudzbaStavka>>(json);

                                if (stavke != null)
                                {
                                    foreach (var stavka in stavke)
                                    {
                                        if (stavka.Kolicina > 10)
                                        {
                                            isAvailable = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            var resultVariables = new Dictionary<string, VariableValue>
                            {
                                { "available", VariableValue.FromObject(isAvailable) }
                            };

                            await _camundaService.Client.ExternalTasks[task.Id].Complete(new CompleteExternalTask
                            {
                                WorkerId = "beer-worker-1",
                                Variables = resultVariables
                            });

                            Console.WriteLine("[Worker]: Provjera zaliha završena.");
                        }

                        // --- AKO JE STIGAO ZADATAK ZA OBAVIJEST O ODBIJANJU ---
                        else if (task.TopicName == "notify-customer")
                        {
                            // Sustav ovdje odrađuje automatiku (npr. slanje maila, logiranje)
                            Console.WriteLine($"[AUTOMATIKA - Worker]: Kupac je uspješno obaviješten u pozadini da nema dovoljno zaliha.");

                            // Odmah automatski javljamo Camundi da je zadatak gotov, bez ikakvog klika na sučelju!
                            await _camundaService.Client.ExternalTasks[task.Id].Complete(new CompleteExternalTask
                            {
                                WorkerId = "beer-worker-1"
                            });

                            Console.WriteLine("[Worker]: Obavijest poslana, proces gurnut do kraja.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Worker Greška]: {ex.Message}");
                }

                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}