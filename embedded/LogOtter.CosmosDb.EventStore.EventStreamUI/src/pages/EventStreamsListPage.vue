<script lang="ts">
import isVisible from "@/helpers/IsVisible";
import type { EventStream, EventStreamsService } from "@/services/EventStreamsService";
import { defineComponent, inject } from "vue";

export default defineComponent({
  setup() {
    return { eventStreamsService: inject<EventStreamsService>("eventStreamsService")! };
  },
  data() {
    return {
      loading: false,
      error: undefined as any,
      eventStreams: [] as EventStream[],
      nextPageUrl: undefined as string | undefined,
    };
  },
  methods: {
    async fetchData() {
      this.loading = true;
      this.eventStreams = [];

      try {
        var response = await this.eventStreamsService.getEventStreams();

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
      } catch (e) {
        this.error = e;
        this.loading = false;
      }
    },
    async fetchNextPage() {
      if (!this.nextPageUrl || this.loading) {
        return;
      }

      this.loading = true;

      var response = await this.eventStreamsService.getEventStreams(this.nextPageUrl);

      for (const definition of response.data) {
        this.eventStreams.push(definition);
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
  mounted() {
    this.fetchData();

    const root = this.$refs.root as HTMLElement;
    root.closest("#rootPage")!.addEventListener("scroll", this.scrollHandler);
  },
  beforeUnmount() {
    const root = this.$refs.root as HTMLElement;
    root.closest("#rootPage")!.removeEventListener("scroll", this.scrollHandler);
  },
});
</script>

<template>
  <div class="m-3" ref="root">
    <h1 class="display-5 fw-bold mb-4 sidebar-margin">Event Streams</h1>
    <div v-if="loading">
      <div class="card mb-2 placeholder-glow" v-for="index in 3" :key="index">
        <div class="card-body">
          <h5 class="card-title">
            <span class="placeholder col-2"></span>
          </h5>
          <div class="card-subtitle text-body-secondary">
            <span class="placeholder col-4"></span>
          </div>
        </div>
      </div>
    </div>
    <div class="card mb-2" v-for="eventStream in eventStreams" :key="eventStream.name">
      <div class="card-body">
        <h5 class="card-title">{{ eventStream.name }}</h5>
        <div class="card-subtitle text-body-secondary">{{ eventStream.typeName }}</div>
        <a :href="'#/' + eventStream.name" class="stretched-link"></a>
      </div>
    </div>
    <span ref="loadMore"></span>
  </div>
  <div v-if="!loading && !eventStreams.length && !error">
    <div class="card m-3 p-3 text-body-secondary">
      <span> <i class="bi-info-square me-2"></i> No event streams configured </span>
    </div>
  </div>
  <div v-if="!loading && !eventStreams.length && error">
    <div class="card m-3 p-3 text-bg-danger">
      <span> <i class="bi-exclamation-square me-2"></i> <strong>Error</strong> - Could not load event streams </span>
    </div>
  </div>
</template>
