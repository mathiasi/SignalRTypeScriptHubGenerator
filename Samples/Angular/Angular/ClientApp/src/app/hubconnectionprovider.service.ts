import { Injectable } from '@angular/core';
import { HubConnection } from '@microsoft/signalr';
import * as signalR from '@microsoft/signalr';


@Injectable({
  providedIn: 'root',
})
export class HubConnectionProvider {
  private hubConnections = new Map<string, Promise<HubConnection>>();
  getHubConnection(hubPattern: string): Promise<HubConnection> {

    if (this.hubConnections.has(hubPattern)) {
      return this.hubConnections.get(hubPattern);
    } else {
      const hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubPattern)
        .configureLogging(signalR.LogLevel.Trace)
        .build();

      hubConnection.stop();

      const prom = hubConnection.start()
        .then(() => {
          console.log(`HubConnection started: ${hubPattern}`);
          return hubConnection;
        })
        .catch(err => {
          console.error(err.toString());
          return err;
        });

      this.hubConnections.set(hubPattern, prom);
      return prom;
    }
  }
}
