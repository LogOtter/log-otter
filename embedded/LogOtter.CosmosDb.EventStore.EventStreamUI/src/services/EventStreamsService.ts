export interface EventStream {
  name: string;
  typeName: string;
  serviceName: string | undefined;
}

export interface Event {
  description: string;
  eventNumber: number;
  bodyType: string;
  timestamp: string;
  eventId: string;
}

export interface Page<T> {
  data: T[];
  nextPage: string | undefined;
}

export interface Version {
  packageVersion: string;
  apiVersion: number;
}

export class EventStreamsService {
  constructor(private baseUrl: string) {
    if (!baseUrl) {
      throw new Error("baseUrl required");
    }

    if (this.baseUrl.endsWith("/")) {
      this.baseUrl = this.baseUrl.substring(0, this.baseUrl.length - 1);
    }
  }

  async getEventStreams(url: string | undefined = undefined): Promise<Page<EventStream>> {
    if (!url) {
      url = `${this.baseUrl}/event-streams/`;
    } else {
      url = `${this.baseUrl}${url}`;
    }

    const response = await fetch(url);
    const jsonResponse = await response.json();

    return {
      data: jsonResponse.definitions as EventStream[],
      nextPage: jsonResponse?._links?.next?.href,
    };
  }

  async getEvents(serviceName: string | undefined, eventStreamName: string, streamId: string): Promise<Page<Event>> {
    return this.getEventsWithUrl(serviceName, `/event-streams/${encodeURIComponent(eventStreamName)}/streams/${encodeURIComponent(streamId)}/events`);
  }

  async getEventsWithUrl(serviceName: string | undefined, url: string): Promise<Page<Event>> {
    let baseUrl = this.baseUrl;

    if (serviceName) {
      baseUrl += `/service/${serviceName}`;
    }

    const response = await fetch(`${baseUrl}${url}`);
    const jsonResponse = await response.json();

    return {
      data: jsonResponse.events as Event[],
      nextPage: jsonResponse?._links?.next?.href,
    };
  }

  async getEventBody(serviceName: string | undefined, eventStreamName: string, streamId: string, eventId: string): Promise<any> {
    let baseUrl = this.baseUrl;

    if (serviceName) {
      baseUrl += `/service/${serviceName}`;
    }

    const url = `${baseUrl}/event-streams/${encodeURIComponent(eventStreamName)}/streams/${encodeURIComponent(streamId)}/events/${eventId}/body`;

    const response = await fetch(url);
    return await response.json();
  }

  async getVersion(): Promise<Version> {
    const url = `${this.baseUrl}/version`;

    const response = await fetch(url);
    return await response.json();
  }
}
