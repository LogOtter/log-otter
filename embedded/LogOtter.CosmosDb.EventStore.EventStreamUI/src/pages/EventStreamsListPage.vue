<script lang="ts">
import isVisible from "@/helpers/IsVisible";
import {
  EventStreamsService,
  type EventStream,
} from "@/services/EventStreamsService";
import { defineComponent } from "vue";

var eventStreamsService = new EventStreamsService();

export default defineComponent({
  data() {
    return {
      loading: false,
      eventStreams: [] as EventStream[],
      nextPageUrl: undefined as string | undefined,
    };
  },
  methods: {
    async fetchData() {
      this.loading = true;
      this.eventStreams = [];

      var response = await eventStreamsService.getEventStreams();

      const eventStreams = [];

      for (const definition of response.data) {
        eventStreams.push(definition);
      }

      this.nextPageUrl = response.nextPage;

      this.eventStreams = eventStreams;
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

      var response = await eventStreamsService.getEventStreams(
        this.nextPageUrl
      );

      for (const definition of response.data) {
        this.eventStreams.push(definition);
      }

      this.nextPageUrl = response.nextPage;

      this.loading = false;

      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
    },
  },
  mounted() {
    this.fetchData();

    const root = this.$refs.root as HTMLElement;
    root.closest("#rootPage")!.addEventListener("scroll", () => {
      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
    });
  },
});
</script>

<template>
  <div class="m-3" ref="root">
    <h1 class="display-5 fw-bold mb-4">Event Streams</h1>
    <div v-if="loading">
      <div class="card mb-2 placeholder-glow" v-for="index in 3" :key="index">
        <div class="card-body">
          <h5 class="card-title">
            <span class="placeholder col-2"></span>
          </h5>
          <div class="card-subtitle text-muted">
            <span class="placeholder col-4"></span>
          </div>
        </div>
      </div>
    </div>
    <div
      class="card mb-2"
      v-for="eventStream in eventStreams"
      :key="eventStream.name"
    >
      <div class="card-body">
        <h5 class="card-title">{{ eventStream.name }}</h5>
        <div class="card-subtitle text-muted">{{ eventStream.typeName }}</div>
        <a :href="'#/' + eventStream.name" class="stretched-link"></a>
      </div>
    </div>
    <span ref="loadMore"></span>
  </div>
</template>