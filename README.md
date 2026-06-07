# Upute za pokretanje projekta

## 1. Pokretanje Camunde u Dockeru

Koristi se verzija Camunda Platform 7 Community Edition. Docker image za Camundu dostupan je na:

```text
https://hub.docker.com/r/camunda/camunda-bpm-platform/
```

Prvo je potrebno povući Docker image:

```bash
docker pull camunda/camunda-bpm-platform:latest
```

Zatim pokrenuti:

```bash
docker run -d --name camunda -p 8080:8080 camunda/camunda-bpm-platform:latest
```

Nakon pokretanja, Camunda je dostupna u pregledniku na adresi:

```text
http://localhost:8080/camunda-welcome/index.html
```

## 2. Deploy BPMN dijagrama u Camundu

Nakon što je Camunda pokrenuta, potrebno je deployati BPMN dijagram.

Koraci:

1. Otvoriti **Camunda Modeler**.
2. Otvoriti BPMN datoteku projekta koja se nalazi u BPMN folderu.
3. Kliknuti na gumb:

```text
Deploy current diagram
```

5. Kao endpoint za deploy upisati:

```text
http://localhost:8080/engine-rest
```

6. Kliknuti **Deploy**.

Ako je deploy uspješan, BPMN proces je registriran u Camunda engineu.

## 3. Pokretanje .NET projekta 

1. Povući i otvoriti .NET projekt u Visual Studiju.
2. Pokrenuti .NET aplikaciju.

