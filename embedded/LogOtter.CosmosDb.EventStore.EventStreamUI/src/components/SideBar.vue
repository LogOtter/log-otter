<script lang="ts">
import { defineComponent, inject } from "vue";
import isVisible from "@/helpers/IsVisible";
import type { EventStreamsService, EventStream } from "@/services/EventStreamsService";

export default defineComponent({
  setup() {
    return { eventStreamsService: inject<EventStreamsService>("eventStreamsService")! };
  },
  data() {
    return {
      loading: false,
      currentHash: window.location.hash,
      eventStreams: [] as EventStream[],
      nextPageUrl: undefined as string | undefined,
      version: "",
      isExpanded: false,
    };
  },
  methods: {
    async fetchData() {
      this.loading = true;
      this.eventStreams = [];

      try {
        const response = await this.eventStreamsService.getEventStreams();

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
      } catch {
        // Ignore errors as sidebar error will be displayed on Event Stream list page
        this.loading = false;
      }
    },
    async fetchNextPage() {
      if (!this.nextPageUrl || this.loading) {
        return;
      }

      this.loading = true;

      const response = await this.eventStreamsService.getEventStreams(this.nextPageUrl);

      for (const definition of response.data) {
        this.eventStreams.push(definition);
      }

      this.nextPageUrl = response.nextPage;

      this.loading = false;

      if (isVisible(this.$refs.loadMore as HTMLElement)) {
        this.fetchNextPage();
      }
    },
    async fetchVersion() {
      var version = await this.eventStreamsService.getVersion();

      this.version = version.packageVersion;
    },
    isActive(eventStream: EventStream) {
      return this.currentPath === "/" + eventStream.name || this.currentPath.startsWith("/" + eventStream.name + "/");
    },
  },
  computed: {
    currentPath() {
      return this.currentHash.slice(1) || "/";
    },
  },
  mounted() {
    this.fetchData();

    this.fetchVersion();

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
  <div class="close" :class="{ show: isExpanded }" @click="() => (isExpanded = false)">
    <i class="bi-x-circle-fill"></i>
  </div>
  <div class="open" :class="{ show: !isExpanded }" @click="() => (isExpanded = true)">
    <i class="bi-list"></i>
  </div>
  <div class="flex-column flex-shrink-0 p-3 text-bg-dark sidebar" :class="{ collapsed: !isExpanded }" ref="sidebar">
    <a href="./" class="d-flex align-items-center text-white text-decoration-none" @click="() => (isExpanded = false)">
      <img src="@/assets/log-otter-grayscale.svg" class="me-2" width="32" alt="LogOtter" />
      <span class="fs-5">Event Stream Viewer</span>
    </a>
    <hr />
    <ul class="nav nav-pills flex-column mb-auto">
      <li>
        <a href="#" class="nav-link text-white" :class="{ active: currentPath == '/' }" @click="() => (isExpanded = false)">
          <i class="bi-house-door me-2"></i>
          Home
        </a>
      </li>
      <li v-for="eventStream in eventStreams" :key="eventStream.name">
        <a :href="'#/' + eventStream.name" class="nav-link text-white" :class="{ active: isActive(eventStream) }" @click="() => (isExpanded = false)">
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
    <hr />
    <div class="text-muted">
      <small v-if="version">v{{ version }}</small>
    </div>
  </div>
</template>

<style scoped>
.close {
  display: none;
}
.open {
  display: none;
}

.sidebar {
  width: 280px;
  overflow-y: auto;
  display: flex;
}

@media (max-width: 576px) {
  .sidebar {
    position: fixed;
    top: 0;
    left: 0;
    bottom: 0;
    z-index: 999;
  }

  .sidebar.collapsed {
    margin-left: -280px;
  }

  .close.show {
    display: block;
    position: absolute;
    font-size: 32px;
    color: #ffffff;
    background: #212529;
    top: 10px;
    left: 264px;
    width: 41px;
    height: 49px;
    border-radius: 0 28px 28px 0;
    z-index: 9999;
    cursor: pointer;
  }

  .open.show {
    display: block;
    position: absolute;
    font-size: 32px;
    background: rgba(255, 255, 255, 0.5);
    top: 12px;
    left: 24px;
    width: 32px;
    height: 32px;
    z-index: 9999;
    cursor: pointer;
  }
}
</style>
