import { EventStreamsService } from "./EventStreamsService";

const HOST = `${import.meta.env.VITE_OVERRIDE_HOST}`;
const BASE_PATH = `${import.meta.env.VITE_OVERRIDE_BASE_PATH}`;

export class EventStreamsServiceFactory {
  private static config: { apiBaseUrl: string };

  public async create(): Promise<EventStreamsService> {
    if (!EventStreamsServiceFactory.config) {
      const response = await fetch(`${HOST}${BASE_PATH}config`);

      EventStreamsServiceFactory.config = await response.json();
    }

    const service = new EventStreamsService(HOST + EventStreamsServiceFactory.config.apiBaseUrl);

    return service;
  }
}
