<script lang="ts">
import { defineComponent } from "vue";
import {
  EventStreamsService,
  type EventStream,
} from "@/services/EventStreamsService";
import isVisible from "@/helpers/IsVisible";

var eventStreamsService = new EventStreamsService();

export default defineComponent({
  data() {
    return {
      loading: false,
      currentHash: window.location.hash,
      eventStreams: [] as EventStream[],
      nextPageUrl: undefined as string | undefined,
    };
  },
  methods: {
    async fetchData() {
      this.loading = true;
      this.eventStreams = [];

      const response = await eventStreamsService.getEventStreams();

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

      const response = await eventStreamsService.getEventStreams(
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
    isActive(eventStream: EventStream) {
      return (
        this.currentPath === "/" + eventStream.name ||
        this.currentPath.startsWith("/" + eventStream.name + "/")
      );
    },
  },
  computed: {
    currentPath() {
      return this.currentHash.slice(1) || "/";
    },
  },
  mounted() {
    this.fetchData();

    const sidebar = this.$refs.sidebar as HTMLElement;

    sidebar.addEventListener("scroll", () => {
      if (!this.nextPageUrl) {
        return;
      }

      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
    });

    window.addEventListener("hashchange", () => {
      this.currentHash = window.location.hash;
    });
  },
});
</script>

<template>
  <div
    class="d-flex flex-column flex-shrink-0 p-3 text-bg-dark sidebar"
    ref="sidebar"
  >
    <a
      href="./"
      class="d-flex align-items-center mb-3 mb-md-0 me-md-auto text-white text-decoration-none"
    >
      <img
        src="@/assets/log-otter-grayscale.svg"
        class="me-2"
        width="32"
        alt="LogOtter"
      />
      <span class="fs-5">Event Stream Viewer</span>
    </a>
    <hr />
    <ul class="nav nav-pills flex-column mb-auto">
      <li>
        <a
          href="#"
          class="nav-link text-white"
          :class="{ active: currentPath == '/' }"
        >
          <i class="bi-house-door me-2"></i>
          Home
        </a>
      </li>
      <li v-for="eventStream in eventStreams" :key="eventStream.name">
        <a
          :href="'#/' + eventStream.name"
          class="nav-link text-white"
          :class="{ active: isActive(eventStream) }"
        >
          <i class="bi-journals me-2"></i>
          {{ eventStream.name }}
        </a>
      </li>
      <li v-if="loading">
        <div class="px-3 py-2 placeholder-glow">
          <span class="placeholder col-1"></span>
          <span class="ms-2 placeholder col-8"></span>
        </div>
      </li>
      <span ref="loadMore"></span>
    </ul>
  </div>
</template>

<style scoped>
.sidebar {
  width: 280px;
  overflow-y: auto;
}
</style>
