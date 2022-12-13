<script lang="ts">
import { defineComponent } from "vue";
import EventStreamsListPage from "@/pages/EventStreamsListPage.vue";
import EventStreamPage from "@/pages/EventStreamPage.vue";
import EventListPage from "@/pages/EventListPage.vue";
import NotFoundPage from "@/pages/NotFoundPage.vue";

export default defineComponent({
  data() {
    return { currentHash: window.location.hash };
  },
  computed: {
    currentPath() {
      return this.currentHash.slice(1) || "/";
    },
    currentView() {
      if (this.currentPath === "/") {
        return {
          view: EventStreamsListPage,
          properties: {},
        };
      }

      const eventStreamRegex = /^\/([^/]+)$/gm.exec(this.currentPath);
      if (eventStreamRegex !== null) {
        return {
          view: EventStreamPage,
          properties: {
            eventStreamName: decodeURIComponent(eventStreamRegex[1]),
          },
        };
      }

      const eventListRegex = /^\/([^/]+)\/([^/]+)$/gm.exec(this.currentPath);
      if (eventListRegex !== null) {
        return {
          view: EventListPage,
          properties: {
            eventStreamName: eventListRegex[1],
            streamId: decodeURIComponent(eventListRegex[2]),
          },
        };
      }

      return { view: NotFoundPage, properties: {} };
    },
  },
  mounted() {
    window.addEventListener("hashchange", () => {
      this.currentHash = window.location.hash;
    });
  },
});
</script>

<template>
  <div class="flex-grow-1" id="rootPage" style="overflow-y: auto">
    <div class="container mx-0">
      <component :is="currentView.view" v-bind="currentView.properties" />
    </div>
  </div>
</template>
