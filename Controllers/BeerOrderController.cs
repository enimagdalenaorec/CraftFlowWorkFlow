using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Camunda.Api.Client;
using Camunda.Api.Client.ProcessDefinition;
using Camunda.Api.Client.UserTask;
using Camunda.Api.Client.Message;
using CraftFlowWorkFlow.Models;
using CraftFlowWorkFlow.Services;

namespace CraftFlowWorkFlow.Controllers
{
    public class BeerOrderController : Controller
    {
        private readonly ICamundaService _camundaService;
        private const string ProcessKey = "NarudzbaPiva";

        public BeerOrderController(ICamundaService camundaService)
        {
            _camundaService = camundaService;
        }

        // ==========================================
        // 1. FORMA: NOVA NARUDŽBA (Kupac)
        // ==========================================
        [HttpGet]
        public IActionResult NovaNarudzba()
        {
            var model = new BeerOrderViewModel();
            model.Stavke.Add(new NarudzbaStavka { VrstaPiva = "Lager", Kolicina = 1 });
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> NovaNarudzba(BeerOrderViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ImeKupca))
            {
                ModelState.AddModelError("", "Ime kupca je obavezno.");
                return View(model);
            }

            model.Stavke.RemoveAll(s => string.IsNullOrWhiteSpace(s.VrstaPiva) || s.Kolicina <= 0);

            if (model.Stavke.Count == 0)
            {
                ModelState.AddModelError("", "Morate dodati barem jedno pivo u košaricu.");
                return View(model);
            }

            try
            {
                string stavkeJson = JsonSerializer.Serialize(model.Stavke);

                var variables = new Dictionary<string, VariableValue>
                {
                    { "imeKupca", VariableValue.FromObject(model.ImeKupca) },
                    { "stavkeJson", VariableValue.FromObject(stavkeJson) }
                };

                string uniqueBusinessKey = "NAR-" + Guid.NewGuid().ToString().Substring(0, 8);

                // POPRAVLJENO prema linijama 66 i 67 s tvoje slike grešaka
                var result = await _camundaService.Client.ProcessDefinitions.ByKey(ProcessKey)
                    .StartProcessInstance(new StartProcessInstance
                    {
                        Variables = variables,
                        BusinessKey = uniqueBusinessKey
                    });

                TempData["SuccessMessage"] = $"Narudžba {uniqueBusinessKey} je poslana! Sustav provjerava zalihe.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Greška pri komunikaciji s Camundom: {ex.Message}");
                return View(model);
            }
        }

        // ==========================================
        // DASHBOARD: CENTRALNI PREGLED ZADATAKA
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // PROMIJENJENO: Iz UserTaskQuery() u TaskQuery()
                var tasks = await _camundaService.Client.UserTasks.Query(new TaskQuery()).List();
                ViewBag.AktivniZadaci = tasks;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Nije moguće dohvatiti zadatke: {ex.Message}. Je li Docker pokrenut?";
                ViewBag.AktivniZadaci = new List<UserTaskInfo>();
            }

            return View();
        }

        // ==========================================
        // 2. FORMA: POTVRDA NARUDŽBE (Radnik)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PotvrdiNarudzbu(string taskId)
        {
            var variables = await _camundaService.Client.UserTasks[taskId].Variables.GetAll();

            var model = new PotvrdaNarudzbeViewModel
            {
                TaskId = taskId,
                ImeKupca = variables.ContainsKey("imeKupca") ? variables["imeKupca"].Value.ToString() : "Nepoznato"
            };

            if (variables.ContainsKey("stavkeJson"))
            {
                string json = variables["stavkeJson"].Value.ToString();
                model.Stavke = JsonSerializer.Deserialize<List<NarudzbaStavka>>(json) ?? new List<NarudzbaStavka>();
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PotvrdiNarudzbu(PotvrdaNarudzbeViewModel model)
        {
            try
            {
                var variables = new Dictionary<string, VariableValue>
                {
                    { "approved", VariableValue.FromObject(model.Approved) }
                };

                // POPRAVLJENO prema liniji 145 s tvoje slike grešaka
                await _camundaService.Client.UserTasks[model.TaskId].Complete(new CompleteTask
                {
                    Variables = variables
                });

                TempData["SuccessMessage"] = model.Approved
                    ? "Narudžba odobrena! Korisnik sada ima 1 minutu za uplatu."
                    : "Narudžba je odbijena.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // ==========================================
        // 3. FORMA: SIMULACIJA UPLATE (Kupac)
        // ==========================================
        [HttpGet]
        public IActionResult Placanje(string businessKey)
        {
            ViewBag.BusinessKey = businessKey;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IzvrsiPlacanje(string businessKey, string brojKartice)
        {
            if (string.IsNullOrWhiteSpace(brojKartice))
            {
                TempData["ErrorMessage"] = "Broj kartice je obavezan za plaćanje.";
                return RedirectToAction("Placanje", new { businessKey });
            }

            try
            {
                await _camundaService.Client.Messages.DeliverMessage(new CorrelationMessage
                {
                    MessageName = "PaymentReceived",
                    BusinessKey = businessKey
                });

                TempData["SuccessMessage"] = $"Uplata za narudžbu {businessKey} uspješno poslana Camundi!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Uplata nije prošla (moguće da je isteklo 1 min): {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // ==========================================
        // 4. FORMA: POTVRDI ISPORUKU (Skladištar)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PotvrdiIsporuku(string taskId)
        {
            var variables = await _camundaService.Client.UserTasks[taskId].Variables.GetAll();

            var model = new IsporukaViewModel
            {
                TaskId = taskId,
                ImeKupca = variables.ContainsKey("imeKupca") ? variables["imeKupca"].Value.ToString() : "Nepoznato"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PotvrdiIsporuku(IsporukaViewModel model)
        {
            try
            {
                var variables = new Dictionary<string, VariableValue>
                {
                    { "imeDostavljaca", VariableValue.FromObject(model.ImeDostavljaca) },
                    { "brojOtpremnice", VariableValue.FromObject(model.BrojOtpremnice) }
                };

                // POPRAVLJENO prema liniji 227 s tvoje slike grešaka
                await _camundaService.Client.UserTasks[model.TaskId].Complete(new CompleteTask
                {
                    Variables = variables
                });

                TempData["SuccessMessage"] = "Narudžba uspješno otpremljena. Proces sretno završen!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }
    }
}