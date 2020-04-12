import { Component, Inject } from '@angular/core';
import { WeatherForecast, WeatherForecastHubClient, AngularFrontendInstance } from '../hub-backend';

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public forecasts: WeatherForecast[];

  constructor(hub: WeatherForecastHubClient,
    hubEvents: AngularFrontendInstance) {
    hubEvents.onNewWeatherForecast.subscribe((weatherForecast: WeatherForecast) => {
      console.log(weatherForecast);
    });
    hub.get().then(result => {
      this.forecasts = result;
    }, error => console.error(error));
  }
}
