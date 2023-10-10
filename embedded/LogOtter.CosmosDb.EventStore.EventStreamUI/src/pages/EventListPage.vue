<script lang="ts">
import StreamIdSearchPanel from "../components/StreamIdSearchPanel.vue";
import EventCard from "../components/EventCard.vue";
import type { Event, EventStreamsService } from "../services/EventStreamsService";
import isVisible from "../helpers/IsVisible";
import { inject } from "vue";

export default {
  setup() {
    return { eventStreamsService: inject<EventStreamsService>("eventStreamsService")! };
  },
  components: {
    StreamIdSearchPanel,
    EventCard,
  },
  data() {
    return {
      loading: false,
      error: undefined as any,
      events: [] as Event[],
      nextPageUrl: undefined as string | undefined,
    };
  },
  methods: {
    search(streamId: string) {
      window.location.hash = `#/${encodeURIComponent(this.eventStreamName)}/${encodeURIComponent(streamId)}`;
    },
    async fetchEvents() {
      this.loading = true;
      const events: Event[] = [];

      try {
        const response = await this.eventStreamsService.getEvents(this.eventStreamName, this.streamId);

        for (const event of response.data) {
          events.push(event);
        }

        this.nextPageUrl = response.nextPage;

        this.events = events;
        this.loading = false;
      } catch (e) {
        this.error = e;
        this.loading = false;
      }

      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
    },
    async fetchNextPage() {
      if (!this.nextPageUrl || this.loading) {
        return;
      }

      this.loading = true;

      var response = await this.eventStreamsService.getEventsWithUrl(this.nextPageUrl);

      for (const event of response.data) {
        this.events.push(event);
      }

      this.nextPageUrl = response.nextPage;

      this.loading = false;

      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
    },
    scrollHandler() {
      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
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
  watch: {
    streamId() {
      this.fetchEvents();
    },
    eventStreamName() {
      this.fetchEvents();
    },
  },
  mounted() {
    this.fetchEvents();

    const root = this.$refs.root as HTMLElement;
    root.closest("#rootPage")!.addEventListener("scroll", this.scrollHandler);
  },
  beforeUnmount() {
    const root = this.$refs.root as HTMLElement;
    root.closest("#rootPage")!.removeEventListener("scroll", this.scrollHandler);
  },
};
</script>

<template>
  <div class="m-3" ref="root">
    <h1 class="display-5 fw-bold mb-4 sidebar-margin">{{ eventStreamName }}</h1>
    <stream-id-search-panel @search="search" :starting-stream-id="streamId"></stream-id-search-panel>
    <div>
      <event-card :event-stream-name="eventStreamName" :stream-id="streamId" :event="event" v-for="event in events" :key="event.eventId"></event-card>

      <div v-if="!loading && !events.length && !error">
        <div class="card mb-1 p-3 text-body-secondary">
          <span><i class="bi-info-square me-2"></i> No events found </span>
        </div>
      </div>

      <div v-if="!loading && !events.length && error">
        <div class="card mb-1 p-3 text-bg-danger">
          <span><i class="bi-exclamation-square me-2"></i> <strong>Error</strong> - Could not load events</span>
        </div>
      </div>

      <div v-if="loading">
        <div class="card mb-1 px-3 py-4 placeholder-glow" v-for="index in 3" :key="index">
          <span class="placeholder col-5 mb-2"></span>
          <span class="placeholder col-3"></span>
        </div>
      </div>
    </div>
    <span ref="loadMore"></span>
  </div>
</template>
