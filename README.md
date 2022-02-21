# Tech specs (from customer specs below)

## Stack

* not .Net Core, let's use .Net 6 :)
* Web API
* sqlite => let's try Microsoft.Data.Sqlite
* docker => I'll put a dockerfile and a github ci to build this docker :)

## Design

* one class TemperatureCaptor (returns temp)
* one api GET SensorStatus = Hot/Cold/Warm
* one api GET History = IEnumerable<SensorStatus>
** implies GET SensorStatus will store the result (sqlite)
* one api POST SensorStatusLimit { Hot = float , Cold = float } + validation

> SensorService will return SensorStatus

> there is something not 100% clear: 
* should we store the temperature, and be able to return the history of the status with the current rules
* should we store the status, and if the rules change in the future, we'll return the status calculated on past rules
I choose to store temperature, but in the real world, I would ask the customer :)

The specs don't tell us what to do when there is more than 15 values of history. Let's not purge them. We can ask customer and do whatever he wants later :)

DONE:
* CI (build & tests)

I believe I'm done with those specs, but I think there is still a couple of things to refactor:
1) I'm retrieving the thresholds each time I calculate the status, which is fine for one status, but not great for history
2) I could have used also a value wrapper for the temperature to not manipulate a primitive directly
3) build should also: watermark (with version + git sha), code coverage, code quality
4) have renovate or equivalent
5) improve swagger (ProduceResponseType, comments, ...)
6) add /metrics, /health (db check & captor check)
7) add logs in key places

# Customer Specs

Objectifs:

- Voir comment tu codes dans la vrai vie :D
- Faire une PR sur la main pour qu'on puisse te faire des retours

Dans le cadre de ce projet, nous souhaitons avoir une api :
1°) Je veux que mon sensor récupère la température provenant du composant TemperatureCaptor (renvoi la température en °C)<br/>
2°) Je veux que l'état de mon Sensor soit à "HOT" lorsque la température captée est suppérieure ou égale a 40 °C.<br/>
3°) Je veux l'état de mon Sensor soit à "COLD" lorsque la température captée est inferieur a 22 °C.<br/>
4°) Je veux l'état de mon Sensor soit à "WARM" lorsque la température captée est entre 22 et inferieur à 40 °C.<br/>
5°) Je veux récuperer l'historique des 15 dernieres demandes des températures.<br/>
6°) Je veux pouvoir redefinir les limites pour "HOT", "COLD", "WARM"<br/><br/>

Stack mandatory: .NET CORE, SQL Lite, docker
