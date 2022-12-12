<script lang="ts">
import StreamIdSearchPanel from "../components/StreamIdSearchPanel.vue";
import EventCard from "../components/EventCard.vue";
import type Event from "@/types/Event";

export default {
  components: {
    StreamIdSearchPanel,
    EventCard,
  },
  data() {
    return {
      events: [] as Event[],
    };
  },
  methods: {
    search(streamId: string) {
      window.location.hash = `#/${this.eventStreamName}/${encodeURIComponent(
        streamId
      )}`;
    },
    async fetchEvents() {
      const events: Event[] = [];

      let url = `${import.meta.env.VITE_API_BASE_URL}api/${
        this.eventStreamName
      }/${encodeURIComponent(this.streamId)}/events`;

      do {
        const response = await fetch(url);
        const jsonResponse = await response.json();
        for (const event of jsonResponse.events) {
          events.push(event);
        }
        url = jsonResponse?._links?.next?.href;
      } while (url);

      this.events = events;
    },
  },
  props: {
    eventStreamName: {
      type: String,
      required: true,
    },
    streamId: {
      type: String,
      required: true,
    },
  },
  mounted() {
    this.fetchEvents();
  },
};
</script>

<template>
  <div class="m-3">
    <h1 class="display-5 fw-bold mb-4">{{ eventStreamName }}</h1>
    <stream-id-search-panel
      @search="search"
      :starting-stream-id="streamId"
    ></stream-id-search-panel>
    <div>
      <event-card
        :event-stream-name="eventStreamName"
        :stream-id="streamId"
        :event="event"
        v-for="event in events"
        :key="event.eventId"
      ></event-card>
    </div>
  </div>
</template>
