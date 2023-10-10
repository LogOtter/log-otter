export interface EventStream {
  name: string;
  typeName: string;
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

    if (!this.baseUrl.endsWith("/")) {
      this.baseUrl += "/";
    }
  }

  async getEventStreams(url: string | undefined = undefined): Promise<Page<EventStream>> {
    if (!url) {
      url = `${this.baseUrl}event-streams/`;
    } else {
      if (!url.startsWith(this.baseUrl)) {
        throw new Error("invalid url");
      }
    }

    const response = await fetch(url);
    const jsonResponse = await response.json();

    return {
      data: jsonResponse.definitions as EventStream[],
      nextPage: jsonResponse?._links?.next?.href,
    };
  }

  async getEvents(eventStreamName: string, streamId: string): Promise<Page<Event>> {
    return this.getEventsWithUrl(
      `${this.baseUrl}event-streams/${encodeURIComponent(eventStreamName)}/streams/${encodeURIComponent(streamId)}/events`,
    );
  }

  async getEventsWithUrl(url: string): Promise<Page<Event>> {
    if (!url.startsWith(this.baseUrl)) {
      throw new Error("invalid url");
    }

    const response = await fetch(url);
    const jsonResponse = await response.json();

    return {
      data: jsonResponse.events as Event[],
      nextPage: jsonResponse?._links?.next?.href,
    };
  }

  async getEventBody(eventStreamName: string, streamId: string, eventId: string): Promise<any> {
    const url = `${this.baseUrl}event-streams/${encodeURIComponent(eventStreamName)}/streams/${encodeURIComponent(streamId)}/events/${eventId}/body`;

    const response = await fetch(url);
    return await response.json();
  }

  async getVersion(): Promise<Version> {
    const url = `${this.baseUrl}version`;

    const response = await fetch(url);
    return await response.json();
  }
}
