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

const BASE_URL = `${import.meta.env.VITE_API_BASE_URL}api/`;

export class EventStreamsService {
  async getEventStreams(
    url: string | undefined = undefined
  ): Promise<Page<EventStream>> {
    if (!url) {
      url = BASE_URL;
    } else {
      if (!url.startsWith(BASE_URL)) {
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

  async getEvents(
    eventStreamName: string,
    streamId: string
  ): Promise<Page<Event>> {
    return this.getEventsWithUrl(
      `${BASE_URL}${encodeURIComponent(eventStreamName)}/${encodeURIComponent(
        streamId
      )}/events`
    );
  }

  async getEventsWithUrl(url: string): Promise<Page<Event>> {
    if (!url.startsWith(BASE_URL)) {
      throw new Error("invalid url");
    }

    const response = await fetch(url);
    const jsonResponse = await response.json();

    return {
      data: jsonResponse.events as Event[],
      nextPage: jsonResponse?._links?.next?.href,
    };
  }

  async getEventBody(
    eventStreamName: string,
    streamId: string,
    eventId: string
  ): Promise<any> {
    const url = `${BASE_URL}${encodeURIComponent(
      eventStreamName
    )}/${encodeURIComponent(streamId)}/events/${eventId}/body`;

    const response = await fetch(url);
    return await response.json();
  }
}
