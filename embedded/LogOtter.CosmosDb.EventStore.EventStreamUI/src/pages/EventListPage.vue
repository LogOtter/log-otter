<script lang="ts">
import StreamIdSearchPanel from "@/components/StreamIdSearchPanel.vue";
import EventCard from "@/components/EventCard.vue";
import {
  EventStreamsService,
  type Event,
} from "@/services/EventStreamsService";
import isVisible from "@/helpers/IsVisible";

const eventStreamsService = new EventStreamsService();

export default {
  components: {
    StreamIdSearchPanel,
    EventCard,
  },
  data() {
    return {
      loading: false,
      events: [] as Event[],
      nextPageUrl: undefined as string | undefined,
    };
  },
  methods: {
    search(streamId: string) {
      window.location.hash = `#/${encodeURIComponent(
        this.eventStreamName
      )}/${encodeURIComponent(streamId)}`;
    },
    async fetchEvents() {
      this.loading = true;
      const events: Event[] = [];

      const response = await eventStreamsService.getEvents(
        this.eventStreamName,
        this.streamId
      );

      for (const event of response.data) {
        events.push(event);
      }

      this.nextPageUrl = response.nextPage;

      this.events = events;
      this.loading = false;

      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
    },
    async fetchNextPage() {
      if (!this.nextPageUrl || this.loading) {
        return;
      }

      this.loading = true;

      var response = await eventStreamsService.getEventsWithUrl(
        this.nextPageUrl
      );

      for (const event of response.data) {
        this.events.push(event);
      }

      this.nextPageUrl = response.nextPage;

      this.loading = false;

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
    root.closest("#rootPage")!.addEventListener("scroll", () => {
      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
    });
  },
};
</script>

<template>
  <div class="m-3" ref="root">
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

      <div v-if="!loading && !events.length">
        <div class="card mb-1 p-3 text-muted">
          <span><i class="bi-info-square me-2"></i> No events found </span>
        </div>
      </div>

      <div v-if="loading">
        <div
          class="card mb-1 px-3 py-4 placeholder-glow"
          v-for="index in 3"
          :key="index"
        >
          <span class="placeholder col-5 mb-2"></span>
          <span class="placeholder col-3"></span>
        </div>
      </div>
    </div>
    <span ref="loadMore"></span>
  </div>
</template>
