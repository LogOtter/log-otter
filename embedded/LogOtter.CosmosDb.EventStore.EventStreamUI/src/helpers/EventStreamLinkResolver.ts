import type { EventStream } from "@/services/EventStreamsService";

export default function eventStreamLinkResolver(eventStream: EventStream): string {
  return eventStream.serviceName ? `#/service/${eventStream.serviceName}/${eventStream.name}` : `#/${eventStream.name}`;
}
